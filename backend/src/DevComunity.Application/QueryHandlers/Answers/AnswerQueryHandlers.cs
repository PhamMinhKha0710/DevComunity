using DevComunity.Application.Common.DTOs;
using DevComunity.Application.Interfaces.Repositories;
using DevComunity.Application.Queries.Answers;

namespace DevComunity.Application.QueryHandlers.Answers;

/// <summary>
/// Handler for GetAnswersByQuestionQuery
/// </summary>
public class GetAnswersByQuestionQueryHandler
{
    private readonly IAnswerRepository _answerRepository;

    public GetAnswersByQuestionQueryHandler(IAnswerRepository answerRepository)
    {
        _answerRepository = answerRepository;
    }

    public async Task<List<AnswerDto>> HandleAsync(GetAnswersByQuestionQuery query, CancellationToken cancellationToken = default)
    {
        var answers = await _answerRepository.GetByQuestionIdAsync(query.QuestionId, cancellationToken);

        var answerDtos = answers.Select(a => new AnswerDto
        {
            AnswerId = a.AnswerId,
            QuestionId = a.QuestionId,
            Body = a.Body,
            Score = a.Score,
            IsAccepted = a.IsAccepted,
            CreatedDate = a.CreatedDate,
            UpdatedDate = a.UpdatedDate,
            AuthorId = a.UserId,
            AuthorUsername = a.User?.Username,
            AuthorProfilePicture = a.User?.ProfilePicture,
            AuthorReputation = a.User?.ReputationPoints,
            ParentAnswerId = a.ParentAnswerId,
            Comments = a.Comments?.Select(c => new CommentDto
            {
                CommentId = c.CommentId,
                Body = c.Body,
                CreatedDate = c.CreatedDate,
                AuthorId = c.UserId,
                AuthorUsername = c.User?.Username ?? "",
                AuthorProfilePicture = c.User?.ProfilePicture
            }).ToList() ?? new List<CommentDto>()
        }).ToList();

        // Sort answers
        answerDtos = query.Sort switch
        {
            "oldest" => answerDtos.OrderBy(a => a.CreatedDate).ToList(),
            "newest" => answerDtos.OrderByDescending(a => a.CreatedDate).ToList(),
            _ => answerDtos.OrderByDescending(a => a.IsAccepted).ThenByDescending(a => a.Score).ToList() // votes
        };

        return answerDtos;
    }
}

/// <summary>
/// Handler for GetAnswerByIdQuery
/// </summary>
public class GetAnswerByIdQueryHandler
{
    private readonly IAnswerRepository _answerRepository;

    public GetAnswerByIdQueryHandler(IAnswerRepository answerRepository)
    {
        _answerRepository = answerRepository;
    }

    public async Task<AnswerDto?> HandleAsync(GetAnswerByIdQuery query, CancellationToken cancellationToken = default)
    {
        var answer = await _answerRepository.GetByIdAsync(query.AnswerId, cancellationToken);
        
        if (answer == null)
            return null;

        return new AnswerDto
        {
            AnswerId = answer.AnswerId,
            QuestionId = answer.QuestionId,
            Body = answer.Body,
            Score = answer.Score,
            IsAccepted = answer.IsAccepted,
            CreatedDate = answer.CreatedDate,
            UpdatedDate = answer.UpdatedDate,
            AuthorId = answer.UserId,
            AuthorUsername = answer.User?.Username,
            AuthorProfilePicture = answer.User?.ProfilePicture,
            AuthorReputation = answer.User?.ReputationPoints,
            ParentAnswerId = answer.ParentAnswerId
        };
    }
}
