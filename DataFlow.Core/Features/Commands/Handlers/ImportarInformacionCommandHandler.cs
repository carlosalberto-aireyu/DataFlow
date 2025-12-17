using DataFlow.Core.Common;
using DataFlow.Core.Features.Dtos;
using DataFlow.Core.Models;
using DataFlow.Core.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DataFlow.Core.Features.Commands.Handlers
{
    public class ImportarInformacionCommandHandler : ICommandHandler<ImportarInformacionCommand, Result<bool>>
    {
        private readonly IConfigTemplateRepository _configTemplateRepository;
        private readonly IConfigColumnRepository _configColumnRepository;
        private readonly IColumnRangeRepository _rangeRepository;
        private readonly ILogger<ImportarInformacionCommandHandler> _logger;

        public ImportarInformacionCommandHandler(
            IConfigTemplateRepository configTemplateRepository,
            IConfigColumnRepository configColumnRepository,
            IColumnRangeRepository rangeRepository,
            ILogger<ImportarInformacionCommandHandler> logger)
        {
            _configTemplateRepository = configTemplateRepository ?? throw new ArgumentNullException(nameof(configTemplateRepository));
            _configColumnRepository = configColumnRepository ?? throw new ArgumentNullException(nameof(configColumnRepository));
            _rangeRepository = rangeRepository ?? throw new ArgumentNullException(nameof(rangeRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<bool>> HandleAsync(ImportarInformacionCommand command, CancellationToken cancellationToken = default)
        {
            if (command is null)
            {
                _logger.LogError("El comando es nulo.");
                return Result<bool>.Failure("El comando ImportarInformacionCommand es nulo.");
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(command.FilePath))
                {
                    _logger.LogError("Ruta de archivo vacía.");
                    return Result<bool>.Failure("La ruta del archivo es requerida.");
                }

                var filePath = Path.GetFullPath(command.FilePath);
                if (!File.Exists(filePath))
                {
                    _logger.LogError("El archivo de importación no existe: {FilePath}", filePath);
                    return Result<bool>.Failure($"El archivo no existe: {filePath}");
                }

                var json = await File.ReadAllTextAsync(filePath, cancellationToken);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var importRoot = JsonSerializer.Deserialize<ImportRootDto>(json, options);
                if (importRoot == null || importRoot.Templates == null || !importRoot.Templates.Any())
                {
                    _logger.LogWarning("Archivo de importación vacío o con formato no valido: {FilePath}", filePath);
                    return Result<bool>.Failure("Archivo de importación vacío o con formato no valido.");
                }

                var existingTemplates = await _configTemplateRepository.GetAllAsync(cancellationToken);

                foreach (var tplDto in importRoot.Templates)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Buscar existencia por Id primero, si falla por descripción (case-insensitive)
                    ConfigTemplate? existingFull = null;

                    if (tplDto.Id > 0)
                    {
                        var byId = await _configTemplateRepository.GetByIdAsync(tplDto.Id, cancellationToken);
                        if (byId != null)
                        {
                            existingFull = await _configTemplateRepository.GetWithDetailAsync(byId.Id, cancellationToken);
                        }
                    }

                    if (existingFull == null && !string.IsNullOrWhiteSpace(tplDto.Description))
                    {
                        var match = existingTemplates.FirstOrDefault(t => string.Equals(t.Description?.Trim(), tplDto.Description?.Trim(), StringComparison.OrdinalIgnoreCase));
                        if (match != null)
                        {
                            existingFull = await _configTemplateRepository.GetWithDetailAsync(match.Id, cancellationToken);
                        }
                    }

                    if (existingFull != null && command.OverwriteExisting)
                    {
                        _logger.LogInformation("Reemplazando plantilla existente: Id {Id}, Description {Description}", existingFull.Id, existingFull.Description);
                        
                        await _configTemplateRepository.DeleteAsync(existingFull, cancellationToken);
                        var changesDel = await _configTemplateRepository.SaveChangesAsync(cancellationToken);
                        _logger.LogDebug("Cambios tras eliminar plantilla existente: {Changes}", changesDel);
                    }
                    else if (existingFull != null && !command.OverwriteExisting)
                    {
                        _logger.LogInformation("Plantilla ya existe y sobre ecribir=No, se la omite: {Description}", tplDto.Description);
                        continue;
                    }

                    
                    var now = DateTime.UtcNow;
                    var newTemplate = new ConfigTemplate
                    {
                        Description = tplDto.Description?.Trim(),
                        CreatedAt = tplDto.CreatedAt ?? now,
                        UpdatedAt = tplDto.UpdatedAt ?? now
                    };

                    await _configTemplateRepository.AddAsync(newTemplate, cancellationToken);
                    await _configTemplateRepository.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("Plantilla importada: Nuevo ID {Id} - Description {Description}", newTemplate.Id, newTemplate.Description);

                    
                    if (tplDto.ConfigColumns != null)
                    {
                        foreach (var colDto in tplDto.ConfigColumns)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            var newColumn = new ConfigColumn
                            {
                                ConfigTemplateId = newTemplate.Id,
                                IndexColumn = colDto.IndexColumn,
                                Name = colDto.Name?.Trim(),
                                NameDisplay = colDto.NameDisplay?.Trim(),
                                Description = colDto.Description?.Trim(),
                                DataTypeId = colDto.DataTypeId,
                                ColumnTypeId = colDto.ColumnTypeId,
                                DefaultValue = colDto.DefaultValue,
                                CreatedAt = colDto.CreatedAt ?? now,
                                UpdatedAt = colDto.UpdatedAt ?? now
                            };

                            await _configColumnRepository.AddAsync(newColumn, cancellationToken);
                            await _configColumnRepository.SaveChangesAsync(cancellationToken);

                            _logger.LogDebug("Columna importada: Nuevo ID {Id} - Name {Name}", newColumn.Id, newColumn.Name);

                            
                            if (colDto.Ranges != null)
                            {
                                foreach (var rDto in colDto.Ranges)
                                {
                                    cancellationToken.ThrowIfCancellationRequested();

                                    var newRange = new ColumnRange
                                    {
                                        ConfigColumnId = newColumn.Id,
                                        RFrom = rDto.RFrom,
                                        RTo = rDto.RTo,
                                        DefaultValue = rDto.DefaultValue,
                                        CreatedAt = rDto.CreatedAt ?? now,
                                        UpdatedAt = rDto.UpdatedAt ?? now
                                    };

                                    await _rangeRepository.AddAsync(newRange, cancellationToken);
                                    await _rangeRepository.SaveChangesAsync(cancellationToken);

                                    _logger.LogDebug("Rango importado: Nuevo ID {Id} for ColumnId {ColumnId}", newRange.Id, newRange.ConfigColumnId);
                                }
                            }
                        }
                    }

                    // HistProcess No esta incluido en la recuperacion por ahora
                }

                _logger.LogInformation("Importación completada desde {FilePath}", filePath);
                return Result<bool>.Success(true);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Importación cancelada.");
                return Result<bool>.Failure("Importación cancelada.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al importar la información.");
                return Result<bool>.Failure($"Error al importar la información: {ex.Message}");
            }
        }
    }
}
