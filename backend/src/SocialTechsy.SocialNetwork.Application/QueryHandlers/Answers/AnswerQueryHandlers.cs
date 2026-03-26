using MediatR;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Common;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Question;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Social;
using SocialTechsy.SocialNetwork.Application.Common.Mappings;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Queries.Answers;
using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Application.QueryHandlers.Answers;

public class GetAnswersByQuestionQueryHandler : IRequestHandler<GetAnswersByQuestionQuery, PaginatedResponse<AnswerDto>>
{
    private readonly IAnswerRepository _answerRepository;
    private readonly IVoteRepository _voteRepository;

    public GetAnswersByQuestionQueryHandler(
        IAnswerRepository answerRepository,
        IVoteRepository voteRepository)
    {
        _answerRepository = answerRepository;
        _voteRepository = voteRepository;
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

        var ids = answerDtos.Select(a => a.AnswerId).ToArray();

        if (ids.Length > 0)
        {
            var scoresByAnswerId = await _voteRepository.GetAnswerScoresForAnswerIdsAsync(ids, cancellationToken);
            IReadOnlyDictionary<int, Vote>? userVotes = null;
            if (request.CurrentUserId is int uid && uid > 0)
                userVotes = await _voteRepository.GetUserVotesForAnswerIdsAsync(uid, ids, cancellationToken);

            foreach (var dto in answerDtos)
            {
                dto.Score = scoresByAnswerId.TryGetValue(dto.AnswerId, out var s) ? s : 0;
                if (userVotes != null)
                {
                    if (userVotes.TryGetValue(dto.AnswerId, out var vote))
                        dto.UserVoteType = vote.IsUpvote ? "up" : "down";
                    else
                        dto.UserVoteType = null;
                }
            }
        }

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
        return answer?.ToDto();
    }
}
