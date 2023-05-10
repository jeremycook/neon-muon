using DatabaseMod.Models;
using DataCore;

namespace ContentMod;

public sealed class ContentContext {
    public static FromQuery<ContentContext, AuthorInfo> AuthorInfos => new();
    public static FromQuery<ContentContext, ContentTitle> ContentTitles => new();
    public static FromQuery<ContentContext, HtmlBody> HtmlBodies => new();
    public static FromQuery<ContentContext, PublicationInfo> PublicationInfos => new();

    public static IReadOnlyDatabase<ContentContext> Database { get; }
    static ContentContext() {
        var database = new Database<ContentContext>();
        database.ContributeQueryContext();
        Database = database;
    }

    private ContentContext() { throw new InvalidOperationException("Cannot construct. Static reference only."); }
}
