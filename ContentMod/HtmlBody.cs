using System.ComponentModel.DataAnnotations;

namespace ContentMod;

public class HtmlBody
{
    [Key]
    public Guid ContentId { get; set; }

    [Required, DataType(DataType.Html)]
    public string Body { get; set; }
}
