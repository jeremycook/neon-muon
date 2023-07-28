using System.Collections.Immutable;

namespace DatabaseMod.Models;

public enum StoreType {
    General = 0,
    Text,
    Blob,
    Boolean,
    Numeric,
    Date,
    Integer,
    Real,
    Time,
    Timestamp,
    Uuid,
}

public static class StoreTypeHelpers {

    public static IImmutableDictionary<StoreType, Type> StoreToClrMap { get; } = new Dictionary<StoreType, Type>() {
        { StoreType.General, typeof(object) },
        { StoreType.Text, typeof(string) },
        { StoreType.Blob, typeof(byte[]) },
        { StoreType.Boolean, typeof(bool) },
        { StoreType.Date, typeof(DateOnly) },
        { StoreType.Integer, typeof(int) },
        { StoreType.Numeric, typeof(decimal) },
        { StoreType.Real, typeof(double) },
        { StoreType.Time, typeof(TimeOnly) },
        { StoreType.Timestamp, typeof(DateTime) },
        { StoreType.Uuid, typeof(Guid) },
    }.ToImmutableDictionary();

    public static IImmutableDictionary<Type, StoreType> ClrToStoreMap { get; } = StoreToClrMap
        .ToImmutableDictionary(o => o.Value, o => o.Key);

    public static StoreType ConvertClrTypeToStoreType(Type clrType) {
        var type = Nullable.GetUnderlyingType(clrType) ?? clrType;
        return ClrToStoreMap.TryGetValue(type, out var storeType)
            ? storeType
            : throw new NotImplementedException(type.FullName ?? type.Name);
    }

    public static Type ConvertStoreToClr(StoreType storeType) {
        return StoreToClrMap.TryGetValue(storeType, out var type)
            ? type
            : throw new NotImplementedException(storeType.ToString());
    }
}
