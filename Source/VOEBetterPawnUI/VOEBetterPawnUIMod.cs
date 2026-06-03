using System;
using HarmonyLib;
using Verse;

namespace VOEBetterPawnUI
{
    [StaticConstructorOnStartup]
    public static class VOEBetterPawnUIStartup
    {
        static VOEBetterPawnUIStartup()
        {
            try
            {
                var harmony = new Harmony("com.voebetterpawnui.patch");
                harmony.PatchAll();
                Log.Message("[VOEBetterPawnUI] Patches applied successfully.");
            }
            catch (Exception ex)
            {
                Log.Error("[VOEBetterPawnUI] Exception during Harmony patching: " + ex);
            }
        }
    }

    public class VOEBetterPawnUIMod : Mod
    {
        public VOEBetterPawnUIMod(ModContentPack content) : base(content) { }
    }
}