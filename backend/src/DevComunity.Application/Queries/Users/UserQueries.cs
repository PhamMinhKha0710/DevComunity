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
