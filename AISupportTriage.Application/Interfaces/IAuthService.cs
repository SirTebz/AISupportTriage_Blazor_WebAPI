using AISupportTriage.Application.DTOs.Auth;

namespace AISupportTriage.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterTenantAsync(RegisterTenantDto dto);
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
}