using System.Diagnostics.CodeAnalysis;

namespace WebApiApp;

public static class PublicAppExtensions
{
    public static RouteHandlerBuilder Publish<TService>(this IEndpointRouteBuilder endpoints, Action<TService, HttpContext> handler, [StringSyntax("Route")] string? pattern = null)
    {
        throw new NotImplementedException();
    }
}
