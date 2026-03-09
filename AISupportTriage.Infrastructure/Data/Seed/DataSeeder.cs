using AISupportTriage.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AISupportTriage.Infrastructure.Data.Seed;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Apply any pending migrations
        await context.Database.MigrateAsync();

        // Seed Roles
        string[] roles = ["SuperAdmin", "CompanyAdmin", "SupportAgent", "Customer"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // Seed Demo Tenant + Admin (only if no tenants exist)
        if (!await context.Tenants.AnyAsync())
        {
            var demoTenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = "Acme Corp (Demo)",
                Plan = "Pro",
                IsActive = true
            };
            context.Tenants.Add(demoTenant);
            await context.SaveChangesAsync();

            var adminUser = new ApplicationUser
            {
                UserName = "admin@acme.com",
                Email = "admin@acme.com",
                EmailConfirmed = true,
                FirstName = "Admin",
                LastName = "User",
                TenantId = demoTenant.Id,
                Department = "Management",
                IsActive = true
            };

            var result = await userManager.CreateAsync(adminUser, "Admin@123");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(adminUser, "CompanyAdmin");

            // Demo Support Agent
            var agentUser = new ApplicationUser
            {
                UserName = "agent@acme.com",
                Email = "agent@acme.com",
                EmailConfirmed = true,
                FirstName = "Support",
                LastName = "Agent",
                TenantId = demoTenant.Id,
                Department = "Technical",
                IsActive = true
            };

            var agentResult = await userManager.CreateAsync(agentUser, "Agent@123");
            if (agentResult.Succeeded)
                await userManager.AddToRoleAsync(agentUser, "SupportAgent");

            // Demo Customer
            var customerUser = new ApplicationUser
            {
                UserName = "customer@acme.com",
                Email = "customer@acme.com",
                EmailConfirmed = true,
                FirstName = "John",
                LastName = "Customer",
                TenantId = demoTenant.Id,
                Department = "",
                IsActive = true
            };

            var custResult = await userManager.CreateAsync(customerUser, "Customer@123");
            if (custResult.Succeeded)
                await userManager.AddToRoleAsync(customerUser, "Customer");
        }
    }
}