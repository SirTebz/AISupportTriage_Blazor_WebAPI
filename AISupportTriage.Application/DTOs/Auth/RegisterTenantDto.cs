namespace AISupportTriage.Application.DTOs.Auth;

public class RegisterTenantDto
{
    public string CompanyName { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
    public string AdminFirstName { get; set; } = string.Empty;
    public string AdminLastName { get; set; } = string.Empty;
    public string Plan { get; set; } = "Free";
}