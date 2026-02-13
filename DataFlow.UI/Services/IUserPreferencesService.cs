using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataFlow.UI.Services
{
    public interface IUserPreferencesService
    {
        /// <summary>
        /// Obtiene la última carpeta utilizada para seleccionar archivos Excel.
        /// </summary>
        /// <returns>Ruta de la última carpeta o carpeta de documentos por defecto.</returns>
        string GetLastExcelFolder();

        /// <summary>
        /// Guarda la última carpeta utilizada para seleccionar archivos Excel.
        /// </summary>
        /// <param name="folderPath">Ruta de la carpeta a guardar.</param>
        void SaveLastExcelFolder(string folderPath);

        /// <summary>
        /// Obtiene si se debe abrir automaticamente el archivo de Excel resultante.
        /// </summary>
        /// <returns>true si debe abrirse automaticamente, false en caso contrario</returns>
        bool GetAutoOpenExcelFile();

        /// <summary>
        /// Establece si se debe abrir automaticamente el archivo de Excel resultante.
        /// </summary>
        /// <param name="autoOpen">true para abrir automaticamente, false para no abrir</param>
        void SaveAutoOpenExcelFile(bool autoOpen);

        /// <summary>
        /// Obtiene todas las preferencias del usuario.
        /// </summary>
        /// <returns>Objeto con todas las preferencias</returns>
        UserPreferences GetAllPreferences();

        /// <summary>
        /// Guarda todas las preferencias del usuario.
        /// </summary>
        void SaveAllPreferences(UserPreferences preferences);
    }

    /// <summary>
    /// Modelo que contiene todas las preferencias del usuario.
    /// </summary>
    public class UserPreferences
    {
        public string? LastExcelFolder { get; set; }
        public bool AutoOpenExcelFile { get; set; } = false;
        
    }

}
