using Anything.Core.Models;

namespace Anything.Core.Abstractions;

public interface IFileIndexProvider
{
    Task BuildInitialIndexAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<FileEntry>> SearchAsync(string query, CancellationToken cancellationToken = default);
}
