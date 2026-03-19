using SocialTechsy.SocialNetwork.Application.Common.DTOs;

namespace SocialTechsy.SocialNetwork.Application.Interfaces.Services;

public interface IQuestionEventDispatcher
{
    Task NotifyNewCommentAsync(int questionId, string targetType, int targetId, CommentDto comment, CancellationToken cancellationToken = default);
    Task NotifyAnswerUpdatedAsync(int questionId, int answerId, string newBody, CancellationToken cancellationToken = default);
    Task NotifyAnswerAcceptedAsync(int questionId, int answerId, CancellationToken cancellationToken = default);
}
