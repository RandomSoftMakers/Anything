using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using Anything.Core.Services;
using Microsoft.Extensions.Logging;

namespace Anything.Indexer.Daemon;

public sealed class FileIndexerDaemon : IAsyncDisposable
{
    private readonly FileIndexer _indexer;
    private readonly ILogger<FileIndexerDaemon> _logger;
    private CancellationTokenSource? _cts;
    private Task? _pipeServerTask;
    private const string PipeName = "anything-indexer";

    public FileIndexerDaemon(ILogger<FileIndexerDaemon> logger)
    {
        _logger = logger;
        _indexer = new FileIndexer();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _logger.LogInformation("Building initial file index...");
        await _indexer.BuildInitialIndexAsync(_cts.Token);
        _logger.LogInformation("Index build complete. Starting named pipe server...");

        _pipeServerTask = RunPipeServerAsync(_cts.Token);
    }

    private async Task RunPipeServerAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var transmission = OperatingSystem.IsWindows()
                    ? PipeTransmissionMode.Message
                    : PipeTransmissionMode.Byte;
                await using var server = new NamedPipeServerStream(
                    PipeName,
                    PipeDirection.InOut,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    transmission,
                    PipeOptions.Asynchronous);

                _logger.LogDebug("Waiting for pipe connection...");
                await server.WaitForConnectionAsync(cancellationToken);

                _logger.LogDebug("Pipe client connected");
                _ = HandleClientAsync(server, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Pipe server error");
            }
        }
    }

    private async Task HandleClientAsync(NamedPipeServerStream server, CancellationToken cancellationToken)
    {
        try
        {
            using var reader = new StreamReader(server, Encoding.UTF8, leaveOpen: true);
            var requestJson = await reader.ReadLineAsync(cancellationToken);

            if (string.IsNullOrEmpty(requestJson))
                return;

            var request = JsonSerializer.Deserialize<IndexerRequest>(requestJson);
            if (request == null)
                return;

            var response = await ProcessRequestAsync(request, cancellationToken);
            var responseJson = JsonSerializer.Serialize(response);
            var responseBytes = Encoding.UTF8.GetBytes(responseJson + "\n");

            await server.WriteAsync(responseBytes, cancellationToken);
            await server.FlushAsync(cancellationToken);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling pipe client");
        }
        finally
        {
            server.Disconnect();
        }
    }

    private async Task<IndexerResponse> ProcessRequestAsync(IndexerRequest request, CancellationToken cancellationToken)
    {
        if (request.Action == "search")
        {
            var options = request.ToSearchOptions();
            var results = await _indexer.SearchAsync(request.Query, options, cancellationToken);
            return IndexerResponse.CreateSuccess(results.Select(r => new FileEntryDto
            {
                Path = r.Path,
                Name = r.Name,
                Size = r.Size,
                LastModifiedUtc = r.LastModifiedUtc,
                IsDirectory = r.IsDirectory
            }).ToList());
        }

        if (request.Action == "ping")
        {
            return IndexerResponse.CreateSuccess(new List<FileEntryDto>());
        }

        return IndexerResponse.CreateError($"Unknown action: {request.Action}");
    }

    public async ValueTask DisposeAsync()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        await _indexer.DisposeAsync();
    }
}
