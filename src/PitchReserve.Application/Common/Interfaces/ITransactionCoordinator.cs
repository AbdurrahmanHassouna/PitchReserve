namespace PitchReserve.Application.Common.Interfaces;

/// <summary>Runs database work on the scoped context while holding transaction-owned resource locks.</summary>
public interface ITransactionCoordinator
{
    Task<T> ExecuteAsync<T>(IReadOnlyCollection<string> resourceNames,
        Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default);
}
