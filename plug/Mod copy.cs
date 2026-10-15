using System.Runtime.InteropServices;
using HarmonyLib.Tools;
using MelonLoader;
using MelonLoader.NativeUtils;
using MelonLoader.Utils;
using UnityEngine;
using NativeLibrary = MelonLoader.NativeLibrary;

namespace OnAirTap.Melon;

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
        _instance.LoggerInstance.Msg($"ins: {ins}");

        klak(ins);

        hook.Trampoline(ins);
    }

    public override void OnPreInitialization()
    {
        _instance = this;
        if (MelonEnvironment.IsDotnetRuntime)
        {
            //We're not on mono. This must be an il2cpp game.
            var klakhandle = NativeLibrary.Load($@"{MelonEnvironment.UnityGameDataDirectory}\Plugins\x86_64\{Spout.Misc.pluginFilename}");
            var klakUPL = klakhandle.GetExport("UnityPluginLoad");

            klak = Marshal.GetDelegateForFunctionPointer<UPLDelegate>(klakUPL);

            var bridgehandle = NativeLibrary.Load($@"{MelonEnvironment.UnityGameDataDirectory}\Plugins\x86_64\UnityOpenXR.dll");
            var bridgeUPL = bridgehandle.GetExport("UnityPluginLoad");

            _patch = UnityPluginLoad;

            var patchptr = Marshal.GetFunctionPointerForDelegate(_patch);

            hook = new NativeHook<UPLDelegate>(bridgeUPL, patchptr);
            hook.Attach();
        }
    }

}