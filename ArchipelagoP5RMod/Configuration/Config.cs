using System.ComponentModel;
using ArchipelagoP5RMod.Template.Configuration;

namespace ArchipelagoP5RMod.Configuration;

public class Config : Configurable<Config>
{
    [DisplayName("Server Address")]
    [Description("The archipelago server address.")]
    [DefaultValue("archipelago.gg:99999")]
    public string ServerAddress { get; set; } = "archipelago.gg:99999";

    [DisplayName("Server Password")]
    [Description("The password for the archipelago server.")]
    [DefaultValue("")]
    public string ServerPassword { get; set; } = "";
    
    [DisplayName("Slot Name")]
    [Description("The P5R slot to connect to.")]
    [DefaultValue("Slot Name")]
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