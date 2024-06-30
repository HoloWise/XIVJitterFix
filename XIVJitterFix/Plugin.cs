using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Game;
using System.Runtime.InteropServices;
using Dalamud.Interface.Windowing;

namespace XIVJitterFix;

public sealed class Plugin : IDalamudPlugin
{
    private readonly nint hookAddr;

    private readonly IPluginLog logger;
    private readonly IFramework framework;
    private readonly WindowSystem windowSystem;
    private readonly IDalamudPluginInterface dalamudPluginInterface;
    private readonly MainWindow mainWindow;

    public unsafe Plugin(IPluginLog logger, ISigScanner sigScanner,
        IFramework framework, IDalamudPluginInterface dalamudPluginInterface)
    {
        this.logger = logger;
        this.framework = framework;
        this.dalamudPluginInterface = dalamudPluginInterface;
        windowSystem = new("XIVJitterFix");
        mainWindow = new MainWindow();
        windowSystem.AddWindow(mainWindow);

        hookAddr = sigScanner.GetStaticAddressFromSig("48 89 05 ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8D 8F ?? ?? ?? ??");

        framework.Update += Framework_Update;
        dalamudPluginInterface.UiBuilder.OpenConfigUi += UiBuilder_OpenConfigUi;
        dalamudPluginInterface.UiBuilder.Draw += UiBuilder_Draw;
    }

    private void UiBuilder_Draw()
    {
        windowSystem.Draw();
    }

    private void UiBuilder_OpenConfigUi()
    {
        mainWindow.Toggle();
    }

    private unsafe void Framework_Update(IFramework framework)
    {
        if (hookAddr == nint.Zero) return;
        var addr1 = *(nint*)hookAddr;
        if (addr1 == nint.Zero) return;

        ProbablySomeGraphicsConfig* config = (ProbablySomeGraphicsConfig*)addr1;

        var prevValue1 = config->NpcGposeJitter;
        var prevValue2 = config->CutsceneJitter;

        if (prevValue1 != 1 || prevValue2 != 1)
        {
            config->NpcGposeJitter = 1;
            config->CutsceneJitter = 1;
            logger.Verbose("Detected change NpcGpose {0} / Cutscene {1} -> NpcGpose {2} / Cutscene {3}", prevValue1, prevValue2, config->NpcGposeJitter, config->CutsceneJitter);
        }
    }

    public void Dispose()
    {
        windowSystem.RemoveAllWindows();

        dalamudPluginInterface.UiBuilder.OpenConfigUi -= UiBuilder_OpenConfigUi;
        dalamudPluginInterface.UiBuilder.Draw -= UiBuilder_Draw;

        framework.Update -= Framework_Update;
    }

    [StructLayout(LayoutKind.Explicit)]
    public partial struct ProbablySomeGraphicsConfig
    {
        [FieldOffset(0x19)] public byte NpcGposeJitter;
        [FieldOffset(0x1a)] public byte CutsceneJitter;
    }
}
