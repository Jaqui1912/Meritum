namespace Meritum.Core.Entities;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class Project
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    //  ¿A qué categoría pertenece?
    [BsonRepresentation(BsonType.ObjectId)]
    public string CategoryId { get; set; } = null!;

    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    
    
    public string VideoUrl { get; set; }      // Ej: "https://youtube.com/..."
    public string DocumentUrl { get; set; }   // Ej: "https://drive.google.com/..."
    

    public string? ImageUrl { get; set; }
    public string TeamMembers { get; set; } = null!;
    //public string Duration { get; set; } = null!;
}