using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Akavache;

namespace Ideine.Cache
{
	public class CacheService : ICacheService
	{
		private readonly IBlobCache _cacheSystem;
		
		public CacheService(StorageType storageType, string appName = "Ideine_Cache")
		{
			BlobCache.ApplicationName = appName;
			BlobCache.EnsureInitialized();

			switch (storageType)
			{
				case StorageType.Local:
					_cacheSystem = BlobCache.LocalMachine;
					break;
				case StorageType.Secure:
					_cacheSystem = BlobCache.Secure;
					break;
				default:
					_cacheSystem = BlobCache.InMemory;
					break;
			}
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
			var paginatedKey = GetObjectKey(key, page);
			await _cacheSystem.InsertObject(paginatedKey, item, absoluteExpiration);
			await _cacheSystem.InsertObject(Guid.NewGuid().ToString(), new PaginatedCache(key, page), absoluteExpiration);
		}

		public async Task<T> GetFromCache<T>(string key, int page, bool raiseNotFoundException = false)
		{
			return raiseNotFoundException
				? await _cacheSystem.GetObject<T>(GetObjectKey(key, page))
				: await _cacheSystem.GetObject<T>(GetObjectKey(key, page))
					.Catch<T, KeyNotFoundException>(_ => Observable.Return(default(T)));
		}

		public async Task<T> GetOrFetch<T>(string key, Func<Task<T>> fetchFunc, DateTimeOffset? absoluteExpiration = null)
		{
			return await _cacheSystem.GetOrFetchObject<T>(
				key,
				fetchFunc: () => Observable.FromAsync(fetchFunc),
				absoluteExpiration: absoluteExpiration);
		}

		public async Task<T> GetOrFetch<T>(string key, int page, Func<Task<T>> fetchFunc, DateTimeOffset? absoluteExpiration = null)
		{
			var paginatedKey = GetObjectKey(key, page);
			return await _cacheSystem.GetOrFetchObject<T>(
				paginatedKey,
				fetchFunc: () => Observable.FromAsync(FetchAndSavePage),
				absoluteExpiration: absoluteExpiration);

			async Task<T> FetchAndSavePage()
			{
				var result = await fetchFunc();
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
			var pageList = await _cacheSystem.GetAllObjects<PaginatedCache>();
			if (pageList != null)
			{
				var allPages = pageList.Where(x => x.Key == key).Select(pageCache => GetObjectKey(key, pageCache.Page)).ToList();
				if (allPages.Any())
				{
					await _cacheSystem.Invalidate(allPages);
				}
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
			return raiseNotFoundException
				? await _cacheSystem.GetObject<T>(key)
				: await _cacheSystem.GetObject<T>(key)
					.Catch<T, KeyNotFoundException>(_ => Observable.Return(default(T)));
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
}
