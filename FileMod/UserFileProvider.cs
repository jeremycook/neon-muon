using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;

namespace FileMod;

public class UserFileProvider : PhysicalFileProvider {
    public UserFileProvider(string root)
        : base(root) { }

    public UserFileProvider(string root, ExclusionFilters filters)
        : base(root, filters) { }
}
