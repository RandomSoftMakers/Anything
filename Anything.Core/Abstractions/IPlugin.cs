using Anything.Core.Models;

namespace Anything.Core.Abstractions;

public interface IPlugin
{
    string Id { get; }
    string Name { get; }
    string Version { get; }
    string Description { get; }

    Task OnLoadAsync();
    Task OnUnloadAsync();
    Task<IEnumerable<FileEntry>> OnSearchAsync(string query, SearchOptions options, IEnumerable<FileEntry> results);
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class PluginAttribute : Attribute
{
    public string Id { get; }
    public string Name { get; }
    public string Version { get; }
    public string Description { get; }

    public PluginAttribute(string id, string name, string version, string description = "")
    {
        Id = id;
        Name = name;
        Version = version;
        Description = description;
    }
}