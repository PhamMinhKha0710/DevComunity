namespace DevComunity.Application.Queries.Search;

/// <summary>
/// Query for unified search across questions, users, and tags
/// </summary>
public class SearchQuery
{
    public string Query { get; set; } = null!;
    public int MaxResults { get; set; } = 5; // Results per type
}
