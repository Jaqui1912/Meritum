namespace Meritum.Core.Entities;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;



public class Project
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } // Antes era int

    public string Title { get; set; }
    public string Description { get; set; }
    public string ImageUrl { get; set; }
    public string TeamMembers { get; set; }

    // Relación simple (solo guardamos el ID como string)
    [BsonRepresentation(BsonType.ObjectId)]
    public string CategoryId { get; set; } 
}