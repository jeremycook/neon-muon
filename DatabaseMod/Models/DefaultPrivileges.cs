using System.Text.RegularExpressions;

namespace DatabaseMod.Models
{
    // ALTER DEFAULT PRIVILEGES FOR ROLE postgres IN SCHEMA "Finance"
    //  GRANT ALL ON TABLES TO "t:DB1:d";
    // ALTER DEFAULT PRIVILEGES FOR ROLE postgres IN SCHEMA "Finance"
    //  GRANT ALL ON TABLES TO "t:DB1:m";
    // ALTER DEFAULT PRIVILEGES FOR ROLE postgres IN SCHEMA "Finance"
    //  GRANT SELECT ON TABLES TO "t:DB1:q";

    // ALTER DEFAULT PRIVILEGES FOR ROLE postgres IN SCHEMA "Finance"
    //  GRANT ALL ON SEQUENCES TO "t:DB1:d";
    // ALTER DEFAULT PRIVILEGES FOR ROLE postgres IN SCHEMA "Finance"
    //  GRANT SELECT ON SEQUENCES TO "t:DB1:m";

    // ALTER DEFAULT PRIVILEGES FOR ROLE postgres IN SCHEMA "Finance"
    //  GRANT EXECUTE ON FUNCTIONS TO "t:DB1:d";
    // ALTER DEFAULT PRIVILEGES FOR ROLE postgres IN SCHEMA "Finance"
    //  GRANT EXECUTE ON FUNCTIONS TO "t:DB1:m";
    // ALTER DEFAULT PRIVILEGES FOR ROLE postgres IN SCHEMA "Finance"
    //  GRANT EXECUTE ON FUNCTIONS TO "t:DB1:q";

    /// <summary>
    /// See https://www.postgresql.org/docs/14/sql-alterdefaultprivileges.html
    /// </summary>
    public class DefaultPrivileges
    {
        public DefaultPrivileges(string grantee, DefaultPrivilegesEnum objectType, string privileges)
        {
            this.privileges = privileges;
            this.objectType = objectType.ToString().ToUpperInvariant();
            Grantee = grantee;
        }

        private readonly string privileges;
        private bool privilegesIsValid;
        /// <summary>
        /// Examples: ALL, SELECT INSERT, EXECUTE
        /// </summary>
        public string Privileges
        {
            get
            {
                if (privilegesIsValid)
                {
                    return privileges;
                }
                else if (Regex.IsMatch(privileges, "^[A-Z, ]+$"))
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

        private readonly string objectType;
        private bool objectTypeIsValid;
        /// <summary>
        /// Examples: TABLES, SEQUENCES, FUNCTIONS, TYPES
        /// </summary>
        public string ObjectType
        {
            get
            {
                if (objectTypeIsValid)
                {
                    return objectType;
                }
                else if (Regex.IsMatch(objectType, "^[A-Z]+$"))
                {
                    objectTypeIsValid = true;
                    return objectType;
                }
                else
                {
                    throw new InvalidOperationException($"The value \"{objectType}\" of {nameof(ObjectType)} is invalid.");
                }
            }
        }

        public string Grantee { get; }
    }

}