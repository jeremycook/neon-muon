namespace NeonMuon.Utils;

public class ScopedLazy<T> where T : class
{
    private readonly IServiceProvider provider;

    public ScopedLazy(IServiceProvider provider)
    {
        this.provider = provider;
    }

    public T Value => provider.GetRequiredService<T>();
}
