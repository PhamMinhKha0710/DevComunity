namespace DevComunity.Application.Queries.Tags;

/// <summary>
/// Query for getting paginated tags
/// </summary>
public class GetTagsQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 36;
    public string? Search { get; set; }
    public string Sort { get; set; } = "popular"; // popular, name, newest
}

/// <summary>
/// Query for getting a tag by name
/// </summary>
public class GetTagByNameQuery
{
    public string TagName { get; set; } = null!;
}
