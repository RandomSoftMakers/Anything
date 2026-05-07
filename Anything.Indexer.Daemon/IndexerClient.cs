using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using Anything.Core.Models;

namespace Anything.Indexer.Daemon;

public sealed class IndexerClient : IAsyncDisposable
{
    private const string PipeName = "anything-indexer";
    private readonly int _connectTimeoutMs;

    public IndexerClient(int connectTimeoutMs = 3000)
    {
        _connectTimeoutMs = connectTimeoutMs;
    }

    public async Task<bool> PingAsync()
    {
        try
        {
            var response = await SendRequestAsync(new IndexerRequest { Action = "ping" });
            return response?.IsSuccess == true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IEnumerable<FileEntry>> SearchAsync(string query, SearchOptions? options = null)
    {
        options ??= new SearchOptions();

        var request = new IndexerRequest
        {
            Action = "search",
            Query = query,
            MaxResults = options.MaxResults,
            MatchCase = options.MatchCase,
            MatchWholeWord = options.MatchWholeWord,
            MatchPath = options.MatchPath,
            UseRegex = options.UseRegex,
            TypeFilter = (int)options.TypeFilter,
            MinSize = options.MinSize,
            MaxSize = options.MaxSize
        };

        var response = await SendRequestAsync(request);
        if (response?.IsSuccess == true && response.Results != null)
        {
            return response.Results.Select(dto => dto.ToFileEntry());
        }

        return Array.Empty<FileEntry>();
    }

    private async Task<IndexerResponse?> SendRequestAsync(IndexerRequest request)
    {
        using var cts = new CancellationTokenSource(_connectTimeoutMs);

        try
        {
            await using var client = new NamedPipeClientStream(
                ".",
                PipeName,
                PipeDirection.InOut,
                PipeOptions.Asynchronous);

            await client.ConnectAsync(cts.Token);

            var requestJson = JsonSerializer.Serialize(request) + "\n";
            var requestBytes = Encoding.UTF8.GetBytes(requestJson);
            await client.WriteAsync(requestBytes, cts.Token);
            await client.FlushAsync(cts.Token);

            using var reader = new StreamReader(client, Encoding.UTF8);
            var responseJson = await reader.ReadLineAsync(cts.Token);

            if (string.IsNullOrEmpty(responseJson))
                return null;

            return JsonSerializer.Deserialize<IndexerResponse>(responseJson);
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException("Connection to indexer daemon timed out. Make sure the daemon is running.");
        }
        catch (FileNotFoundException)
        {
            throw new InvalidOperationException("Indexer daemon is not running. Start it with 'anything-indexer' or register as a service.");
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
