using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SocialTechsy.SocialNetwork.Api.Hubs;

[Authorize]
public class CallHub : Hub
{
    private readonly ILogger<CallHub> _logger;

    public CallHub(ILogger<CallHub> logger)
    {
        _logger = logger;
    }

    private string GetUserId() => Context.UserIdentifier ?? string.Empty;

    public async Task InitiateCall(string targetUserId, string callType, string callerName)
    {
        var callerId = GetUserId();
        if (string.IsNullOrEmpty(callerId) || string.IsNullOrEmpty(targetUserId)) return;

        _logger.LogInformation("User {CallerId} initiating {CallType} call to {TargetUserId}", callerId, callType, targetUserId);

        await Clients.User(targetUserId).SendAsync("IncomingCall", new
        {
            callerId,
            callerName,
            callType,
        });
    }

    public async Task AcceptCall(string callerId)
    {
        var receiverId = GetUserId();
        if (string.IsNullOrEmpty(receiverId)) return;

        _logger.LogInformation("User {ReceiverId} accepted call from {CallerId}", receiverId, callerId);

        await Clients.User(callerId).SendAsync("CallAccepted", new
        {
            receiverId,
        });
    }

    public async Task RejectCall(string callerId, string reason = "rejected")
    {
        var receiverId = GetUserId();
        if (string.IsNullOrEmpty(receiverId)) return;

        _logger.LogInformation("User {ReceiverId} rejected call from {CallerId}: {Reason}", receiverId, callerId, reason);

        await Clients.User(callerId).SendAsync("CallRejected", new
        {
            receiverId,
            reason,
        });
    }

    public async Task EndCall(string peerId)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return;

        _logger.LogInformation("User {UserId} ended call with {PeerId}", userId, peerId);

        await Clients.User(peerId).SendAsync("CallEnded", new
        {
            userId,
        });
    }

    public async Task SendOffer(string peerId, string sdp)
    {
        var callerId = GetUserId();
        if (string.IsNullOrEmpty(callerId)) return;

        await Clients.User(peerId).SendAsync("ReceiveOffer", new
        {
            callerId,
            sdp,
        });
    }

    public async Task SendAnswer(string peerId, string sdp)
    {
        var answererId = GetUserId();
        if (string.IsNullOrEmpty(answererId)) return;

        await Clients.User(peerId).SendAsync("ReceiveAnswer", new
        {
            answererId,
            sdp,
        });
    }

    public async Task SendIceCandidate(string peerId, string candidate)
    {
        var senderId = GetUserId();
        if (string.IsNullOrEmpty(senderId)) return;

        await Clients.User(peerId).SendAsync("ReceiveIceCandidate", new
        {
            senderId,
            candidate,
        });
    }
}
