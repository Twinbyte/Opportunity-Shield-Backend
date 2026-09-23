
using OpportunityShield.Api.Middleware;

namespace OpportunityShield.Api.Session;

public class CurrentSession : ICurrentSession
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentSession(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string SessionId =>
        _httpContextAccessor.HttpContext?.Items[SessionIdMiddleware.ItemKey] as string
        ?? throw new InvalidOperationException(
            "Session id not found on HttpContext. Make sure SessionIdMiddleware is registered before this is used.");
}
