using Microsoft.AspNetCore.SignalR;

namespace DevComunity.Api.Hubs;

/// <summary>
/// SignalR Hub for real-time question updates
/// </summary>
public class QuestionHub : Hub
{
    private readonly ILogger<QuestionHub> _logger;

    public QuestionHub(ILogger<QuestionHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Join a question room to receive updates
    /// </summary>
    public async Task JoinQuestion(int questionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"question_{questionId}");
        _logger.LogInformation("Connection {ConnectionId} joined question {QuestionId}", Context.ConnectionId, questionId);
    }

    /// <summary>
    /// Leave a question room
    /// </summary>
    public async Task LeaveQuestion(int questionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"question_{questionId}");
        _logger.LogInformation("Connection {ConnectionId} left question {QuestionId}", Context.ConnectionId, questionId);
    }

    /// <summary>
    /// Notify of new answer
    /// </summary>
    public async Task NotifyNewAnswer(int questionId, object answer)
    {
        await Clients.Group($"question_{questionId}").SendAsync("NewAnswer", answer);
    }

    /// <summary>
    /// Notify of answer update
    /// </summary>
    public async Task NotifyAnswerUpdate(int questionId, object answer)
    {
        await Clients.Group($"question_{questionId}").SendAsync("AnswerUpdated", answer);
    }

    /// <summary>
    /// Notify of new comment
    /// </summary>
    public async Task NotifyNewComment(int questionId, object comment)
    {
        await Clients.Group($"question_{questionId}").SendAsync("NewComment", comment);
    }

    /// <summary>
    /// Notify of vote change
    /// </summary>
    public async Task NotifyVoteChange(int questionId, object voteInfo)
    {
        await Clients.Group($"question_{questionId}").SendAsync("VoteChanged", voteInfo);
    }

    /// <summary>
    /// Notify of answer accepted
    /// </summary>
    public async Task NotifyAnswerAccepted(int questionId, int answerId)
    {
        await Clients.Group($"question_{questionId}").SendAsync("AnswerAccepted", new { answerId });
    }
}
