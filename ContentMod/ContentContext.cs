using DatabaseMod.Models;
using DataCore;

namespace ContentMod;

public sealed class ContentContext {
    public static IQuery<ContentContext, AuthorInfo> AuthorInfos => new FromQuery<ContentContext, AuthorInfo>();
    public static IQuery<ContentContext, ContentTitle> ContentTitles => new FromQuery<ContentContext, ContentTitle>();
    public static IQuery<ContentContext, HtmlBody> HtmlBodies => new FromQuery<ContentContext, HtmlBody>();
    public static IQuery<ContentContext, PublicationInfo> PublicationInfos => new FromQuery<ContentContext, PublicationInfo>();

    public static IReadOnlyDatabase<ContentContext> Database { get; }
    static ContentContext() {
        var database = new Database<ContentContext>();
        database.ContributeQueryContext();
        Database = database;
    }

    private ContentContext() { throw new InvalidOperationException("Static only"); }
}
