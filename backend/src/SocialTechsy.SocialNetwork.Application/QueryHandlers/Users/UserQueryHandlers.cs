using MediatR;
using SocialTechsy.SocialNetwork.Application.Common.DTOs;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Queries.Users;

namespace SocialTechsy.SocialNetwork.Application.QueryHandlers.Users;

/// <summary>
/// Handler for GetCurrentUserQuery
/// </summary>
public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, UserDto?>
{
    private readonly IUserRepository _userRepository;

    public GetCurrentUserQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserDto?> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        
        if (user == null)
            return null;

        return new UserDto
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            DisplayName = user.DisplayName,
            ProfilePicture = user.ProfilePicture,
            Bio = user.Bio,
            Location = user.Location,
            Website = user.Website,
            ReputationPoints = user.ReputationPoints,
            IsEmailVerified = user.IsEmailVerified
        };
    }
}

/// <summary>
/// Handler for GetUserByIdQuery
/// </summary>
public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDto?>
{
    private readonly IUserRepository _userRepository;

    public GetUserByIdQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserDto?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken = default)
    {
        var result = await _userRepository.GetUserWithStatsAsync(request.UserId, cancellationToken);
        
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
            Bio = user.Bio,
            Location = user.Location,
            Website = user.Website,
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
public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PaginatedResponse<UserDto>>
{
    private readonly IUserRepository _userRepository;

    public GetUsersQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<PaginatedResponse<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _userRepository.GetPaginatedAsync(
            request.Page,
            request.PageSize,
            request.Search,
            request.SortBy,
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
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }
}

