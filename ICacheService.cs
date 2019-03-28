using System;
using System.Threading.Tasks;

namespace Ideine.Cache
{
	public interface ICacheService
	{
		Task PutOnCache<T>(T item, string key, int page, TimeSpan duration);
		Task PutOnCache<T>(T item, string key, TimeSpan duration);

		Task<T> GetFromCache<T>(string key, int page);
		Task<T> GetFromCache<T>(string key);

		Task InvalidatePage(string key, int page);
		Task InvalidateAllPages(string key);

		Task InvalidateCache(string key);
		Task InvalidateAllEntities<T>();
		Task InvalidateAllCache();
	}
}