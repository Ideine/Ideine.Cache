using System;
using System.Threading.Tasks;

namespace Ideine.Cache
{
	public interface ICacheService
	{
		Task PutOnCache<T>(T item, string key, int page, DateTimeOffset? absoluteExpiration = default(DateTimeOffset?));
		Task PutOnCache<T>(T item, string key, DateTimeOffset? absoluteExpiration = default(DateTimeOffset?));

		Task<T> GetFromCache<T>(string key, bool raiseNotFoundException = false);
		Task<T> GetFromCache<T>(string key, int page, bool raiseNotFoundException = false);

		/// <summary>
		/// Attempt to return an object from the cache. If the item doesn't
		/// exist or returns an error, call a Func to return the latest
		/// version of an object and insert the result in the cache.
		/// </summary>
		/// <returns>The or fetch.</returns>
		/// <param name="key">The key to associate with the object.</param>
		/// <param name="fetchFunc">A Func which will asynchronously return</param>
		/// <param name="absoluteExpiration">An optional expiration date.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		Task<T> GetOrFetch<T>(string key, Func<Task<T>> fetchFunc, DateTimeOffset? absoluteExpiration = default(DateTimeOffset?));

		/// <summary>
		/// Attempt to return an object from the cache. If the item doesn't
		/// exist or returns an error, call a Func to return the latest
		/// version of an object and insert the result in the cache.
		/// </summary>
		/// <returns>The or fetch.</returns>
		/// <param name="key">The key to associate with the object.</param>
		/// <param name="page"></param>
		/// <param name="fetchFunc">A Func which will asynchronously return</param>
		/// <param name="absoluteExpiration">An optional expiration date.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		Task<T> GetOrFetch<T>(string key, int page, Func<Task<T>> fetchFunc, DateTimeOffset? absoluteExpiration = default(DateTimeOffset?));

		Task InvalidatePage(string key, int page);
		Task InvalidateAllPages(string key);

		Task InvalidateCache(string key);
		Task InvalidateAllEntities<T>();
		Task InvalidateAllCache();
	}
}