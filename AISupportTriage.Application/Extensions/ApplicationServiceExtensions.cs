using AISupportTriage.Application.Interfaces;
using AISupportTriage.Application.Services;
using AISupportTriage.Application.Validators;
using AISupportTriage.Application.DTOs.Tickets;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AISupportTriage.Application.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ITicketService, TicketService>();
        services.AddScoped<IRoutingEngine, RoutingEngine>();
        services.AddScoped<IValidator<CreateTicketDto>, CreateTicketValidator>();
        return services;
    }
}