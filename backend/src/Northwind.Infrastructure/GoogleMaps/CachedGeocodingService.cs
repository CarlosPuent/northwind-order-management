using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Northwind.Application.Abstractions;
using Northwind.Domain.Common;
using Northwind.Domain.ValueObjects;

namespace Northwind.Infrastructure.GoogleMaps;

/// <summary>
/// Decorator around <see cref="IGeocodingService"/> that caches successful
/// geocoding results in memory. Same interface, transparent to consumers.
///
/// Why cache? Two reasons:
/// 1. Cost — Google charges per API call. Validating "123 Main St, NYC" twice
///    in the same session wastes money.
/// 2. Latency — a cache hit returns in under 1ms vs ~200ms for a real API call.
///
/// Cache key is the normalized single-line address. Entries expire after 24h
/// (sliding) because addresses don't change that often.
/// </summary>
internal sealed class CachedGeocodingService : IGeocodingService
{
    private readonly IGeocodingService _inner;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachedGeocodingService> _logger;

    private static readonly MemoryCacheEntryOptions CacheOptions = new()
    {
        SlidingExpiration = TimeSpan.FromHours(24)
    };

    public CachedGeocodingService(
        IGeocodingService inner,
        IMemoryCache cache,
        ILogger<CachedGeocodingService> logger)
    {
        _inner = inner;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Result<GeocodingResult>> ValidateAndGeocodeAsync(
        Address address,
        CancellationToken cancellationToken = default)
    {
        // Normalize the cache key so "123 Main St, NYC" and " 123 Main St , NYC "
        // resolve to the same entry.
        var cacheKey = $"geocode:{address.ToSingleLine().ToLowerInvariant().Trim()}";

        if (_cache.TryGetValue(cacheKey, out GeocodingResult? cached) && cached is not null)
        {
            _logger.LogDebug("Cache HIT for address: {CacheKey}", cacheKey);
            return cached;
        }

        _logger.LogDebug("Cache MISS for address: {CacheKey}", cacheKey);

        var result = await _inner.ValidateAndGeocodeAsync(address, cancellationToken);

        // Only cache successful results — errors might be transient.
        if (result.IsSuccess)
        {
            _cache.Set(cacheKey, result.Value, CacheOptions);
        }

        return result;
    }
}