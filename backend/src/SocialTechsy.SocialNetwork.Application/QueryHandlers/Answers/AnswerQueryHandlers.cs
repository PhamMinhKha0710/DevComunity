using MediatR;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Queries.Answers;

namespace SocialTechsy.SocialNetwork.Application.QueryHandlers.Answers;

/// <summary>
/// Handler for GetAnswersByQuestionQuery
/// </summary>
public class GetAnswersByQuestionQueryHandler : IRequestHandler<GetAnswersByQuestionQuery, PaginatedResponse<AnswerDto>>
{
    private readonly IAnswerRepository _answerRepository;

    public GetAnswersByQuestionQueryHandler(IAnswerRepository answerRepository)
    {
        _answerRepository = answerRepository;
    }

    public async Task<PaginatedResponse<AnswerDto>> Handle(GetAnswersByQuestionQuery request, CancellationToken cancellationToken = default)
    {
        var (answers, totalCount) = await _answerRepository.GetByQuestionIdAsync(request.QuestionId, request.Page, request.PageSize, cancellationToken);

        var answerDtos = answers.Select(a => new AnswerDto
        {
            AnswerId = a.AnswerId,
            QuestionId = a.QuestionId,
            Body = a.Body,
            Score = a.Score,
            IsAccepted = a.IsAccepted,
            CreatedDate = DateTime.SpecifyKind(a.CreatedDate, DateTimeKind.Utc),
            UpdatedDate = a.UpdatedDate.HasValue ? DateTime.SpecifyKind(a.UpdatedDate.Value, DateTimeKind.Utc) : null,
            AuthorId = a.UserId,
            AuthorUsername = a.User?.Username,
            AuthorProfilePicture = a.User?.ProfilePicture,
            AuthorReputation = a.User?.ReputationPoints,
            ParentAnswerId = a.ParentAnswerId,
            Comments = a.Comments?.Select(c => new CommentDto
            {
                CommentId = c.CommentId,
                Body = c.Body,
                CreatedDate = DateTime.SpecifyKind(c.CreatedDate, DateTimeKind.Utc),
                AuthorId = c.UserId,
                AuthorUsername = c.User?.Username ?? "",
                AuthorProfilePicture = c.User?.ProfilePicture
            }).ToList() ?? new List<CommentDto>()
        }).ToList();

        answerDtos = request.Sort switch
        {
            "oldest" => answerDtos.OrderBy(a => a.CreatedDate).ToList(),
            "newest" => answerDtos.OrderByDescending(a => a.CreatedDate).ToList(),
            _ => answerDtos.OrderByDescending(a => a.IsAccepted).ThenByDescending(a => a.Score).ToList()
        };

        return new PaginatedResponse<AnswerDto>
        {
            Items = answerDtos,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }
}

/// <summary>
/// Handler for GetAnswerByIdQuery
/// </summary>
public class GetAnswerByIdQueryHandler : IRequestHandler<GetAnswerByIdQuery, AnswerDto?>
{
    private readonly IAnswerRepository _answerRepository;

    public GetAnswerByIdQueryHandler(IAnswerRepository answerRepository)
    {
        _answerRepository = answerRepository;
    }

    public async Task<AnswerDto?> Handle(GetAnswerByIdQuery request, CancellationToken cancellationToken = default)
    {
        var answer = await _answerRepository.GetByIdAsync(request.AnswerId, cancellationToken);
        
        if (answer == null)
            return null;

        return new AnswerDto
        {
            AnswerId = answer.AnswerId,
            QuestionId = answer.QuestionId,
            Body = answer.Body,
            Score = answer.Score,
            IsAccepted = answer.IsAccepted,
            CreatedDate = DateTime.SpecifyKind(answer.CreatedDate, DateTimeKind.Utc),
            UpdatedDate = answer.UpdatedDate.HasValue ? DateTime.SpecifyKind(answer.UpdatedDate.Value, DateTimeKind.Utc) : null,
            AuthorId = answer.UserId,
            AuthorUsername = answer.User?.Username,
            AuthorProfilePicture = answer.User?.ProfilePicture,
            AuthorReputation = answer.User?.ReputationPoints,
            ParentAnswerId = answer.ParentAnswerId
        };
    }
}
