using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SocialTechsy.SocialNetwork.Api.Hubs;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Question;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Social;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;

namespace SocialTechsy.SocialNetwork.Api.Services;

public class SignalRQuestionEventDispatcher : IQuestionEventDispatcher
{
    private readonly IHubContext<QuestionHub> _questionHub;
    private readonly ILogger<SignalRQuestionEventDispatcher> _logger;

    public SignalRQuestionEventDispatcher(
        IHubContext<QuestionHub> questionHub,
        ILogger<SignalRQuestionEventDispatcher> logger)
    {
        _questionHub = questionHub;
        _logger = logger;
    }

    public async Task NotifyNewCommentAsync(int questionId, string targetType, int targetId, CommentDto comment, CancellationToken cancellationToken = default)
    {
        try
        {
            await _questionHub.Clients.Group($"question_{questionId}").SendAsync("NewComment", new { targetType, targetId, comment }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to broadcast NewComment to question {QuestionId} via SignalR — non-fatal", questionId);
        }
    }

    public async Task NotifyAnswerUpdatedAsync(int questionId, int answerId, string newBody, CancellationToken cancellationToken = default)
    {
        try
        {
            await _questionHub.Clients.Group($"question_{questionId}").SendAsync("AnswerUpdated", new { AnswerId = answerId, Body = newBody }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to broadcast AnswerUpdated to question {QuestionId} via SignalR — non-fatal", questionId);
        }
    }

    public async Task NotifyAnswerAcceptedAsync(int questionId, int answerId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _questionHub.Clients.Group($"question_{questionId}").SendAsync("AnswerAccepted", new { answerId }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to broadcast AnswerAccepted to question {QuestionId} via SignalR — non-fatal", questionId);
        }
    }
}
