using ContentMod;
using DataCore;
using DataCore.EF;

namespace ContentServer;

public class ContentDb : Db<IContentDb>, IContentDb
{
    public ContentDb(IComponentDbContext<IContentDb> dbContext)
        : base(dbContext) { }

    public IQuery<IContentDb, AuthorInfo> AuthorInfo => From<AuthorInfo>();
    public IQuery<IContentDb, ContentTitle> ContentTitle => From<ContentTitle>();
    public IQuery<IContentDb, HtmlBody> HtmlBody => From<HtmlBody>();
    public IQuery<IContentDb, PublicationInfo> PublicationInfo => From<PublicationInfo>();
}
