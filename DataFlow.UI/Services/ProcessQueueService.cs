using DocumentFormat.OpenXml.Office2010.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataFlow.UI.Services
{
    public class ProcessQueueService : IProcessQueueService
    {
        private readonly Queue<ProcessQueueItem> _queue = new();
        private int _itemIdCounter = 0;


        public event EventHandler? QueueChanged;
        public event EventHandler<ProcessQueueItemEventArgs>? ItemProcessed;
        public event EventHandler<ProcessQueueItemEventArgs>? ItemFailed;

        public IReadOnlyList<ProcessQueueItem> GetQueueItems()
        {
            return _queue.ToList().AsReadOnly();
        }

        public void EnqueueFile(string filePath)
        {
            if(string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("La ruta del archivo no debe estar vacia.", nameof(filePath));

            var item = new ProcessQueueItem
            {
                Id= ++_itemIdCounter,
                FilePath = filePath,
                FileName = System.IO.Path.GetFileName(filePath),
                AddedAt = DateTime.Now,
                Status = ProcessQueueItemStatus.Pending
            };
            _queue.Enqueue(item);
            OnQueueChanged();

        }
        public void EnqueueFiles(IEnumerable<string> filePaths)
        {
            foreach(var filePath in filePaths)
            {
                EnqueueFile(filePath);
            }
        }
        public ProcessQueueItem? PeekNextFile()
        {
            return _queue.Count > 0 ? _queue.Peek() : null;
        }
        public ProcessQueueItem? DequeueFile()
        {
            if(_queue.Count <= 0)
                return null;

            var item = _queue.Dequeue();
            OnQueueChanged();
            return item;

        }
        public void RemoveAt(int index)
        {
            if(index < 0 || index >= _queue.Count)
                throw new ArgumentOutOfRangeException(nameof(index), "El índice está fuera de rango.");

            var items = _queue.ToList();
            items.RemoveAt(index);
            _queue.Clear();
            foreach (var item in items)
            {
                _queue.Enqueue(item);
            }
            OnQueueChanged();
        }

        public void Clear()
        {
            _queue.Clear();
            OnQueueChanged();
        }
        
        public int GetQueueCount()
        {
            return _queue.Count;
        }

        public void NotifyItemProcessed(ProcessQueueItem item)
        {
            item.Status = ProcessQueueItemStatus.Completed;
            item.CompletedAt = DateTime.Now;
            ItemProcessed?.Invoke(this, new ProcessQueueItemEventArgs(item));
        }

        public void NotifyItemFailed(ProcessQueueItem item, string errorMessage)
        {
            item.Status = ProcessQueueItemStatus.Failed;
            item.CompletedAt = DateTime.Now;
            ItemFailed?.Invoke(this, new ProcessQueueItemEventArgs(item));
        }

        protected virtual void OnQueueChanged()
        {
            QueueChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
