using MediatR;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;
using SocialTechsy.SocialNetwork.Application.Common.Mappings;
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

        var answerDtos = answers.Select(a =>
        {
            var dto = a.ToDto();
            dto.Comments = a.Comments?.Select(c => new CommentDto
            {
                CommentId = c.CommentId,
                Body = c.Body,
                CreatedDate = DateTime.SpecifyKind(c.CreatedDate, DateTimeKind.Utc),
                AuthorId = c.UserId,
                AuthorUsername = c.User?.Username ?? "",
                AuthorProfilePicture = c.User?.ProfilePicture
            }).ToList() ?? new List<CommentDto>();
            return dto;
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

        return answer.ToDto();
    }
}
