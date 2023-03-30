using System.ComponentModel.DataAnnotations;

namespace ContentMod;

public class ContentTitle
{
    [Key]
    public Guid ContentId { get; set; }

    [Required]
    public string Title { get; set; }
}
