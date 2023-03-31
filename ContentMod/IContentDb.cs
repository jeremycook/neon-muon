using DataCore;

namespace ContentMod;

public interface IContentDb {
    IQuery<IContentDb, AuthorInfo> AuthorInfo { get; }
    IQuery<IContentDb, ContentTitle> ContentTitle { get; }
    IQuery<IContentDb, HtmlBody> HtmlBody { get; }
    IQuery<IContentDb, PublicationInfo> PublicationInfo { get; }
}

public class ContentDb : IContentDb {
    public IQuery<IContentDb, AuthorInfo> AuthorInfo => new FromQuery<IContentDb, AuthorInfo>();

    public IQuery<IContentDb, ContentTitle> ContentTitle => new FromQuery<IContentDb, ContentTitle>();

    public IQuery<IContentDb, HtmlBody> HtmlBody => new FromQuery<IContentDb, HtmlBody>();

    public IQuery<IContentDb, PublicationInfo> PublicationInfo => new FromQuery<IContentDb, PublicationInfo>();
}
