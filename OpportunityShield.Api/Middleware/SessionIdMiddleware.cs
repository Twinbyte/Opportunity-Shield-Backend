namespace OpportunityShield.Api.Middleware;


public class SessionIdMiddleware
{
    public const string HeaderName = "X-Session-Id";
    public const string ItemKey = "SessionId";

    private readonly RequestDelegate _next;

    public SessionIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sessionId = context.Request.Headers[HeaderName].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(sessionId) || !Guid.TryParse(sessionId, out _))
        {
            sessionId = Guid.NewGuid().ToString();
            context.Response.Headers[HeaderName] = sessionId;
        }

        context.Items[ItemKey] = sessionId;

        await _next(context);
    }
}
