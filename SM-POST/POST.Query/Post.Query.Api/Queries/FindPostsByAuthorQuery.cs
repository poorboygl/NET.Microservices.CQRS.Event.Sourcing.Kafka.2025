using CQRS.Core.Queries;

namespace Post.Query.Api.Queries;

public class FindPostsByAuthorQuery : BaseQuery
{
    public required string Author { get; set; }
}
