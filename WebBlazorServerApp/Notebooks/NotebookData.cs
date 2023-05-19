namespace WebBlazorServerApp.Notebooks;

public class Notebook {
    public Notebook(string Name, List<Page> Pages) {
        this.Name = Name;
        this.Pages = Pages.ToList();
    }

    public string Name { get; set; }
    //public List<Table> Tables { get; } = new();
    public List<Page> Pages { get; } = new();
}

public class Page {
    public Page(string Title, List<Content> Contents) {
        this.Title = Title;
        this.Contents = Contents.ToList();
    }

    public string Title { get; set; }
    public List<Content> Contents { get; } = new();
}

public interface Content {
    string Type { get; }
}

public record class HtmlContent : Content {
    public HtmlContent(string Content) {
        this.Content = Content;
    }

    public string Type { get; } = nameof(HtmlContent);
    public string Content { get; }
}

public record class TableContent : Content {
    public TableContent(string Name) {
        this.Name = Name;
    }

    public string Type { get; } = nameof(TableContent);
    public string Name { get; }
}
