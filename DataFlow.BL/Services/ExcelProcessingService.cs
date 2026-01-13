using ClosedXML.Excel;
using DataFlow.BL.Constants;
using DataFlow.BL.Contracts;
using DataFlow.Core.Common;
using DataFlow.Core.Constants;
using DataFlow.Core.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DataFlow.BL.Services
{
    public class ExcelProcessingService : IExcelProcessingService
    {
        private readonly ILogger<ExcelProcessingService> _logger;
        private readonly LookupIds _lookupIds;

        public event EventHandler<ProcessNotification>? NotificationReceived;

        public ExcelProcessingService(
            ILogger<ExcelProcessingService> logger,
            LookupIds lookupIds)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _lookupIds = lookupIds ?? throw new ArgumentNullException(nameof(lookupIds));
        }

        private void Notify(ProcessNotificationLevel level, string message, string? details = null)
        {
            var notification = new ProcessNotification
            {
                Level = level,
                Message = message,
                Details = details
            };

            NotificationReceived?.Invoke(this, notification);

            switch (level)
            {
                case ProcessNotificationLevel.Info:
                    _logger.LogInformation("{Message}", message);
                    break;
                case ProcessNotificationLevel.Warning:
                    _logger.LogWarning("{Message} {Details}", message, details ?? "");
                    break;
                case ProcessNotificationLevel.Error:
                    _logger.LogError("{Message} {Details}", message, details ?? "");
                    break;
            }
        }

        private bool IsFileLocked(string filePath)
        {
            try
            {
                using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
                stream.Close();
            }
            catch (IOException)
            {
                return true;
            }
            return false;
        }

        public async Task<Result<string>> ProcessExcelFileAsync(
            string inputFilePath,
            string outputFilePath,
            ConfigTemplate templateConfig)
        {
            Notify(ProcessNotificationLevel.Info, "Iniciando procesamiento de archivo Excel");

            try
            {
                if (!File.Exists(inputFilePath))
                {
                    Notify(ProcessNotificationLevel.Error, "El archivo de entrada no existe");
                    return Result<string>.Failure("El archivo de entrada no existe.");
                }
                if (IsFileLocked(inputFilePath))
                {
                    Notify(ProcessNotificationLevel.Error, "El archivo está abierto", "Cierre el archivo Excel antes de continuar.");
                    return Result<string>.Failure("El archivo de Excel está siendo usado por otro proceso. Por favor, ciérrelo e intente nuevamente.");
                }

                if (templateConfig?.ConfigColumns == null || templateConfig.ConfigColumns.Count == 0)
                {
                    Notify(ProcessNotificationLevel.Error, "La configuración de columnas está vacía");
                    return Result<string>.Failure("La configuración de columnas está vacía.");
                }

                Notify(ProcessNotificationLevel.Info,
                    $"Plantilla cargada: {templateConfig.Description} con {templateConfig.ConfigColumns.Count} columnas");

                var processedRows = await Task.Run(() => ProcessExcelFile(inputFilePath, templateConfig));

                if (processedRows.Count == 0)
                {
                    Notify(ProcessNotificationLevel.Warning, "No se generaron filas de salida");
                    return Result<string>.Failure("No se generaron filas de salida.");
                }

                Notify(ProcessNotificationLevel.Info,
                    $"Escribiendo archivo de salida con {processedRows.Count} filas");
                WriteOutputFile(outputFilePath, processedRows, templateConfig);

                Notify(ProcessNotificationLevel.Info,
                    $"✓ Proceso completado exitosamente: {processedRows.Count} filas generadas");

                return Result<string>.Success(outputFilePath);
            }
            catch (Exception ex)
            {
                Notify(ProcessNotificationLevel.Error,
                    "Error crítico durante el procesamiento",
                    ex.ToString());
                return Result<string>.Failure($"Error al procesar archivo: {ex.Message}");
            }
        }

        private List<Dictionary<string, object>> ProcessExcelFile(
                string inputFilePath, ConfigTemplate templateConfig)
        {
            var resultRows = new List<Dictionary<string, object>>();

            try
            {
                Notify(ProcessNotificationLevel.Info, "Abriendo archivo Excel...");
                using var workbook = new XLWorkbook(inputFilePath);
                var worksheet = workbook.Worksheet(1);

                Notify(ProcessNotificationLevel.Info, $"Hoja cargada: {worksheet.Name}");

                // Clasificación por tipo usando IDs de lookup
                var constantColumns = templateConfig.ConfigColumns
                    .Where(c => c.ColumnTypeId == _lookupIds.Constante)
                    .ToList();

                var valueColumns = templateConfig.ConfigColumns
                    .Where(c => c.ColumnTypeId == _lookupIds.Valor)
                    .ToList();

                var dimensionColumns = templateConfig.ConfigColumns
                    .Where(c => c.ColumnTypeId == _lookupIds.Dimension)
                    .ToList();

                Notify(ProcessNotificationLevel.Info,
                    $"Columnas - Constantes: {constantColumns.Count}, Valores: {valueColumns.Count}, Dimensiones: {dimensionColumns.Count}");

                // Leer CONSTANTES (valores fijos)
                var constantValues = ReadConstantValues(worksheet, constantColumns);

                // Determinar rango de procesamiento desde columnas de VALOR
                var processingRange = DetermineProcessingRange(valueColumns);

                if (processingRange == null)
                {
                    Notify(ProcessNotificationLevel.Warning, "No se encontró rango de procesamiento en columnas de valor");
                    return resultRows;
                }

                Notify(ProcessNotificationLevel.Info,
                    $"Rango de procesamiento detectado: Filas {processingRange.Value.FromRow} a {processingRange.Value.ToRow}");

                // Preparar índice de DIMENSIONES (si existen)
                var dimensionIndex = BuildDimensionIndex(dimensionColumns);
                var dimensionOrder = DetermineDimensionOrder(dimensionColumns);

                // Columnas de salida ordenadas por IndexColumn
                var orderedOutputColumns = templateConfig.ConfigColumns
                    .OrderBy(c => c.IndexColumn)
                    .ToList();

                // Detectar si hay columnas contenedoras (rangos horizontales multi-columna)
                var contenedoraColumns = valueColumns.Where(IsContenedor).ToList();
                var normalValueColumns = valueColumns.Where(c => !IsContenedor(c)).ToList();
                bool hasContenedoras = contenedoraColumns.Any();

                Notify(ProcessNotificationLevel.Info,
                     $"Columnas de Valor - Normales: {normalValueColumns.Count}, Contenedoras: {contenedoraColumns.Count}");

                Notify(ProcessNotificationLevel.Info,
                    hasContenedoras
                        ? $"Modo: Procesamiento con {contenedoraColumns.Count} columnas contenedoras"
                        : "Modo: Procesamiento fila por fila (sin columnas contenedoras)");

                // Últimos valores válidos por columna 
                var lastValidValues = new Dictionary<int, object>();

                // Procesar según disponibilidad de columnas contenedoras
                if (hasContenedoras)
                {
                    resultRows = ProcessWithContenedoras(
                        worksheet,
                        contenedoraColumns,
                        normalValueColumns,
                        constantValues,
                        dimensionIndex,
                        dimensionOrder,
                        templateConfig,
                        orderedOutputColumns,
                        lastValidValues);
                }
                else
                {
                    // Sin contenedoras (fila por fila)
                    resultRows = ProcessRowByRow(
                        worksheet,
                        processingRange.Value,
                        normalValueColumns,
                        constantValues,
                        dimensionIndex,
                        dimensionOrder,
                        templateConfig,
                        orderedOutputColumns,
                        lastValidValues);
                }

                Notify(ProcessNotificationLevel.Info,
                    $"✓ Procesamiento completado: {resultRows.Count} filas generadas");
            }
            catch (Exception ex)
            {
                Notify(ProcessNotificationLevel.Error,
                    "Error al procesar el archivo Excel",
                    ex.ToString());
                throw;
            }

            return resultRows;
        }

        // Determinar el rango de filas a procesar basado en todas las columnas de valor
        private (int FromRow, int ToRow)? DetermineProcessingRange(List<ConfigColumn> valueColumns)
        {
            if (!(valueColumns.Count > 0) || valueColumns.All(c => c.Ranges == null || !c.Ranges.Any()))
                return null;

            int minRow = int.MaxValue;
            int maxRow = int.MinValue;

            foreach (var col in valueColumns)
            {
                if (col.Ranges == null) continue;

                foreach (var range in col.Ranges)
                {
                    var from = ParseCellAddress(range.RFrom!);
                    var to = ParseCellAddress(range.RTo!);

                    minRow = Math.Min(minRow, from.Row);
                    maxRow = Math.Max(maxRow, to.Row);
                }
            }

            if (minRow == int.MaxValue || maxRow == int.MinValue)
                return null;

            return (minRow, maxRow);
        }

        // CAMBIO: Procesamiento columnas contenedoras
        // Solo se procesan celdas que coincidan con columnas de Valor
        private List<Dictionary<string, object>> ProcessWithContenedoras(
            IXLWorksheet worksheet,
            List<ConfigColumn> contenedoraColumns,
            List<ConfigColumn> valueColumns,
            Dictionary<int, object> constantValues,
            Dictionary<(int row, int col), List<(ConfigColumn Column, ColumnRange Range)>> dimensionIndex,
            List<ConfigColumn> dimensionOrder,
            ConfigTemplate templateConfig,
            List<ConfigColumn> orderedOutputColumns,
            Dictionary<int, object> lastValidValues)
        {
            var resultRows = new List<Dictionary<string, object>>();

            // CAMBIO: Construir mapa de celdas válidas desde columnas de Valor
            var validValueCells = BuildValidValueCellsMap(valueColumns);

            foreach (var contCol in contenedoraColumns)
            {
                int cellsCoveredByDimensions = 0;

                foreach (var range in contCol.Ranges ?? Enumerable.Empty<ColumnRange>())
                {
                    var fromAddr = ParseCellAddress(range.RFrom!);
                    var toAddr = ParseCellAddress(range.RTo!);

                    Notify(ProcessNotificationLevel.Info,
                        $"Procesando rango contenedor {range.RFrom} a {range.RTo} para columna '{contCol.Name}'");

                    for (int row = fromAddr.Row; row <= toAddr.Row; row++)
                    {
                        // Leer valores de contexto para esta fila
                        var contextValues = ReadContextValuesForRow(
                            worksheet, row, valueColumns, contCol, lastValidValues);

                        // CAMBIO: Procesar cada celda dentro del rango horizontal
                        // Solo si coincide con una columna de Valor y tiene dimensión
                        for (int col = fromAddr.Col; col <= toAddr.Col; col++)
                        {
                            // CAMBIO: Verificar que la celda pertenece a una columna de Valor
                            if (!validValueCells.Contains((row, col)))
                                continue;

                            var cell = worksheet.Cell(row, col);

                            if (!cell.TryGetValue(out double cellValue) || cellValue == 0)
                                continue;

                            var applicableDims = GetOrderedDimensionsForCell(
                                row, col, dimensionIndex, dimensionOrder);

                            // Solo procesar si hay dimensiones asociadas
                            if (applicableDims.Any())
                            {
                                cellsCoveredByDimensions++;

                                var outputRow = BuildOutputRow(
                                    worksheet,
                                    row,
                                    cellValue,
                                    contCol,
                                    contextValues,
                                    applicableDims,
                                    constantValues,
                                    templateConfig,
                                    orderedOutputColumns,
                                    validValueCells);

                                resultRows.Add(outputRow);
                            }
                        }
                    }
                }

                if (cellsCoveredByDimensions == 0)
                {
                    Notify(ProcessNotificationLevel.Warning,
                        $"Columna contenedora '{contCol.Name}': ninguna celda en sus rangos está cubierta por dimensiones. No se generarán filas para esta columna.");
                }
            }

            return resultRows;
        }

        // CAMBIO: Construir mapa de celdas válidas desde columnas de Valor
        private HashSet<(int row, int col)> BuildValidValueCellsMap(List<ConfigColumn> valueColumns)
        {
            var validCells = new HashSet<(int row, int col)>();

            foreach (var col in valueColumns)
            {
                if (col.Ranges == null) continue;

                foreach (var range in col.Ranges)
                {
                    var from = ParseCellAddress(range.RFrom!);
                    var to = ParseCellAddress(range.RTo!);

                    for (int r = from.Row; r <= to.Row; r++)
                    {
                        for (int c = from.Col; c <= to.Col; c++)
                        {
                            validCells.Add((r, c));
                        }
                    }
                }
            }

            return validCells;
        }

        // Procesamiento SIN columnas contenedoras (fila por fila)
        private List<Dictionary<string, object>> ProcessRowByRow(
            IXLWorksheet worksheet,
            (int FromRow, int ToRow) processingRange,
            List<ConfigColumn> valueColumns,
            Dictionary<int, object> constantValues,
            Dictionary<(int row, int col), List<(ConfigColumn Column, ColumnRange Range)>> dimensionIndex,
            List<ConfigColumn> dimensionOrder,
            ConfigTemplate templateConfig,
            List<ConfigColumn> orderedOutputColumns,
            Dictionary<int, object> lastValidValues)
        {
            var resultRows = new List<Dictionary<string, object>>();

            Notify(ProcessNotificationLevel.Info,
                $"Procesando fila por fila desde {processingRange.FromRow} hasta {processingRange.ToRow}");

            for (int row = processingRange.FromRow; row <= processingRange.ToRow; row++)
            {
                // Leer todos los valores de esta fila
                var rowValues = new Dictionary<int, object>();
                bool hasValidData = false;

                foreach (var col in valueColumns)
                {
                    if (col.Ranges == null || !col.Ranges.Any()) continue;

                    foreach (var range in col.Ranges)
                    {
                        var from = ParseCellAddress(range.RFrom!);
                        var to = ParseCellAddress(range.RTo!);

                        // Verificar si esta fila está dentro del rango de esta columna
                        if (row < from.Row || row > to.Row)
                            continue;

                        object cellValue = GetDefaultValue(col, range);

                        // Leer valor(es) de esta fila en el rango de columnas
                        for (int c = from.Col; c <= to.Col; c++)
                        {
                            var cell = worksheet.Cell(row, c);
                            var candidate = ReadCellValue(cell, col);

                            if (!IsEmptyValue(candidate, col.DataTypeId))
                            {
                                cellValue = candidate;
                                hasValidData = true;
                                break;
                            }
                        }

                        // Aplicar valor ultimo si la celda está vacía
                        if (IsEmptyValue(cellValue, col.DataTypeId))
                        {
                            if (lastValidValues.TryGetValue(col.Id, out var lastVal))
                            {
                                cellValue = lastVal;
                            }
                        }
                        else
                        {
                            lastValidValues[col.Id] = cellValue;
                        }

                        rowValues[col.Id] = cellValue;
                        break; // Solo procesar el primer rango que aplique
                    }
                }

                // Solo generar fila si tiene datos válidos
                if (!hasValidData)
                    continue;

                // Obtener dimensiones aplicables para esta fila
                var applicableDims = GetDimensionsForRow(
                    worksheet, row, dimensionIndex, dimensionOrder, valueColumns);

                // Construir fila de salida
                var outputRow = BuildOutputRowFromValues(
                    rowValues,
                    applicableDims,
                    constantValues,
                    templateConfig,
                    orderedOutputColumns);

                resultRows.Add(outputRow);
            }

            return resultRows;
        }

        // Obtener dimensiones aplicables para una fila completa
        private List<(ConfigColumn Column, ColumnRange Range)> GetDimensionsForRow(
            IXLWorksheet worksheet,
            int row,
            Dictionary<(int row, int col), List<(ConfigColumn Column, ColumnRange Range)>> dimensionIndex,
            List<ConfigColumn> dimensionOrder,
            List<ConfigColumn> valueColumns)
        {
            // Intentar obtener dimensiones desde la primera celda de valor en esta fila
            var firstValueCol = valueColumns.FirstOrDefault(c => c.Ranges?.Any() == true);
            if (firstValueCol?.Ranges == null)
                return new List<(ConfigColumn, ColumnRange)>();

            var firstRange = firstValueCol.Ranges.First();
            var firstColAddr = ParseCellAddress(firstRange.RFrom!);

            return GetOrderedDimensionsForCell(row, firstColAddr.Col, dimensionIndex, dimensionOrder);
        }

        // Construir fila de salida desde valores leídos (sin columna contenedora)
        private Dictionary<string, object> BuildOutputRowFromValues(
            Dictionary<int, object> rowValues,
            List<(ConfigColumn Column, ColumnRange Range)> dimensions,
            Dictionary<int, object> constantValues,
            ConfigTemplate templateConfig,
            List<ConfigColumn> orderedOutputColumns)
        {
            var row = new Dictionary<string, object>();

            foreach (var column in orderedOutputColumns)
            {
                var columnName = column.NameDisplay ?? column.Name ?? $"Column_{column.IndexColumn}";
                object? value;

                if (column.ColumnTypeId == _lookupIds.Constante)
                {
                    // CONSTANTES: usar valores leídos al inicio
                    value = constantValues.TryGetValue(column.Id, out var v)
                        ? v
                        : GetDefaultValue(column, null);
                }
                else if (column.ColumnTypeId == _lookupIds.Valor)
                {
                    // VALORES: usar valores leídos de la fila
                    value = rowValues.TryGetValue(column.Id, out var v)
                        ? v
                        : GetDefaultValue(column, null);
                }
                else if (column.ColumnTypeId == _lookupIds.Dimension)
                {
                    // DIMENSIONES: usar valores del índice de dimensiones
                    var applicable = dimensions.FirstOrDefault(d => d.Column.Id == column.Id);
                    if (applicable.Column != null && !string.IsNullOrEmpty(applicable.Range.DefaultValue))
                    {
                        value = applicable.Range.DefaultValue;
                    }
                    else
                    {
                        value = GetDefaultValue(column, null);
                    }
                }
                else
                {
                    value = column.DataTypeId == _lookupIds.Numerico ? 0.0 : string.Empty;
                }

                row[columnName] = value ?? string.Empty;
            }

            return row;
        }

        #region Helpers: detección contenedora, indexado dimensiones y orden

        private bool IsContenedor(ConfigColumn col)
        {
            if (col == null) return false;
            if (col.ColumnTypeId != _lookupIds.Valor) return false;
            if (col.DataTypeId != _lookupIds.Numerico) return false;
            if (col.Ranges == null || !col.Ranges.Any()) return false;

            foreach (var r in col.Ranges)
            {
                var from = ParseCellAddress(r.RFrom!);
                var to = ParseCellAddress(r.RTo!);
                if (to.Col > from.Col) // rango horizontal (multi-columna)
                    return true;
            }
            return false;
        }

        private Dictionary<(int row, int col), List<(ConfigColumn Column, ColumnRange Range)>> BuildDimensionIndex(
            List<ConfigColumn> dimensionColumns)
        {
            var index = new Dictionary<(int row, int col), List<(ConfigColumn, ColumnRange)>>();

            foreach (var dim in dimensionColumns)
            {
                if (dim.Ranges == null) continue;

                foreach (var range in dim.Ranges)
                {
                    var from = ParseCellAddress(range.RFrom!);
                    var to = ParseCellAddress(range.RTo!);

                    for (int r = from.Row; r <= to.Row; r++)
                    {
                        for (int c = from.Col; c <= to.Col; c++)
                        {
                            var key = (r, c);
                            if (!index.TryGetValue(key, out var list))
                            {
                                list = new List<(ConfigColumn, ColumnRange)>();
                                index[key] = list;
                            }
                            list.Add((dim, range));
                        }
                    }
                }
            }

            return index;
        }

        private List<ConfigColumn> DetermineDimensionOrder(List<ConfigColumn> dimensionColumns)
        {
            Func<ColumnRange, (int fr, int fc, int tr, int tc)> toCoords = r =>
            {
                var f = ParseCellAddress(r.RFrom!);
                var t = ParseCellAddress(r.RTo!);
                return (f.Row, f.Col, t.Row, t.Col);
            };

            bool RangeContains((int fr, int fc, int tr, int tc) outer, (int fr, int fc, int tr, int tc) inner)
                => inner.fr >= outer.fr && inner.tr <= outer.tr &&
                   inner.fc >= outer.fc && inner.tc <= outer.tc;

            bool DimensionAContainsB(ConfigColumn a, ConfigColumn b)
            {
                if (a.Ranges == null || b.Ranges == null) return false;

                var aRanges = a.Ranges.Select(toCoords).ToList();
                var bRanges = b.Ranges.Select(toCoords).ToList();

                foreach (var br in bRanges)
                {
                    if (!aRanges.Any(ar => RangeContains(ar, br)))
                        return false;
                }
                return true;
            }

            var scores = new Dictionary<int, int>();
            foreach (var a in dimensionColumns)
            {
                int count = dimensionColumns.Count(b => DimensionAContainsB(a, b));
                scores[a.Id] = count;
            }

            var ordered = dimensionColumns
                .OrderByDescending(c => scores[c.Id])
                .ThenBy(c => c.IndexColumn)
                .ToList();

            return ordered;
        }

        private List<(ConfigColumn Column, ColumnRange Range)> GetOrderedDimensionsForCell(
            int row,
            int col,
            Dictionary<(int row, int col), List<(ConfigColumn Column, ColumnRange Range)>> dimensionIndex,
            List<ConfigColumn> dimensionOrder)
        {
            if (!dimensionIndex.TryGetValue((row, col), out var list))
                return new List<(ConfigColumn, ColumnRange)>();

            var orderById = dimensionOrder.Select(d => d.Id).ToList();

            var ordered = list
                .OrderBy(item =>
                {
                    var idx = orderById.IndexOf(item.Column.Id);
                    return idx >= 0 ? idx : int.MaxValue;
                })
                .ToList();

            return ordered;
        }

        #endregion

        #region Helpers: lectura de constantes y contexto (fill-forward)

        private Dictionary<int, object> ReadConstantValues(
            IXLWorksheet worksheet,
            List<ConfigColumn> constantColumns)
        {
            var values = new Dictionary<int, object>();

            if (!constantColumns.Any())
                return values;

            Notify(ProcessNotificationLevel.Info,
                $"Leyendo {constantColumns.Count} valores constantes");

            foreach (var column in constantColumns)
            {
                if (column.Ranges?.Count > 0)
                {
                    var range = column.Ranges.First();
                    var cellAddress = ParseCellAddress(range.RFrom!);
                    var cell = worksheet.Cell(cellAddress.Row, cellAddress.Col);
                    var cellValue = ReadCellValue(cell, column);
                    values[column.Id] = cellValue;

                    Notify(ProcessNotificationLevel.Info,
                        $"  Constante '{column.Name}': {cellValue}");
                }
            }

            return values;
        }

        private Dictionary<int, object> ReadContextValuesForRow(
            IXLWorksheet worksheet,
            int targetRow,
            List<ConfigColumn> valueColumns,
            ConfigColumn excludeColumn,
            Dictionary<int, object> lastValidValues)
        {
            var contextValues = new Dictionary<int, object>();

            foreach (var column in valueColumns)
            {
                if (column.Id == excludeColumn.Id)
                    continue;

                if (column.Ranges?.Any() != true)
                    continue;

                foreach (var range in column.Ranges)
                {
                    var from = ParseCellAddress(range.RFrom!);
                    var to = ParseCellAddress(range.RTo!);

                    if (targetRow < from.Row || targetRow > to.Row)
                        continue;

                    object cellValue = GetDefaultValue(column, range);

                    for (int c = from.Col; c <= to.Col; c++)
                    {
                        var cell = worksheet.Cell(targetRow, c);
                        var candidate = ReadCellValue(cell, column);
                        if (!IsEmptyValue(candidate, column.DataTypeId))
                        {
                            cellValue = candidate;
                            break;
                        }
                    }

                    if (IsEmptyValue(cellValue, column.DataTypeId))
                    {
                        if (lastValidValues.TryGetValue(column.Id, out var lastVal))
                        {
                            cellValue = lastVal;
                        }
                    }
                    else
                    {
                        lastValidValues[column.Id] = cellValue;
                    }

                    contextValues[column.Id] = cellValue;
                    break;
                }
            }

            return contextValues;
        }

        #endregion

        #region Helpers: lectura de celdas y defaults

        private object ReadCellValue(IXLCell cell, ConfigColumn column)
        {
            try
            {
                if (cell.IsEmpty())
                    return GetDefaultValue(column, null);

                if (column.DataTypeId == _lookupIds.Numerico)
                {
                    if (cell.TryGetValue(out double numValue))
                        return numValue;

                    Notify(ProcessNotificationLevel.Warning,
                        $"Esperaba número en {cell.Address}",
                        $"Valor recibido: '{cell.GetString()}'");
                    return GetDefaultValue(column, null);
                }

                if (column.DataTypeId == _lookupIds.Fecha)
                {
                    if (cell.TryGetValue(out DateTime dateValue))
                        return dateValue;

                    var s = cell.GetString();
                    if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                        return parsedDate;

                    Notify(ProcessNotificationLevel.Warning,
                        $"Esperaba fecha en {cell.Address}",
                        $"Valor recibido: '{cell.GetString()}'");
                    return GetDefaultValue(column, null);
                }

                // Texto u otro tipo
                var textValue = cell.GetString();
                if (string.IsNullOrWhiteSpace(textValue))
                    return GetDefaultValue(column, null);
                return textValue;
            }
            catch (Exception ex)
            {
                Notify(ProcessNotificationLevel.Warning,
                    $"Error leyendo celda {cell.Address}",
                    ex.ToString());
                return GetDefaultValue(column, null);
            }
        }

        private bool IsEmptyValue(object value, int dataTypeId)
        {
            if (value == null)
                return true;

            if (value is string strValue)
                return string.IsNullOrWhiteSpace(strValue);

            if (value is double)
            {
                // 0 se considera válido en este dominio
                return false;
            }

            if (value is DateTime dateValue)
            {
                return dateValue == DateTime.MinValue;
            }

            return false;
        }

        private object GetDefaultValue(ConfigColumn column, ColumnRange? range)
        {
            string? defaultValueStr = range?.DefaultValue ?? column.DefaultValue;

            if (string.IsNullOrEmpty(defaultValueStr))
            {
                if (column.DataTypeId == _lookupIds.Numerico)
                    return 0.0;
                if (column.DataTypeId == _lookupIds.Fecha)
                    return string.Empty;
                return string.Empty;
            }

            if (column.DataTypeId == _lookupIds.Numerico)
            {
                return double.TryParse(defaultValueStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var numVal)
                    ? numVal
                    : 0.0;
            }

            if (column.DataTypeId == _lookupIds.Fecha)
            {
                return DateTime.TryParse(defaultValueStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateVal)
                    ? dateVal.ToString("yyyy-MM-dd")
                    : string.Empty;
            }

            return defaultValueStr;
        }

        #endregion

        #region Helpers: construir filas de salida

        private Dictionary<string, object> BuildOutputRow(
    IXLWorksheet worksheet,
    int currentRow,
    double numericValue,
    ConfigColumn contenedoraColumn,
    Dictionary<int, object> contextValues,
    List<(ConfigColumn Column, ColumnRange Range)> dimensions,
    Dictionary<int, object> constantValues,
    ConfigTemplate templateConfig,
    List<ConfigColumn> orderedOutputColumns,
    HashSet<(int row, int col)> validValueCells)
        {
            var row = new Dictionary<string, object>();

            // CAMBIO: Verificar si esta fila tiene al menos una coincidencia en validValueCells
            bool hasValueInRow = validValueCells.Any(cell => cell.row == currentRow);

            foreach (var column in orderedOutputColumns)
            {
                var columnName = column.NameDisplay ?? column.Name ?? $"Column_{column.IndexColumn}";
                object? value;

                if (column.ColumnTypeId == _lookupIds.Constante)
                {
                    value = constantValues.TryGetValue(column.Id, out var v)
                        ? v
                        : GetDefaultValue(column, null);
                }
                else if (column.ColumnTypeId == _lookupIds.Valor)
                {
                    if (column.Id == contenedoraColumn.Id)
                    {
                        value = numericValue;
                    }
                    else
                    {
                        if (column.DataTypeId == _lookupIds.Numerico)
                        {
                            value = 0.0;
                        }
                        else
                        {
                            if (contextValues.TryGetValue(column.Id, out var ctxVal))
                                value = ctxVal;
                            else
                                value = GetDefaultValue(column, null);
                        }
                    }
                }
                else if (column.ColumnTypeId == _lookupIds.Dimension)
                {
                    // CAMBIO: Solo escribir dimension si la fila tiene coincidencia con Valor
                    if (!hasValueInRow)
                    {
                        value = string.Empty;
                    }
                    else
                    {
                        var applicable = dimensions.FirstOrDefault(d => d.Column.Id == column.Id);
                        if (applicable.Column != null && applicable.Range != null)
                        {
                            var rangeFrom = ParseCellAddress(applicable.Range.RFrom!);
                            var cellValue = worksheet.Cell(rangeFrom.Row, rangeFrom.Col);
                            var readValue = ReadCellValue(cellValue, applicable.Column);

                            if (!string.IsNullOrEmpty(applicable.Range.DefaultValue))
                            {
                                value = applicable.Range.DefaultValue;
                            }
                            else if (!IsEmptyValue(readValue, applicable.Column.DataTypeId))
                            {
                                value = readValue;
                            }
                            else
                            {
                                value = GetDefaultValue(applicable.Column, applicable.Range);
                            }
                        }
                        else
                        {
                            value = GetDefaultValue(column, null);
                        }
                    }
                }
                else
                {
                    value = column.DataTypeId == _lookupIds.Numerico ? 0.0 : string.Empty;
                }

                row[columnName] = value ?? string.Empty;
            }

            return row;
        }

        #endregion

        #region Utilidades: Parseo direcciones y tamaños

        private (int Row, int Col) ParseCellAddress(string cellAddress)
        {
            var match = Regex.Match(input: cellAddress.ToUpperInvariant(), pattern: @"^([A-Z]+)(\d+)$");

            if (!match.Success)
                throw new ArgumentException($"Dirección de celda inválida: {cellAddress}");

            var colLetters = match.Groups[1].Value;
            var rowNumber = int.Parse(match.Groups[2].Value);

            int colNumber = 0;
            for (int i = 0; i < colLetters.Length; i++)
            {
                colNumber = colNumber * 26 + (colLetters[i] - 'A' + 1);
            }

            return (rowNumber, colNumber);
        }

        private int GetRangeSize(ColumnRange range)
        {
            try
            {
                var from = ParseCellAddress(range.RFrom!);
                var to = ParseCellAddress(range.RTo!);
                return (to.Row - from.Row + 1) * (to.Col - from.Col + 1);
            }
            catch
            {
                return 0;
            }
        }

        #endregion

        #region Escritura de archivo de salida

        private void WriteOutputFile(
            string outputFilePath,
            List<Dictionary<string, object>> rows,
            ConfigTemplate templateConfig)
        {
            try
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Datos Normalizados");

                var headers = templateConfig.ConfigColumns
                    .OrderBy(c => c.IndexColumn)
                    .Select(c => c.NameDisplay ?? c.Name ?? $"Column_{c.IndexColumn}")
                    .ToList();

                for (int i = 0; i < headers.Count; i++)
                {
                    worksheet.Cell(1, i + 1).Value = headers[i];
                    worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                }

                for (int i = 0; i < rows.Count; i++)
                {
                    var row = rows[i];
                    int colIndex = 1;

                    foreach (var header in headers)
                    {
                        var cell = worksheet.Cell(i + 2, colIndex);
                        if (row.TryGetValue(header, out var value))
                        {
                            if (value is DateTime dateValue)
                                cell.Value = dateValue;
                            else if (value is double doubleValue)
                                cell.Value = doubleValue;
                            else
                                cell.Value = value?.ToString() ?? string.Empty;
                        }
                        colIndex++;
                    }
                }

                worksheet.Columns().AdjustToContents();
                workbook.SaveAs(outputFilePath);

                Notify(ProcessNotificationLevel.Info,
                    $"✓ Archivo guardado: {Path.GetFileName(outputFilePath)}");
            }
            catch (Exception ex)
            {
                Notify(ProcessNotificationLevel.Error,
                    "Error al escribir archivo de salida",
                    ex.ToString());
                throw;
            }
        }

        #endregion
    }
}