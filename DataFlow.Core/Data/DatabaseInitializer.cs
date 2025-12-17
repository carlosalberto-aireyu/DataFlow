using DataFlow.Core.Constants;
using DataFlow.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DataFlow.Core.Extensions;
using System;
using System.IO;
namespace DataFlow.Core.Data
{
    public class DatabaseInitializer : IDatabaseInitializer
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DatabaseInitializer> _logger;
        public DatabaseInitializer(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            ILogger<DatabaseInitializer> logger)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var conn = _configuration.GetConnectionString("DefaultConnection")
                ?? "Data Source=DataFlow.db;Cache=Shared";

            string dataSource = conn.Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Trim())
                .FirstOrDefault(part => part.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
                ?.Split('=', 2)[1].Trim().Trim('"') ?? "DataFlow.db";
            var dbPath = Path.IsPathRooted(dataSource) ? dataSource : Path.Combine(AppContext.BaseDirectory, dataSource);

            try
            {
                if (!File.Exists(dbPath))
                {
                    _logger?.LogInformation("Base de datos no encontrada en '{DbPath}'. Creando base de datos con EnsureCreated().", dbPath);
                    db.Database.EnsureCreated();
                    await SeedInitialData(db, cancellationToken);
                }
                else
                {
                    _logger?.LogInformation("Base de datos encontrada en '{DbPath}'. No se aplicarán migraciones (comportamiento solicitado).", dbPath);
                }
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                _logger?.LogError(ex, "Error al inicializar la base de datos (path={DbPath})", dbPath);
                throw;
            }

        }
        private async Task SeedInitialData(AppDbContext db, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!await db.ColumnTypeLookups.AnyAsync(cancellationToken))
            {
                _logger?.LogInformation("Sembrando ColumnTypeLookup...");
                var tipos = new List<ColumnTypeLookup>
                {
                        new ColumnTypeLookup { Id = 1, Code = "Constante", Description = "Valor constante que se repite en todas las filas" },
                        new ColumnTypeLookup { Id = 2, Code = "Valor", Description = "Valor que se lee por fila o rango" },
                        new ColumnTypeLookup { Id = 3, Code = "Dimension", Description = "Etiqueta o categoría asociada a un rango" }
                };
                db.ColumnTypeLookups.AddRange(tipos);
                await db.SaveChangesAsync(cancellationToken);
                _logger?.LogInformation("ColumnTypeLookup sembrado correctamente.");
            }
            if (!await db.DataTypeLookups.AnyAsync(cancellationToken))
            {
                _logger?.LogInformation("Sembrando DataTypeLookup...");

                var tipos = new List<DataTypeLookup>
                {
                    new DataTypeLookup { Id = 1, Code = "Texto", Description = "Valores literales" },
                    new DataTypeLookup { Id = 2, Code = "Numerico", Description = "Valores numéricos" },
                    new DataTypeLookup { Id = 3, Code = "Fecha", Description = "Valores de tipo fecha" }
                };

                db.DataTypeLookups.AddRange(tipos);
                await db.SaveChangesAsync(cancellationToken);
                _logger?.LogInformation("DataTypeLookup sembrado correctamente.");
            }

            if (!await db.Parametros.AnyAsync(cancellationToken))
            {
                _logger?.LogInformation("Sembrando parámetros iniciales en la base de datos.");

                var parametros = new List<Parametro>
                {
                    new Parametro
                    {
                        ParametroKey = ParametroKey.WorkDirectory.ToString(),
                        ParametroValue = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), ""),
                        Name = ParametroKey.WorkDirectory.GetDisplayName(),
                        Description = ParametroKey.WorkDirectory.GetDisplayDescription(),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new Parametro
                    {
                        ParametroKey = ParametroKey.DataToJsonExporter.ToString(),
                        ParametroValue = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), ""),
                        Name = ParametroKey.DataToJsonExporter.GetDisplayName(),
                        Description = ParametroKey.DataToJsonExporter.GetDisplayDescription(),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },

                };

                db.Parametros.AddRange(parametros);
                await db.SaveChangesAsync(cancellationToken);
                _logger?.LogInformation("{Count} parámetros iniciales sembrados exitosamente.", parametros.Count);
            }
            else
            {
                _logger?.LogInformation("La tabla de Parámetros ya contenía datos. No se sembraron parámetros iniciales.");
            }
        }
    }
}
