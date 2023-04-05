namespace DataCore;

public enum QueryType {
    From = 100,
    Join = 101,
    Filter = 102,
    Sort = 103,
    Skip = 104,
    Take = 105,
    Map = 106,

    Insert = 200,
    Update = 201,
    Delete = 202,
}
