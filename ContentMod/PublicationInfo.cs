using System.ComponentModel.DataAnnotations;

namespace ContentMod;

public class PublicationInfo
{
    [Key]
    public Guid ContentId { get; set; }

    [Required]
    public DateTime Published { get; set; }
}
