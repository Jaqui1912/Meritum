namespace Meritum.Core.Entities; // <--- AGREGA ESTA LÍNEA AL PRINCIPIO

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
public class Evaluation
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }
    
    // Calificaciones individuales (según tu UI)
    public double TechnicalScore { get; set; } // Technical Implementation
    public double UxScore { get; set; }        // User Experience
    public double PresentationScore { get; set; } // Presentation
    
    // Promedio final calculado (Total Score 8.3)
    public double FinalScore { get; set; } 

    // Feedback Rápido (Los chips naranjas: "Excellent Design", "Clear Logic")
    // Lo guardaremos como texto separado por comas
    public List<string> QuickFeedbackTags { get; set; } = new List<string>();
    
    [BsonRepresentation(BsonType.ObjectId)]//ids de otras tablas
    public string ProjectId { get; set; }
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}