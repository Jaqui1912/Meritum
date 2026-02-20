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


        _evaluationsCollection = mongoDatabase.GetCollection<Evaluation>(settings.Value.EvaluationsCollectionName);
    }


    // 1. Guardar un voto nuevo ( clic en "Enviar Evaluación")
    public async Task CreateAsync(Evaluation newEvaluation)
    {

        // validar que los puntos no sean negativos.
        await _evaluationsCollection.InsertOneAsync(newEvaluation);
    }

    // 2. Obtener votos de un proyecto específico 
    public async Task<List<Evaluation>> GetByProjectAsync(string projectId) =>
        await _evaluationsCollection.Find(x => x.ProjectId == projectId).ToListAsync();

    // Buscar si ya existe una evaluación de ESTE usuario para ESTE proyecto
    public async Task<Evaluation?> GetByProjectAndUserAsync(string projectId, string userId) =>
        await _evaluationsCollection.Find(x => x.ProjectId == projectId && x.UserId == userId).FirstOrDefaultAsync();

    //  Obtener todas las evaluaciones hechas por un usuario (Para el Historial)
   // public async Task<List<Evaluation>> GetByUserAsync(string userId) =>
     //   await _evaluationsCollection.Find(x => x.UserId == userId).ToListAsync();

    //Contar evaluaciones de un usuario (Para la estadística del Perfil)
    public async Task<long> CountByUserAsync(string userId) =>
        await _evaluationsCollection.CountDocumentsAsync(x => x.UserId == userId);

    // Obtener lista de evaluaciones hechas por un usuario específico
    public async Task<List<Evaluation>> GetByUserIdAsync(string userId) =>
        await _evaluationsCollection.Find(x => x.UserId == userId).ToListAsync();

    // En EvaluationsService.cs
    public async Task<List<Evaluation>> GetAllAsync() =>
        await _evaluationsCollection.Find(_ => true).ToListAsync();

}
