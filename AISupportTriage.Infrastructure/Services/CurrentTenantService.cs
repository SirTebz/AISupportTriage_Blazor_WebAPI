using AISupportTriage.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace AISupportTriage.Infrastructure.Services;

public class CurrentTenantService : ICurrentTenantService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentTenantService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid GetTenantId()
    {
        var claim = _httpContextAccessor.HttpContext?.User.FindFirstValue("TenantId");
        return Guid.TryParse(claim, out var tenantId) ? tenantId : Guid.Empty;
    }

    public string GetUserId()
    {
        return _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    }

    public string GetUserRole()
    {
        return _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
    }

    public string GetUserEmail()
    {
        return _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
    }
}