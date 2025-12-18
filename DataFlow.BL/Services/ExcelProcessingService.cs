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
            string inputFilePath,
            ConfigTemplate templateConfig)
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

                // Constantes por Id de columna
                var constantValues = ReadConstantValues(worksheet, constantColumns);

                // Columnas contenedoras: columnas de valor numéricas con rango horizontal
                var contenedoraColumns = valueColumns.Where(IsContenedor).ToList();
                Notify(ProcessNotificationLevel.Info,
                    $"Columnas contenedoras detectadas: {contenedoraColumns.Count}");

                // Índice de dimensiones por celda
                var dimensionIndex = BuildDimensionIndex(dimensionColumns);

                // Orden de prioridad de dimensiones
                var dimensionOrder = DetermineDimensionOrder(dimensionColumns);

                // Últimos valores válidos por columna (fill-forward), rellenar con lo ultimo que tiene
                var lastValidValues = new Dictionary<int, object>();

                // Columnas de salida ordenadas por IndexColumn
                var orderedOutputColumns = templateConfig.ConfigColumns
                    .OrderBy(c => c.IndexColumn)
                    .ToList();

                foreach (var contCol in contenedoraColumns)
                {
                    foreach (var range in contCol.Ranges ?? Enumerable.Empty<ColumnRange>())
                    {
                        var fromAddr = ParseCellAddress(range.RFrom!);
                        var toAddr = ParseCellAddress(range.RTo!);

                        Notify(ProcessNotificationLevel.Info,
                            $"Procesando rango contenedor {range.RFrom} a {range.RTo} para columna '{contCol.Name}'");

                        for (int row = fromAddr.Row; row <= toAddr.Row; row++)
                        {
                            // Valores de contexto para esta fila
                            var contextValues = ReadContextValuesForRow(
                                worksheet, row, valueColumns, contCol, lastValidValues);

                            var prodTotalRow = BuildProductionTotalRow(
                                contextValues, constantValues, templateConfig, orderedOutputColumns);


                            if (prodTotalRow != null)
                            {
                                resultRows.Add(prodTotalRow);
                            }

                            // Celdas dentro del rango
                            for (int col = fromAddr.Col; col <= toAddr.Col; col++)
                            {
                                var cell = worksheet.Cell(row, col);

                                if (!cell.TryGetValue(out double cellValue) || cellValue == 0)
                                    continue;

                                var applicableDims = GetOrderedDimensionsForCell(
                                    row, col, dimensionIndex, dimensionOrder);

                                var outputRow = BuildOutputRow(
                                    cellValue,
                                    contCol,
                                    contextValues,
                                    applicableDims,
                                    constantValues,
                                    templateConfig,
                                    orderedOutputColumns);

                                resultRows.Add(outputRow);
                            }
                        }
                    }
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
                if (column.Ranges?.Any() == true)
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
                    return DateTime.MinValue;
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
                    ? dateVal
                    : DateTime.MinValue;
            }

            return defaultValueStr;
        }

        #endregion

        #region Helpers: construir filas de salida

        private Dictionary<string, object> BuildOutputRow(
            double numericValue,
            ConfigColumn contenedoraColumn,
            Dictionary<int, object> contextValues,
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
                    var applicable = dimensions.FirstOrDefault(d => d.Column.Id == column.Id);
                    if (applicable.Column != null && !string.IsNullOrEmpty(applicable.Range.DefaultValue))
                    {
                        value = applicable.Range.DefaultValue;
                    }
                    else
                    {
                        value = string.Empty;
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

        //Helper para encontrar una columna totalizadora
        private bool IsTotalizadora(ConfigColumn col)
        {
            if (col == null) return false;
            if (col.ColumnTypeId != _lookupIds.Valor) return false;
            if (col.DataTypeId != _lookupIds.Numerico) return false;
            if (col.Ranges == null || !col.Ranges.Any()) return false;

            // Aqui buscamos que nosean mas de 1 columna
            foreach (var r in col.Ranges)
            {
                var from = ParseCellAddress(r.RFrom!);
                var to = ParseCellAddress(r.RTo!);

                if (to.Col > from.Col)
                    return false;
            }

            return true;
        }

        // Buscamos columna totalizadora y construimos fila

        private Dictionary<string, object>? BuildProductionTotalRow(
            Dictionary<int, object> contextValues,
            Dictionary<int, object> constantValues,
            ConfigTemplate templateConfig,
            List<ConfigColumn> orderedOutputColumns)
        {
            
            var totalizadoraColumns = templateConfig.ConfigColumns
                .Where(c => IsTotalizadora(c))
                .ToList();

            if (!totalizadoraColumns.Any())
                return null;

            
            ConfigColumn? selectedTotalizadora = null;
            object? totalizadoraValue = null;

            foreach (var totCol in totalizadoraColumns)
            {
                if (contextValues.TryGetValue(totCol.Id, out var value))
                {
                    if (!IsEmptyValue(value, totCol.DataTypeId))
                    {
                        selectedTotalizadora = totCol;
                        totalizadoraValue = value;
                        break;
                    }
                }
            }

            if (selectedTotalizadora == null || totalizadoraValue == null)
                return null;

            Notify(ProcessNotificationLevel.Info,
                $"Generando fila totalizadora para '{selectedTotalizadora.Name}' con valor: {totalizadoraValue}");

            var row = new Dictionary<string, object>();

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
                    if (column.Id == selectedTotalizadora.Id)
                    {
                        value = totalizadoraValue;
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
                    // Dimensiones no aplican en la fila totalizadora
                    value = string.Empty;
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
            var match = Regex.Match(cellAddress.ToUpper(), @"^([A-Z]+)(\d+)$");

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
