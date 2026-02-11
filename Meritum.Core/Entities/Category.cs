namespace Meritum.Core.Entities; 
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

public class Category
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    public string Name { get; set; }=null!; // Ej: "5to Cuatrimestre"
    public string IconUrl { get; set; } =null!; // Para la tarjeta visual
    
    // Relación: Un grupo tiene muchos proyectos
    //public List<Project>? Projects { get; set; }
}