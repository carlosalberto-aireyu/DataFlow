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
        /// Obtiene solo los archivos pendientes de procesar.
        /// </summary>
        IReadOnlyList<ProcessQueueItem> GetPendingItems();

        /// <summary>
        /// Añade un archivo a la cola.
        /// Retorna true si el archivo fue agregado, false si ya existía.
        /// </summary>
        bool EnqueueFile(string filePath);

        /// <summary>
        /// Añade múltiples archivos a la cola y retorna la cantidad agregada.
        /// </summary>
        int EnqueueFiles(IEnumerable<string> filePaths);

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
        /// Vacía toda la cola incluyendo historial.
        /// </summary>
        void Clear();

        /// <summary>
        /// Limpia solo el historial (completados y errores).
        /// </summary>
        void ClearHistory();

        /// <summary>
        /// Obtiene la cantidad de archivos pendientes en la cola.
        /// </summary>
        int GetQueueCount();

        /// <summary>
        /// Obtiene el token de cancelación para interrumpir el procesamiento.
        /// </summary>
        CancellationToken GetCancellationToken();

        /// <summary>
        /// Solicita la cancelación del procesamiento.
        /// </summary>
        void RequestCancellation();

        /// <summary>
        /// Resetea el token de cancelación para un nuevo procesamiento.
        /// </summary>
        void ResetCancellationToken();

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