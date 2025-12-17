namespace DataFlow.Core.Data
{
    public interface IDatabaseInitializer
    {
        Task InitializeAsync(CancellationToken cancellationToken = default);
    }
}
