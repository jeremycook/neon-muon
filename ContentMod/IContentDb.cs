using DataCore;

namespace ContentMod;

public interface IContentDb : IDb<IContentDb>
{
    IQuery<IContentDb, AuthorInfo> AuthorInfo { get; }
    IQuery<IContentDb, ContentTitle> ContentTitle { get; }
    IQuery<IContentDb, HtmlBody> HtmlBody { get; }
    IQuery<IContentDb, PublicationInfo> PublicationInfo { get; }

    //public async ValueTask CreateAsync(LocalLogin component, CancellationToken cancellationToken)
    //{
    //    Set<LocalLogin>().Add(component);
    //    await SaveChangesAsync(cancellationToken);
    //}
}
