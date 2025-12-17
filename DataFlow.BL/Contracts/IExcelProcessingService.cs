using DataFlow.BL.Constants;
using DataFlow.BL.Services;
using DataFlow.Core.Common;
using DataFlow.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataFlow.BL.Contracts
{
    public interface IExcelProcessingService
    {
        event EventHandler<ProcessNotification>? NotificationReceived;
        Task<Result<string>> ProcessExcelFileAsync(
            string inputFilePath,
            string outputFilePath,
            ConfigTemplate templateConfig);
    }
}
