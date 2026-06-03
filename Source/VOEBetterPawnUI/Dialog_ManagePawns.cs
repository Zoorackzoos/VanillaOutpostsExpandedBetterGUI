using System.Collections.Generic;
using System.Linq;
using Outposts;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace VOEBetterPawnUI
{
    public class Dialog_ManagePawns : Window
    {
        public enum Mode { Add, Remove }

        private const float RowHeight    = 48f;
        private const float ButtonW      = 150f;
        private const float ButtonH      = 40f;
        private const float PortraitSize = 36f;
        private const float CheckboxSize = 24f;
        private const float ScrollBarW   = 16f;
        private const float HeaderH      = 35f;
        private const float SubtitleH    = 22f;
        private const float ButtonGap    = 8f;

        private readonly Outpost outpost;
        private readonly Caravan caravan;
        private readonly Mode    mode;

        private List<PawnRow> rows;
        private Vector2       scrollPos;

        public Dialog_ManagePawns(Outpost outpost, Caravan caravan, Mode mode)
        {
            this.outpost = outpost;
            this.caravan = caravan;
            this.mode    = mode;

            doCloseX              = true;
            doCloseButton         = false;
            closeOnClickedOutside = false;
            absorbInputAroundWindow = true;
            forcePause            = true;
        }

        public override Vector2 InitialSize =>
            new Vector2(620f, Mathf.Min(700f, UI.screenHeight - 100f));

        private void BuildRows()
        {
            rows = new List<PawnRow>();
            if (mode == Mode.Add)
            {
                foreach (var p in caravan.PawnsListForReading)
                {
                    bool ok = outpost.Ext.CanAddPawn(p, out string reason);
                    rows.Add(new PawnRow(p, ok, reason));
                }
            }
            else
            {
                var list = outpost.AllPawns.ToList();
                foreach (var p in list)
                {
                    bool ok    = list.Count > 1;
                    string reason = ok ? null : "Outposts.Command.Remove.Only1".Translate();
                    rows.Add(new PawnRow(p, ok, reason));
                }
            }
        }

        public override void PostOpen()
        {
            base.PostOpen();
            BuildRows();
        }

        public override void DoWindowContents(Rect inRect)
        {
            float y = 0f;

            // Title
            Text.Font = GameFont.Medium;
            string title = mode == Mode.Add
                ? "Outposts.Commands.AddPawn.Label".Translate().ToString()
                : "Outposts.Commands.Remove.Label".Translate().ToString();
            Widgets.Label(new Rect(0f, y, inRect.width, HeaderH), title);
            y += HeaderH;
            Text.Font = GameFont.Small;

            // Subtitle
            GUI.color = Color.gray;
            string subtitle = mode == Mode.Add
                ? caravan.Name + "  →  " + (outpost.HasName ? outpost.Label : outpost.def.LabelCap.ToString())
                : (outpost.HasName ? outpost.Label : outpost.def.LabelCap.ToString()) + "  →  new caravan";
            Widgets.Label(new Rect(0f, y, inRect.width, SubtitleH), subtitle);
            GUI.color = Color.white;
            y += SubtitleH + 4f;

            // Scroll list
            float bottomAreaH = ButtonH + 12f;
            float listH       = rows.Count * RowHeight;
            Rect  outerRect   = new Rect(0f, y, inRect.width, inRect.height - y - bottomAreaH);
            Rect  viewRect    = new Rect(0f, 0f, outerRect.width - ScrollBarW, listH);

            Widgets.BeginScrollView(outerRect, ref scrollPos, viewRect);
            for (int i = 0; i < rows.Count; i++)
                DrawRow(new Rect(0f, i * RowHeight, viewRect.width, RowHeight), rows[i], i);
            Widgets.EndScrollView();

            // Separator
            float sepY = inRect.height - bottomAreaH - 2f;
            Widgets.DrawLineHorizontal(0f, sepY, inRect.width);

            // Bottom buttons
            DrawBottomButtons(inRect, inRect.height - ButtonH - 4f);
        }

        private void DrawRow(Rect rect, PawnRow row, int index)
        {
            if (index % 2 == 0) Widgets.DrawAltRect(rect);
            if (row.enabled && Mouse.IsOver(rect)) Widgets.DrawHighlight(rect);

            float x = rect.x + 6f;

            // --- Checkbox (draw only, no interaction here) ---
            Rect cbRect = new Rect(x, rect.y + (RowHeight - CheckboxSize) / 2f, CheckboxSize, CheckboxSize);
            if (row.enabled)
                Widgets.CheckboxDraw(cbRect.x, cbRect.y, row.selected, false, CheckboxSize);
            else
            {
                GUI.color = Color.gray;
                Widgets.CheckboxDraw(cbRect.x, cbRect.y, false, true, CheckboxSize);
                GUI.color = Color.white;
            }
            x += CheckboxSize + 8f;

            // --- Portrait ---
            Rect portraitRect = new Rect(x, rect.y + (RowHeight - PortraitSize) / 2f, PortraitSize, PortraitSize);
            GUI.DrawTexture(portraitRect,
                PortraitsCache.Get(row.pawn, new Vector2(PortraitSize, PortraitSize), Rot4.South));
            x += PortraitSize + 8f;

            // --- Name + skill line ---
            if (!row.enabled) GUI.color = Color.gray;

            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(x, rect.y + 6f, rect.width - x - 4f, 22f),
                row.pawn.NameFullColored.CapitalizeFirst().Resolve());

            Text.Font = GameFont.Tiny;
            GUI.color = row.enabled ? Color.gray : new Color(1f, 0.5f, 0.5f);
            string infoLine = row.enabled ? PawnSkillSummary(row.pawn) : row.disabledReason;
            Widgets.Label(new Rect(x, rect.y + 26f, rect.width - x - 4f, 18f), infoLine);

            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            if (!row.disabledReason.NullOrEmpty())
                TooltipHandler.TipRegion(rect, row.disabledReason);

            // --- Single click handler on whole row ---
            if (row.enabled && Widgets.ButtonInvisible(rect))
            {
                row.selected = !row.selected;
                if (row.selected)
                    SoundDefOf.Tick_High.PlayOneShotOnCamera();
                else
                    SoundDefOf.Tick_Low.PlayOneShotOnCamera();
            }
        }

        private void DrawBottomButtons(Rect inRect, float y)
        {
            // Layout: [Cancel]  [Select All]  [Deselect All]  [Confirm]
            // Four buttons spread across the width with equal gaps
            float totalButtons = 4 * ButtonW + 3 * ButtonGap;
            float startX = (inRect.width - totalButtons) / 2f;

            Rect cancelRect    = new Rect(startX,                                  y, ButtonW, ButtonH);
            Rect selAllRect    = new Rect(startX + ButtonW + ButtonGap,            y, ButtonW, ButtonH);
            Rect deselAllRect  = new Rect(startX + 2 * (ButtonW + ButtonGap),     y, ButtonW, ButtonH);
            Rect confirmRect   = new Rect(startX + 3 * (ButtonW + ButtonGap),     y, ButtonW, ButtonH);

            // Cancel
            if (Widgets.ButtonText(cancelRect, "CancelButton".Translate()))
                Close();

            // Select All
            if (Widgets.ButtonText(selAllRect, "Select all"))
            {
                foreach (var r in rows.Where(r => r.enabled)) r.selected = true;
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
            }

            // Deselect All
            if (Widgets.ButtonText(deselAllRect, "Deselect all"))
            {
                foreach (var r in rows) r.selected = false;
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
            }

            // Confirm (greyed if nothing selected)
            bool anySelected = rows.Any(r => r.selected);
            if (!anySelected) GUI.color = Color.gray;
            string confirmLabel = mode == Mode.Add
                ? "Outposts.Commands.AddPawn.Label".Translate().ToString()
                : "Outposts.Commands.Remove.Label".Translate().ToString();
            if (Widgets.ButtonText(confirmRect, confirmLabel) && anySelected)
                Confirm();
            GUI.color = Color.white;
        }

        private void Confirm()
        {
            var selected = rows.Where(r => r.selected && r.enabled).Select(r => r.pawn).ToList();
            if (!selected.Any()) return;

            if (mode == Mode.Add)
                foreach (var p in selected)
                    outpost.AddPawn(p);
            else
                foreach (var p in selected)
                    CaravanMaker.MakeCaravan(
                        Gen.YieldSingle(outpost.RemovePawn(p)), p.Faction, outpost.Tile, true);

            Close();
        }

        private static string PawnSkillSummary(Pawn pawn)
        {
            if (pawn.skills == null) return pawn.def.LabelCap;
            return string.Join("  ·  ",
                pawn.skills.skills
                    .OrderByDescending(s => s.Level)
                    .Take(3)
                    .Select(s => s.def.LabelCap + " " + s.Level));
        }

        private class PawnRow
        {
            public readonly Pawn   pawn;
            public readonly bool   enabled;
            public readonly string disabledReason;
            public          bool   selected;

            public PawnRow(Pawn pawn, bool enabled, string disabledReason)
            {
                this.pawn           = pawn;
                this.enabled        = enabled;
                this.disabledReason = disabledReason;
                this.selected       = false;
            }
        }
    }
}