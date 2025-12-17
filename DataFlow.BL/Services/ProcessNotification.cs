using DataFlow.BL.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataFlow.BL.Services
{
    public class ProcessNotification
    {
        public ProcessNotificationLevel Level { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string? Details { get; set; }
    }
}
