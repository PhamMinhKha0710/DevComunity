using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;
using SocialTechsy.SocialNetwork.Application.QueryHandlers.Chat;
using SocialTechsy.SocialNetwork.Application.CommandHandlers.Chat;
using System.Security.Claims;

namespace SocialTechsy.SocialNetwork.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly ILogger<ChatController> _logger;
    private readonly IMediator _mediator;

    public ChatController(ILogger<ChatController> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    [HttpGet("conversations")]
    [ProducesResponseType(typeof(PaginatedResponse<ConversationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<ConversationDto>>> GetConversations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        _logger.LogInformation("Getting conversations for user {UserId}", userId);

        var result = await _mediator.Send(new GetConversationsQuery { UserId = userId, Page = page, PageSize = pageSize }, cancellationToken);
        return Ok(result);
    }

    [HttpGet("conversations/{id:int}")]
    [ProducesResponseType(typeof(ConversationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConversationDto>> GetConversation(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        _logger.LogInformation("Getting conversation {ConversationId} for user {UserId}", id, userId);

        var result = await _mediator.Send(new GetConversationByIdQuery { ConversationId = id, UserId = userId }, cancellationToken);
        if (result == null)
            return NotFound(new { message = "Conversation not found or access denied" });

        return Ok(result);
    }

    [HttpGet("conversations/{id:int}/messages")]
    [ProducesResponseType(typeof(PaginatedResponse<MessageDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<MessageDto>>> GetMessages(
        int id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        _logger.LogInformation("Getting messages for conversation {ConversationId}", id);

        var result = await _mediator.Send(new GetMessagesQuery { ConversationId = id, Page = page, PageSize = pageSize }, cancellationToken);
        return Ok(result);
    }

    [HttpPost("conversations")]
    [ProducesResponseType(typeof(ConversationDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ConversationDto>> StartConversation(
        [FromBody] StartConversationRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        _logger.LogInformation("User {UserId} starting conversation with user {RecipientId}", userId, request.RecipientId);

        var command = new StartConversationCommand
        {
            InitiatorId = userId,
            RecipientId = request.RecipientId,
            InitialMessage = request.InitialMessage
        };

        var result = await _mediator.Send(command, cancellationToken);
        return Created($"/api/chat/conversations/{result.ConversationId}", result);
    }

    [HttpPost("conversations/{id:int}/messages")]
    [ProducesResponseType(typeof(MessageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MessageDto>> SendMessage(
        int id,
        [FromBody] SendMessageRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        _logger.LogInformation("User {UserId} sending message in conversation {ConversationId}", userId, id);

        var command = new SendMessageCommand
        {
            ConversationId = id,
            SenderId = userId,
            Content = request.Content
        };

        var result = await _mediator.Send(command, cancellationToken);
        if (result == null)
            return NotFound(new { message = "Conversation not found or access denied" });

        return Created($"/api/chat/conversations/{id}/messages/{result.Message.MessageId}", result.Message);
    }

    [HttpGet("conversations/{id:int}/messages/since/{sinceMessageId:int}")]
    [ProducesResponseType(typeof(List<MessageDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<MessageDto>>> GetMessagesSince(
        int id, int sinceMessageId,
        [FromQuery] int limit = 200,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var result = await _mediator.Send(new GetMessagesSinceQuery
        {
            ConversationId = id,
            UserId = userId,
            SinceMessageId = sinceMessageId,
            Limit = Math.Min(limit, 500)
        }, cancellationToken);

        return Ok(result);
    }

    [HttpPut("conversations/{id:int}/read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAsRead(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        _logger.LogInformation("User {UserId} marking conversation {ConversationId} as read", userId, id);

        await _mediator.Send(new MarkConversationReadCommand { ConversationId = id, UserId = userId }, cancellationToken);
        return Ok(new { message = "Messages marked as read" });
    }

    [HttpDelete("conversations/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteConversationForCurrentUser(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        _logger.LogInformation("User {UserId} leaving conversation {ConversationId}", userId, id);

        var success = await _mediator.Send(new LeaveConversationCommand
        {
            ConversationId = id,
            UserId = userId
        }, cancellationToken);

        if (!success)
        {
            return NotFound(new { message = "Conversation not found or user is not a participant" });
        }

        return NoContent();
    }

    [HttpPost("messages/{messageId:int}/reactions")]
    [ProducesResponseType(typeof(MessageReactionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MessageReactionDto>> AddReaction(
        int messageId,
        [FromBody] AddReactionDto request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        _logger.LogInformation("User {UserId} adding {ReactionType} reaction to message {MessageId}",
            userId, request.ReactionType, messageId);

        var command = new AddReactionCommand
        {
            MessageId = messageId,
            UserId = userId,
            ReactionType = request.ReactionType
        };

        var result = await _mediator.Send(command, cancellationToken);
        if (result == null)
            return BadRequest(new { message = "Invalid reaction type or message not found" });

        return Ok(result);
    }

    [HttpDelete("messages/{messageId:int}/reactions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveReaction(int messageId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        _logger.LogInformation("User {UserId} removing reaction from message {MessageId}", userId, messageId);

        var success = await _mediator.Send(new RemoveReactionCommand { MessageId = messageId, UserId = userId }, cancellationToken);
        if (!success)
            return NotFound(new { message = "Reaction not found" });

        return Ok(new { message = "Reaction removed" });
    }
}

public class StartConversationRequest
{
    public int RecipientId { get; set; }
    public string? InitialMessage { get; set; }
}

public class SendMessageRequest
{
    public string Content { get; set; } = null!;
}
