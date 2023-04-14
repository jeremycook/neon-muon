using System.Collections;
using System.Collections.Immutable;

namespace Sqlil.Core;

public static class StableList {
    public static StableList<T> ToStableList<T>(this IEnumerable<T> enumerable) {
        return new(enumerable.ToImmutableList());
    }

    public static StableList<T> Create<T>(params T[] array) {
        return new(ImmutableList.Create(array));
    }

    public static StableList<T> Create<T>(T item) {
        return new(ImmutableList.Create(item));
    }
}

public class StableList<T> : IReadOnlyList<T> {
    private static readonly EqualityComparer<T> ItemEqualityComparer = EqualityComparer<T>.Default;

    private readonly ImmutableList<T> List;
    private int? HashCode;

    public StableList(ImmutableList<T> immutableList) {
        List = immutableList;
    }

    public T this[int index] => List[index];

    public int Count => List.Count;

    public static StableList<T> Empty { get; } = new(ImmutableList<T>.Empty);

    public IEnumerator<T> GetEnumerator() {
        return List.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return List.GetEnumerator();
    }

    public override int GetHashCode() {
        unchecked {
            if (HashCode == null) {
                int hashCode = 17;
                foreach (var item in List) {
                    hashCode = hashCode * 23 + ItemEqualityComparer.GetHashCode(item!);
                }
                HashCode = hashCode;
            }

            return HashCode.Value;
        }
    }

    public override bool Equals(object? obj) {
        if (obj is not StableList<T> other) {
            return false;
        }

        return GetHashCode() == other.GetHashCode()
            && Enumerable.SequenceEqual(this, other);
    }

    public static bool operator ==(StableList<T> left, StableList<T> right) {
        return left.GetHashCode() == right.GetHashCode()
            && Enumerable.SequenceEqual(left, right);
    }

    public static bool operator !=(StableList<T> left, StableList<T> right) {
        return !(left == right);
    }
}
