namespace Meritum.Core.Entities;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic; // Para List
using System; // Para DateTime

public class Evaluation
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
  
    public string? Id { get; set; }

    // --- CALIFICACIONES (Tu UI) ---
    public double TechnicalScore { get; set; }    // 0 a 10
    public double UxScore { get; set; }           // 0 a 10
    public double PresentationScore { get; set; } // 0 a 10
    
    public double FinalScore { get; set; }        // Promedio (Ej: 8.3)

    // --- FEEDBACK ---
    // Chips (Ej: ["Excellent Design", "Clear Logic"])
    public List<string> QuickFeedbackTags { get; set; } = new List<string>();

    // ¿Dónde escribe el juez "Me gustó mucho pero..."?
    //
    //public string? Comment { get; set; }

    // --- RELACIONES ---
    [BsonRepresentation(BsonType.ObjectId)]
    public string ProjectId { get; set; } = null!; // Obligatorio

    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = null!;    // Obligatorio (El Juez)

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}