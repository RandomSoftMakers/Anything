using Anything.Core.Abstractions;
using Anything.Core.Models;

namespace Anything.Core.Services;

public sealed class AnythingSearchService
{
    private readonly IFileIndexProvider _indexProvider;

    public AnythingSearchService(IFileIndexProvider indexProvider)
    {
        _indexProvider = indexProvider;
    }

    public Task BuildIndexAsync(CancellationToken cancellationToken = default) =>
        _indexProvider.BuildInitialIndexAsync(cancellationToken);

    public Task<IEnumerable<FileEntry>> SearchAsync(string query, CancellationToken cancellationToken = default) =>
        _indexProvider.SearchAsync(query, cancellationToken);
}
