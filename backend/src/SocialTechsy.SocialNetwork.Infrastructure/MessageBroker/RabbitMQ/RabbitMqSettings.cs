namespace SocialTechsy.SocialNetwork.Infrastructure.MessageBroker.RabbitMQ;

public class RabbitMqSettings
{
    public const string SectionName = "RabbitMQ";
    public bool Enabled { get; set; } = false;
    public string HostName { get; set; } = "localhost";
    public ushort Port { get; set; } = 5672;
    public string VirtualHost { get; set; } = "/";
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
}
