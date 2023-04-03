using System.Data.Common;

namespace DataCore;

/// <summary>
/// Convert an <see cref="IQuery{TDb}"/> into a <see cref="DbCommand"/>.
/// </summary>
/// <typeparam name="TDb"></typeparam>
public interface IDbCommandComposer<TDb> {

    /// <summary>
    /// Convert <paramref name="query"/> into a <see cref="DbCommand"/>.
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    DbCommand CreateCommand(IQuery<TDb> query);
}
