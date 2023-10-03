using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Akavache;

namespace Xmf2.Cache;

public class CacheService : ICacheService
{
	private readonly IBlobCache _cacheSystem;

	public CacheService(StorageType storageType, string appName = "Ideine_Cache")
	{
		BlobCache.ApplicationName = appName;
		BlobCache.EnsureInitialized();

		_cacheSystem = storageType switch
		{
			StorageType.Local => BlobCache.LocalMachine,
			StorageType.Secure => BlobCache.Secure,
			_ => BlobCache.InMemory
		};
	}

	public CacheService(IBlobCache blobCache) => _cacheSystem = blobCache;

	private static string GetObjectKey(string key, int page) => $"{key}-{page}";

	#region Pagination

	private class PaginatedCache
	{
		public string Key { get; set; }
		public int Page { get; set; }

		public PaginatedCache() { }

		public PaginatedCache(string key, int page)
		{
			Key = key;
			Page = page;
		}
	}

	public async Task PutOnCache<T>(T item, string key, int page, DateTimeOffset? absoluteExpiration = null)
	{
		string paginatedKey = GetObjectKey(key, page);
		await _cacheSystem.InsertObject(paginatedKey, item, absoluteExpiration);
		await _cacheSystem.InsertObject(Guid.NewGuid().ToString(), new PaginatedCache(key, page), absoluteExpiration);
	}

	public async Task<T> GetFromCache<T>(string key, int page, bool raiseNotFoundException = false)
	{
#pragma warning disable IDE0046
		if (raiseNotFoundException)
		{
			return await _cacheSystem.GetObject<T>(GetObjectKey(key, page));
		}
		else
		{
			return await _cacheSystem.GetObject<T>(GetObjectKey(key, page)).Catch<T, KeyNotFoundException>(_ => Observable.Return(default(T)));
		}
#pragma warning restore IDE0046
	}

	public async Task<T> GetOrFetch<T>(string key, Func<Task<T>> fetchFunc, DateTimeOffset? absoluteExpiration = null)
	{
		return await _cacheSystem.GetOrFetchObject(key, () => Observable.FromAsync(fetchFunc), absoluteExpiration);
	}

	public async Task<T> GetOrFetch<T>(string key, int page, Func<Task<T>> fetchFunc, DateTimeOffset? absoluteExpiration = null)
	{
		string paginatedKey = GetObjectKey(key, page);
		return await _cacheSystem.GetOrFetchObject(paginatedKey, () => Observable.FromAsync(FetchAndSavePage), absoluteExpiration);

		async Task<T> FetchAndSavePage()
		{
			T result = await fetchFunc();
			await _cacheSystem.InsertObject(Guid.NewGuid().ToString(), new PaginatedCache(key, page), absoluteExpiration);
			return result;
		}
	}

	public async Task InvalidatePage(string key, int page)
	{
		await _cacheSystem.Invalidate(GetObjectKey(key, page));
	}

	public async Task InvalidateAllPages(string key)
	{
		IEnumerable<PaginatedCache> pageList = await _cacheSystem.GetAllObjects<PaginatedCache>();
		List<string> allPages = pageList.Where(x => x.Key == key).Select(pageCache => GetObjectKey(key, pageCache.Page)).ToList();
		if (allPages.Any())
		{
			await _cacheSystem.Invalidate(allPages);
		}
	}

	#endregion Pagination

	#region Normal

	public async Task PutOnCache<T>(T item, string key, DateTimeOffset? absoluteExpiration = null)
	{
		await _cacheSystem.InsertObject(key, item, absoluteExpiration);
	}

	public async Task<T> GetFromCache<T>(string key, bool raiseNotFoundException = false)
	{
#pragma warning disable IDE0046
		if (raiseNotFoundException)
		{
			return await _cacheSystem.GetObject<T>(key);
		}
		else
		{
			return await _cacheSystem.GetObject<T>(key).Catch<T, KeyNotFoundException>(_ => Observable.Return(default(T)));
		}
#pragma warning restore IDE0046
	}

	public async Task InvalidateCache(string key)
	{
		await _cacheSystem.Invalidate(key);
	}

	public async Task InvalidateAllEntities<T>()
	{
		await _cacheSystem.InvalidateAllObjects<T>();
	}

	public async Task InvalidateAllCache()
	{
		await _cacheSystem.InvalidateAll();
	}

	#endregion Normal
}