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

                // Separar columnas Valor en Normales y Contenedoras
                var normalValueColumns = valueColumns.Where(c => !IsContenedor(c)).ToList();
                var contenedoraColumns = valueColumns.Where(c => IsContenedor(c)).ToList();

                Notify(ProcessNotificationLevel.Info,
                    $"Columnas Valor - Normales: {normalValueColumns.Count}, Contenedoras: {contenedoraColumns.Count}");

                // PASO 1: Identificar filas válidas desde columnas Valor Normal
                var validRows = IdentifyValidRows(worksheet, normalValueColumns);

                Notify(ProcessNotificationLevel.Info,
                    $"Filas válidas identificadas: {validRows.Count} ({string.Join(", ", validRows.OrderBy(r => r))})");

                if (validRows.Count == 0)
                {
                    Notify(ProcessNotificationLevel.Warning, "No se encontraron filas válidas para procesar");
                    return resultRows;
                }

                // Columnas de salida ordenadas por IndexColumn
                var orderedOutputColumns = templateConfig.ConfigColumns
                    .OrderBy(c => c.IndexColumn)
                    .ToList();

                // PASO 2: Procesar columnas Valor Normal (generar filas base)
                var normalValueData = ProcessNormalValueColumns(worksheet, normalValueColumns, validRows);

                Notify(ProcessNotificationLevel.Info,
                    $"Datos de columnas normales procesados: {normalValueData.Count} filas");

                // CONJUNTO 1: Generar filas solo con columnas normales
                var normalRows = BuildNormalValueRows(
                    normalValueData,
                    constantValues,
                    orderedOutputColumns,
                    contenedoraColumns);

                resultRows.AddRange(normalRows);

                Notify(ProcessNotificationLevel.Info,
                    $"Filas de valores normales generadas: {normalRows.Count}");

                // CONJUNTO 2: Procesar columnas Contenedoras + Dimensiones (si existen)
                if (contenedoraColumns.Any() && dimensionColumns.Any())
                {
                    var dimensionRows = ProcessAndBuildDimensionRows(
                        worksheet,
                        dimensionColumns,
                        contenedoraColumns,
                        normalValueData,
                        constantValues,
                        orderedOutputColumns,
                        validRows);

                    resultRows.AddRange(dimensionRows);

                    Notify(ProcessNotificationLevel.Info,
                        $"Filas de dimensiones generadas: {dimensionRows.Count}");
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

        #region PASO 1: Identificar filas válidas

        private HashSet<int> IdentifyValidRows(IXLWorksheet worksheet, List<ConfigColumn> normalValueColumns)
        {
            var validRows = new HashSet<int>();

            foreach (var column in normalValueColumns)
            {
                if (column.Ranges == null || !column.Ranges.Any())
                    continue;

                foreach (var range in column.Ranges)
                {
                    var from = ParseCellAddress(range.RFrom!);
                    var to = ParseCellAddress(range.RTo!);

                    for (int row = from.Row; row <= to.Row; row++)
                    {
                        for (int col = from.Col; col <= to.Col; col++)
                        {
                            var cell = worksheet.Cell(row, col);
                            var value = ReadCellValue(cell, column);

                            if (!IsEmptyValue(value, column.DataTypeId))
                            {
                                validRows.Add(row);
                                break;
                            }
                        }
                    }
                }
            }

            return validRows;
        }

        #endregion

        #region PASO 2: Procesar columnas Valor Normal con fill-forward

        private Dictionary<int, Dictionary<int, object>> ProcessNormalValueColumns(
            IXLWorksheet worksheet,
            List<ConfigColumn> normalValueColumns,
            HashSet<int> validRows)
        {
            // Estructura: [fila][columnId] = valor
            var data = new Dictionary<int, Dictionary<int, object>>();

            // Últimos valores válidos por columna (para fill-forward)
            var lastValidValues = new Dictionary<int, object>();

            // Ordenar filas válidas para procesamiento secuencial (fill-forward)
            var sortedRows = validRows.OrderBy(r => r).ToList();

            foreach (int row in sortedRows)
            {
                data[row] = new Dictionary<int, object>();

                foreach (var column in normalValueColumns)
                {
                    if (column.Ranges == null || !column.Ranges.Any())
                        continue;

                    object? foundValue = null;

                    foreach (var range in column.Ranges)
                    {
                        var from = ParseCellAddress(range.RFrom!);
                        var to = ParseCellAddress(range.RTo!);

                        if (row < from.Row || row > to.Row)
                            continue;

                        for (int col = from.Col; col <= to.Col; col++)
                        {
                            var cell = worksheet.Cell(row, col);
                            var value = ReadCellValue(cell, column);

                            if (!IsEmptyValue(value, column.DataTypeId))
                            {
                                foundValue = value;
                                break;
                            }
                        }

                        if (foundValue != null)
                            break;
                    }

                    // Fill-forward: usar último valor válido o default
                    if (foundValue == null || IsEmptyValue(foundValue, column.DataTypeId))
                    {
                        if (lastValidValues.TryGetValue(column.Id, out var lastVal))
                        {
                            foundValue = lastVal;
                        }
                        else
                        {
                            foundValue = GetDefaultValue(column, null);
                        }
                    }
                    else
                    {
                        // Actualizar último valor válido
                        lastValidValues[column.Id] = foundValue;
                    }

                    data[row][column.Id] = foundValue;
                }
            }

            return data;
        }

        #endregion

        #region CONJUNTO 1: Generar filas solo con valores normales

        private List<Dictionary<string, object>> BuildNormalValueRows(
            Dictionary<int, Dictionary<int, object>> normalValueData,
            Dictionary<int, object> constantValues,
            List<ConfigColumn> orderedOutputColumns,
            List<ConfigColumn> contenedoraColumns)
        {
            var rows = new List<Dictionary<string, object>>();

            foreach (var row in normalValueData.Keys.OrderBy(r => r))
            {
                var normalData = normalValueData[row];
                var outputRow = new Dictionary<string, object>();

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
                        if (IsContenedor(column))
                        {
                            // Contenedora = 0 o default en filas normales
                            value = GetDefaultValue(column, null);
                        }
                        else
                        {
                            value = normalData.TryGetValue(column.Id, out var v)
                                ? v
                                : GetDefaultValue(column, null);
                        }
                    }
                    else if (column.ColumnTypeId == _lookupIds.Dimension)
                    {
                        // Dimensión = vacío en filas normales
                        value = string.Empty;
                    }
                    else
                    {
                        value = GetDefaultValue(column, null);
                    }

                    outputRow[columnName] = value ?? string.Empty;
                }

                rows.Add(outputRow);
            }

            return rows;
        }

        #endregion

        #region CONJUNTO 2: Procesar y generar filas de dimensiones

        private List<Dictionary<string, object>> ProcessAndBuildDimensionRows(
            IXLWorksheet worksheet,
            List<ConfigColumn> dimensionColumns,
            List<ConfigColumn> contenedoraColumns,
            Dictionary<int, Dictionary<int, object>> normalValueData,
            Dictionary<int, object> constantValues,
            List<ConfigColumn> orderedOutputColumns,
            HashSet<int> validRows)
        {
            var dimensionRows = new List<Dictionary<string, object>>();

            // Identificar todas las celdas únicas a procesar
            var cellsToProcess = new HashSet<(int row, int col)>();

            foreach (var dimColumn in dimensionColumns)
            {
                if (dimColumn.Ranges == null || !dimColumn.Ranges.Any())
                    continue;

                foreach (var range in dimColumn.Ranges)
                {
                    var from = ParseCellAddress(range.RFrom!);
                    var to = ParseCellAddress(range.RTo!);

                    for (int row = from.Row; row <= to.Row; row++)
                    {
                        if (!validRows.Contains(row))
                            continue;

                        for (int col = from.Col; col <= to.Col; col++)
                        {
                            cellsToProcess.Add((row, col));
                        }
                    }
                }
            }

            // Procesar cada celda única
            foreach (var (row, col) in cellsToProcess.OrderBy(c => c.row).ThenBy(c => c.col))
            {
                var cell = worksheet.Cell(row, col);

                // Determinar qué columna contenedora corresponde
                var contenedora = GetContenedoraForCell(contenedoraColumns, row, col);
                if (contenedora == null)
                    continue;

                // Leer valor de la celda
                var cellValue = ReadCellValue(cell, contenedora);

                // Solo procesar si hay un valor válido (no vacío)
                if (IsEmptyValue(cellValue, contenedora.DataTypeId))
                    continue;

                // Obtener valores normales de esta fila
                var normalData = normalValueData.ContainsKey(row)
                    ? normalValueData[row]
                    : new Dictionary<int, object>();

                // Obtener todas las dimensiones aplicables a esta celda
                var dimensionsAtCell = GetAllDimensionsAtCell(
                    dimensionColumns,
                    row,
                    col);

                // Construir fila de salida
                var outputRow = BuildDimensionRow(
                    normalData,
                    constantValues,
                    contenedora,
                    cellValue,
                    dimensionsAtCell,
                    orderedOutputColumns);

                dimensionRows.Add(outputRow);
            }

            return dimensionRows;
        }

        private ConfigColumn? GetContenedoraForCell(
            List<ConfigColumn> contenedoraColumns,
            int row,
            int col)
        {
            foreach (var cont in contenedoraColumns)
            {
                if (cont.Ranges == null)
                    continue;

                foreach (var contRange in cont.Ranges)
                {
                    var from = ParseCellAddress(contRange.RFrom!);
                    var to = ParseCellAddress(contRange.RTo!);

                    if (row >= from.Row && row <= to.Row && col >= from.Col && col <= to.Col)
                    {
                        return cont;
                    }
                }
            }

            return null;
        }

        private Dictionary<int, string> GetAllDimensionsAtCell(
            List<ConfigColumn> dimensionColumns,
            int row,
            int col)
        {
            var dimensions = new Dictionary<int, string>();

            foreach (var dimCol in dimensionColumns)
            {
                if (dimCol.Ranges == null)
                    continue;

                foreach (var range in dimCol.Ranges)
                {
                    var from = ParseCellAddress(range.RFrom!);
                    var to = ParseCellAddress(range.RTo!);

                    if (row >= from.Row && row <= to.Row && col >= from.Col && col <= to.Col)
                    {
                        dimensions[dimCol.Id] = range.DefaultValue ?? string.Empty;
                        break;
                    }
                }
            }

            return dimensions;
        }

        private Dictionary<string, object> BuildDimensionRow(
            Dictionary<int, object> normalData,
            Dictionary<int, object> constantValues,
            ConfigColumn contenedoraColumn,
            object contenedoraValue,
            Dictionary<int, string> dimensionsAtCell,
            List<ConfigColumn> orderedOutputColumns)
        {
            var outputRow = new Dictionary<string, object>();

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
                        // Columna contenedora: usar valor de la celda
                        value = contenedoraValue;
                    }
                    else if (IsContenedor(column))
                    {
                        // Otra contenedora: poner en 0 o default
                        value = GetDefaultValue(column, null);
                    }
                    else
                    {
                        // Columna normal: solo copiar si es TEXTO (llave)
                        if (column.DataTypeId == _lookupIds.Texto)
                        {
                            // Es una "llave": copiar desde normalData
                            value = normalData.TryGetValue(column.Id, out var v)
                                ? v
                                : GetDefaultValue(column, null);
                        }
                        else
                        {
                            // Es numérica o fecha: poner en default (0 para numérico)
                            value = GetDefaultValue(column, null);
                        }
                    }
                }
                else if (column.ColumnTypeId == _lookupIds.Dimension)
                {
                    // Dimensión: usar DefaultValue si aplica a esta celda
                    value = dimensionsAtCell.TryGetValue(column.Id, out var dimValue)
                        ? dimValue
                        : string.Empty;
                }
                else
                {
                    value = GetDefaultValue(column, null);
                }

                outputRow[columnName] = value ?? string.Empty;
            }

            return outputRow;
        }

        #endregion

        #region Helpers: detección contenedora

        private bool IsContenedor(ConfigColumn col)
        {
            if (col == null) return false;
            if (col.ColumnTypeId != _lookupIds.Valor) return false;
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

        #endregion

        #region Helpers: lectura de constantes

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

            if (value is double doubleValue)
            {
                // 0 se considera vacío para validación de filas válidas
                // pero válido para datos numéricos
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

        #region Utilidades: Parseo direcciones

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