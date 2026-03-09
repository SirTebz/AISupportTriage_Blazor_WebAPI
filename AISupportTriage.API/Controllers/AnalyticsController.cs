using AISupportTriage.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AISupportTriage.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "CompanyAdmin,SupportAgent")]
public class AnalyticsController : ControllerBase
{
    private readonly ITicketService _ticketService;

    public AnalyticsController(ITicketService ticketService)
    {
        _ticketService = ticketService;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var summary = await _ticketService.GetAnalyticsAsync();
        return Ok(summary);
    }
}