using System.Security.Claims;
using SmartRail.Application.Core.Abstractions;

namespace SmartRail.Api.Extensions;

internal sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public string? UserId => Principal?.FindFirstValue(ClaimTypes.NameIdentifier) ?? Principal?.FindFirstValue("sub");

    public string? UserName => Principal?.FindFirstValue("preferred_username") ?? Principal?.Identity?.Name;

    public IReadOnlyCollection<string> Roles =>
        Principal?.FindAll("roles").Select(c => c.Value).ToArray() ?? [];

    public bool IsInRole(string role) => Roles.Contains(role);
}
