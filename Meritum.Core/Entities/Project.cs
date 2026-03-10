using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Meritum.Core.Entities;

[BsonIgnoreExtraElements]
public class Project
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonRepresentation(BsonType.ObjectId)]
    public string CategoryId { get; set; } = null!;

    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string TeamMembers { get; set; } = null!;

    public string? ImageUrl { get; set; }

    // Backward compat: lee el campo viejo "VideoUrl" (string) de documentos existentes
    [BsonIgnoreIfNull]
    public string? VideoUrl { get; set; }

    // Multi-video (campo nuevo)
    [BsonIgnoreIfNull]
    public List<string>? VideoUrls { get; set; } = new List<string>();

    public List<string>? DocumentUrls { get; set; } = new List<string>();

    /// <summary>
    /// Después de deserializar, migra VideoUrl viejo a VideoUrls si es necesario
    /// </summary>
    public void MigrateVideoUrl()
    {
        if (!string.IsNullOrEmpty(VideoUrl))
        {
            if (VideoUrls == null) VideoUrls = new List<string>();
            if (!VideoUrls.Contains(VideoUrl))
            {
                VideoUrls.Insert(0, VideoUrl);
            }
            VideoUrl = null; // limpiar el campo viejo
        }
    }
}
