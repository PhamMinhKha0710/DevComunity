using MediatR;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;
using SocialTechsy.SocialNetwork.Application.Common.Mappings;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using SocialTechsy.SocialNetwork.Application.Queries.Answers;

namespace SocialTechsy.SocialNetwork.Application.QueryHandlers.Answers;

public class GetAnswersByQuestionQueryHandler : IRequestHandler<GetAnswersByQuestionQuery, PaginatedResponse<AnswerDto>>
{
    private readonly IAnswerRepository _answerRepository;
    private readonly IVoteRepository _voteRepository;
    private readonly ILikeService? _likeService;

    public GetAnswersByQuestionQueryHandler(
        IAnswerRepository answerRepository,
        IVoteRepository voteRepository,
        ILikeService? likeService = null)
    {
        _answerRepository = answerRepository;
        _voteRepository = voteRepository;
        _likeService = likeService;
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

        if (_likeService != null && ids.Length > 0)
        {
            var likeCounts = await _likeService.GetLikeCountsBatchAsync("answer", ids);
            foreach (var dto in answerDtos)
            {
                dto.Score = likeCounts.TryGetValue(dto.AnswerId, out var c) ? (int)c : 0;
                if (request.CurrentUserId is > 0)
                {
                    var isLiked = await _likeService.IsLikedAsync("answer", dto.AnswerId, request.CurrentUserId.Value);
                    dto.UserVoteType = isLiked ? "up" : null;
                }
            }
        }
        else
        {
            foreach (var dto in answerDtos)
            {
                dto.Score = await _voteRepository.GetAnswerScoreAsync(dto.AnswerId, cancellationToken);
                if (request.CurrentUserId is > 0)
                {
                    var vote = await _voteRepository.GetUserVoteOnAnswerAsync(request.CurrentUserId.Value, dto.AnswerId, cancellationToken);
                    dto.UserVoteType = vote == null ? null : (vote.IsUpvote ? "up" : "down");
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
