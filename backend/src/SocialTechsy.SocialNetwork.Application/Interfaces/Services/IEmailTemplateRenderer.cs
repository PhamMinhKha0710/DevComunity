using SocialTechsy.SocialNetwork.Domain.Enums;

namespace SocialTechsy.SocialNetwork.Application.Interfaces.Services;

public interface IEmailTemplateRenderer
{
    Task<string> RenderAsync(EmailTemplateType template, object model);
    string GetSubject(EmailTemplateType template);
}
