﻿using Ardalis.SampleApp.Core.Interfaces;
using Ardalis.Specification;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ardalis.SampleApp.Infrastructure.Data;

/// <inheritdoc/>
public class CachedRepository<T> : IReadRepository<T> where T : class, IAggregateRoot
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachedRepository<T>> _logger;
    private readonly MyRepository<T> _sourceRepository;
    private readonly MemoryCacheEntryOptions _cacheOptions;

    public CachedRepository(IMemoryCache cache,
        ILogger<CachedRepository<T>> logger,
        MyRepository<T> sourceRepository)
    {
        _cache = cache;
        _logger = logger;
        _sourceRepository = sourceRepository;

        _cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(relative: TimeSpan.FromSeconds(10));
    }

    /// <inheritdoc/>
    public virtual IAsyncEnumerable<T> AsAsyncEnumerable(ISpecification<T> specification)
    {
        return _sourceRepository.AsAsyncEnumerable(specification);
    }

    /// <inheritdoc/>
    public Task<bool> AnyAsync(Specification.ISpecification<T> specification, CancellationToken cancellationToken = default)
    {
        // TODO: Add Caching
        return _sourceRepository.AnyAsync(specification, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<bool> AnyAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Add Caching
        return _sourceRepository.AnyAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task<int> CountAsync(Specification.ISpecification<T> specification, CancellationToken cancellationToken = default)
    {
        // TODO: Add Caching
        return _sourceRepository.CountAsync(specification, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Add Caching
        return _sourceRepository.CountAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task<T> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _sourceRepository.GetByIdAsync(id, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<T> GetByIdAsync<TId>(TId id, CancellationToken cancellationToken = default)
    {
        return _sourceRepository.GetByIdAsync(id, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<T> FirstOrDefaultAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
    {
        if (specification.CacheEnabled)
        {
            var key = $"{specification.CacheKey}-GetBySpecAsync";
            _logger.LogInformation("Checking cache for {cache_key}", key);
            return _cache.GetOrCreate(key, entry =>
            {
                entry.SetOptions(_cacheOptions);
                _logger.LogWarning("Fetching source data for {cache_key}", key);
                return _sourceRepository.FirstOrDefaultAsync(specification, cancellationToken);
            });
        }
        return _sourceRepository.FirstOrDefaultAsync(specification, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<TResult> GetBySpecAsync<TResult>(ISpecification<T, TResult> specification, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public virtual async Task<TResult> FirstOrDefaultAsync<TResult>(ISpecification<T, TResult> specification, CancellationToken cancellationToken = default)
    {
        return await _sourceRepository.FirstOrDefaultAsync(specification, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<T> SingleOrDefaultAsync(ISingleResultSpecification<T> specification, CancellationToken cancellationToken = default)
    {
        return await _sourceRepository.SingleOrDefaultAsync(specification, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<TResult> SingleOrDefaultAsync<TResult>(ISingleResultSpecification<T, TResult> specification, CancellationToken cancellationToken = default)
    {
        return await _sourceRepository.SingleOrDefaultAsync(specification, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<List<T>> ListAsync(CancellationToken cancellationToken = default)
    {
        var key = $"{nameof(T)}-ListAsync";
        return _cache.GetOrCreate(key, entry =>
        {
            entry.SetOptions(_cacheOptions);
            return _sourceRepository.ListAsync(cancellationToken);
        });
    }

    /// <inheritdoc/>
    public Task<List<T>> ListAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
    {
        if (specification.CacheEnabled)
        {
            var key = $"{specification.CacheKey}-ListAsync";
            _logger.LogInformation("Checking cache for {cache_key}", key);
            return _cache.GetOrCreate(key, entry =>
            {
                entry.SetOptions(_cacheOptions);
                _logger.LogWarning("Fetching source data for {cache_key}", key);
                return _sourceRepository.ListAsync(specification, cancellationToken);
            });
        }
        return _sourceRepository.ListAsync(specification, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<List<TResult>> ListAsync<TResult>(ISpecification<T, TResult> specification, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task<T> GetBySpecAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
