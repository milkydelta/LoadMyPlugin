using System.Runtime.InteropServices;
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

    static UPLDelegate klak = null;



    MelonPreferences_Category category;
    MelonPreferences_Entry<bool> entryLoadFromFolder;
    MelonPreferences_Entry<string[]> entryLoadPlugins;
    MelonPreferences_Entry<string> entryHookPlugin;

    private static UPLDelegate _holdingDelegate = UnityPluginLoad; //static so there's no chance of it being GC'ed
    public static void UnityPluginLoad(IntPtr ins)
    {
        //Store the interface pointer
        interfaces = ins;
        _instance.LoggerInstance.Msg($"IUnityInterfaces: 0x{ins.ToString("X16")}");

        try {
            //Load any registered plugins
            klak?.Invoke(ins);
        } finally {
            //Call the original function
            hook.Trampoline(ins);
        }
    }

    private void AddPlugin(string path)
    {
        if (!File.Exists(path)) {
            LoggerInstance.Error($"Plugin {path} does not exist!");
            return;
        }

        LoggerInstance.Msg($"Opening plugin library: {path}");
        NativeLibrary handle = NativeLibrary.Load(path);
        IntPtr UPL = handle.GetExport("UnityPluginLoad");
        LoggerInstance.Msg($"Plugin UnityPluginLoad: 0x{UPL.ToString("X16")}");

        var deleg = Marshal.GetDelegateForFunctionPointer<UPLDelegate>(UPL);

        if (klak == null) {klak = deleg;}
        else {klak += deleg;}

        LoggerInstance.Msg($"Added Plugin {Path.GetFileName(path)}");
    }

    public override void OnPreInitialization()
    {
        _instance = this;

        if (!MelonEnvironment.IsDotnetRuntime) { return; }
        
        category = MelonPreferences.CreateCategory("milkydelta_LoadMyPlugin");
        entryHookPlugin = category.CreateEntry<string>("PluginToHook", "x86_64\\UnityOpenXR.dll");
        entryLoadPlugins = category.CreateEntry<string[]>("PluginsToLoad", new string[] { });
        entryLoadFromFolder = category.CreateEntry<bool>("LoadFromFolder", false);

        {
            var path = Path.Combine(MelonEnvironment.UnityGameDataDirectory, "Plugins",entryHookPlugin.Value);
            if (!File.Exists(path))
            {
                LoggerInstance.Error($"Target Plugin {path} does not exist!");
                return;
            }

            LoggerInstance.Msg($"Opening target library: {path}");
            try
            {
                //Get the function
                NativeLibrary lib = NativeLibrary.Load(path);
                IntPtr targetUPL = lib.GetExport("UnityPluginLoad");
                LoggerInstance.Msg($"Target UnityPluginLoad: 0x{targetUPL.ToString("X16")}");

                //Patch it to call me instead.
                IntPtr patchPtr = Marshal.GetFunctionPointerForDelegate(_holdingDelegate);
                hook = new NativeHook<UPLDelegate>(targetUPL, patchPtr);
                hook.Attach();

                LoggerInstance.Msg("Native hook attached.");

            }
            catch (Exception e)
            {
                LoggerInstance.Error(e);
                LoggerInstance.Error("Could not load and hook target library.");
                return;
            }

        }

        HashSet<string> pluginsUnique = new();

        {
            string bas = Path.Combine(MelonEnvironment.UnityGameDataDirectory, "Plugins");
            foreach (string file in entryLoadPlugins.Value)
            {
                string path = Path.Combine(bas, file);
                pluginsUnique.Add(path);
            }
        }

        foreach (string path in pluginsUnique)
        {
            AddPlugin(path);
        }
    }
}