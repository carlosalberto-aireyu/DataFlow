using System;
using System.Collections.Generic;
using System.Linq;

namespace DataFlow.UI.Services
{
    public class ProcessQueueService : IProcessQueueService
    {
        private readonly Queue<ProcessQueueItem> _queue = new();
        private int _itemIdCounter = 0;
        private CancellationTokenSource _cancellationTokenSource = new();

        public event EventHandler? QueueChanged;
        public event EventHandler<ProcessQueueItemEventArgs>? ItemProcessed;
        public event EventHandler<ProcessQueueItemEventArgs>? ItemFailed;

        public IReadOnlyList<ProcessQueueItem> GetQueueItems()
        {
            return _queue.ToList().AsReadOnly();
        }

        public IReadOnlyList<ProcessQueueItem> GetPendingItems()
        {
            return _queue
                .Where(item => item.Status == ProcessQueueItemStatus.Pending)
                .ToList()
                .AsReadOnly();
        }

        public bool EnqueueFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("La ruta del archivo no debe estar vacía.", nameof(filePath));

            // Normalizar la ruta para comparación
            string normalizedPath = System.IO.Path.GetFullPath(filePath).ToLowerInvariant();

            // Verificar si el archivo ya existe en la cola (pendiente)
            if (_queue.Any(item =>
                System.IO.Path.GetFullPath(item.FilePath).ToLowerInvariant() == normalizedPath &&
                item.Status == ProcessQueueItemStatus.Pending))
            {
                System.Diagnostics.Debug.WriteLine($"[EnqueueFile] El archivo ya existe en la cola: {filePath}");
                return false;
            }

            var item = new ProcessQueueItem
            {
                Id = ++_itemIdCounter,
                FilePath = filePath,
                FileName = System.IO.Path.GetFileName(filePath),
                AddedAt = DateTime.Now,
                Status = ProcessQueueItemStatus.Pending
            };
            _queue.Enqueue(item);
            OnQueueChanged();
            return true;
        }

        public int EnqueueFiles(IEnumerable<string> filePaths)
        {
            int addedCount = 0;
            foreach (var filePath in filePaths)
            {
                if(EnqueueFile(filePath))
                    addedCount++;
            }
            return addedCount;
        }

        public ProcessQueueItem? PeekNextFile()
        {
            return _queue
                .FirstOrDefault(item => item.Status == ProcessQueueItemStatus.Pending);
        }

        public ProcessQueueItem? DequeueFile()
        {
            var item = _queue
                .FirstOrDefault(item => item.Status == ProcessQueueItemStatus.Pending);

            if (item == null)
                return null;

            item.Status = ProcessQueueItemStatus.Processing;
            OnQueueChanged();
            return item;
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= _queue.Count)
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

        public void ClearHistory()
        {
            var itemsToKeep = _queue
                .Where(item => item.Status == ProcessQueueItemStatus.Pending)
                .ToList();

            _queue.Clear();
            foreach (var item in itemsToKeep)
            {
                _queue.Enqueue(item);
            }
            OnQueueChanged();
        }

        public int GetQueueCount()
        {
            return _queue.Count(item => item.Status == ProcessQueueItemStatus.Pending);
        }

        public CancellationToken GetCancellationToken()
        {
            return _cancellationTokenSource.Token;
        }

        public void RequestCancellation()
        {
            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
            }
        }

        public void ResetCancellationToken()
        {
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public void NotifyItemProcessed(ProcessQueueItem item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            item.Status = ProcessQueueItemStatus.Completed;
            item.CompletedAt = DateTime.Now;
            ItemProcessed?.Invoke(this, new ProcessQueueItemEventArgs(item));
        }

        public void NotifyItemFailed(ProcessQueueItem item, string errorMessage)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            item.Status = ProcessQueueItemStatus.Failed;
            item.CompletedAt = DateTime.Now;
            item.ErrorMessage = errorMessage;
            ItemFailed?.Invoke(this, new ProcessQueueItemEventArgs(item));
        }

        protected virtual void OnQueueChanged()
        {
            QueueChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}