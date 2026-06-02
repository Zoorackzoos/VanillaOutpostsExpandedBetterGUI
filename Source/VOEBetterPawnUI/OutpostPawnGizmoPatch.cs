using System.Collections.Generic;
using HarmonyLib;
using Outposts;
using RimWorld.Planet;
using Verse;

namespace VOEBetterPawnUI
{
    [HarmonyPatch]
    public static class OutpostPawnGizmoPatch
    {
        [HarmonyPatch(typeof(Outpost), nameof(Outpost.GetCaravanGizmos))]
        [HarmonyPostfix]
        public static IEnumerable<Gizmo> GetCaravanGizmos_Postfix(
            IEnumerable<Gizmo> __result,
            Outpost __instance,
            Caravan caravan)
        {
            Log.Message("[VOEBetterPawnUI] GetCaravanGizmos_Postfix fired for outpost: " + __instance?.Label);

            string addLabel = "Outposts.Commands.AddPawn.Label".Translate().ToString();

            foreach (var gizmo in __result)
            {
                if (gizmo is Command_Action cmd && cmd.defaultLabel == addLabel)
                {
                    Log.Message("[VOEBetterPawnUI] Found 'Add pawn' gizmo - replacing with Dialog_ManagePawns.");
                    yield return new Command_Action
                    {
                        action       = () => Find.WindowStack.Add(
                                           new Dialog_ManagePawns(__instance, caravan, Dialog_ManagePawns.Mode.Add)),
                        defaultLabel = cmd.defaultLabel,
                        defaultDesc  = cmd.defaultDesc,
                        icon         = cmd.icon
                    };
                }
                else
                {
                    yield return gizmo;
                }
            }
        }

        [HarmonyPatch(typeof(Outpost), nameof(Outpost.GetGizmos))]
        [HarmonyPostfix]
        public static IEnumerable<Gizmo> GetGizmos_Postfix(
            IEnumerable<Gizmo> __result,
            Outpost __instance)
        {
            Log.Message("[VOEBetterPawnUI] GetGizmos_Postfix fired for outpost: " + __instance?.Label);

            string removeLabel = "Outposts.Commands.Remove.Label".Translate().ToString();

            foreach (var gizmo in __result)
            {
                if (gizmo is Command_Action cmd && cmd.defaultLabel == removeLabel)
                {
                    Log.Message("[VOEBetterPawnUI] Found 'Remove pawn' gizmo - replacing with Dialog_ManagePawns.");
                    yield return new Command_Action
                    {
                        action         = () => Find.WindowStack.Add(
                                             new Dialog_ManagePawns(__instance, null, Dialog_ManagePawns.Mode.Remove)),
                        defaultLabel   = cmd.defaultLabel,
                        defaultDesc    = cmd.defaultDesc,
                        icon           = cmd.icon,
                        Disabled       = cmd.Disabled,
                        disabledReason = cmd.disabledReason
                    };
                }
                else
                {
                    yield return gizmo;
                }
            }
        }
    }
}