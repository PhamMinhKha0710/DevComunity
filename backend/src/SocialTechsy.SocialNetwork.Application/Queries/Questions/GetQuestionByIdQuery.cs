using MediatR;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;

namespace SocialTechsy.SocialNetwork.Application.Queries.Questions;

/// <summary>
/// Query to get a single question by ID
/// </summary>
public class GetQuestionByIdQuery : IRequest<QuestionDetailDto?>
{
    public int QuestionId { get; set; }
    public int? CurrentUserId { get; set; }
}
