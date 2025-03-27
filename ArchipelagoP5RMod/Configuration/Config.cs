using System.ComponentModel;
using ArchipelagoP5RMod.Template.Configuration;
using Reloaded.Mod.Interfaces.Structs;

namespace ArchipelagoP5RMod.Configuration;

public class Config : Configurable<Config>
{
    [DisplayName("Save Directory")]
    [Description("A path to a place on your computer to save additional data. Different from your P5R save directory.")]
    [FolderPickerParams(
        initialFolderPath: Environment.SpecialFolder.MyDocuments,
        userCanEditPathText: true,
        title: "AP Mod Save Directory",
        okButtonLabel: "Choose Folder",
        fileNameLabel: "AP Mod Save Folder",
        multiSelect: false,
        forceFileSystem: false)]
    public string SaveDirectory { get; set; } = "";
    
    [DisplayName("Server Address")]
    [Description("The archipelago server address.")]
    [DefaultValue("archipelago.gg:99999")]
    public string ServerAddress { get; set; } = "archipelago.gg:99999";

    [DisplayName("Server Password")]
    [Description("The password for the archipelago server.")]
    public string ServerPassword { get; set; } = "";
    
    [DisplayName("Slot Name")]
    [Description("The P5R slot to connect to.")]
    [DefaultValue("Player1")]
    public string SlotName { get; set; } = "Slot Name";
}

/// <summary>
/// Allows you to override certain aspects of the configuration creation process (e.g. create multiple configurations).
/// Override elements in <see cref="ConfiguratorMixinBase"/> for finer control.
/// </summary>
public class ConfiguratorMixin : ConfiguratorMixinBase
{
    // 
}