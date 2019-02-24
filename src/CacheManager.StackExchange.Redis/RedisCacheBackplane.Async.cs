using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CacheManager.Core;
using CacheManager.Core.Internal;
using CacheManager.Core.Logging;

namespace CacheManager.Redis
{
    /// <summary>
    /// Implementation of the cache backplane using a Redis Pub/Sub channel.
    /// <para>
    /// Redis Pub/Sub is used to send messages to the redis server on any key change, cache clear, region
    /// clear or key remove operation.
    /// Every cache manager with the same configuration subscribes to the
    /// same channel and can react on those messages to keep other cache handles in sync with the 'master'.
    /// </para>
    /// </summary>
    /// <remarks>
    /// The cache manager must have at least one cache handle configured with <see cref="CacheHandleConfiguration.IsBackplaneSource"/> set to <c>true</c>.
    /// Usually this is the redis cache handle, if configured. It should be the distributed and bottom most cache handle.
    /// </remarks>
    public sealed partial class RedisCacheBackplane : CacheBackplane
    {
        private SemaphoreSlim _messageAsyncLock = new SemaphoreSlim(0, 1);
        private SemaphoreSlim _messageSendAsyncLock = new SemaphoreSlim(0, 1);

        /// <summary>
        /// Notifies other cache clients about a changed cache key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="action">The cache action.</param>
        public override ValueTask NotifyChangeAsync(string key, CacheItemChangedEventAction action)
        {
            return PublishMessageAsync(BackplaneMessage.ForChanged(_identifier, key, action));
        }

        /// <summary>
        /// Notifies other cache clients about a changed cache key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="region">The region.</param>
        /// <param name="action">The cache action.</param>
        public override ValueTask NotifyChangeAsync(string key, string region, CacheItemChangedEventAction action)
        {
            return PublishMessageAsync(BackplaneMessage.ForChanged(_identifier, key, region, action));
        }

        /// <summary>
        /// Notifies other cache clients about a cache clear.
        /// </summary>
        public override ValueTask NotifyClearAsync()
        {
            return PublishMessageAsync(BackplaneMessage.ForClear(_identifier));
        }

        /// <summary>
        /// Notifies other cache clients about a cache clear region call.
        /// </summary>
        /// <param name="region">The region.</param>
        public override ValueTask NotifyClearRegionAsync(string region)
        {
            return PublishMessageAsync(BackplaneMessage.ForClearRegion(_identifier, region));
        }

        /// <summary>
        /// Notifies other cache clients about a removed cache key.
        /// </summary>
        /// <param name="key">The key.</param>
        public override ValueTask NotifyRemoveAsync(string key)
        {
            return PublishMessageAsync(BackplaneMessage.ForRemoved(_identifier, key));
        }

        /// <summary>
        /// Notifies other cache clients about a removed cache key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="region">The region.</param>
        public override ValueTask NotifyRemoveAsync(string key, string region)
        {
            return PublishMessageAsync(BackplaneMessage.ForRemoved(_identifier, key, region));
        }

        private async ValueTask PublishAsync(byte[] message)
        {
            await _connection.Subscriber.PublishAsync(_channelName, message);
        }

        private async ValueTask PublishMessageAsync(BackplaneMessage message)
        {
            await _messageAsyncLock.WaitAsync();
            try
            {
                if (message.Action == BackplaneAction.Clear)
                {
                    Interlocked.Exchange(ref _skippedMessages, _messages.Count);
                    _messages.Clear();
                }

                if (_messages.Count > HardLimit)
                {
                    if (!loggedLimitWarningOnce)
                    {
                        _logger.LogError("Exceeded hard limit of number of messages pooled to send through the backplane. Skipping new messages...");
                        loggedLimitWarningOnce = true;
                    }
                }
                else if (!_messages.Add(message))
                {
                    Interlocked.Increment(ref _skippedMessages);
                    if (_logger.IsEnabled(LogLevel.Trace))
                    {
                        _logger.LogTrace("Skipped duplicate message: {0}.", message);
                    }
                }

                await SendMessagesAsync(null);
            }
            finally
            {
                _messageAsyncLock.Release();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "No other way")]
        private async ValueTask SendMessagesAsync(object state)
        {
            if (_sending || _messages == null || _messages.Count == 0)
            {
                return;
            }

            if (_sending || _messages == null || _messages.Count == 0)
            {
                return;
            }

            _sending = true;
            if (state != null && state is bool boolState && boolState == true)
            {
                _logger.LogInfo($"Backplane is sending {_messages.Count} messages triggered by timer.");
            }
#if !NET40
            await Task.Delay(10);
#endif
            byte[] msgs = null;
            await _messageSendAsyncLock.WaitAsync();
            try
            {
                if (_messages != null && _messages.Count > 0)
                {
                    msgs = BackplaneMessage.Serialize(_messages.ToArray());

                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug("Backplane is sending {0} messages ({1} skipped).", _messages.Count, _skippedMessages);
                    }

                    try
                    {
                        if (msgs != null)
                        {
                            await PublishAsync(msgs);
                            Interlocked.Increment(ref SentChunks);
                            Interlocked.Add(ref MessagesSent, _messages.Count);
                            _skippedMessages = 0;

                            // clearing up only after successfully sending. Basically retrying...
                            _messages.Clear();

                            // reset log limmiter because we just send stuff
                            loggedLimitWarningOnce = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error occurred sending backplane messages.");
                    }
                }

                _sending = false;
            }
            finally
            {
                _messageSendAsyncLock.Release();
            }
        }
    }
}
