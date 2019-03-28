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

		public CacheService(StorageType storageType)
		{
			BlobCache.ApplicationName = "Ideine_Cache";
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

		public async Task PutOnCache<T>(T item, string key, int page, TimeSpan duration)
		{
			await BlobCache.LocalMachine.InsertObject(GetObjectKey(key, page), item, duration);
			await BlobCache.LocalMachine.InsertObject(Guid.NewGuid().ToString(), new PaginatedCache(key, page), duration);
		}

		public async Task<T> GetFromCache<T>(string key, int page)
		{
			return await BlobCache.LocalMachine.GetObject<T>(GetObjectKey(key, page))
				.Catch<T, KeyNotFoundException>(_ => Observable.Return(default(T)));
		}

		public async Task InvalidatePage(string key, int page)
		{
			await BlobCache.LocalMachine.Invalidate(GetObjectKey(key, page));
		}

		public async Task InvalidateAllPages(string key)
		{
			var pageList = await BlobCache.LocalMachine.GetAllObjects<PaginatedCache>();
			if (pageList != null)
			{
				var allPages = pageList.Where(x => x.Key == key).Select(pageCache => GetObjectKey(key, pageCache.Page)).ToList();
				if (allPages.Any())
				{
					await BlobCache.LocalMachine.Invalidate(allPages);
				}
			}
		}

		#endregion

		#region Normal

		public async Task PutOnCache<T>(T item, string key, TimeSpan duration)
		{
			await _cacheSystem.InsertObject(key, item, duration);
		}

		public async Task<T> GetFromCache<T>(string key)
		{
			return await _cacheSystem.GetObject<T>(key)
				.Catch<T, KeyNotFoundException>(_ => Observable.Return(default(T)));
		}

		public async Task InvalidateCache(string key)
		{
			await _cacheSystem.Invalidate(key);
		}

		#endregion

		public async Task InvalidateAllEntities<T>()
		{
			await BlobCache.LocalMachine.InvalidateAllObjects<T>();
		}

		public async Task InvalidateAllCache()
		{
			await BlobCache.LocalMachine.InvalidateAll();
		}
	}
}