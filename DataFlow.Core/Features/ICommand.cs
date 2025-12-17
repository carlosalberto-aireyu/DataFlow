namespace DataFlow.Core.Features
{
    //No retorna nada
    public interface ICommand
    {
    }
    //retorna un TResult
    public interface ICommand<out TResult>
    {

    }

    //Comando in rertorno
    public interface ICommandHandler<in TCommand> where TCommand : ICommand
    {
        Task HandleAsync(TCommand command, CancellationToken cancellationToken = default);
    }

    public interface ICommandHandler<in TCommand, TResult> where TCommand : ICommand<TResult>
    {
        Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
    }

}
