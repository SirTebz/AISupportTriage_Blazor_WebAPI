using AISupportTriage.Application.DTOs.Auth;
using AISupportTriage.Application.Interfaces;
using AISupportTriage.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AISupportTriage.API.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IApplicationDbContext _context;
    private readonly IConfiguration _config;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        IApplicationDbContext context,
        IConfiguration config)
    {
        _userManager = userManager;
        _context = context;
        _config = config;
    }

    public async Task<AuthResponseDto> RegisterTenantAsync(RegisterTenantDto dto)
    {
        // Check if email already exists
        var existing = await _userManager.FindByEmailAsync(dto.AdminEmail);
        if (existing != null)
            throw new InvalidOperationException("An account with this email already exists.");

        // Create tenant
        var tenant = new Tenant
        {
            Name = dto.CompanyName,
            Plan = dto.Plan
        };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        // Create admin user
        var user = new ApplicationUser
        {
            UserName = dto.AdminEmail,
            Email = dto.AdminEmail,
            EmailConfirmed = true,
            FirstName = dto.AdminFirstName,
            LastName = dto.AdminLastName,
            TenantId = tenant.Id,
            Department = "Management",
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, dto.AdminPassword);
        if (!result.Succeeded)
        {
            // Rollback tenant
            _context.Tenants.Remove(tenant);
            await _context.SaveChangesAsync();
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        await _userManager.AddToRoleAsync(user, "CompanyAdmin");

        return GenerateToken(user, "CompanyAdmin", tenant);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null || !user.IsActive)
            throw new UnauthorizedAccessException("Invalid email or password.");

        var passwordValid = await _userManager.CheckPasswordAsync(user, dto.Password);
        if (!passwordValid)
            throw new UnauthorizedAccessException("Invalid email or password.");

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "Customer";

        var tenant = await _context.Tenants.FindAsync(user.TenantId)
            ?? throw new InvalidOperationException("Tenant not found.");

        return GenerateToken(user, role, tenant);
    }

    private AuthResponseDto GenerateToken(ApplicationUser user, string role, Tenant tenant)
    {
        var jwtSettings = _config.GetSection("JwtSettings");
        var secret = jwtSettings["Secret"]!;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiration = DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["ExpirationInMinutes"]!));

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, role),
            new("TenantId", tenant.Id.ToString()),
            new("TenantName", tenant.Name),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: expiration,
            signingCredentials: creds
        );

        return new AuthResponseDto
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            UserId = user.Id,
            Email = user.Email!,
            FullName = user.FullName,
            Role = role,
            TenantId = tenant.Id,
            TenantName = tenant.Name,
            ExpiresAt = expiration
        };
    }
}