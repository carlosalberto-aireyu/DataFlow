using System.Windows.Input;

namespace DataFlow.UI.Commands
{
    public interface IAsyncCommand : ICommand
    {
        Task ExecuteAsync(object? parameter);
    }

     public interface IAsyncCommand<in T> : ICommand
    {
        Task ExecuteAsync(T? parameter);
    }
}