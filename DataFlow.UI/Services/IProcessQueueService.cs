using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataFlow.UI.Services
{
    /// <summary>
    /// Servicio para gestionar una cola de archivos a procesar.
    /// </summary>
    public interface IProcessQueueService
    {
        /// <summary>
        /// Obtiene la lista de archivos en la cola.
        /// </summary>
        IReadOnlyList<ProcessQueueItem> GetQueueItems();

        /// <summary>
        /// Añade un archivo a la cola.
        /// </summary>
        void EnqueueFile(string filePath);

        /// <summary>
        /// Añade múltiples archivos a la cola.
        /// </summary>
        void EnqueueFiles(IEnumerable<string> filePaths);

        /// <summary>
        /// Obtiene el siguiente archivo de la cola sin eliminarlo.
        /// </summary>
        ProcessQueueItem? PeekNextFile();

        /// <summary>
        /// Obtiene y elimina el siguiente archivo de la cola.
        /// </summary>
        ProcessQueueItem? DequeueFile();

        /// <summary>
        /// Elimina un archivo específico de la cola por índice.
        /// </summary>
        void RemoveAt(int index);

        /// <summary>
        /// Vacía toda la cola.
        /// </summary>
        void Clear();

        /// <summary>
        /// Obtiene la cantidad de archivos en la cola.
        /// </summary>
        int GetQueueCount();

        /// <summary>
        /// Notifica que un elemento fue procesado correctamente.
        /// </summary>
        void NotifyItemProcessed(ProcessQueueItem item);

        /// <summary>
        /// Notifica que un elemento falló al procesarse.
        /// </summary>
        void NotifyItemFailed(ProcessQueueItem item, string errorMessage);

        /// <summary>
        /// Se dispara cuando la cola cambia.
        /// </summary>
        event EventHandler? QueueChanged;

        /// <summary>
        /// Se dispara cuando se procesa un archivo correctamente.
        /// </summary>
        event EventHandler<ProcessQueueItemEventArgs>? ItemProcessed;

        /// <summary>
        /// Se dispara cuando falla el procesamiento de un archivo.
        /// </summary>
        event EventHandler<ProcessQueueItemEventArgs>? ItemFailed;
    }
}
