using DevComunity.Application.Common.DTOs;
using DevComunity.Application.Interfaces.Repositories;
using DevComunity.Application.Queries.Users;

namespace DevComunity.Application.QueryHandlers.Users;

/// <summary>
/// Handler for GetCurrentUserQuery
/// </summary>
public class GetCurrentUserQueryHandler
{
    private readonly IUserRepository _userRepository;

    public GetCurrentUserQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserDto?> HandleAsync(GetCurrentUserQuery query, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(query.UserId, cancellationToken);
        
        if (user == null)
            return null;

        return new UserDto
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            DisplayName = user.DisplayName,
            ProfilePicture = user.ProfilePicture,
            ReputationPoints = user.ReputationPoints,
            IsEmailVerified = user.IsEmailVerified
        };
    }
}

/// <summary>
/// Handler for GetUserByIdQuery
/// </summary>
public class GetUserByIdQueryHandler
{
    private readonly IUserRepository _userRepository;

    public GetUserByIdQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserDto?> HandleAsync(GetUserByIdQuery query, CancellationToken cancellationToken = default)
    {
        var result = await _userRepository.GetUserWithStatsAsync(query.UserId, cancellationToken);
        
        if (result == null)
            return null;

        var (user, questionCount, answerCount) = result.Value;

        return new UserDto
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            DisplayName = user.DisplayName,
            ProfilePicture = user.ProfilePicture,
            ReputationPoints = user.ReputationPoints,
            IsEmailVerified = user.IsEmailVerified,
            QuestionCount = questionCount,
            AnswerCount = answerCount,
            CreatedDate = user.CreatedDate
        };
    }
}

/// <summary>
/// Handler for GetUsersQuery - paginated list of users
/// </summary>
public class GetUsersQueryHandler
{
    private readonly IUserRepository _userRepository;

    public GetUsersQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<PaginatedResponse<UserDto>> HandleAsync(GetUsersQuery query, CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _userRepository.GetPaginatedAsync(
            query.Page,
            query.PageSize,
            query.Search,
            query.SortBy,
            cancellationToken);

        return new PaginatedResponse<UserDto>
        {
            Items = items.Select(u => new UserDto
            {
                UserId = u.UserId,
                Username = u.Username,
                DisplayName = u.DisplayName,
                ProfilePicture = u.ProfilePicture,
                ReputationPoints = u.ReputationPoints,
                CreatedDate = u.CreatedDate
            }).ToList(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }
}

