namespace Meritum.Infrastructure.Services;

using Meritum.Core.Entities;
using Meritum.Core.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

public class EvaluationsService
{
    private readonly IMongoCollection<Evaluation> _evaluationsCollection;

    public EvaluationsService(IOptions<MeritumDatabaseSettings> settings)
    {
        var mongoClient = new MongoClient(settings.Value.ConnectionString);
        var mongoDatabase = mongoClient.GetDatabase(settings.Value.DatabaseName);

        // Aquí le decimos: "Tú te encargas de la colección 'Evaluations'"
        _evaluationsCollection = mongoDatabase.GetCollection<Evaluation>(settings.Value.EvaluationsCollectionName);
    }

    // MÉTODOS:

    // 1. Guardar un voto nuevo (Lo que pasa al dar clic en "Enviar Evaluación")
    public async Task CreateAsync(Evaluation newEvaluation)
    {
        // Aquí podrías agregar lógica extra antes de guardar, 
        // por ejemplo: validar que los puntos no sean negativos.
        await _evaluationsCollection.InsertOneAsync(newEvaluation);
    }

    // 2. Obtener votos de un proyecto específico (Para calcular promedios después)
    public async Task<List<Evaluation>> GetByProjectAsync(string projectId) =>
        await _evaluationsCollection.Find(x => x.ProjectId == projectId).ToListAsync();

    // 3. Verificar si un usuario ya votó por un proyecto (Para evitar fraude)
    // Devuelve TRUE si ya existe un voto de ese usuario para ese proyecto
    public async Task<bool> HasUserVotedAsync(string userId, string projectId)
    {
        var count = await _evaluationsCollection
            .CountDocumentsAsync(x => x.UserId == userId && x.ProjectId == projectId);
        return count > 0; 
    }

    // NUEVO: Obtener todas las evaluaciones hechas por un usuario (Para el Historial)
    public async Task<List<Evaluation>> GetByUserAsync(string userId) =>
        await _evaluationsCollection.Find(x => x.UserId == userId).ToListAsync();

    // NUEVO: Contar evaluaciones de un usuario (Para la estadística del Perfil)
    public async Task<long> CountByUserAsync(string userId) =>
        await _evaluationsCollection.CountDocumentsAsync(x => x.UserId == userId);
}