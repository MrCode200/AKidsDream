using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AKidsDream.Utilities.Logging;
using Serilog;

namespace AKidsDream.Common.Registry;

public class Registry<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
{
    private static readonly ILogger Log = GameLogger.For<Registry<TKey, TValue>>();

    public string Name { get; init; }
    public Func<TValue, bool>[] Validations { get; private set; }
    public bool IsFrozen { get; private set; }

    private readonly Dictionary<TKey, TValue> _itemRegister = new();
    
    // CREATION
    public Registry(string name, Func<TValue, bool>[] validations = null)
    {
        Name = name;
        Validations = validations ?? [];
    }
    
    public void Freeze()
    {
        IsFrozen = true;
        Log.Here().Debug("Registry {RegistryName} frozen with {Count} entries", Name, _itemRegister.Count);
    }
    
    // ------------------------------------------------------ API -----------------------------------------------------
    // ITEMS
    public IReadOnlyDictionary<TKey, TValue> Item => _itemRegister;
    
    public bool Register(TKey key, TValue item)
    {
        if (IsFrozen)
        {
            Log.Here().Warn("Attempted to register key {Key} to frozen registry {RegistryName}", key, Name);
            return false;
        }

        if (!Validate(item))
        {
            Log.Here().Warn("Validation failed for item with key {Key} in registry {RegistryName}", key, Name);
            return false;
        }

        var success = _itemRegister.TryAdd(key, item);
        if (!success)
        {
            Log.Here().Warn("Key {Key} already exists in registry {RegistryName}", key, Name);
        }
        return success;
    }

    public bool TryGet(TKey key, out TValue item)
    {
        if (_itemRegister.TryGetValue(key, out item))
            return true;

        item = default;
        return false;
    }
    
    public TValue Get(TKey key)
    {
        if (TryGet(key, out var item))
            return item;

        throw new KeyNotFoundException($"Key '{key}' was not found in registry '{Name}'.");
    }

    public bool ContainsKey(TKey key)
    {
        return _itemRegister.ContainsKey(key);
    }
    
    public List<TValue> GetAllValues()
    {
        return new List<TValue>(_itemRegister.Values);
    }
    
    public List<TKey> GetAllKeys()
    {
        return new List<TKey>(_itemRegister.Keys);
    }

    public IReadOnlyDictionary<TKey, TValue> Entries => _itemRegister;
    
    public bool Remove(TKey key)
    {
        if (IsFrozen)
        {
            Log.Here().Warn("Attempted to remove key {Key} from frozen registry {RegistryName}", key, Name);
            return false;
        }

        return _itemRegister.Remove(key);
    }

    public void Clear()
    {
        if (IsFrozen)
        {
            Log.Here().Warn("Attempted to clear frozen registry {RegistryName}", Name);
            return;
        }

        _itemRegister.Clear();
    }
    
    public int Count => _itemRegister.Count;

    // -------------------------------------------------- VALIDATION --------------------------------------------------
    private bool Validate(TValue item)
    {
        return Validations.All(v => v(item));
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _itemRegister.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}