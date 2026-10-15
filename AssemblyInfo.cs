using MelonLoader;
using OnAirTap.Melon;

[assembly: MelonInfo(typeof(Plugin), "spoutplugin", "1.2.0", "milkydelta")]
[assembly: MelonGame()]
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.IL2CPP)]
[assembly: HarmonyDontPatchAll]
