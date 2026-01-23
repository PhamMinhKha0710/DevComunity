using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DevComunity.Application.Common.DTOs;
using DevComunity.Application.QueryHandlers.Chat;
using DevComunity.Application.CommandHandlers.Chat;
using System.Security.Claims;

namespace DevComunity.Api.Controllers;

/// <summary>
/// API Controller for Chat/Messaging
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly ILogger<ChatController> _logger;
    private readonly GetConversationsQueryHandler _getConversationsHandler;
    private readonly GetConversationByIdQueryHandler _getConversationByIdHandler;
    private readonly GetMessagesQueryHandler _getMessagesHandler;
    private readonly StartConversationCommandHandler _startConversationHandler;
    private readonly SendMessageCommandHandler _sendMessageHandler;
    private readonly MarkConversationReadCommandHandler _markReadHandler;

    public ChatController(
        ILogger<ChatController> logger,
        GetConversationsQueryHandler getConversationsHandler,
        GetConversationByIdQueryHandler getConversationByIdHandler,
        GetMessagesQueryHandler getMessagesHandler,
        StartConversationCommandHandler startConversationHandler,
        SendMessageCommandHandler sendMessageHandler,
        MarkConversationReadCommandHandler markReadHandler)
    {
        _logger = logger;
        _getConversationsHandler = getConversationsHandler;
        _getConversationByIdHandler = getConversationByIdHandler;
        _getMessagesHandler = getMessagesHandler;
        _startConversationHandler = startConversationHandler;
        _sendMessageHandler = sendMessageHandler;
        _markReadHandler = markReadHandler;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    /// <summary>
    /// Get user's conversations
    /// </summary>
    [HttpGet("conversations")]
    [ProducesResponseType(typeof(IEnumerable<ConversationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ConversationDto>>> GetConversations(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        _logger.LogInformation("Getting conversations for user {UserId}", userId);

        var result = await _getConversationsHandler.HandleAsync(userId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get conversation by ID
    /// </summary>
    [HttpGet("conversations/{id:int}")]
    [ProducesResponseType(typeof(ConversationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConversationDto>> GetConversation(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        _logger.LogInformation("Getting conversation {ConversationId} for user {UserId}", id, userId);

        var result = await _getConversationByIdHandler.HandleAsync(id, userId, cancellationToken);
        if (result == null)
            return NotFound(new { message = "Conversation not found or access denied" });

        return Ok(result);
    }

    /// <summary>
    /// Get messages in a conversation
    /// </summary>
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

        var result = await _getMessagesHandler.HandleAsync(id, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Start a new conversation
    /// </summary>
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

        var result = await _startConversationHandler.HandleAsync(command, cancellationToken);
        return Created($"/api/chat/conversations/{result.ConversationId}", result);
    }

    /// <summary>
    /// Send a message
    /// </summary>
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

        var result = await _sendMessageHandler.HandleAsync(command, cancellationToken);
        if (result == null)
            return NotFound(new { message = "Conversation not found or access denied" });

        return Created($"/api/chat/conversations/{id}/messages/{result.MessageId}", result);
    }

    /// <summary>
    /// Mark messages as read
    /// </summary>
    [HttpPut("conversations/{id:int}/read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAsRead(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        _logger.LogInformation("User {UserId} marking conversation {ConversationId} as read", userId, id);

        var command = new MarkConversationReadCommand
        {
            ConversationId = id,
            UserId = userId
        };

        await _markReadHandler.HandleAsync(command, cancellationToken);
        return Ok(new { message = "Messages marked as read" });
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
