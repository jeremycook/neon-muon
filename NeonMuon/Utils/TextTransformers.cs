using System.Text;

namespace NeonMuon.Utils;

public static class TextTransformers
{
    private static readonly Rune dash = new('-');
    private static readonly Rune underscore = new('_');

    /// <summary>
    /// Transforms camelCase and PascalCase to dash-case.
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public static string Dashify(string text)
    {
        var sb = new List<Rune>();

        bool lastWasLower = false;
        foreach (var rune in text.EnumerateRunes())
        {
            if (Rune.IsUpper(rune))
            {
                if (lastWasLower)
                {
                    sb.Add(dash);
                }
                sb.Add(Rune.ToLowerInvariant(rune));
            }
            else
            {
                sb.Add(rune);
            }

            lastWasLower = Rune.IsLower(rune);
        }

        return string.Concat(sb);
    }

    /// <summary>
    /// Transforms camelCase and PascalCase to snake_case.
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public static string Snakify(string text)
    {
        var sb = new List<Rune>();

        bool lastWasLower = false;
        foreach (var rune in text.EnumerateRunes())
        {
            if (Rune.IsUpper(rune))
            {
                if (lastWasLower)
                {
                    sb.Add(underscore);
                }
                sb.Add(Rune.ToLowerInvariant(rune));
            }
            else
            {
                sb.Add(rune);
            }

            lastWasLower = Rune.IsLower(rune);
        }

        return string.Concat(sb);
    }
}
