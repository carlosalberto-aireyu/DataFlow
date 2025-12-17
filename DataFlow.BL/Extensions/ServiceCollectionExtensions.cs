using DataFlow.BL.Contracts;
using DataFlow.BL.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataFlow.BL.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDataFlowBLServices(this IServiceCollection services)
        {
            services.AddSingleton<IExcelProcessingService, ExcelProcessingService>();
            return services;
        }
    }
}
