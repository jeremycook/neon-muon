using DatabaseMod.Models;

namespace DatabaseMod
{
    public class DatabaseVisitor
    {
        private readonly Action<object> visitor;

        public DatabaseVisitor(Action<object> visitor)
        {
            this.visitor = visitor;
        }

        public void Visit(Database database)
        {
            visitor(database);

            foreach (var schema in database.Schemas)
            {
                visitor(schema);
                foreach (var defaultPrivileges in schema.DefaultPrivileges)
                {
                    visitor(defaultPrivileges);
                }
                foreach (var privileges in schema.Privileges)
                {
                    visitor(privileges);
                }
                foreach (var table in schema.Tables)
                {
                    visitor(table);
                    foreach (var column in table.Columns)
                    {
                        visitor(column);
                    }
                    foreach (var index in table.Indexes)
                    {
                        visitor(index);
                    }
                }
            }
        }
    }
}
