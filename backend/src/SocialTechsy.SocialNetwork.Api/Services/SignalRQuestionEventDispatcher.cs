using Microsoft.AspNetCore.SignalR;
using SocialTechsy.SocialNetwork.Api.Hubs;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Question;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Social;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;

namespace SocialTechsy.SocialNetwork.Api.Services;

public class SignalRQuestionEventDispatcher : IQuestionEventDispatcher
{
    private readonly IHubContext<QuestionHub> _questionHub;

    public SignalRQuestionEventDispatcher(IHubContext<QuestionHub> questionHub)
    {
        _questionHub = questionHub;
    }

    public async Task NotifyNewCommentAsync(int questionId, string targetType, int targetId, CommentDto comment, CancellationToken cancellationToken = default)
    {
        await _questionHub.Clients.Group($"question_{questionId}").SendAsync("NewComment", new { targetType, targetId, comment }, cancellationToken);
    }

    public async Task NotifyAnswerUpdatedAsync(int questionId, int answerId, string newBody, CancellationToken cancellationToken = default)
    {
        await _questionHub.Clients.Group($"question_{questionId}").SendAsync("AnswerUpdated", new { AnswerId = answerId, Body = newBody }, cancellationToken);
    }

    public async Task NotifyAnswerAcceptedAsync(int questionId, int answerId, CancellationToken cancellationToken = default)
    {
        await _questionHub.Clients.Group($"question_{questionId}").SendAsync("AnswerAccepted", new { answerId }, cancellationToken);
    }
}
