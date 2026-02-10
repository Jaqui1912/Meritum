namespace Meritum.Infrastructure.Services;

// Meritum.Infrastructure/Services/ProjectsService.cs
using Meritum.Core.Entities;


using Meritum.Core.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

public class ProjectsService
{
    private readonly IMongoCollection<Project> _projectsCollection;

    public ProjectsService(IOptions<MeritumDatabaseSettings> settings)
    {
        var mongoClient = new MongoClient(settings.Value.ConnectionString);
        var mongoDatabase = mongoClient.GetDatabase(settings.Value.DatabaseName);
        _projectsCollection = mongoDatabase.GetCollection<Project>(settings.Value.ProjectsCollectionName);
    }

    // Método para obtener todos (filtrado por categoría)
    public async Task<List<Project>> GetByCategoryAsync(string categoryId) =>
        await _projectsCollection.Find(x => x.CategoryId == categoryId).ToListAsync();

    // Método para obtener uno (detalle)
    public async Task<Project?> GetAsync(string id) =>
        await _projectsCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

    // Método para crear (si necesitaras subir proyectos)
    public async Task CreateAsync(Project newProject) =>
        await _projectsCollection.InsertOneAsync(newProject);
}