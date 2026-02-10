namespace Meritum.Core.Entities; // <--- AGREGA ESTA LÍNEA AL PRINCIPIO

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
public class Category
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }
    public string Name { get; set; } // Ej: "5to Cuatrimestre"
    public string IconUrl { get; set; } // Para la tarjeta visual
    
    // Relación: Un grupo tiene muchos proyectos
    public ICollection<Project> Projects { get; set; }
}