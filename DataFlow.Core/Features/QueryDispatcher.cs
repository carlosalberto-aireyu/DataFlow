using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DataFlow.Core.Features
{
    public interface IQueryDispatcher
    {
        Task<TResult> DispatchAsync<TQuery, TResult>(TQuery query, CancellationToken cancellationToken = default) where TQuery : IQuery<TResult>;
    }

    public class QueryDispatcher : IQueryDispatcher
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<QueryDispatcher>? _logger;

        public QueryDispatcher(
            IServiceScopeFactory scopeFactory,
            ILogger<QueryDispatcher>? logger = null
            )
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _logger = logger;
        }

        public async Task<TResult> DispatchAsync<TQuery, TResult>(TQuery query, CancellationToken cancellationToken = default) where TQuery : IQuery<TResult>
        {
            using var scope = _scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<IQueryHandler<TQuery, TResult>>();

            return await handler.HandleAsync(query, cancellationToken);
        }
    }
    //Despachador de comandos
    public interface ICommandDispatcher
    {
        Task DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default) where TCommand : ICommand;
        Task<TResult> DispatchAsync<TCommand, TResult>(TCommand command, CancellationToken cancellationToken = default) where TCommand : ICommand<TResult>;
    }
    public class CommandDispatcher : ICommandDispatcher
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<CommandDispatcher>? _logger;

        public CommandDispatcher(
            IServiceScopeFactory scopeFactory,
            ILogger<CommandDispatcher>? logger = null
            )
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _logger = logger;
        }

        public async Task DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default) where TCommand : ICommand
        {
            using var scope = _scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<TCommand>>();
            cancellationToken.ThrowIfCancellationRequested();
            await handler.HandleAsync(command, cancellationToken);
        }

        public async Task<TResult> DispatchAsync<TCommand, TResult>(TCommand command, CancellationToken cancellationToken = default) where TCommand : ICommand<TResult>
        {
            using var scope = _scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<TCommand, TResult>>();
            cancellationToken.ThrowIfCancellationRequested();
            return await handler.HandleAsync(command, cancellationToken);
        }
    }
}
