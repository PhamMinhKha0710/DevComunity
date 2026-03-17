using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Application.Common.DTOs;

// ============ Friendship DTOs ============

public class FriendshipDto
{
    public int FriendshipId { get; set; }
    public UserSummaryDto Requester { get; set; } = null!;
    public UserSummaryDto Addressee { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
}

public class FriendDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = null!;
    public string? DisplayName { get; set; }
    public string? ProfilePicture { get; set; }
    public DateTime FriendsSince { get; set; }
}

// ============ Follow DTOs ============

public class FollowDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = null!;
    public string? DisplayName { get; set; }
    public string? ProfilePicture { get; set; }
    public DateTime FollowedAt { get; set; }
}

public class FollowStatsDto
{
    public int FollowersCount { get; set; }
    public int FollowingCount { get; set; }
    public bool IsFollowing { get; set; }
}

// ============ Group DTOs ============

public class GroupDto
{
    public int GroupId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? CoverImage { get; set; }
    public UserSummaryDto Creator { get; set; } = null!;
    public bool IsPrivate { get; set; }
    public DateTime CreatedAt { get; set; }
    public int MemberCount { get; set; }
    public bool IsMember { get; set; }
    public string? CurrentUserRole { get; set; }
}

public class GroupMemberDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = null!;
    public string? DisplayName { get; set; }
    public string? ProfilePicture { get; set; }
    public string Role { get; set; } = null!;
    public DateTime JoinedAt { get; set; }
}

public class CreateGroupRequest
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsPrivate { get; set; }
}

public class UpdateGroupRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool? IsPrivate { get; set; }
}

public class UpdateMemberRoleRequest
{
    public string Role { get; set; } = null!;  // "Admin", "Moderator", "Member"
}

// ============ Post DTOs ============

public class PostDto
{
    public int PostId { get; set; }
    public UserSummaryDto Author { get; set; } = null!;
    public int? GroupId { get; set; }
    public string? GroupName { get; set; }
    public string Content { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int LikeCount { get; set; }
    public bool UserLiked { get; set; }
    public int CommentCount { get; set; }
    public bool UserSaved { get; set; }
}

public class CreatePostRequest
{
    public string Content { get; set; } = null!;
    public int? GroupId { get; set; }
}

public class UpdatePostRequest
{
    public string Content { get; set; } = null!;
}

// ============ User Summary DTO ============

public class UserSummaryDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = null!;
    public string? DisplayName { get; set; }
    public string? ProfilePicture { get; set; }
}
