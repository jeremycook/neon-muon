using NeonMS.Authentication;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Text.Json;

namespace NeonMS.DataAccess;

public class CurrentCredentials : IReadOnlyDictionary<string, ConnectionCredential>
{
    readonly Dictionary<string, ConnectionCredential> _connections = new(StringComparer.OrdinalIgnoreCase);

    public CurrentCredentials(ClaimsPrincipal principal)
    {
        if (principal.FindFirstValue("cns") is string cns &&
            JsonSerializer.Deserialize<Dictionary<string, ConnectionCredential>>(cns) is Dictionary<string, ConnectionCredential> creds)
        {
            foreach (var (key, value) in creds)
            {
                _connections.Add(key, value);
            }
        }
    }

    public ConnectionCredential this[string key] => ((IReadOnlyDictionary<string, ConnectionCredential>)_connections)[key];

    public IEnumerable<string> Keys => ((IReadOnlyDictionary<string, ConnectionCredential>)_connections).Keys;

    public IEnumerable<ConnectionCredential> Values => ((IReadOnlyDictionary<string, ConnectionCredential>)_connections).Values;

    public int Count => ((IReadOnlyCollection<KeyValuePair<string, ConnectionCredential>>)_connections).Count;

    public bool ContainsKey(string key)
    {
        return ((IReadOnlyDictionary<string, ConnectionCredential>)_connections).ContainsKey(key);
    }

    public IEnumerator<KeyValuePair<string, ConnectionCredential>> GetEnumerator()
    {
        return ((IEnumerable<KeyValuePair<string, ConnectionCredential>>)_connections).GetEnumerator();
    }

    public bool TryGetValue(string key, [MaybeNullWhen(false)] out ConnectionCredential value)
    {
        return ((IReadOnlyDictionary<string, ConnectionCredential>)_connections).TryGetValue(key, out value);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_connections).GetEnumerator();
    }
}
