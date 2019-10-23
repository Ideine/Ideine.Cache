# Ideine.Cache

Ideine.Cache is a plugin built over Akavache which give an abstraction to store data.

## Storage type

To store data you can use default storage of Akavache (`Local`, `Secure` or `InMemory`) by using `StorageType` definition or you can provide an implementation of `IBlobCache`

## Pagination

In case of you want to store data in many chunk you can use paginated method's version by using `page` argument. In case of you want invalidate all page you must use `InvalidateAllCache()` or `InvalidateAllEntities<T>()`

