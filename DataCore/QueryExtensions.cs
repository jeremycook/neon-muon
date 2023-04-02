﻿using System.Linq.Expressions;

namespace DataCore;

public static class QueryExtensions {
    public static IQuery<TDb, (T1, T2)> Join<TDb, T1, T2>(this IQuery<TDb, T1> query1, IQuery<TDb, T2> query2, Expression<Func<T1, T2, bool>> condition) {
        return new JoinQuery<TDb, T1, T2>(query1, query2, condition);
    }

    public static IQuery<TDb, T1> Filter<TDb, T1>(this IQuery<TDb, T1> query, Expression<Func<T1, bool>> condition) {
        return new FilterQuery<TDb, T1>(query, condition);
    }

    public static IQuery<TDb, T1> Asc<TDb, T1, TProperty>(this IQuery<TDb, T1> query, Expression<Func<T1, TProperty>> sort)
        where TProperty : IComparable<TProperty> {

        return new SortQuery<TDb, T1, TProperty>(query, sort, SortDirection.Asc);
    }

    public static IQuery<TDb, T1> Desc<TDb, T1, TProperty>(this IQuery<TDb, T1> query, Expression<Func<T1, TProperty>> sort)
        where TProperty : IComparable<TProperty> {

        return new SortQuery<TDb, T1, TProperty>(query, sort, SortDirection.Desc);
    }

    public static IQuery<TDb, T1> Take<TDb, T1>(this IQuery<TDb, T1> query, int take) {

        return new TakeQuery<TDb, T1>(query, take);
    }

    public static IQuery<TDb, TMapped> Map<TDb, T1, TMapped>(this IQuery<TDb, T1> query, Expression<Func<T1, TMapped>> map) {
        return new MapQuery<TDb, T1, TMapped>(query, map);
    }

    public static IQuery<TDb, T1> Produce<TDb, T1>(this IQuery<TDb, T1> query) {
        return new ProduceQuery<TDb, T1>(query);
    }

    public static async ValueTask<List<T1>> ToListAsync<TDb, T1>(this IQuery<TDb, T1> query, IQueryComposer<TDb> composer, CancellationToken cancellationToken = default) {
        var commands = composer.Compose(query);

        if (commands.Count < 1) {
            throw new Exception();
        }

        // Process commands that preceded the Produce we just added
        foreach (var command in commands.Take(commands.Count - 1)) {
            await command.ExecuteAsync(cancellationToken);
        }

        // Now process the Produce we just added
        var lastCommand = commands.Last();
        await lastCommand.ExecuteAsync(cancellationToken);
        if (lastCommand.Response is IEnumerable<T1> list) {
            return list.ToList();
        }

        throw new Exception();
    }

    public static async ValueTask<T1> ToItemAsync<TDb, T1>(this IQuery<TDb, T1> query, IQueryComposer<TDb> composer, CancellationToken cancellationToken = default) {
        var list = await query
            .Take(2)
            .ToListAsync(composer, cancellationToken);

        return list.Single();
    }

    public static async ValueTask<T1?> ToOptionalAsync<TDb, T1>(this IQuery<TDb, T1> query, IQueryComposer<TDb> composer, CancellationToken cancellationToken = default)
        where T1 : struct {
        var list = await query
            .Take(2)
            .ToListAsync(composer, cancellationToken);

        return list.Any() ? list.Single() : new T1?();
    }


    public static IQuery<TDb, T1> Insert<TDb, T1>(this IQuery<TDb, T1> query, T1 item) {
        return InsertQuery.Create(query, item);
    }

    public static IQuery<TDb, T1> InsertRange<TDb, T1>(this IQuery<TDb, T1> query, params T1[] items) {
        return InsertQuery.Create(query, items);
    }

    public static IQuery<TDb, T1> Update<TDb, T1>(this IQuery<TDb, T1> query, Expression<Func<T1, T1>> update) {
        throw new NotImplementedException();
    }

    public static async ValueTask<int> ExecuteAsync<TDb, T1>(this IQuery<TDb, T1> query, IQueryComposer<TDb> composer, CancellationToken cancellationToken = default) {
        var commands = composer.Compose(query);

        if (commands.Count <= 0) {
            throw new Exception();
        }

        // Process commands that preceded the Produce we just added
        foreach (var command in commands.Take(commands.Count - 1)) {
            await command.ExecuteAsync(cancellationToken);
        }

        // Now process the Produce we just added
        var lastCommand = commands.Last();
        await lastCommand.ExecuteAsync(cancellationToken);
        if (lastCommand.Response is int modifications) {
            return modifications;
        }

        throw new Exception();
    }
}
