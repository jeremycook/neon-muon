namespace DatabaseMod.Models;

public interface IDatabase<TDb> {
}

public class Database<TDb> : Database, IDatabase<TDb> {
}
