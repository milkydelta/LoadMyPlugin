using System.Runtime.InteropServices;
using HarmonyLib.Tools;
using MelonLoader;
using MelonLoader.NativeUtils;
using MelonLoader.Utils;
using UnityEngine;

namespace OnAirTap.Melon;


public class Mod : MelonMod
{


    const string configPath = "UserData/OnAirTap.cfg";

    OnAirTap.Spout.SpoutSender sp;

    NativeHook<UPLDelegate> hook;


    public override void OnInitializeMelon()
    {
        if (MelonLoader.Utils.MelonEnvironment.IsDotnetRuntime)
        {
            //We're not on mono. This must be an il2cpp game.
            LoggerInstance.Msg("BAZINGA");
            LoggerInstance.Msg(MelonEnvironment.UnityGameDataDirectory);

            // var klakhandle = System.Runtime.InteropServices.NativeLibrary.Load($@"{MelonEnvironment.UnityGameDataDirectory}\Plugins\x86_64\{Spout.Misc.pluginFilename}");
            // var klakUPL = System.Runtime.InteropServices.NativeLibrary.GetExport(klakhandle, "UnityPluginLoad");
            // var bridgehandle = System.Runtime.InteropServices.NativeLibrary.Load($@"{MelonEnvironment.UnityGameDataDirectory}\Plugins\x86_64\LIV_Bridge.dll");
            // var bridgeUPL = System.Runtime.InteropServices.NativeLibrary.GetExport(bridgehandle, "UnityPluginLoad");

            // hook = new NativeHook<UPLDelegate>(bridgehandle, klakhandle);
            // hook.Attach();
        }

        sp = new Spout.SpoutSender();
        sp.spoutName = "demo";
        Debug.Log(Spout.Misc.pluginFilename);

    }

    public override void OnUpdate()
    {
        LoggerInstance.Msg(Plugin.interfaces);
        try
        {
            //LoggerInstance.Msg("begin");

            if (Time.frameCount % 30 >= 15)
            {
                sp.sourceTexture = Texture2D.redTexture;
            }
            else
            {
                sp.sourceTexture = Texture2D.whiteTexture;
            }
            //LoggerInstance.Msg("bing");
            //LoggerInstance.Msg(sp.sourceTexture.ToString());
            sp.CaptureFrame();
            //LoggerInstance.Msg("bong");
        }
        catch (Exception e)
        {
            LoggerInstance.Msg(e.ToString());
        }

    }
}
