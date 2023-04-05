using DataCore;

namespace ContentMod;

public class BlogSystem {
    private readonly IQueryRunner<ContentContext> Runner;

    public BlogSystem(IQueryRunner<ContentContext> runner) {
        Runner = runner;
    }

    public async Task<List<(string Title, string Body)>> GetObjectAsync() {
        var list = await Runner.List(ContentContext
            .ContentTitles
            .Join(ContentContext.HtmlBodies, (ct, hb) => ct.ContentId == hb.ContentId)
            .Asc(((ContentTitle Title, HtmlBody Body) t) => t.Title.Title)
            .Map(((ContentTitle Title, HtmlBody Body) t) => ValueTuple.Create(t.Title.Title, t.Body.Body)));

        return list;
    }
}
