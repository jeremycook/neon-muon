using DataCore;
using LoginMod;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace UnitTests;

[TestClass]
public class DataTests {
    [TestMethod]
    public async Task DataTest1() {
        using var scope = DependencyInjector.Services.CreateScope();
        var services = scope.ServiceProvider;
        var loginDb = services.GetRequiredService<ILoginDb>();
        var orchestrator = services.GetRequiredService<IQueryOrchestrator<ILoginDb>>();

        var from = loginDb.LocalLogin;
        var produce = from.Produce();
        var commands = produce.Compose(orchestrator);

        foreach (IQueryCommand<object> command in commands) {
            await command.ExecuteAsync();
            Console.WriteLine(JsonConvert.SerializeObject(command));
        }
    }
}
