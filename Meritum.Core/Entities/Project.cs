using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Meritum.Core.Entities;

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

    // 👇 CAMPOS NUEVOS QUE FALTABAN
    public string? ImageUrl { get; set; }      // Portada
    public string? VideoUrl { get; set; }      // Video

    // 👇 ESTE DEBE SER LISTA (List<string>) y PLURAL (Urls)
    public List<string>? DocumentUrls { get; set; } = new List<string>(); 
}