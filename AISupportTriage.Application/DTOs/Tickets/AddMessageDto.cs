namespace AISupportTriage.Application.DTOs.Tickets;

public class AddMessageDto
{
    public string Content { get; set; } = string.Empty;
    public bool IsInternal { get; set; } = false;
}