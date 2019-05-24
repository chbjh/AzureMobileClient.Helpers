using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.MobileServices;

namespace AzureMobileClient.Helpers
{
    /// <summary>
    /// ICloudSyncTable
    /// </summary>
    public interface ICloudSyncTable<T> : ICloudTable<T> where T : IEntityData
    {
        /// <summary>
        /// Purges table asynchronously
        /// </summary>
        Task PurgeAsync();
        Task PurgeAsync(bool force);
        Task PurgeAsync(string query);
        Task PurgeAsync(string queryId, string query, bool force, CancellationToken cancellationToken);
        Task PurgeAsync<U>(IMobileServiceTableQuery<U> query);
        Task PurgeAsync<U>(string queryId, IMobileServiceTableQuery<U> query, CancellationToken cancellationToken);
        Task PurgeAsync<U>(string queryId, IMobileServiceTableQuery<U> query, bool force, CancellationToken cancellationToken);

        /// <summary>
        /// Pulls the latest data from the server and ensures proper syncing
        /// </summary>
        Task PullAsync();

        /// <summary>
        /// Gets the count of any pending operations
        /// </summary>
        long PendingOperations { get; }

        /// <summary>
        /// Synchronize the table with the cloud store
        /// </summary>
        Task SyncAsync(CancellationToken cancellationToken = default(CancellationToken));
    }
}