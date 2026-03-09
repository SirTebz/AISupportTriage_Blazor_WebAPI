namespace AISupportTriage.Application.Interfaces;

public interface ICurrentTenantService
{
    Guid GetTenantId();
    string GetUserId();
    string GetUserRole();
    string GetUserEmail();
}