using HarmonyLib;
using Verse;

namespace VOEBetterPawnUI
{
    public class VOEBetterPawnUIMod : Mod
    {
        public VOEBetterPawnUIMod(ModContentPack content) : base(content)
        {
            var harmony = new Harmony("com.voebetterpawnui.patch");
            harmony.PatchAll();
        }
    }
}
