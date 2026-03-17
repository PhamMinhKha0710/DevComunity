namespace SocialTechsy.SocialNetwork.Application.Common.DTOs;

/// <summary>
/// Request to exchange an OAuth auth code for tokens.
/// </summary>
public class AuthCodeExchangeRequest
{
    public string Code { get; set; } = null!;
}
