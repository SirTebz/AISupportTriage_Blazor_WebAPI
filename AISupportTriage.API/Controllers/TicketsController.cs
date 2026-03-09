using AISupportTriage.Application.DTOs.Tickets;
using AISupportTriage.Application.Interfaces;
using AISupportTriage.Infrastructure.Hubs;
using AISupportTriage.Infrastructure.Jobs;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace AISupportTriage.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TicketsController : ControllerBase
{
    private readonly ITicketService _ticketService;
    private readonly IBackgroundJobClient _backgroundJobs;
    private readonly IHubContext<TicketNotificationHub> _hub;
    private readonly ICurrentTenantService _currentTenant;

    public TicketsController(
        ITicketService ticketService,
        IBackgroundJobClient backgroundJobs,
        IHubContext<TicketNotificationHub> hub,
        ICurrentTenantService currentTenant)
    {
        _ticketService = ticketService;
        _backgroundJobs = backgroundJobs;
        _hub = hub;
        _currentTenant = currentTenant;
    }

    [HttpGet]
    public async Task<IActionResult> GetTickets()
    {
        var tickets = await _ticketService.GetTicketsAsync();
        return Ok(tickets);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTicket(Guid id)
    {
        var ticket = await _ticketService.GetTicketByIdAsync(id);
        if (ticket == null) return NotFound();
        return Ok(ticket);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTicket([FromBody] CreateTicketDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var ticket = await _ticketService.CreateTicketAsync(dto);

        // Enqueue AI analysis as background job (never blocks the user)
        _backgroundJobs.Enqueue<AiAnalysisJob>(job => job.ProcessAsync(ticket.Id));

        // Notify all connected clients
        await _hub.Clients.All.SendAsync("TicketCreated", ticket.Id.ToString(), ticket.Title);

        return CreatedAtAction(nameof(GetTicket), new { id = ticket.Id }, ticket);
    }

    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = "CompanyAdmin,SupportAgent")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateTicketStatusDto dto)
    {
        var ticket = await _ticketService.UpdateStatusAsync(id, dto);
        await _hub.Clients.All.SendAsync("TicketUpdated", id.ToString());
        return Ok(ticket);
    }

    [HttpPut("{id:guid}/assign")]
    [Authorize(Roles = "CompanyAdmin,SupportAgent")]
    public async Task<IActionResult> AssignAgent(Guid id, [FromBody] AssignAgentDto dto)
    {
        var ticket = await _ticketService.AssignAgentAsync(id, dto);
        await _hub.Clients.All.SendAsync("TicketUpdated", id.ToString());
        return Ok(ticket);
    }

    [HttpPost("{id:guid}/messages")]
    public async Task<IActionResult> AddMessage(Guid id, [FromBody] AddMessageDto dto)
    {
        var message = await _ticketService.AddMessageAsync(id, dto);
        await _hub.Clients.All.SendAsync("NewMessage", id.ToString());
        return Ok(message);
    }
}