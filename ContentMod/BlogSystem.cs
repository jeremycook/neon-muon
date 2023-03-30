namespace ContentMod;

public class BlogSystem
{
    private readonly IContentDb contentDb;

    public BlogSystem(IContentDb contentDb)
    {
        this.contentDb = contentDb;
    }

    public async Task<List<(string Title, string Body)>> GetObjectAsync()
    {
        var list = await contentDb
            .From(db => db.ContentTitle)
            .Join(db => db.HtmlBody, (ct, hb) => ct.ContentId == hb.ContentId)
            .Asc(ct_hb => ct_hb.Item1.Title)
            .Map(ct_hb => ValueTuple.Create(ct_hb.Item1.Title, ct_hb.Item2.Body))
            .ToListAsync();

        return list;
    }
}
