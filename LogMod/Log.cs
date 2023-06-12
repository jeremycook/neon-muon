using Microsoft.Extensions.Logging;

namespace LogMod;

public class Log {
    public static ILoggerFactory Factory { get; set; } = new ThrowingLoggerFactory();

    public static ILogger CreateLogger(Type type) {
        return Factory.CreateLogger(type);
    }

    private class ThrowingLoggerFactory : ILoggerFactory {
        public void AddProvider(ILoggerProvider provider) { }

        public ILogger CreateLogger(string categoryName) {
            throw new Exception("The Log.Factory is not ready to be used because it has not been configured.");
        }

        public void Dispose() { }
    }
}
