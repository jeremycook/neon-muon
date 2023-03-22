namespace DatabaseMod.Models
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// Generated with <code>Enum.GetNames<NpgsqlDbType>().Where(ndt => typeof(NpgsqlDbType).GetField(ndt)!.GetCustomAttribute<ObsoleteAttribute>() is null).Select(ndt => $"public const string {ndt} = \"{ndt.ToString().ToLower()}\";").Order()</code>
    /// </remarks>
    public static class StoreType
    {
        public const string Array = "array";
        public const string Bigint = "bigint";
        public const string BigIntMultirange = "bigintmultirange";
        public const string BigIntRange = "bigintrange";
        public const string Bit = "bit";
        public const string Boolean = "boolean";
        public const string Box = "box";
        public const string Bytea = "bytea";
        public const string Char = "char";
        public const string Cid = "cid";
        public const string Cidr = "cidr";
        public const string Circle = "circle";
        public const string Citext = "citext";
        public const string Date = "date";
        public const string DateMultirange = "datemultirange";
        public const string DateRange = "daterange";
        public const string Double = "double";
        public const string Geography = "geography";
        public const string Geometry = "geometry";
        public const string Hstore = "hstore";
        public const string Inet = "inet";
        public const string Int2Vector = "int2vector";
        public const string Integer = "integer";
        public const string IntegerMultirange = "integermultirange";
        public const string IntegerRange = "integerrange";
        public const string InternalChar = "internalchar";
        public const string Interval = "interval";
        public const string Json = "json";
        public const string Jsonb = "jsonb";
        public const string JsonPath = "jsonpath";
        public const string Line = "line";
        public const string LQuery = "lquery";
        public const string LSeg = "lseg";
        public const string LTree = "ltree";
        public const string LTxtQuery = "ltxtquery";
        public const string MacAddr = "macaddr";
        public const string MacAddr8 = "macaddr8";
        public const string Money = "money";
        public const string Multirange = "multirange";
        public const string Name = "name";
        public const string Numeric = "numeric";
        public const string NumericMultirange = "numericmultirange";
        public const string NumericRange = "numericrange";
        public const string Oid = "oid";
        public const string Oidvector = "oidvector";
        public const string Path = "path";
        public const string PgLsn = "pglsn";
        public const string Point = "point";
        public const string Polygon = "polygon";
        public const string Range = "range";
        public const string Real = "real";
        public const string Refcursor = "refcursor";
        public const string Regconfig = "regconfig";
        public const string Regtype = "regtype";
        public const string Smallint = "smallint";
        public const string Text = "text";
        public const string Tid = "tid";
        public const string Time = "time";
        public const string Timestamp = "timestamp";
        public const string TimestampMultirange = "timestampmultirange";
        public const string TimestampRange = "timestamprange";
        public const string TimestampTz = "timestamptz";
        public const string TimestampTzMultirange = "timestamptzmultirange";
        public const string TimestampTzRange = "timestamptzrange";
        public const string TimeTz = "timetz";
        public const string TsQuery = "tsquery";
        public const string TsVector = "tsvector";
        public const string Unknown = "unknown";
        public const string Uuid = "uuid";
        public const string Varbit = "varbit";
        public const string Varchar = "varchar";
        public const string Xid = "xid";
        public const string Xid8 = "xid8";
        public const string Xml = "xml";
    }
}
