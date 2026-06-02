using System;
using HarmonyLib;
using Verse;

namespace VOEBetterPawnUI
{
    public class VOEBetterPawnUIMod : Mod
    {
        public VOEBetterPawnUIMod(ModContentPack content) : base(content)
        {
            Log.Message("[VOEBetterPawnUI] Mod constructor called. Starting Harmony patching...");
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
}