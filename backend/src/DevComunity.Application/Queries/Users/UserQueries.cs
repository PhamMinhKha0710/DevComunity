namespace DevComunity.Application.Queries.Users;

/// <summary>
/// Query to get current user by ID
/// </summary>
public class GetCurrentUserQuery
{
    public int UserId { get; set; }
}

/// <summary>
/// Query to get user profile by ID
/// </summary>
public class GetUserByIdQuery
{
    public int UserId { get; set; }
}

/// <summary>
/// Query to get user profile by username
/// </summary>
public class GetUserByUsernameQuery
{
    public string Username { get; set; } = null!;
}

/// <summary>
/// Query to get paginated list of users
/// </summary>
public class GetUsersQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 36;
    public string? Search { get; set; }
    public string SortBy { get; set; } = "reputation"; // reputation, newest
}
