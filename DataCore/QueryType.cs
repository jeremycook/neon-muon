namespace DataCore;

public record struct QueryType {
    public static implicit operator QueryType(string name) => new(name);

    public string Name { get; }

    public QueryType(string name) =>
        Name = name;
    public static QueryType Create(string name) =>
        cache.TryGetValue(name, out var value) ? value : cache[name] = new(name);

    public const string Filter = "Filter";
    public const string From = "From";
    public const string Join = "Join";
    public const string Map = "Map";
    public const string Produce = "Produce";
    public const string Sort = "Sort";
    public const string Take = "Take";

    private static readonly Dictionary<string, QueryType> cache = new();
}