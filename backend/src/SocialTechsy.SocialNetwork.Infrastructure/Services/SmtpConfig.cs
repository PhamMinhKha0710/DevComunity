namespace SocialTechsy.SocialNetwork.Infrastructure.Services;

public class SmtpConfig
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 587;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public bool EnableSsl { get; set; } = true;
    public string FromAddress { get; set; } = "noreply@socialtechsy.com";
    public string FromName { get; set; } = "SocialTechsy";
    public int MaxRetries { get; set; } = 3;
    public int MaxPerMinute { get; set; } = 30;
}
