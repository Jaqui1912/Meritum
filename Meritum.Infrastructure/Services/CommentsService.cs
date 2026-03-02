using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Meritum.Core.Entities;
using Meritum.Core.Settings;

namespace Meritum.Infrastructure.Services;

public class CommentsService
{
    private readonly IMongoCollection<Comment> _commentsCollection;

    public CommentsService(IOptions<MeritumDatabaseSettings> databaseSettings)
    {
        var mongoClient = new MongoClient(databaseSettings.Value.ConnectionString);
        var mongoDatabase = mongoClient.GetDatabase(databaseSettings.Value.DatabaseName);

        _commentsCollection = mongoDatabase.GetCollection<Comment>(databaseSettings.Value.CommentsCollectionName);
    }

    public async Task<List<Comment>> GetByProjectIdAsync(string projectId) =>
        await _commentsCollection.Find(c => c.ProjectId == projectId)
                                 .SortByDescending(c => c.CreatedAt)
                                 .ToListAsync();

    public async Task CreateAsync(Comment newComment) =>
        await _commentsCollection.InsertOneAsync(newComment);

    public async Task RemoveAsync(string id) =>
        await _commentsCollection.DeleteOneAsync(x => x.Id == id);
}
