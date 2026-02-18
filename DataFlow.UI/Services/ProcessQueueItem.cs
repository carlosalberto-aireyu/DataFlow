using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataFlow.UI.Services
{
    /// <summary>
    /// Estados posibles de un elemento en la cola.
    /// </summary>
    public enum ProcessQueueItemStatus
    {
        Pending,
        Processing,
        Completed,
        Failed
    }
    /// <summary>
    /// Represebta un elemento en la cola de procesamiento.
    /// </summary>
    public class ProcessQueueItem
    {
        public int Id { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public DateTime AddedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public ProcessQueueItemStatus Status { get; set; } = ProcessQueueItemStatus.Pending;
        public string? ErrorMessage { get; set; }
    }

    public class  ProcessQueueItemEventArgs : EventArgs
    {
        public ProcessQueueItem Item { get; }
        public ProcessQueueItemEventArgs(ProcessQueueItem item)
        {
            Item = item ?? throw new ArgumentNullException(nameof(item));
        }

    }
}
