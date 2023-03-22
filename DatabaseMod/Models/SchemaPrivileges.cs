using System.Text.RegularExpressions;

namespace DatabaseMod.Models;

//GRANT ALL ON SCHEMA "Finance" TO "t:DB1:d";
//GRANT USAGE ON SCHEMA "Finance" TO "t:DB1:m";
//GRANT USAGE ON SCHEMA "Finance" TO "t:DB1:q";

public class SchemaPrivileges
{
    /// <summary>
    /// Examples privileges: "ALL", "CREATE", "USAGE" or "CREATE, USAGE".
    /// </summary>
    /// <param name="grantee">Recipient of privileges.</param>
    /// <param name="privileges">Granted privileges. Examples: "ALL", "CREATE", "USAGE" or "CREATE, USAGE".</param>
    public SchemaPrivileges(string grantee, string privileges)
    {
        Grantee = grantee;
        this.privileges = privileges;
    }

    public string Grantee { get; }

    private readonly string privileges;
    private bool privilegesIsValid;
    /// <summary>
    /// Examples: "ALL", "CREATE", "USAGE" or "CREATE, USAGE".
    /// </summary>
    public string Privileges
    {
        get
        {
            if (privilegesIsValid)
            {
                return privileges;
            }
            else if (Regex.IsMatch(privileges, "^[A-Z][A-Z, ][A-Z]+$"))
            {
                privilegesIsValid = true;
                return privileges;
            }
            else
            {
                throw new InvalidOperationException($"The value \"{privileges}\" of {nameof(Privileges)} is invalid.");
            }
        }
    }
}