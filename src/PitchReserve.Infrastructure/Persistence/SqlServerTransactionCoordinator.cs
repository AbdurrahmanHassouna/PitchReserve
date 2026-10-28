using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using PitchReserve.Application.Common.Interfaces;
using PitchReserve.Domain.Exceptions;

namespace PitchReserve.Infrastructure.Persistence;

public sealed class SqlServerTransactionCoordinator : ITransactionCoordinator
{
    private readonly PitchReserveDbContext _context;
    private const int LockTimeoutMilliseconds = 5000;
    private readonly ILogger<SqlServerTransactionCoordinator> _logger;

    public SqlServerTransactionCoordinator(PitchReserveDbContext context,
        ILogger<SqlServerTransactionCoordinator> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<T> ExecuteAsync<T>(IReadOnlyCollection<string> resourceNames,
        Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        if (_context.Database.CurrentTransaction is not null)
            throw new InvalidOperationException("Nested coordinated transactions are not supported.");


        var resources = resourceNames.Distinct(StringComparer.Ordinal).OrderBy(name => name, StringComparer.Ordinal);
        if (resources.Count() == 0 || resources.Any(name => string.IsNullOrWhiteSpace(name) || name.Length > 255))
            throw new ArgumentException("At least one valid database resource name is required.", nameof(resourceNames));

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var resource in resources)
            {
                await using var command = _context.Database.GetDbConnection().CreateCommand();
                command.Transaction = transaction.GetDbTransaction();
                command.CommandText = "EXEC @result = sys.sp_getapplock @Resource = @resource, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = @timeout;";
                command.CommandTimeout = 10;
                command.Parameters.Add(new SqlParameter("@resource", SqlDbType.NVarChar, 255) { Value = resource });
                command.Parameters.Add(new SqlParameter("@timeout", SqlDbType.Int) { Value = LockTimeoutMilliseconds });
                var result = new SqlParameter("@result", SqlDbType.Int) { Direction = ParameterDirection.Output };
                command.Parameters.Add(result);
                await command.ExecuteNonQueryAsync(cancellationToken);
                var code = (int)result.Value;
                if (code == -2) cancellationToken.ThrowIfCancellationRequested();
                if (code is -1 or -2 or -3)
                    throw new ConflictException("This resource is being changed. Please try again.");
                if (code < 0) throw new InvalidOperationException("Database resource lock acquisition failed.");
            }
            var value = await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return value;
        }
        catch (DbUpdateConcurrencyException)
        {
            await RollbackSafelyAsync(transaction);
            throw new ConflictException("The resource changed during this request. Refresh and try again.");
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            await RollbackSafelyAsync(transaction);
            throw new ConflictException("This record already exists. Refresh and try again.");
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 1205 or 1222 })
        {
            await RollbackSafelyAsync(transaction);
            throw new ConflictException("This resource is busy. Please try again.");
        }
        catch (SqlException exception) when (exception.Number is 1205 or 1222)
        {
            await RollbackSafelyAsync(transaction);
            throw new ConflictException("This resource is busy. Please try again.");
        }
        catch
        {
            await RollbackSafelyAsync(transaction);
            throw;
        }
    }

    private async Task RollbackSafelyAsync(IDbContextTransaction transaction)
    {
        try
        {
            await transaction.RollbackAsync(CancellationToken.None);
        }
        catch (Exception exception)
        {
            // SQL Server may have already rolled back a deadlock victim. Preserve the original error.
            _logger.LogWarning("Database rollback could not complete with {ExceptionType}.", exception.GetType().Name);
        }
    }
}
