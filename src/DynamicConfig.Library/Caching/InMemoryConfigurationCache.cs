using DynamicConfig.Domain.Entities;

namespace DynamicConfig.Library.Caching;

internal sealed class InMemoryConfigurationCache
{
    private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);
    private Dictionary<string, ConfigurationEntry> _entries = new(StringComparer.OrdinalIgnoreCase);
    private long _maxVersion;

    /// <summary>Returns a snapshot of all cached entries (key=Name).</summary>
    public IReadOnlyDictionary<string, ConfigurationEntry> Snapshot()
    {
        _lock.EnterReadLock();
        try
        {
            return new Dictionary<string, ConfigurationEntry>(_entries, StringComparer.OrdinalIgnoreCase);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>The highest Version seen across all cached entries.</summary>
    public long MaxVersion
    {
        get
        {
            _lock.EnterReadLock();
            try { return _maxVersion; }
            finally { _lock.ExitReadLock(); }
        }
    }

    public void ReplaceAll(IEnumerable<ConfigurationEntry> entries)
    {
        var list = entries.ToList();
        var next = list.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
        var newMaxVersion = list.Count > 0 ? list.Max(x => x.Version) : 0L;

        _lock.EnterWriteLock();
        try
        {
            _entries = next;
            _maxVersion = newMaxVersion;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Merges a set of changed/added entries into the cache without a full reload.
    /// Removed (deactivated) entries are excluded since we only cache active ones;
    /// callers should pass the full active set instead.
    /// </summary>
    public void MergeChanges(IEnumerable<ConfigurationEntry> changedEntries)
    {
        _lock.EnterWriteLock();
        try
        {
            foreach (var entry in changedEntries)
            {
                if (entry.IsActive)
                {
                    _entries[entry.Name] = entry;
                }
                else
                {
                    _entries.Remove(entry.Name);
                }

                if (entry.Version > _maxVersion)
                {
                    _maxVersion = entry.Version;
                }
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public bool HasChanges(IReadOnlyList<ConfigurationEntry> incoming)
    {
        _lock.EnterReadLock();
        try
        {
            if (incoming.Count != _entries.Count)
            {
                return true;
            }

            foreach (var entry in incoming)
            {
                if (!_entries.TryGetValue(entry.Name, out var existing))
                {
                    return true;
                }

                if (!string.Equals(existing.Value, entry.Value, StringComparison.Ordinal) ||
                    existing.Type != entry.Type ||
                    existing.IsActive != entry.IsActive ||
                    existing.Version != entry.Version)
                {
                    return true;
                }
            }

            return false;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }
}
