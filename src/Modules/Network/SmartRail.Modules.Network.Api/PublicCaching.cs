using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace SmartRail.Modules.Network.Api;

// Public reads are cacheable for five minutes and revalidated with an ETag.
internal static class PublicCaching
{
    internal static IResult Ok<T>(HttpContext http, T value)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonSerializerOptions.Web);
        var etag = $"\"{Convert.ToHexString(SHA256.HashData(bytes))[..16]}\"";

        http.Response.Headers[HeaderNames.CacheControl] = "public, max-age=300";
        http.Response.Headers[HeaderNames.Vary] = HeaderNames.AcceptLanguage;
        http.Response.Headers[HeaderNames.ETag] = etag;

        if (http.Request.Headers.IfNoneMatch.Contains(etag))
            return Results.StatusCode(StatusCodes.Status304NotModified);

        return Results.Bytes(bytes, "application/json");
    }
}
