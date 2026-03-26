using System.Text.Json.Serialization;

namespace SocialTechsy.SocialNetwork.Application.Common.DTOs.Chat;

public class ReplyToMessageDto
{
    [JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)]
    public long MessageId { get; set; }
    public int SenderId { get; set; }
    public string SenderUsername { get; set; } = null!;
    public string Content { get; set; } = null!;
}
