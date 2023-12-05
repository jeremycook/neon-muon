using Microsoft.IdentityModel.Tokens;
using System.Collections.Immutable;

namespace NeonMuon.Security;

public class Keys
{
    public Keys(byte[][] signingKeys, byte[][] decryptionKeys)
    {
        SigningKeys = signingKeys
            .Select(data => new SymmetricSecurityKey(data))
            .ToImmutableArray();

        SigningKey = SigningKeys[0];

        DecryptionKeys = decryptionKeys
            .Select(data => new SymmetricSecurityKey(data))
            .ToImmutableArray();

        EncryptingKey = DecryptionKeys[0];
    }

    public ImmutableArray<SymmetricSecurityKey> SigningKeys { get; }
    public SymmetricSecurityKey SigningKey { get; }
    public ImmutableArray<SymmetricSecurityKey> DecryptionKeys { get; }
    public SymmetricSecurityKey EncryptingKey { get; }
}
