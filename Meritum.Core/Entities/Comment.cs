using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Meritum.Core.Entities;

public class Comment
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonElement("projectId")]
    public string ProjectId { get; set; } = null!;

    [BsonElement("userId")]
    public string UserId { get; set; } = null!;

    [BsonElement("userName")]
    public string? UserName { get; set; }

    [BsonElement("content")]
    public string Content { get; set; } = null!;

    [BsonElement("rating")]
    public double Rating { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
