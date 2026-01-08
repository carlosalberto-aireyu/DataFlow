using System;
using System.Text.RegularExpressions;

namespace DataFlow.Core.Utilities
{
    /// <summary>
    /// Utilidades para convertir y manipular direcciones de Excel
    /// </summary>
    public static class ExcelAddressConverter
    {
        /// <summary>
        /// Parsea una dirección de celda (ej: "A1") en componentes
        /// </summary>
        public static (string ColumnLetter, int Row) ParseCellAddress(string cellAddress)
        {
            if (string.IsNullOrWhiteSpace(cellAddress))
                throw new ArgumentException("La dirección de celda no puede estar vacía", nameof(cellAddress));

            var match = Regex.Match(cellAddress.ToUpperInvariant(), @"^([A-Z]+)(\d+)$");
            if (!match.Success)
                throw new ArgumentException($"Dirección de celda inválida: {cellAddress}", nameof(cellAddress));

            var columnLetter = match.Groups[1].Value;
            var row = int.Parse(match.Groups[2].Value);

            return (columnLetter, row);
        }

        /// <summary>
        /// Convierte letras de columna (A, B, etc.) a número (1, 2, etc.)
        /// </summary>
        public static int ColumnLettersToNumber(string columnLetter)
        {
            if (string.IsNullOrWhiteSpace(columnLetter))
                throw new ArgumentException("Las letras de columna no pueden estar vacías", nameof(columnLetter));

            columnLetter = columnLetter.ToUpperInvariant();
            int columnNumber = 0;

            for (int i = 0; i < columnLetter.Length; i++)
            {
                columnNumber = columnNumber * 26 + (columnLetter[i] - 'A' + 1);
            }

            return columnNumber;
        }

        /// <summary>
        /// Cambia la columna Excel en una dirección manteniendo la fila
        /// </summary>
        /// <example>
        /// ChangeColumnInAddress("A15", "B") → "B15"
        /// </example>
        public static string ChangeColumnInAddress(string cellAddress, string newColumnLetter)
        {
            if (string.IsNullOrWhiteSpace(cellAddress))
                throw new ArgumentException("La dirección de celda no puede estar vacía", nameof(cellAddress));

            if (string.IsNullOrWhiteSpace(newColumnLetter))
                throw new ArgumentException("La nueva columna no puede estar vacía", nameof(newColumnLetter));

            var (_, row) = ParseCellAddress(cellAddress);
            return $"{newColumnLetter.ToUpperInvariant()}{row}";
        }

        /// <summary>
        /// Cambia la columna en un rango completo (RFrom:RTo)
        /// </summary>
        /// <example>
        /// ChangeColumnInRange("A1:A10", "B") → ("B1:B10")
        /// </example>
        public static (string NewRFrom, string NewRTo) ChangeColumnInRange(
            string rFrom,
            string rTo,
            string newColumnLetter)
        {
            if (string.IsNullOrWhiteSpace(rFrom) || string.IsNullOrWhiteSpace(rTo))
                throw new ArgumentException("RFrom y RTo no pueden estar vacíos");

            var newRFrom = ChangeColumnInAddress(rFrom, newColumnLetter);
            var newRTo = ChangeColumnInAddress(rTo, newColumnLetter);

            return (newRFrom, newRTo);
        }
    }
}