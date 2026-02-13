using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace DataFlow.UI.Services
{
    public class UserPreferencesService : IUserPreferencesService
    {
        private const string PreferencesFilesName = "user_preferences.json";
        private readonly string _configDirectory;
        private readonly string _preferencesFilePath;
        private readonly ILogger<UserPreferencesService> _logger;
        private UserPreferences _cachedPreferences;

        public UserPreferencesService(ILogger<UserPreferencesService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _configDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DataFlow");

            _preferencesFilePath = Path.Combine(_configDirectory, PreferencesFilesName);
            if (!Directory.Exists(_configDirectory))
            {
                try
                {
                    Directory.CreateDirectory(_configDirectory);
                    _logger.LogInformation("Directorio de configuracion creado: {ConfigDirectory}", _configDirectory);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creando el directorio de configuracion: {ConfigDirectory}", _configDirectory);
                }
            }
            _cachedPreferences = LoadPreferencesFromFile() ?? CreateDefaultPreferences();
        }

        public string GetLastExcelFolder()
        {
            if (!string.IsNullOrWhiteSpace(_cachedPreferences.LastExcelFolder) && Directory.Exists(_cachedPreferences.LastExcelFolder))
            {
                return _cachedPreferences.LastExcelFolder;
            }
            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }
        public void SaveLastExcelFolder(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            {
                _logger.LogWarning("Intento de guardar una ruta de carpeta no válida: {FolderPath}", folderPath);
                return;
            }
            _cachedPreferences.LastExcelFolder = folderPath.Trim();
            SavePreferencesToFile();
            _logger.LogInformation("Última carpeta de Excel guardada: {FolderPath}", folderPath);
        }
        public bool GetAutoOpenExcelFile()
        {
            return _cachedPreferences.AutoOpenExcelFile;
        }
        public void SaveAutoOpenExcelFile(bool autoOpen)
        {
            _cachedPreferences.AutoOpenExcelFile = autoOpen;
            SavePreferencesToFile();
            _logger.LogInformation("Preferencia de abrir automáticamente el archivo de Excel guardada: {AutoOpen}", autoOpen);
        }
        public UserPreferences GetAllPreferences()
        {
            return new UserPreferences
            {
                LastExcelFolder = _cachedPreferences.LastExcelFolder,
                AutoOpenExcelFile = _cachedPreferences.AutoOpenExcelFile
            };
        }
        public void SaveAllPreferences(UserPreferences preferences)
        {
            if (preferences == null)
            {
                _logger.LogWarning("Intento de guardar preferencias nulas");
                return;
            }
            _cachedPreferences = new UserPreferences
            {
                LastExcelFolder = preferences.LastExcelFolder,
                AutoOpenExcelFile = preferences.AutoOpenExcelFile
            };
            SavePreferencesToFile();
            _logger.LogInformation("Todas las preferencias de usuario guardadas");

        }

        /// <summary>
        /// Carga las preferencias de usuario desde el archivo JSON. Si el archivo no existe o hay un error, devuelve null.
        /// </summary>
        private UserPreferences? LoadPreferencesFromFile()
        {
            try
            {
                if (!File.Exists(_preferencesFilePath))
                {
                    _logger.LogInformation("Archivo de preferencias no encontrado, se crearán preferencias por defecto");
                    return null;
                }
                string jsonContent = File.ReadAllText(_preferencesFilePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                };
                var preferences = JsonSerializer.Deserialize<UserPreferences>(jsonContent, options);
                if (preferences == null)
                {
                    _logger.LogWarning("La deserialización resultó en null");
                    return null;
                }

                _logger.LogInformation("Preferencias de usuario cargadas desde el archivo - AutoOpenExcelFile: {AutoOpen}", preferences.AutoOpenExcelFile);
                return preferences;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar las preferencias de usuario desde el archivo");
                return null;
            }
        }

        /// <summary>
        /// Guarda las preferencias en el archivo JSON
        /// </summary>
        private void SavePreferencesToFile()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true,
                    
                };
                string jsonContent = JsonSerializer.Serialize(_cachedPreferences, options);
                File.WriteAllText(_preferencesFilePath, jsonContent);
                _logger.LogInformation("Preferencias de usuario guardadas en el archivo");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar las preferencias de usuario en el archivo");
            }
        }
        private UserPreferences CreateDefaultPreferences()
        {
            return new UserPreferences
            {
                LastExcelFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                AutoOpenExcelFile = false
            };
        }
    }
}
