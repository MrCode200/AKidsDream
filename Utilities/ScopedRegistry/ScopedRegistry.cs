using System;
using AKidsDream.Util.Identifiers;
using AKidsDream.Common.Registry;
using Serilog;

namespace AKidsDream.Utilities.Registry;

public interface IScopedRegistry<T>
{
    string Namespace { get; }
    bool Register(string path, T item);
    bool TryGet(string path, out T item);
    T Get(string path);
    bool Contains(string path);
}

public class ScopedRegistry<T> : IScopedRegistry<T>
{
    private readonly Registry<NamespacedKey, T> _registry;
    public string Namespace { get; }

    public ScopedRegistry(Registry<NamespacedKey, T> registry, string @namespace)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        Namespace = string.IsNullOrWhiteSpace(@namespace) ? NamespacedKey.DefaultNamespace : @namespace.Trim().ToLowerInvariant();
    }

    public bool Register(string path, T item)
    {
        var key = new NamespacedKey(Namespace, path);
        return _registry.Register(key, item);
    }

    public bool TryGet(string path, out T item)
    {
        var key = NamespacedKey.From(path, Namespace);
        return _registry.TryGet(key, out item);
    }

    public T Get(string path)
    {
        var key = NamespacedKey.From(path, Namespace);
        return _registry.Get(key);
    }

    public bool Contains(string path)
    {
        var key = NamespacedKey.From(path, Namespace);
        return _registry.ContainsKey(key);
    }
}