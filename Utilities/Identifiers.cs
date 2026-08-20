global using UnitId = AKidsDream.Core.Controllers.Id<AKidsDream.Core.Controllers.UnitIdTag>;
global using PlayerId = AKidsDream.Core.Controllers.Id<AKidsDream.Core.Controllers.PlayerIdTag>;
global using TeamId = AKidsDream.Core.Controllers.Id<AKidsDream.Core.Controllers.TeamIdTag>;

using System;
using AKidsDream.Common.Logging;
using Serilog;


namespace AKidsDream.Core.Controllers;

public enum TeamRelation
{
    Ally,
    Enemy
}

public interface IIdTag { }

public readonly struct UnitIdTag : IIdTag { }
public readonly struct PlayerIdTag : IIdTag { }
public readonly struct TeamIdTag: IIdTag { }
public readonly struct PoolIdTag: IIdTag { }

public readonly struct Id<TTag> : IEquatable<Id<TTag>> where TTag : IIdTag
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
                    .Error("{Type} initialized with unrecommended id <= 0: {Id}", typeof(TTag).Name, id);
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