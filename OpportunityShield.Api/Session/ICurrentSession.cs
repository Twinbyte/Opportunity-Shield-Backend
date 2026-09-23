namespace OpportunityShield.Api.Session;

/// <summary>
/// Gives controllers/services access to the anonymous session id for the
/// current request without reaching into HttpContext directly.
/// </summary>
public interface ICurrentSession
{
    string SessionId { get; }
}
