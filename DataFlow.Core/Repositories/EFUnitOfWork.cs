using DataFlow.Core.Data;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataFlow.Core.Repositories
{
    public class EFUnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private IDbContextTransaction? _currentTransaction;

        public IConfigTemplateRepository ConfigTemplates { get; }
        public IConfigColumnRepository ConfigColumns { get; }
        public IColumnRangeRepository ColumnRanges { get; }

        public EFUnitOfWork(AppDbContext context,
            IConfigTemplateRepository configTemplateRepository,
            IConfigColumnRepository configColumnRepository,
            IColumnRangeRepository columnRangeRepository)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            ConfigTemplates = configTemplateRepository ?? throw new ArgumentNullException(nameof(configTemplateRepository));
            ConfigColumns = configColumnRepository ?? throw new ArgumentNullException(nameof(configColumnRepository));
            ColumnRanges = columnRangeRepository ?? throw new ArgumentNullException(nameof(columnRangeRepository));
        }

        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            if (_currentTransaction == null) return;
            await _currentTransaction.CommitAsync(cancellationToken);
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;

        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore();
            GC.SuppressFinalize(this);
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                _currentTransaction?.Dispose();
                _context.Dispose();
            }
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            if (_currentTransaction == null) return;
            await  _currentTransaction.RollbackAsync(cancellationToken);
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
        protected virtual async ValueTask DisposeAsyncCore()
        {
            if (_currentTransaction != null) 
                await _currentTransaction.DisposeAsync();
            await _context.DisposeAsync();
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => await _context.SaveChangesAsync(cancellationToken);
    }
}
