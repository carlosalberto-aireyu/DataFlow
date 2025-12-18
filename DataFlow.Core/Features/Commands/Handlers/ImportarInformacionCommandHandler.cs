using DataFlow.Core.Common;
using DataFlow.Core.Data;
using DataFlow.Core.Features.Dtos;
using DataFlow.Core.Models;
using DataFlow.Core.Repositories;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DataFlow.Core.Features.Commands.Handlers
{
    public class ImportarInformacionCommandHandler : ICommandHandler<ImportarInformacionCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ImportarInformacionCommandHandler> _logger;

        public ImportarInformacionCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<ImportarInformacionCommandHandler> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<bool>> HandleAsync(ImportarInformacionCommand command, CancellationToken cancellationToken = default)
        {
            if (command is null) return Result<bool>.Failure("El comando es nulo.");

            
            var filePath = Path.GetFullPath(command.FilePath);
            if (!File.Exists(filePath)) return Result<bool>.Failure($"El archivo no existe: {filePath}");

            try
            {

                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                var json = await File.ReadAllTextAsync(filePath, cancellationToken);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var importRoot = JsonSerializer.Deserialize<ImportRootDto>(json, options);

                if (importRoot?.Templates == null || !importRoot.Templates.Any())
                    return Result<bool>.Failure("Archivo vacío o formato no válido.");

                var existingTemplates = await _unitOfWork.ConfigTemplates.GetAllAsync(cancellationToken);
                var now = DateTime.UtcNow;

                foreach (var tplDto in importRoot.Templates)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    ConfigTemplate? existing = null;
                    if (tplDto.Id > 0)
                    {
                        var byId = await _unitOfWork.ConfigTemplates.GetByIdAsync(tplDto.Id, cancellationToken);
                        if (byId != null) 
                            existing = await _unitOfWork.ConfigTemplates.GetWithDetailAsync(byId.Id, cancellationToken);
                    }

                    if (existing == null && !string.IsNullOrWhiteSpace(tplDto.Description))
                    {
                        var match = existingTemplates.FirstOrDefault(t => string.Equals(t.Description?.Trim(), tplDto.Description?.Trim(), StringComparison.OrdinalIgnoreCase));
                        if (match != null) existing = await _unitOfWork.ConfigTemplates.GetWithDetailAsync(match.Id, cancellationToken);
                    }

                    
                    if (existing != null)
                    {
                        if (command.OverwriteExisting)
                        {
                            _logger.LogInformation("Eliminando existente para sobreescribir: {Desc}", existing.Description);
                            await _unitOfWork.ConfigTemplates.DeleteAsync(existing, cancellationToken);
                            
                        }
                        else
                        {
                            _logger.LogInformation("Omitiendo plantilla existente: {Desc}", tplDto.Description);
                            continue;
                        }
                    }

                    
                    var newTemplate = new ConfigTemplate
                    {
                        Description = tplDto.Description?.Trim(),
                        CreatedAt = tplDto.CreatedAt ?? now,
                        UpdatedAt = tplDto.UpdatedAt ?? now,
                        ConfigColumns = tplDto.ConfigColumns != null ?  tplDto.ConfigColumns.Select(colDto => new ConfigColumn
                        {
                            IndexColumn = colDto.IndexColumn,
                            Name = colDto.Name?.Trim(),
                            NameDisplay = colDto.NameDisplay?.Trim(),
                            Description = colDto.Description?.Trim(),
                            DataTypeId = colDto.DataTypeId,
                            ColumnTypeId = colDto.ColumnTypeId,
                            DefaultValue = colDto.DefaultValue,
                            CreatedAt = colDto.CreatedAt ?? now,
                            UpdatedAt = colDto.UpdatedAt ?? now,
                            Ranges = colDto.Ranges != null ? colDto.Ranges.Select(rDto => new ColumnRange
                            {
                                RFrom = rDto.RFrom,
                                RTo = rDto.RTo,
                                DefaultValue = rDto.DefaultValue,
                                CreatedAt = rDto.CreatedAt ?? now,
                                UpdatedAt = rDto.UpdatedAt ?? now
                            }).ToList() : new List<ColumnRange>()
                        }).ToList() : new List<ConfigColumn>()
                    };

                    await _unitOfWork.ConfigTemplates.AddAsync(newTemplate, cancellationToken);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                _logger.LogInformation("Importación completada con éxito.");
                return Result<bool>.Success(true);
            }
            catch (OperationCanceledException)
            {
                await _unitOfWork.RollbackAsync(CancellationToken.None);
                return Result<bool>.Failure("Importación cancelada.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync(CancellationToken.None);
                _logger.LogError(ex, "Error en importación. Se realizó Rollback.");
                return Result<bool>.Failure($"Error crítico: {ex.Message}");
            }
        }
    }
}