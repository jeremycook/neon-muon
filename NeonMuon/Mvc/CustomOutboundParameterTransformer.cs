namespace NeonMuon.Mvc;

public class CustomOutboundParameterTransformer : IOutboundParameterTransformer
{
    private readonly Func<string, string> transformer;

    public CustomOutboundParameterTransformer(Func<string, string> transformer)
    {
        this.transformer = transformer;
    }

    public string? TransformOutbound(object? value)
    {
        if (value is string text)
        {
            return transformer(text);
        }
        else
        {
            return null;
        }
    }
}