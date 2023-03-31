using System.Collections.Immutable;

namespace DatabaseMod.Models;

public enum StoreType {
    Unknown = 0,
    Blob,
    Boolean,
    Double,
    Guid,
    Integer,
    Text,
    Timestamp,
}

public static class StoreTypeHelpers {

    public static IImmutableDictionary<StoreType, Type> StoreToClrMap { get; } = new Dictionary<StoreType, Type>() {
        { StoreType.Blob, typeof(byte[]) },
        { StoreType.Boolean, typeof(bool) },
        { StoreType.Double, typeof(double) },
        { StoreType.Guid, typeof(Guid) },
        { StoreType.Integer, typeof(int) },
        { StoreType.Text, typeof(string) },
        { StoreType.Timestamp, typeof(DateTime) },
    }.ToImmutableDictionary();

    public static IImmutableDictionary<Type, StoreType> ClrToStoreMap { get; } = new Dictionary<Type, StoreType>() {
        { typeof(bool), StoreType.Boolean },
        { typeof(byte[]), StoreType.Blob },
        { typeof(DateTime), StoreType.Timestamp },
        { typeof(double), StoreType.Double },
        { typeof(Guid), StoreType.Guid },
        { typeof(int), StoreType.Integer },
        { typeof(string), StoreType.Text },
    }.ToImmutableDictionary();

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
