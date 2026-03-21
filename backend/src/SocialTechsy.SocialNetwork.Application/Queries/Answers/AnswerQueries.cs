using MediatR;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Question;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Social;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Common;

namespace SocialTechsy.SocialNetwork.Application.Queries.Answers;

/// <summary>
/// Query to get answers for a question
/// </summary>
public class GetAnswersByQuestionQuery : IRequest<PaginatedResponse<AnswerDto>>
{
    public int QuestionId { get; set; }
    public int? CurrentUserId { get; set; }
    public string Sort { get; set; } = "votes";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

/// <summary>
/// Query to get a single answer by ID
/// </summary>
public class GetAnswerByIdQuery : IRequest<AnswerDto?>
{
    public int AnswerId { get; set; }
    public int? CurrentUserId { get; set; }
}
