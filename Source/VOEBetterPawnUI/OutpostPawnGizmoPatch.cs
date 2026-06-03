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
            string addLabel  = "Outposts.Commands.AddPawn.Label".Translate().ToString();
            string takeLabel = "VOEPrisonerPatch.Commands.TakePawn.Label".Translate().ToString();

            foreach (var gizmo in __result)
            {
                if (gizmo is Command_Action cmd)
                {
                    if (cmd.defaultLabel == addLabel)
                    {
                        yield return new Command_Action
                        {
                            action       = () => Find.WindowStack.Add(
                                new Dialog_ManagePawns(__instance, caravan, Dialog_ManagePawns.Mode.Add)),
                            defaultLabel = cmd.defaultLabel,
                            defaultDesc  = cmd.defaultDesc,
                            icon         = cmd.icon
                        };
                    }
                    else if (cmd.defaultLabel == takeLabel)
                    {
                        yield return new Command_Action
                        {
                            action         = () => Find.WindowStack.Add(
                                new Dialog_ManagePawns(__instance, caravan, Dialog_ManagePawns.Mode.Remove)),
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
            string removeLabel = "Outposts.Commands.Remove.Label".Translate().ToString();
            string takeLabel   = "VOEPrisonerPatch.Commands.TakePawn.Label".Translate().ToString();

            foreach (var gizmo in __result)
            {
                if (gizmo is Command_Action cmd)
                {
                    if (cmd.defaultLabel == removeLabel)
                    {
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
                else
                {
                    yield return gizmo;
                }
            }
        }
    }
}