using System.Reflection;
using System.Runtime.Loader;
using Anything.Core.Abstractions;
using Anything.Core.Models;

namespace Anything.Core.Services;

public sealed class PluginManager : IDisposable
{
    private readonly List<IPlugin> _plugins = new();
    private readonly List<AssemblyLoadContext> _loadContexts = new();

    public IReadOnlyList<IPlugin> Plugins => _plugins.AsReadOnly();

    public void LoadFromDirectory(string pluginsDir)
    {
        if (!Directory.Exists(pluginsDir))
            return;

        foreach (var dll in Directory.GetFiles(pluginsDir, "*.dll"))
        {
            try
            {
                var context = new AssemblyLoadContext(Path.GetFileNameWithoutExtension(dll), isCollectible: true);
                var assembly = context.LoadFromAssemblyPath(dll);
                _loadContexts.Add(context);

                foreach (var type in assembly.GetExportedTypes())
                {
                    if (typeof(IPlugin).IsAssignableFrom(type) && !type.IsAbstract)
                    {
                        if (Activator.CreateInstance(type) is IPlugin plugin)
                        {
                            _plugins.Add(plugin);
                        }
                    }
                }
            }
            catch { }
        }
    }

    public async Task LoadAllAsync()
    {
        foreach (var plugin in _plugins)
        {
            try { await plugin.OnLoadAsync(); } catch { }
        }
    }

    public async Task UnloadAllAsync()
    {
        foreach (var plugin in _plugins)
        {
            try { await plugin.OnUnloadAsync(); } catch { }
        }
        _plugins.Clear();

        foreach (var ctx in _loadContexts)
        {
            try { ctx.Unload(); } catch { }
        }
        _loadContexts.Clear();
    }

    public async Task<IEnumerable<FileEntry>> ApplyPluginsAsync(string query, SearchOptions options, IEnumerable<FileEntry> results)
    {
        var current = results;
        foreach (var plugin in _plugins)
        {
            try { current = await plugin.OnSearchAsync(query, options, current); } catch { }
        }
        return current;
    }

    public void Dispose()
    {
        _ = UnloadAllAsync();
    }
}