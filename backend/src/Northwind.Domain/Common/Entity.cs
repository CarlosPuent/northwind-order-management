namespace Northwind.Domain.Common;

/// <summary>
/// Base class for all domain entities. An entity is an object with a distinct
/// identity (Id) that runs through its lifetime, regardless of attribute changes.
/// Two entities are equal if and only if they share the same Id and same type.
/// </summary>
/// <typeparam name="TId">The type of the identifier (int for Northwind tables,
/// Guid for new tables we introduce like ShippingGeocodes).</typeparam>
public abstract class Entity<TId> where TId : notnull
{
    public TId Id { get; protected set; }

    protected Entity(TId id)
    {
        Id = id;
    }

    // Parameterless constructor required by EF Core for materialization.
    // Marked protected so domain code cannot accidentally create an entity
    // without an Id — only EF Core can, via reflection.
    protected Entity()
    {
        Id = default!;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override int GetHashCode() =>
        EqualityComparer<TId>.Default.GetHashCode(Id);

    public static bool operator ==(Entity<TId>? a, Entity<TId>? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;
        return a.Equals(b);
    }

    public static bool operator !=(Entity<TId>? a, Entity<TId>? b) => !(a == b);
}