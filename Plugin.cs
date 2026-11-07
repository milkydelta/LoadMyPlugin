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

    bool ApplyTargetHook(string targetPath) {
        if (!File.Exists(targetPath)) {
            LoggerInstance.Error($"Target Plugin {targetPath} does not exist!");
            return false;
        }

        LoggerInstance.Msg($"Opening target library: {targetPath}");
        try {
            //Get the function
            NativeLibrary lib = NativeLibrary.Load(targetPath);
            IntPtr targetUPL = lib.GetExport("UnityPluginLoad");
            LoggerInstance.Msg($"Target UnityPluginLoad: 0x{targetUPL.ToString("X16")}");

            //Patch it to call me instead.
            IntPtr patchPtr = Marshal.GetFunctionPointerForDelegate(_holdingDelegate);
            hook = new NativeHook<UPLDelegate>(targetUPL, patchPtr);
            hook.Attach();

            LoggerInstance.Msg("Native hook attached.");
        } catch (Exception e) {
            LoggerInstance.Error(e);
            LoggerInstance.Error("Could not load and hook target library.");
            return false;
        }

        return true;
    }

    public override void OnPreInitialization()
    {
        _instance = this;

        if (!MelonEnvironment.IsDotnetRuntime) { return; }

        // Set up config
        category = MelonPreferences.CreateCategory("milkydelta_LoadMyPlugin");
        entryHookPlugin = category.CreateEntry<string>("PluginToHook", "x86_64\\UnityOpenXR.dll");
        entryLoadPlugins = category.CreateEntry<string[]>("PluginsToLoad", new string[] { });
        // entryLoadFromFolder = category.CreateEntry<bool>("LoadFromFolder", false);

        // Base path for all a game's included plugins. Also, the place where you should put plugins on Mono.
        // Plugin files may be in this folder, or in an architecture-specific subfolder (x86, x86_64, ...)
        string bas = Path.Combine(MelonEnvironment.UnityGameDataDirectory, "Plugins");
        
        if (ApplyTargetHook(Path.Combine(bas,entryHookPlugin.Value)) == false) {
            return;
        } 

        HashSet<string> pluginsUnique = new();

        AddOATSpoutPluginToSet(pluginsUnique);

        foreach (string file in entryLoadPlugins.Value)
        {
            string path = Path.Combine(bas, file);
            pluginsUnique.Add(path);
        }

        LoggerInstance.Msg($"{pluginsUnique.Count} plugins to load.");

        foreach (string path in pluginsUnique)
        {
            AddPlugin(path);
        }
    }

    // Blatant preferential treatment for myself, but this is the entire reason the project exists.
    void AddOATSpoutPluginToSet(HashSet<string> set) {
        Type miscType = null;

        // Get the OAT Spout misc type from loaded assemblies (UserLibs load before MelonPlugins)
        var asse = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.FullName.Contains("OAT"));
        foreach (var a in asse) {
            foreach (var t in a.ExportedTypes) {
                if (t.FullName == "OnAirTap.Spout.Misc") { miscType = t; break; }
            }
            if (miscType != null) { break; }
        }

        // Couldn't find it. No point in continuing.
        if (miscType == null) {return;}

        // Pull the filename through reflection.
        string filename = "";
        try {
            filename = (string)miscType.GetProperty("pluginFilename").GetValue(null);
        } catch { return; }

        // try to get the full path
        string basePath = Path.Combine(MelonEnvironment.UnityGameDataDirectory, "Plugins");
        string f = Path.Combine(basePath, filename);
        if (!File.Exists(f)) { f = Path.Combine(basePath, "x86_64", filename);}


        if (File.Exists(f))
        {
            LoggerInstance.Msg($"Found OAT spout plugin {filename}");
            set.Add(f);
        }
    }
}