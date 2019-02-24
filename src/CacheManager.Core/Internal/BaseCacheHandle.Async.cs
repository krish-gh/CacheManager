using System;
using System.Threading.Tasks;
using CacheManager.Core.Logging;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Core.Internal
{
#if !NET40
    public abstract partial class BaseCacheHandle<TCacheValue>
    {
        /// <inheritdoc />
        protected internal override Task<bool> AddInternalAsync(CacheItem<TCacheValue> item)
        {
            CheckDisposed();
            item = GetItemExpiration(item);
            return AddInternalPreparedAsync(item);
        }

        /// <summary>
        /// Adds a value to the cache.
        /// </summary>
        /// <param name="item">The <c>CacheItem</c> to be added to the cache.</param>
        /// <returns>
        /// <c>true</c> if the key was not already added to the cache, <c>false</c> otherwise.
        /// </returns>
        protected virtual Task<bool> AddInternalPreparedAsync(CacheItem<TCacheValue> item)
        {
            var result = AddInternalPrepared(item);
            return Task.FromResult(result);
        }

        /// <summary>
        /// Clears this cache, removing all items in the base cache and all regions.
        /// </summary>
        public override Task ClearAsync()
        {
            Clear();
            return Task.FromResult(0);
        }

        /// <summary>
        /// Clears the cache region, removing all items from the specified <paramref name="region"/> only.
        /// </summary>
        /// <param name="region">The cache region.</param>
        /// <exception cref="ArgumentNullException">If the <paramref name="region"/> is null.</exception>
        public override Task ClearRegionAsync(string region)
        {
            ClearRegion(region);
            return Task.FromResult(0);
        }

        /// <inheritdoc />
        public override Task<bool> ExistsAsync(string key)
        {
            var result = Exists(key);
            return Task.FromResult(result);
        }

        /// <inheritdoc />
        public override Task<bool> ExistsAsync(string key, string region)
        {
            var result = Exists(key, region);
            return Task.FromResult(result);
        }

        /// <inheritdoc />
        protected override Task<CacheItem<TCacheValue>> GetCacheItemInternalAsync(string key)
        {
            var result = GetCacheItemInternal(key);
            return Task.FromResult(result);
        }

        /// <inheritdoc />
        protected override Task<CacheItem<TCacheValue>> GetCacheItemInternalAsync(string key, string region)
        {
            var result = GetCacheItemInternal(key, region);
            return Task.FromResult(result);
        }
        
        /// <summary>
        /// Puts the <paramref name="item"/> into the cache. If the item exists it will get updated
        /// with the new value. If the item doesn't exist, the item will be added to the cache.
        /// </summary>
        /// <param name="item">The <c>CacheItem</c> to be added to the cache.</param>
        protected internal override Task PutInternalAsync(CacheItem<TCacheValue> item)
        {
            CheckDisposed();
            item = GetItemExpiration(item);
            return PutInternalPreparedAsync(item);
        }

        /// <summary>
        /// Puts the <paramref name="item"/> into the cache. If the item exists it will get updated
        /// with the new value. If the item doesn't exist, the item will be added to the cache.
        /// </summary>
        /// <param name="item">The <c>CacheItem</c> to be added to the cache.</param>
        protected virtual Task PutInternalPreparedAsync(CacheItem<TCacheValue> item)
        {
            PutInternalPrepared(item);
            return Task.FromResult(0);
        }
        
        /// <inheritdoc />
        protected override Task<bool> RemoveInternalAsync(string key)
        { 
            var result = RemoveInternal(key);
            return Task.FromResult(result);
        }

        /// <inheritdoc />
        protected override Task<bool> RemoveInternalAsync(string key, string region)
        {
            var result = RemoveInternal(key, region);
            return Task.FromResult(result);
        }
    }
#endif
}
