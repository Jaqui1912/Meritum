namespace Meritum.Infrastructure.Services;

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

    // 1. Obtener TODOS (Para el Admin general)
    public async Task<List<Project>> GetAllAsync() =>
        await _projectsCollection.Find(_ => true).ToListAsync();

    // 2. Obtener por ID (Para ver detalle o editar)
    public async Task<Project?> GetByIdAsync(string id) =>
        await _projectsCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

    // 3. Obtener por categoria
    public async Task<List<Project>> GetByCategoryAsync(string categoryId) =>
        await _projectsCollection.Find(x => x.CategoryId == categoryId).ToListAsync();

    // 4. Validar Duplicados
    public async Task<Project?> GetByNameAsync(string title) =>
        await _projectsCollection.Find(x => x.Title == title).FirstOrDefaultAsync();

    // CRUD 
    public async Task CreateAsync(Project newProject) =>
        await _projectsCollection.InsertOneAsync(newProject);

    public async Task UpdateAsync(string id, Project updatedProject) =>
        await _projectsCollection.ReplaceOneAsync(x => x.Id == id, updatedProject);

    public async Task RemoveAsync(string id) =>
        await _projectsCollection.DeleteOneAsync(x => x.Id == id);
}