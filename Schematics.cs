using Minecraft.Server.FourKit;
using Minecraft.Server.FourKit.Plugin;

using Schematics.Commands;

namespace Schematics;

public class Schematics : ServerPlugin
{
    public override string name    => "Schematics";
    public override string version => "0.1.0";
    public override string author  => "BanditVault";

    public override void onEnable()
    {
        FourKit.getCommand("schem").setExecutor(new SchemCommand(this));
        FourKit.getCommand("schem").setDescription("Copy and paste block regions across saves");
        FourKit.getCommand("schem").setUsage("/schem export|paste|list|info");

        Console.WriteLine("[Schematics] enabled. Data folder: " + Schematic.SchematicStore.DataFolder);
    }

    public override void onDisable()
    {
        Console.WriteLine("[Schematics] disabled.");
    }
}
