using System.ComponentModel.DataAnnotations;

namespace ContentMod;

public class AuthorInfo
{
    [Key]
    public Guid UserId { get; set; }
}
