using Anything.Core.Abstractions;
using Anything.Core.Models;

namespace Anything.Indexer.Daemon;

public sealed class DaemonFileIndexProvider : IFileIndexProvider
{
    private readonly IndexerClient _client;

    public DaemonFileIndexProvider(IndexerClient client)
    {
        _client = client;
    }

    public Task BuildInitialIndexAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<IEnumerable<FileEntry>> SearchAsync(string query, SearchOptions? options = null, CancellationToken cancellationToken = default)
    {
        return _client.SearchAsync(query, options);
    }
}
