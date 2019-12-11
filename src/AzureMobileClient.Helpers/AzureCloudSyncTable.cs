using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.MobileServices;
using Microsoft.WindowsAzure.MobileServices.Sync;
using Newtonsoft.Json.Linq;
using Plugin.Connectivity;
using Plugin.Connectivity.Abstractions;

namespace AzureMobileClient.Helpers
{
    /// <summary>
    /// AzureCloudSyncTable
    /// </summary>
    public class AzureCloudSyncTable<T> : ICloudSyncTable<T> where T : EntityData
    {
        private IMobileServiceClient _client { get; }
        public IMobileServiceSyncTable<T> table { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="AzureCloudSyncTable(IMobileServiceClient)" />
        /// </summary>
        public AzureCloudSyncTable(IMobileServiceClient client)
        {
            _client = client;
            table = _client.GetSyncTable<T>();
            //CrossConnectivity.Current.ConnectivityChanged += OnConnectivityChanged;
        }

        /// <summary>
        /// Gets the pending operation count
        /// </summary>
        public long PendingOperations { get { return _client.SyncContext.PendingOperations; } }

        #region ICloudSyncTable interface

        public Task PurgeAsync()
        {
            return table.PurgeAsync();
        }

        public Task PurgeAsync(bool force)
        {
            return table.PurgeAsync(force);
        }

        public Task PurgeAsync(string query)
        {
            return table.PurgeAsync(query);
        }

        public Task PurgeAsync(string queryId, string query, bool force, CancellationToken cancellationToken)
        {
            return table.PurgeAsync(queryId, query, force, cancellationToken);
        }

        public Task PurgeAsync<U>(IMobileServiceTableQuery<U> query)
        {
            return table.PurgeAsync<T, U>(query);
        }

        public Task PurgeAsync<U>(string queryId, IMobileServiceTableQuery<U> query, CancellationToken cancellationToken)
        {
            return table.PurgeAsync<U>(queryId, query, cancellationToken);
        }

        public Task PurgeAsync<U>(string queryId, IMobileServiceTableQuery<U> query, bool force, CancellationToken cancellationToken)
        {
            return table.PurgeAsync<U>(queryId, query, force, cancellationToken);
        }


        public Task PurgeItemAsync(T item)
        {
            return this.PurgeAsync(this.table.Where(x => x.Id == item.Id));
            //string queryName = $"purge_item_{typeof(T).Name}";
            //var query = this.table.CreateQuery().Where(x => x.Id == item.Id);
            //return this.PurgeAsync<T>(queryName, query, true, CancellationToken.None);
        }
        

        /// <inheritDoc />
        public virtual async Task PullAsync()
        {
            try
            {
                string queryName = $"incsync_{typeof(T).Name}";
                await table.PullAsync(queryName, table.CreateQuery());
            }
            catch (MobileServicePushFailedException ex)
            {
                Debug.WriteLine("PullAsync(): MobileServicePushFailedException hit. Message = " + ex.Message);
                if (ex.PushResult != null)
                {
                    foreach (var error in ex.PushResult.Errors)
                    {
                        await ResolveConflictAsync(error);
                    }
                }
            }

        }

        /// <inheritDoc />
        public virtual async Task<T> CreateItemAsync(T item)
        {
            await table.InsertAsync(item);
            //if(_client.SyncContext.PendingOperations > 0 && CrossConnectivity.Current.IsConnected)
            //{
            //    await _client.SyncContext.PushAsync();
            //}
            return item;
        }

        /// <inheritDoc />
        public virtual async Task DeleteItemAsync(T item)
            => await table.DeleteAsync(item);

        /// <inheritDoc />
        public virtual async Task<ICollection<T>> ReadAllItemsAsync()
        {
            var list = await table.ToListAsync();
            foreach (T item in list)
                item.IsModified = false;
            return list;
        }

        /// <inheritDoc />
        public virtual async Task<T> ReadItemAsync(string id)
        {
            var item = await table.LookupAsync(id);
            if (item != null)
                item.IsModified = false;
            return item;
        }

        /// <inheritDoc />
        public virtual async Task<T> ReadItemAsync(Expression<System.Func<T, bool>> predicate)
        {
            var item = (await table.Where(predicate).Take(1).ToListAsync()).FirstOrDefault();
            if (item != null)
                item.IsModified = false;
            return item;
        }

        /// <inheritDoc />
        public virtual async Task<ICollection<T>> ReadItemsAsync(int start, int count)
        {
            var list = await table.Skip(start).Take(count).ToListAsync();
            foreach (T item in list)
                item.IsModified = false;
            return list;
        }

        /// <inheritDoc />
        public virtual async Task<ICollection<T>> ReadItemsAsync(Expression<System.Func<T, bool>> predicate)
        {
            var list = await table.Where(predicate).ToListAsync();
            foreach (T item in list)
                item.IsModified = false;
            return list;
        }

        /// <inheritDoc />
        public virtual async Task<T> UpdateItemAsync(T item)
        {
            await table.UpdateAsync(item);
            return item;
        }

        public DateTimeOffset? LastSync { get; private set; }

        async Task ResolveConflictAsync(MobileServiceTableOperationError error)
        {
            Debug.WriteLine("ResolveConflictAsync(): Going to allow Client to win.");

            var serverItem = error.Result.ToObject<T>();
            var localItem = error.Item.ToObject<T>();

            //// Note that you need to implement the public override Equals(TodoItem item)
            //// method in the Model for this to work
            //if (serverItem.Equals(localItem))
            //{
            //    // Items are the same, so ignore the conflict
            //    await error.CancelAndDiscardItemAsync();
            //    return;
            //}

            // Client Always Wins
            localItem.Version = serverItem.Version;
            await error.UpdateOperationAsync(JObject.FromObject(localItem));

            // Server Always Wins
            // await error.CancelAndDiscardItemAsync();
        }

        public virtual async Task PushOnlyAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!CrossConnectivity.Current.IsConnected) return;

            if (PendingOperations > 0)
            {
                try
                {
                    await _client.SyncContext.PushAsync(cancellationToken);
                }
                catch (MobileServicePushFailedException ex)
                {
                    Debug.WriteLine("PushOnlyAsync(): MobileServicePushFailedException hit. Message = " + ex.Message);
                    if (ex.PushResult != null)
                    {
                        foreach (var error in ex.PushResult.Errors)
                        {
                            await ResolveConflictAsync(error);
                        }
                    }
                }
            }
        }

        /// <inheritDoc />
        public virtual async Task SyncAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if(!CrossConnectivity.Current.IsConnected) return;

            if(PendingOperations > 0)
            {
                try
                {
                    await _client.SyncContext.PushAsync(cancellationToken);
                }
                catch (MobileServicePushFailedException ex)
                {
                    Debug.WriteLine("SyncAsync(): MobileServicePushFailedException hit. Message = " + ex.Message);
                    if (ex.PushResult != null)
                    {
                        foreach (var error in ex.PushResult.Errors)
                        {
                            await ResolveConflictAsync(error);
                        }
                    }
                }
            }

            await PullAsync();

            LastSync = DateTimeOffset.Now;
        }
        #endregion

        private async void OnConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
        {
            await OnConnectivityChangedAsync(e);
        }

        /// <summary>
        /// Handles Activity Connection Changes. 
        /// </summary>
        protected virtual async Task OnConnectivityChangedAsync(ConnectivityChangedEventArgs e)
        {
            if(e.IsConnected && (LastSync == null || DateTimeOffset.Now - LastSync.Value > TimeSpan.FromSeconds(10)))
            {
                await SyncAsync();
            }
        }
    }
}