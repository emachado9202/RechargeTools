using EFCache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Web;

namespace RechargeTools.Models.Handlers
{
    public class CacheManager
    {
        public static readonly InMemoryCache Stock = new InMemoryCache();

        public static MemoryCache MemoryCache = new MemoryCache("RechargeTools");

        public static CacheItemPolicy GetCacheItemPolicy(TimeSpan? duration)
        {
            var absoluteExpiration = ObjectCache.InfiniteAbsoluteExpiration;

            if (duration.HasValue)
            {
                absoluteExpiration = DateTime.UtcNow + duration.Value;
            }

            var cacheItemPolicy = new CacheItemPolicy
            {
                AbsoluteExpiration = absoluteExpiration,
                SlidingExpiration = ObjectCache.NoSlidingExpiration
            };

            return cacheItemPolicy;
        }
    }
}