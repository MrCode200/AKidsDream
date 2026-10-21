global using UnitId = AKidsDream.Util.Identifiers.Id<AKidsDream.Util.Identifiers.UnitIdTag>;
global using PlayerId = AKidsDream.Util.Identifiers.Id<AKidsDream.Util.Identifiers.PlayerIdTag>;
global using TeamId = AKidsDream.Util.Identifiers.Id<AKidsDream.Util.Identifiers.TeamIdTag>;
global using CardId = AKidsDream.Util.Identifiers.Id<AKidsDream.Util.Identifiers.CardIdTag>;

using System;
using AKidsDream.Utilities.Logging;
using Serilog;


namespace AKidsDream.Util.Identifiers;

public enum TeamRelation
{
    Ally,
    Enemy
}

public interface IIdTag;

public readonly struct UnitIdTag : IIdTag;

public readonly struct PlayerIdTag : IIdTag;

public readonly struct TeamIdTag : IIdTag;

public readonly struct CardIdTag : IIdTag;

public readonly struct Id<TTag> : IEquatable<Id<TTag>>, IIdTag where TTag : IIdTag
{
    public static readonly Id<TTag> None = new(0);

    private static ILogger Log => GameLogger.For(typeof(Id<TTag>));
    private static bool _noneValueSet;
    private static int _nextId = 1;
    
    public int Value { get; }

    public Id(int id)
    {
        switch (id)
        {
            case <= 0 when _noneValueSet:
                Log.Here()
                    .Err("{Type} initialized with unrecommended id <= 0: {Id}", typeof(TTag).Name, id);
                break;
            case <= 0:
                _noneValueSet = true;
                break;
        }

        Value = id;
    }

    public static void SetNextId(int id)
    {
        if (id < _nextId)
            Log.Here()
                .Warn("NextId {NextId} is set to less than to the current id {CurrentId}. " +
                       "This can cause issues with id generation.", id, _nextId);
        _nextId = id;
    }
    
    public static Id<TTag> GetNextId() => new(_nextId++);
    
    public bool Equals(Id<TTag> other) => Value == other.Value;
    public bool Equals(TTag other)
    {
        throw new NotImplementedException();
    }

    public override bool Equals(object obj) => obj is Id<TTag> other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => $"{typeof(TTag).Name}#{Value}";
    
    public static bool operator ==(Id<TTag> a, Id<TTag> b) => a.Equals(b);
    public static bool operator !=(Id<TTag> a, Id<TTag> b) => !a.Equals(b);
    
    public static bool operator ==(Id<TTag> a, int b) => a.Value == b;
    public static bool operator !=(Id<TTag> a, int b) => a.Value != b;
    public static bool operator ==(int a, Id<TTag> b) => a == b.Value;
    public static bool operator !=(int a, Id<TTag> b) => a != b.Value;
}

public readonly record struct NamespacedKey : IEquatable<NamespacedKey>, IComparable<NamespacedKey>
{
    public const string DefaultNamespace = "core";

    public string Namespace { get; }
    public string Path { get; }

    public NamespacedKey(string @namespace, string path)
    {
        Namespace = string.IsNullOrWhiteSpace(@namespace) ? DefaultNamespace : @namespace.Trim().ToLowerInvariant();
        Path = (path ?? throw new ArgumentNullException(nameof(path))).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(Path))
            throw new ArgumentException("Path cannot be empty or whitespace.", nameof(path));
    }

    public static NamespacedKey From(string raw, string defaultNs = DefaultNamespace)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new ArgumentException("Key cannot be empty or whitespace.", nameof(raw));

        int colon = raw.IndexOf(':');
        return colon >= 0
            ? new NamespacedKey(raw[..colon], raw[(colon + 1)..])
            : new NamespacedKey(defaultNs, raw);
    }

    public static bool TryParse(string raw, out NamespacedKey key, string defaultNs = DefaultNamespace)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            key = default;
            return false;
        }

        try
        {
            key = From(raw, defaultNs);
            return true;
        }
        catch
        {
            key = default;
            return false;
        }
    }

    public int CompareTo(NamespacedKey other)
    {
        int nsComparison = string.Compare(Namespace, other.Namespace, StringComparison.OrdinalIgnoreCase);
        return nsComparison != 0 ? nsComparison : string.Compare(Path, other.Path, StringComparison.OrdinalIgnoreCase);
    }

    public override string ToString() => $"{Namespace}:{Path}";

    public static implicit operator string(NamespacedKey key) => key.ToString();
    public static implicit operator NamespacedKey(string raw) => From(raw);
}