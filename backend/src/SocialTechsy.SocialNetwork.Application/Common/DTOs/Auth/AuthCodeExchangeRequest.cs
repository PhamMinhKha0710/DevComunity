namespace SocialTechsy.SocialNetwork.Application.Common.DTOs.Auth;

/// <summary>
/// Request to exchange an OAuth auth code for tokens.
/// </summary>
public class AuthCodeExchangeRequest
{
    public string Code { get; set; } = null!;
}
