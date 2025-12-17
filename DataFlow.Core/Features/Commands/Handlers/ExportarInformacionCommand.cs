using DataFlow.Core.Common;
using DataFlow.Core.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DataFlow.Core.Features.Commands.Handlers
{
    public class ExportarInformacionCommandHandler : ICommandHandler<ExportarInformacionCommand, Result<bool>>
    {
        private readonly IParametroRepository _parametroRepository;
        private readonly IConfigColumnRepository _configColumnRepository;
        private readonly IColumnRangeRepository _rangeRepository;
        private readonly IConfigTemplateRepository _configTemplateRepository;
        private readonly ILogger<ExportarInformacionCommandHandler> _logger;
        public ExportarInformacionCommandHandler(
            IParametroRepository parametroRepository,
            IConfigColumnRepository configColumnRepository,
            IColumnRangeRepository rangeRepository,
            IConfigTemplateRepository configTemplateRepository,
            ILogger<ExportarInformacionCommandHandler> logger)
        {
            _parametroRepository = parametroRepository ?? throw new ArgumentNullException(nameof(parametroRepository));
            _configColumnRepository = configColumnRepository ?? throw new ArgumentNullException(nameof(configColumnRepository));
            _rangeRepository = rangeRepository ?? throw new ArgumentNullException(nameof(rangeRepository));
            _configTemplateRepository = configTemplateRepository ?? throw new ArgumentNullException(nameof(configTemplateRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        public async Task<Result<bool>> HandleAsync(ExportarInformacionCommand command, CancellationToken cancellationToken = default)
        {
            if(command is null)
            {
                _logger.LogError("El comando es nulo.");
                return Result<bool>.Failure("El comando ExportarInformacionCommand es nulo.");
            }
            if (command.ParametroId <= 0)
            {
                _logger.LogError("El ParametroId proporcionado no es válido: {ParametroId}", command.ParametroId);
                return Result<bool>.Failure("El ParametroId proporcionado no es válido.");
            }
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var paramOption = await _parametroRepository.GetByIdAsync(command.ParametroId, cancellationToken);
                if (paramOption == null) {
                    _logger.LogError("No se encontró el parámetro con ID: {ParametroId}", command.ParametroId);
                    return Result<bool>.Failure($"No se encontró el parámetro con ID: {command.ParametroId}");
                }
                if(string.IsNullOrWhiteSpace(paramOption.ParametroValue))
                {
                    _logger.LogError("El parámetro con ID: {ParametroId} tiene un valor nulo o vacío.", command.ParametroId);
                    return Result<bool>.Failure($"El parámetro con ID: {command.ParametroId} tiene un valor nulo o vacío.");
                }
                var folderPath = Path.GetFullPath( paramOption.ParametroValue!);
                if (!System.IO.Directory.Exists(folderPath))
                {
                    _logger.LogError("El direcorio no existe: {FolderPath}", folderPath);
                    return Result<bool>.Failure($"El directorio no existe: {folderPath}.");
                }
                cancellationToken.ThrowIfCancellationRequested();
                var templates = await _configTemplateRepository.GetAllWithDetailAsync(cancellationToken);
                var exportTemplates = templates.Select(tpl => new
                {
                    tpl.Id,
                    tpl.Description,
                    tpl.CreatedAt,
                    tpl.UpdatedAt,
                    ConfigColumns = tpl.ConfigColumns?.Select( col => (object)new
                    {
                        col.Id,
                        col.ConfigTemplateId,
                        col.IndexColumn,
                        col.Name,
                        col.NameDisplay,
                        col.Description,
                        col.DataTypeId,
                        col.ColumnTypeId,
                        col.DefaultValue,
                        col.CreatedAt,
                        col.UpdatedAt,
                        Ranges = col.Ranges?.Select( r => (object)new
                        {
                            r.Id,
                            r.ConfigColumnId,
                            r.RFrom,
                            r.RTo,
                            r.DefaultValue,
                            r.CreatedAt,
                            r.UpdatedAt
                        }).ToList() ?? new List<object>()
                    }).ToList() ?? new List<object>(),
                    HistProcess = tpl.HistProcess?.Select(h => (object)new
                    {
                        h.Id,
                        h.ConfigTemplateId,
                        h.FileSource,
                        h.FileTarget,
                        h.FinalStatus,
                        h.DataProcess,
                        h.CreatedAt,
                        h.UpdatedAt
                    }).ToList() ?? new List<object>()
                }).ToList();
                cancellationToken.ThrowIfCancellationRequested();
                var exportRoot = new                 {
                    ExportedAt = DateTime.UtcNow,
                    Templates = exportTemplates
                };
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                var fileName = $"dataflow_export_{DateTime.UtcNow:ddMMyyyy_HHmmss}.json";
                var filePath = Path.Combine(folderPath, fileName);

                var json = JsonSerializer.Serialize(exportRoot, options);
                await File.WriteAllTextAsync(filePath, json, cancellationToken);
                _logger.LogInformation("Exportación completada. Archivo generado: {FilePath}", filePath);
                return Result<bool>.Success(true);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Exportación cancelada por token.");
                return Result<bool>.Failure("Exportación cancelada.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al exportar la información.");
                return Result<bool>.Failure($"Error al exportar la información: {ex.Message}");
            }
        }
    }
}
