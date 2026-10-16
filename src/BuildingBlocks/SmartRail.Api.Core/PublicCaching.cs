using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace SmartRail.Api.Core;

// Public reads are cacheable for a short time and revalidated with an ETag.
public static class PublicCaching
{
    public static IResult Ok<T>(HttpContext http, T value, int maxAgeSeconds = 300)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonSerializerOptions.Web);
        var etag = $"\"{Convert.ToHexString(SHA256.HashData(bytes))[..16]}\"";

        http.Response.Headers[HeaderNames.CacheControl] = $"public, max-age={maxAgeSeconds}";
        http.Response.Headers[HeaderNames.Vary] = HeaderNames.AcceptLanguage;
        http.Response.Headers[HeaderNames.ETag] = etag;

        if (http.Request.Headers.IfNoneMatch.Contains(etag))
            return Results.StatusCode(StatusCodes.Status304NotModified);

        return Results.Bytes(bytes, "application/json");
    }
}
