using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Meritum.Core.Entities;

[BsonIgnoreExtraElements]
public class Evaluation
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string ProjectId { get; set; } = null!;
    public string UserId { get; set; } = null!; // El maestro/juez

    // LOS 8 CRITERIOS DE LA RÚBRICA (Calificaciones del 0 al 10)
    public double Funcionalidad { get; set; }
    public double Rendimiento { get; set; }
    public double Arquitectura { get; set; }
    public double UXUI { get; set; }
    public double MVP { get; set; }
    public double AnálisisMercado { get; set; }
    public double ObjetivosInteligentes { get; set; }
    public double Innovación { get; set; }

    // Promedio final calculado por el Backend
    public double FinalScore { get; set; } 
}