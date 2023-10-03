using System;
using System.Threading.Tasks;

namespace Xmf2.Cache;

public static class CacheServiceExtension
{
	public static Task PutOnCache<T>(this ICacheService cache, T item, string key, int page, TimeSpan duration)
	{
		return cache.PutOnCache(item, key, page, DateTimeOffset.Now.Add(duration));
	}

	public static Task PutOnCache<T>(this ICacheService cache, T item, string key, TimeSpan duration)
	{
		return cache.PutOnCache(item, key, DateTimeOffset.Now.Add(duration));
	}

	public static Task<T> GetOrFetch<T>(this ICacheService cache, string key, Func<Task<T>> fetchFunc, TimeSpan duration)
	{
		return cache.GetOrFetch(key, fetchFunc, DateTimeOffset.Now.Add(duration));
	}

	public static Task<T> GetOrFetch<T>(this ICacheService cache, string key, int page, Func<Task<T>> fetchFunc, TimeSpan duration)
	{
		return cache.GetOrFetch(key, page, fetchFunc, DateTimeOffset.Now.Add(duration));
	}
}