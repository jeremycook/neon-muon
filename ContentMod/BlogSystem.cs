using DataCore;

namespace ContentMod;

public class BlogSystem {
    private readonly IContentDb contentDb;
    private readonly IQueryRunner<IContentDb> runner;

    public BlogSystem(IContentDb contentDb, IQueryRunner<IContentDb> runner) {
        this.contentDb = contentDb;
        this.runner = runner;
    }

    public async Task<List<(string Title, string Body)>> GetObjectAsync() {
        var list = await runner.List(contentDb
            .ContentTitle
            .Join(contentDb.HtmlBody, (ct, hb) => ct.ContentId == hb.ContentId)
            .Asc(ct_hb => ct_hb.Item1.Title)
            .Map(ct_hb => ValueTuple.Create(ct_hb.Item1.Title, ct_hb.Item2.Body)));

        return list;
    }
}
