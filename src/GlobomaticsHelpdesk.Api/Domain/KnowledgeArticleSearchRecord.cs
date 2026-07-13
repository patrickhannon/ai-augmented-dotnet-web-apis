using Microsoft.Extensions.VectorData;

namespace GlobomaticsHelpdesk.Api.Domain;

public class KnowledgeArticleSearchRecord
{
    [VectorStoreKey]
    public Guid Id { get; set; }

    [VectorStoreData]
    public string Title { get; set; } = string.Empty;

    [VectorStoreData]
    public string Body { get; set; } = string.Empty;

    [VectorStoreData]
    public string Tags { get; set; } = string.Empty;

    [VectorStoreVector(1536)]
    public string Content { get; set; } = string.Empty;
}
