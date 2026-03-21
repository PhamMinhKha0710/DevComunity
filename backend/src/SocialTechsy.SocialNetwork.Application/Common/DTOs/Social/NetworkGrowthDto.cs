namespace SocialTechsy.SocialNetwork.Application.Common.DTOs.Social;

public class NetworkGrowthDto
{
    public int TotalConnections { get; set; }
    public List<int> WeeksData { get; set; } = new();
}
