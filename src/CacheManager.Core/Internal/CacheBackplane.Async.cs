using System;
using System.Threading.Tasks;

namespace CacheManager.Core.Internal
{
    /// <summary>
    /// In CacheManager, a cache backplane is used to keep in process and distributed caches in
    /// sync. <br/> If the cache manager runs inside multiple nodes or applications accessing the
    /// same distributed cache, and an in process cache is configured to be in front of the
    /// distributed cache handle. All Get calls will hit the in process cache. <br/> Now when an
    /// item gets removed for example by one client, all other clients still have that cache item
    /// available in the in process cache. <br/> This could lead to errors and unexpected behavior,
    /// therefore a cache backplane will send a message to all other cache clients to also remove
    /// that item.
    /// <para>
    /// The same mechanism will apply to any Update, Put, Remove, Clear or ClearRegion call of the cache.
    /// </para>
    /// </summary>
    public abstract partial class CacheBackplane : IDisposable
    {
        /// <summary>
        /// The event gets fired whenever a change message for a key comes in,
        /// which means, another client changed a key.
        /// </summary>
        internal event EventHandler<CacheItemChangedEventArgs> ChangedAsync;

        /// <summary>
        /// The event gets fired whenever a cache clear message comes in.
        /// </summary>
        internal event EventHandler<EventArgs> ClearedAsync;

        /// <summary>
        /// The event gets fired whenever a clear region message comes in.
        /// </summary>
        internal event EventHandler<RegionEventArgs> ClearedRegionAsync;

        /// <summary>
        /// The event gets fired whenever a removed message for a key comes in.
        /// </summary>
        internal event EventHandler<CacheItemEventArgs> RemovedAsync;

        /// <summary>
        /// Notifies other cache clients about a changed cache key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="action">The action.</param>
        public abstract ValueTask NotifyChangeAsync(string key, CacheItemChangedEventAction action);

        /// <summary>
        /// Notifies other cache clients about a changed cache key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="region">The region.</param>
        /// <param name="action">The action.</param>
        public abstract ValueTask NotifyChangeAsync(string key, string region, CacheItemChangedEventAction action);

        /// <summary>
        /// Notifies other cache clients about a cache clear.
        /// </summary>
        public abstract ValueTask NotifyClearAsync();

        /// <summary>
        /// Notifies other cache clients about a cache clear region call.
        /// </summary>
        /// <param name="region">The region.</param>
        public abstract ValueTask NotifyClearRegionAsync(string region);

        /// <summary>
        /// Notifies other cache clients about a removed cache key.
        /// </summary>
        /// <param name="key">The key.</param>
        public abstract ValueTask NotifyRemoveAsync(string key);

        /// <summary>
        /// Notifies other cache clients about a removed cache key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="region">The region.</param>
        public abstract ValueTask NotifyRemoveAsync(string key, string region);

        /// <summary>
        /// Sends a changed message for the given <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="action">The action.</param>
        protected internal void TriggerChangedAsync(string key, CacheItemChangedEventAction action)
        {
            ChangedAsync?.Invoke(this, new CacheItemChangedEventArgs(key, action));
        }

        /// <summary>
        /// Sends a changed message for the given <paramref name="key"/> in <paramref name="region"/>.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="region">The region.</param>
        /// <param name="action">The action.</param>
        protected internal void TriggerChangedAsync(string key, string region, CacheItemChangedEventAction action)
        {
            ChangedAsync?.Invoke(this, new CacheItemChangedEventArgs(key, region, action));
        }

        /// <summary>
        /// Sends a cache cleared message.
        /// </summary>
        protected internal void TriggerClearedAsync()
        {
            ClearedAsync?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Sends a region cleared message for the given <paramref name="region"/>.
        /// </summary>
        /// <param name="region">The region.</param>
        protected internal void TriggerClearedRegionAsync(string region)
        {
            ClearedRegionAsync?.Invoke(this, new RegionEventArgs(region));
        }

        /// <summary>
        /// Sends a removed message for the given <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key</param>
        protected internal void TriggerRemovedAsync(string key)
        {
            RemovedAsync?.Invoke(this, new CacheItemEventArgs(key));
        }

        /// <summary>
        /// Sends a removed message for the given <paramref name="key"/> in <paramref name="region"/>.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="region">The region.</param>
        protected internal void TriggerRemovedAsync(string key, string region)
        {
            RemovedAsync?.Invoke(this, new CacheItemEventArgs(key, region));
        }
    }
}
