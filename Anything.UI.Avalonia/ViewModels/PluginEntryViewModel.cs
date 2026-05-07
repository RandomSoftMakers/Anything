using Anything.Core.Abstractions;

namespace Anything.UI.Avalonia.ViewModels;

public class PluginEntryViewModel
{
    public string Id { get; }
    public string Name { get; }
    public string Version { get; }
    public string Description { get; }
    public bool IsEnabled { get; set; } = true;

    public PluginEntryViewModel(IPlugin plugin)
    {
        Id = plugin.Id;
        Name = plugin.Name;
        Version = plugin.Version;
        Description = plugin.Description;
    }
}