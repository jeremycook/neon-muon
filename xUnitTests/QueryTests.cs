using DataCore;
using DataMod.Sqlite;
using Microsoft.Data.Sqlite;

namespace xUnitTests;

public class QueryTests {

    private readonly record struct Component(Guid UserId, string Username);
    private readonly record struct Dto(Guid UserId, string Username);

    private static readonly SqliteConnection Connection = CreateConnection();
    private static readonly SqliteQueryRunner<UserContext> Runner = new(Composer, new StaticDbConnectionPool<UserContext, SqliteConnection>(Connection));

    [Fact]
    public async Task Join() {
        List<(User, UserRole, Role)> list = await Runner.List(
            UserContext.Users
            .Join(UserContext.UserRoles, (User user, UserRole userRole) => user.UserId == userRole.UserId)
            .Join(UserContext.Roles, (User user, UserRole userRole, Role role) => userRole.RoleId == role.RoleId))
            //.Filter(((User user, UserRole userRole, Role role) t) => t.user.Username == "Alice"))
            ;

        Assert.Single(list);
        Assert.Equal((Users["Alice"], UserRoles.Single(x => x.UserId == Users["Alice"].UserId && x.RoleId == Roles["Admin"].RoleId), Roles["Admin"]), list[0]);
    }

    [Fact]
    public async Task Filter() {
        var list = await Runner.List(UserContext
            .Users
            .Filter(o => o.Username == "Jeremy"));

        Assert.Single(list);
        Assert.Equal(Users["Jeremy"], list[0]);
    }

    [Fact]
    public async Task MapProperty() {
        // Act

        var list = await Runner.List(UserContext
            .Users
            .Filter(o => o.Username == "Jeremy")
            .Map(o => o.UserId));

        var jeremyId = list.Single();

        Assert.Equal(Users["Jeremy"].UserId, jeremyId);
    }

    [Fact]
    public async Task MapConstructor() {
        // Act

        var dto = await Runner.Nullable(UserContext
            .Users
            .Filter(o => o.Username == "Jeremy")
            .Map(o => new Dto(o.UserId, o.Username)));

        Assert.Equal(new Dto(Users["Jeremy"].UserId, Users["Jeremy"].Username), dto);
    }

    [Fact]
    public async Task MapTuple() {
        // Act

        var tuple = await Runner.Nullable(UserContext
            .Users
            .Filter(o => o.Username == "Jeremy")
            .Map(o => ValueTuple.Create(o.UserId, o.Username)));

        Assert.Equal((Users["Jeremy"].UserId, Users["Jeremy"].Username), tuple);
    }
}