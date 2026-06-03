using System;
using HarmonyLib;
using Verse;

namespace VOEBetterPawnUI
{
    // The Mod constructor runs on a background thread - unsafe for textures.
    // StaticConstructorOnStartup runs on the main thread after all content loads.
    [StaticConstructorOnStartup]
    public static class VOEBetterPawnUIStartup
    {
        static VOEBetterPawnUIStartup()
        {
            Log.Message("[VOEBetterPawnUI] StaticConstructorOnStartup fired. Applying Harmony patches...");
            try
            {
                var harmony = new Harmony("com.voebetterpawnui.patch");
                harmony.PatchAll();
                Log.Message("[VOEBetterPawnUI] Harmony.PatchAll() completed successfully.");

                foreach (var method in harmony.GetPatchedMethods())
                    Log.Message("[VOEBetterPawnUI] Patched method: " + method.DeclaringType?.FullName + "." + method.Name);
            }
            catch (Exception ex)
            {
                Log.Error("[VOEBetterPawnUI] Exception during Harmony patching: " + ex);
            }
        }
    }

    // Keep a minimal Mod class so RimWorld still recognizes the mod content pack.
    public class VOEBetterPawnUIMod : Mod
    {
        public VOEBetterPawnUIMod(ModContentPack content) : base(content)
        {
            Log.Message("[VOEBetterPawnUI] Mod content pack registered: " + content.Name);
        }
    }
}