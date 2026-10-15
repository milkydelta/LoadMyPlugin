using System.Runtime.InteropServices;
using HarmonyLib.Tools;
using MelonLoader;
using MelonLoader.NativeUtils;
using MelonLoader.Utils;
using NativeLibrary = MelonLoader.NativeLibrary;

namespace milkydelta.LoadMyPlugin;

[UnmanagedFunctionPointer(CallingConvention.StdCall)]
public delegate void UPLDelegate(
IntPtr interfaces
);

public class Plugin : MelonPlugin
{
    static Plugin _instance;

    public static IntPtr interfaces = IntPtr.Zero;

    static NativeHook<UPLDelegate> hook;

    static UPLDelegate klak;

    static UPLDelegate _patch;


    public static void UnityPluginLoad(IntPtr ins)
    {
        interfaces = ins;
        _instance.LoggerInstance.Msg($"IUnityInterfaces: 0x{ins.ToString("X16")}");

        klak(ins);

        hook.Trampoline(ins);
    }

    public override void OnPreInitialization()
    {
        _instance = this;
        if (MelonEnvironment.IsDotnetRuntime)
        {
            //We're not on mono. This must be an il2cpp game.
            string klakpath = $@"{MelonEnvironment.UnityGameDataDirectory}\Plugins\x86_64\{OnAirTap.Spout.Misc.pluginFilename}";
            LoggerInstance.Msg($"Opening plugin library: {klakpath}");
            var klakhandle = NativeLibrary.Load(klakpath);
            var klakUPL = klakhandle.GetExport("UnityPluginLoad");
            LoggerInstance.Msg($"Plugin UnityPluginLoad: 0x{klakUPL.ToString("X16")}");

            klak = Marshal.GetDelegateForFunctionPointer<UPLDelegate>(klakUPL);

            string bridgepath = $@"{MelonEnvironment.UnityGameDataDirectory}\Plugins\x86_64\UnityOpenXR.dll";
            LoggerInstance.Msg($"Opening target library: {bridgepath}");
            var bridgehandle = NativeLibrary.Load(bridgepath);
            var bridgeUPL = bridgehandle.GetExport("UnityPluginLoad");
            LoggerInstance.Msg($"Target UnityPluginLoad: 0x{bridgeUPL.ToString("X16")}");

            _patch = UnityPluginLoad;

            var patchptr = Marshal.GetFunctionPointerForDelegate(_patch);

            hook = new NativeHook<UPLDelegate>(bridgeUPL, patchptr);
            hook.Attach();
            LoggerInstance.Msg("Native hook attached.");
        }
    }

}