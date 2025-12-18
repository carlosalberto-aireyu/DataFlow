using DataFlow.Core.Common;
using DataFlow.Core.Constants;
using DataFlow.Core.Data;
using DataFlow.Core.Features;
using DataFlow.Core.Features.Commands;
using DataFlow.Core.Features.Commands.Handlers;
using DataFlow.Core.Features.Queries;
using DataFlow.Core.Features.Queries.Handlers;
using DataFlow.Core.Models;
using DataFlow.Core.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DataFlow.Core.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDataFlowCoreServices(this IServiceCollection services, IConfiguration configuration)
        {
         
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? "Data Source=DataFlow.db;Cache=Shared";

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(connectionString), ServiceLifetime.Scoped);

         
            services.AddScoped<IUnitOfWork, EFUnitOfWork>();


            services.AddScoped<ILookupRepository, LookupRepository>();
            services.AddSingleton<LookupIds>(provider =>
            {
                var repo = provider.GetRequiredService<ILookupRepository>();
                var loader = new LookupIdProvider(repo);
                return loader.LoadAsync().GetAwaiter().GetResult();
            });

            services.AddScoped<IConfigTemplateRepository, EFConfigTemplateRepository>();
            services.AddScoped<IConfigColumnRepository, EFConfigColumnRepository>();
            services.AddScoped<IColumnRangeRepository, EFColumnRangeRepository>();
            services.AddScoped<IParametroRepository, EFParametroRepository>();
            services.AddScoped<IHistProcessRepository, EFHistProcessRepository>();

            services.AddScoped<ICommandDispatcher, CommandDispatcher>();
            services.AddScoped<IQueryDispatcher, QueryDispatcher>();
            services.AddScoped<IServiceScopeFactory>(sp => sp.GetRequiredService<IServiceProvider>() as IServiceScopeFactory ??
                throw new InvalidOperationException("IServiceScopeFactory no disponible"));

         
            RegisterQueryHandlers(services);
            RegisterCommandHandlers(services);

         
            services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();

            return services;
        }

        private static void RegisterQueryHandlers(IServiceCollection services)
        {
            services.AddScoped<IQueryHandler<GetAllConfigTemplatesQuery, Result<IReadOnlyList<ConfigTemplate>>>, GetAllConfigTemplatesQueryHandler>();
            services.AddScoped<IQueryHandler<GetConfigTemplateByIdQuery, Result<ConfigTemplate>>, GetConfigTemplateByIdQueryHandler>();
            services.AddScoped<IQueryHandler<GetConfigColumnByIdQuery, Result<ConfigColumn>>, GetConfigColumnByIdQueryHandler>();
            services.AddScoped<IQueryHandler<GetConfigColumnsByTemplateIdQuery, Result<IReadOnlyList<ConfigColumn>>>, GetConfigColumnsByTemplateIdQueryHandler>();

            services.AddScoped<IQueryHandler<GetAllParametrosQuery, Result<IReadOnlyList<Parametro>>>, GetAllParametrosQueryHandler>();
            services.AddScoped<IQueryHandler<GetParametroByKeyQuery, Result<Parametro?>>, GetParametroByKeyQueryHandler>();
            
            services.AddScoped<IQueryHandler<ColumnTypeExistsQuery, Result<bool>>, ColumnTypeExistsQueryHandler>();
            services.AddScoped<IQueryHandler<DataTypeExistsQuery, Result<bool>>, DataTypeExistsQueryHandler>();
            services.AddScoped<IQueryHandler<GetColumnTypesQuery, Result<IReadOnlyList<ColumnTypeLookup>>>, GetColumnTypesQueryHandler>();
            services.AddScoped<IQueryHandler<GetDataTypesQuery, Result<IReadOnlyList<DataTypeLookup>>>, GetDataTypesQueryHandler>();

            services.AddScoped<IQueryHandler<GetHistProcessByIdQuery, Result<HistProcess>>, GetHistProcessByIdQueryHandler>();
            services.AddScoped<IQueryHandler<GetHistProcessByConfigTemplateIdQuery, Result<IReadOnlyList<HistProcess>>>, GetHistProcessByConfigTemplateIdQueryHandler>();

        }

        private static void RegisterCommandHandlers(IServiceCollection services)
        {
            services.AddScoped<ICommandHandler<CreateConfigTemplateCommand, Result<ConfigTemplate>>, CreateConfigTemplateCommandHandler>();
            services.AddScoped<ICommandHandler<UpdateConfigTemplateCommand, Result<ConfigTemplate>>, UpdateConfigTemplateCommandHandler>();
            services.AddScoped<ICommandHandler<DeleteConfigTemplateCommand, Result<bool>>, DeleteConfigTemplateCommandHandler>();

            services.AddScoped<ICommandHandler<CreateConfigColumnCommand, Result<ConfigColumn>>, CreateConfigColumnCommandHandler>();
            services.AddScoped<ICommandHandler<UpdateConfigColumnCommand, Result<ConfigColumn>>, UpdateConfigColumnCommandHandler>();
            services.AddScoped<ICommandHandler<DeleteConfigColumnCommand, Result<bool>>, DeleteConfigColumnCommandHandler>();

            services.AddScoped<ICommandHandler<CreateColumnRangeCommand, Result<ColumnRange>>, CreateColumnRangeCommandHandler>();
            services.AddScoped<ICommandHandler<UpdateColumnRangeCommand, Result<ColumnRange>>, UpdateColumnRangeCommandHandler>();
            services.AddScoped<ICommandHandler<DeleteColumnRangeCommand, Result<bool>>, DeleteColumnRangeCommandHandler>();

            services.AddScoped<ICommandHandler<CreateParametroCommand, Result<Parametro>>, CreateParametroCommandHandler>();
            services.AddScoped<ICommandHandler<UpdateParametroCommand, Result<Parametro>>, UpdateParametroCommandHandler>();
            services.AddScoped<ICommandHandler<DeleteParametroCommand, Result<bool>>, DeleteParametroCommandHandler>();

            services.AddScoped<ICommandHandler<CreateHistProcessCommand, Result<HistProcess>>, CreateHistProcessCommandHandler>();

            services.AddScoped<ICommandHandler<ExportarInformacionCommand, Result<bool>>, ExportarInformacionCommandHandler>();
            services.AddScoped<ICommandHandler<ImportarInformacionCommand, Result<bool>>, ImportarInformacionCommandHandler>();

        }
    }
}
