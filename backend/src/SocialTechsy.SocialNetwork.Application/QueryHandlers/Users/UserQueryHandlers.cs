using MediatR;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Auth;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Social;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Common;
using SocialTechsy.SocialNetwork.Application.Common.Mappings;
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

        return user.ToDto();
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

        return user.ToDto(questionCount, answerCount);
    }
}

public class GetUserByUsernameQueryHandler : IRequestHandler<GetUserByUsernameQuery, UserDto?>
{
    private readonly IUserRepository _userRepository;

    public GetUserByUsernameQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserDto?> Handle(GetUserByUsernameQuery request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);
        if (user == null)
            return null;

        var statsResult = await _userRepository.GetUserWithStatsAsync(user.UserId, cancellationToken);
        var questionCount = statsResult.HasValue ? statsResult.Value.QuestionCount : 0;
        var answerCount = statsResult.HasValue ? statsResult.Value.AnswerCount : 0;

        return user.ToDto(questionCount, answerCount);
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
            Items = items.Select(u => u.ToDto()).ToList(),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }
}

