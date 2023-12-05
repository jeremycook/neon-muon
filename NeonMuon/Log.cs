namespace NeonMuon;

public static class Log
{
    private static ILoggerFactory? _factory;

    /// <summary>
    /// Configure at application start similar to this:
    /// <c>Log.Factory = LoggerFactory.Create(options => options.AddConfiguration(builder.Configuration.GetSection("Logging")).AddConsole())</c>
    /// </summary>
    public static ILoggerFactory Factory
    {
        get => _factory ?? throw new NullReferenceException("Th>e Factory has not be set yet. The calling program should set it.");
        set
        {
            if (_factory is null)
            {
                _factory = value;
            }
            else
            {
                try
                {
                    throw new InvalidOperationException("The Factory has already been set. It can only be set once.");
                }
                catch (Exception ex)
                {
                    // To get the call stack.
                    _factory.CreateLogger(typeof(Log)).LogWarning(ex, "Suppressed {ExceptionType}: {ExceptionMessage}", ex.GetType(), ex.Message);
                }
            }
        }
    }


    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2254:Template should be a static expression", Justification = "Don't care")]
    public static void Critical(Type type, Exception? exception, string message, params object?[] args)
    {
        Factory.CreateLogger(type).LogCritical(exception, message, args);
    }


    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2254:Template should be a static expression", Justification = "Don't care")]
    public static void Debug<T>(string message, params object?[] args)
    {
        Factory.CreateLogger<T>().LogDebug(message, args);
    }


    public static void Error<T>(Exception? exception, string message, params object?[] args)
    {
        Error(typeof(T), exception, message, args);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2254:Template should be a static expression", Justification = "Don't care")]
    public static void Error(Type type, Exception? exception, string message, params object?[] args)
    {
        Factory.CreateLogger(type).LogError(exception, message, args);
    }

    public static void SuppressedError<T>(Exception ex)
    {
        SuppressedError(typeof(T), ex);
    }

    public static void SuppressedError(Type type, Exception ex)
    {
        Error(type, ex, "Suppressed {ExceptionType}: {ExceptionMessage}", ex.GetBaseException().GetType(), ex.GetBaseException().Message);
    }


    public static void SuppressedWarn<T>(Exception ex, string message = "Suppressed {ExceptionType}: {ExceptionMessage}")
    {
        SuppressedWarn(typeof(T), ex, message);
    }

    public static void SuppressedWarn(Type type, Exception ex, string message = "Suppressed {ExceptionType}: {ExceptionMessage}")
    {
        Warn(type, ex, message, ex.GetBaseException().GetType(), ex.GetBaseException().Message);
    }


    public static void Info<T>(Exception? exception, string message, params object?[] args)
    {
        Info(typeof(T), exception, message, args);
    }

    public static void Info<T>(string message, params object?[] args)
    {
        Info(typeof(T), null, message, args);
    }

    public static void Info(Type type, string message, params object?[] args)
    {
        Info(type, null, message, args);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2254:Template should be a static expression", Justification = "Don't care")]
    public static void Info(Type type, Exception? exception, string message, params object?[] args)
    {
        Factory.CreateLogger(type).LogInformation(exception, message, args);
    }


    public static void Warn<T>(Exception? exception, string message, params object?[] args)
    {
        Warn(typeof(T), exception, message, args);
    }

    public static void Warn(Type type, string message, params object?[] args)
    {
        Warn(type, null, message, args);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2254:Template should be a static expression", Justification = "Don't care")]
    public static void Warn(Type type, Exception? exception, string message, params object?[] args)
    {
        Factory.CreateLogger(type).LogWarning(exception, message, args);
    }
}
