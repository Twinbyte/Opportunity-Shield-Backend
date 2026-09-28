using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using OpportunityShield.Application.Collectors;



namespace Oppurtunityshield.Infrastructure.Collectors;

public class HttpPageContentFetcher : IPageContentFetcher
{
    private const int MaxRedirects = 3;
    private const int MaxBytes = 500_000;
    private const int MaxChars = 6_000;

    private readonly HttpClient _http;

    public HttpPageContentFetcher(HttpClient http)
    {
        _http = http;
        _http.Timeout = TimeSpan.FromSeconds(8);
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; OpportunityShieldBot/1.0)");
    }

    public async Task<string?> FetchTextAsync(string url, CancellationToken ct = default)
    {
        try
        {
            var current = new Uri(url);

           
            for (var hop = 0; hop <= MaxRedirects; hop++)
            {
                if (!await IsSafePublicHostAsync(current, ct)) return null;

                using var response = await _http.GetAsync(current, HttpCompletionOption.ResponseHeadersRead, ct);

                if ((int)response.StatusCode is >= 300 and < 400 && response.Headers.Location is { } location)
                {
                    current = location.IsAbsoluteUri ? location : new Uri(current, location);
                    continue;
                }

                if (!response.IsSuccessStatusCode) return null;

                var mediaType = response.Content.Headers.ContentType?.MediaType ?? "";
                if (!mediaType.Contains("html", StringComparison.OrdinalIgnoreCase)
                    && !mediaType.StartsWith("text/", StringComparison.OrdinalIgnoreCase))
                    return null; 

                return ExtractText(await ReadLimitedAsync(response, ct));
            }

            return null; 
        }
        catch
        {
            
            return null;
        }
    }

    private static async Task<string> ReadLimitedAsync(HttpResponseMessage response, CancellationToken ct)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        var buffer = new byte[MaxBytes];
        var total = 0;
        while (total < MaxBytes)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(total, MaxBytes - total), ct);
            if (read == 0) break;
            total += read;
        }
        return Encoding.UTF8.GetString(buffer, 0, total);
    }

    private static string? ExtractText(string html)
    {
        const RegexOptions opts = RegexOptions.Singleline | RegexOptions.IgnoreCase;

        var title = Regex.Match(html, @"<title[^>]*>(.*?)</title>", opts).Groups[1].Value;
        var metaDescription = Regex.Match(
            html, "<meta[^>]+name=[\"']description[\"'][^>]+content=[\"'](.*?)[\"']", opts).Groups[1].Value;

        var body = Regex.Replace(html, @"<(script|style|noscript|svg|head)\b.*?</\1>", " ", opts);
        body = Regex.Replace(body, @"<[^>]+>", " ");
        body = WebUtility.HtmlDecode(body);
        body = Regex.Replace(body, @"\s+", " ").Trim();

        var text = string.Join(" ", new[] { title, metaDescription, body }
            .Select(WebUtility.HtmlDecode)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!.Trim()));

        if (text.Length < 50) return null; 
        return text.Length > MaxChars ? text[..MaxChars] : text;
    }

    private static async Task<bool> IsSafePublicHostAsync(Uri uri, CancellationToken ct)
    {
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return false;

        var addresses = await Dns.GetHostAddressesAsync(uri.Host, ct);
        return addresses.Length > 0 && addresses.All(IsPublic);
    }

    private static bool IsPublic(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip)) return false;
        if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6UniqueLocal) return false;

        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = ip.GetAddressBytes();
            if (b[0] == 0 || b[0] == 10) return false;                     
            if (b[0] == 172 && b[1] is >= 16 and <= 31) return false;      
            if (b[0] == 192 && b[1] == 168) return false;                  
            if (b[0] == 169 && b[1] == 254) return false;                 
        }
        return true;
    }
}