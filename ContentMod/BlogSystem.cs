using DataCore;

namespace ContentMod;

public class BlogSystem {
    private readonly IContentDb contentDb;
    private readonly IQueryComposer<IContentDb> orchestrator;

    public BlogSystem(IContentDb contentDb, IQueryComposer<IContentDb> orchestrator) {
        this.contentDb = contentDb;
        this.orchestrator = orchestrator;
    }

    public async Task<List<(string Title, string Body)>> GetObjectAsync() {
        var list = await contentDb
            .ContentTitle
            .Join(contentDb.HtmlBody, (ct, hb) => ct.ContentId == hb.ContentId)
            .Asc(ct_hb => ct_hb.Item1.Title)
            .Map(ct_hb => ValueTuple.Create(ct_hb.Item1.Title, ct_hb.Item2.Body))
            .ToListAsync(orchestrator);

        return list;
    }
}
