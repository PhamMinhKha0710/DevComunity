namespace DevComunity.Application.Queries.SavedItems;

/// <summary>
/// Query to get saved items for a user
/// </summary>
public class GetSavedItemsQuery
{
    public int UserId { get; set; }
    public string? Type { get; set; } // "question", "answer", or null for all
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 15;
}
