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
    /// <summary>
    /// Trade-screen style dialog for moving pawns between a caravan and an outpost.
    /// Mode.Add  : caravan -> outpost
    /// Mode.Remove : outpost -> caravan  (disabled when only 1 occupant remains)
    /// </summary>
    public class Dialog_ManagePawns : Window
    {
        public enum Mode { Add, Remove }

        // ── layout constants ────────────────────────────────────────────────
        private const float RowHeight       = 40f;
        private const float ButtonW         = 160f;
        private const float ButtonH         = 40f;
        private const float PortraitSize    = 32f;
        private const float CheckboxSize    = 24f;
        private const float ScrollBarW      = 16f;
        private const float HeaderH         = 35f;

        // ── state ────────────────────────────────────────────────────────────
        private readonly Outpost  outpost;
        private readonly Caravan  caravan;   // null when mode == Remove and no caravan nearby
        private readonly Mode     mode;

        private List<PawnRow> rows;
        private Vector2       scrollPos;

        // ── constructor ──────────────────────────────────────────────────────
        public Dialog_ManagePawns(Outpost outpost, Caravan caravan, Mode mode)
        {
            this.outpost = outpost;
            this.caravan = caravan;
            this.mode    = mode;

            doCloseX          = true;
            doCloseButton     = false;
            closeOnClickedOutside = false;
            absorbInputAroundWindow = true;
            forcePause        = true;
        }

        public override Vector2 InitialSize => new Vector2(600f, Mathf.Min(700f, UI.screenHeight - 100f));

        // ── build row list ───────────────────────────────────────────────────
        private void BuildRows()
        {
            rows = new List<PawnRow>();

            if (mode == Mode.Add)
            {
                foreach (var p in caravan.PawnsListForReading)
                {
                    bool canAdd = outpost.Ext.CanAddPawn(p, out string reason);
                    rows.Add(new PawnRow(p, canAdd, reason));
                }
            }
            else // Remove
            {
                var occupantList = outpost.AllPawns.ToList();
                foreach (var p in occupantList)
                {
                    // Can't remove if this is the last pawn
                    bool canRemove = occupantList.Count > 1;
                    string reason  = canRemove ? null : "Outposts.Command.Remove.Only1".Translate();
                    rows.Add(new PawnRow(p, canRemove, reason));
                }
            }
        }

        public override void PostOpen()
        {
            base.PostOpen();
            BuildRows();
        }

        // ── main draw ────────────────────────────────────────────────────────
        public override void DoWindowContents(Rect inRect)
        {
            // Title
            Text.Font = GameFont.Medium;
            string title = mode == Mode.Add
                ? "Outposts.Commands.AddPawn.Label".Translate()
                : "Outposts.Commands.Remove.Label".Translate();
            Widgets.Label(new Rect(0f, 0f, inRect.width, HeaderH), title);
            Text.Font = GameFont.Small;

            // Subtitle
            string subtitle = mode == Mode.Add
                ? caravan.Name + "  →  " + (outpost.HasName ? outpost.Label : outpost.def.LabelCap.ToString())
                : (outpost.HasName ? outpost.Label : outpost.def.LabelCap.ToString()) + "  →  new caravan";
            GUI.color = Color.gray;
            Widgets.Label(new Rect(0f, HeaderH, inRect.width, 22f), subtitle);
            GUI.color = Color.white;

            // Scroll area
            float topY      = HeaderH + 26f;
            float bottomY   = inRect.height - ButtonH - 10f;
            float listHeight = rows.Count * RowHeight;
            Rect  outerRect = new Rect(0f, topY, inRect.width, bottomY - topY);
            Rect  viewRect  = new Rect(0f, 0f, outerRect.width - ScrollBarW, listHeight);

            Widgets.BeginScrollView(outerRect, ref scrollPos, viewRect);

            for (int i = 0; i < rows.Count; i++)
            {
                Rect rowRect = new Rect(0f, i * RowHeight, viewRect.width, RowHeight);
                DrawRow(rowRect, rows[i], i);
            }

            Widgets.EndScrollView();

            // Separator
            Widgets.DrawLineHorizontal(0f, bottomY - 2f, inRect.width);

            // Bottom buttons
            DrawBottomButtons(inRect, bottomY + 4f);
        }

        // ── draw one pawn row ─────────────────────────────────────────────────
        private void DrawRow(Rect rect, PawnRow row, int index)
        {
            // Alternating background
            if (index % 2 == 0)
                Widgets.DrawAltRect(rect);

            // Highlight on hover
            if (Mouse.IsOver(rect) && row.enabled)
                Widgets.DrawHighlight(rect);

            float x = rect.x + 4f;

            // Checkbox
            Rect cbRect = new Rect(x, rect.y + (RowHeight - CheckboxSize) / 2f, CheckboxSize, CheckboxSize);
            if (row.enabled)
            {
                bool prev = row.selected;
                Widgets.Checkbox(cbRect.x, cbRect.y, ref row.selected, CheckboxSize);
                if (row.selected != prev)
                    SoundDefOf.Tick_High.PlayOneShotOnCamera();
            }
            else
            {
                // Greyed-out locked checkbox
                GUI.color = Color.gray;
                Widgets.CheckboxDraw(cbRect.x, cbRect.y, false, true, CheckboxSize);
                GUI.color = Color.white;
            }
            x += CheckboxSize + 6f;

            // Portrait
            Rect portraitRect = new Rect(x, rect.y + (RowHeight - PortraitSize) / 2f, PortraitSize, PortraitSize);
            GUI.DrawTexture(portraitRect, PortraitsCache.Get(row.pawn, new Vector2(PortraitSize, PortraitSize), Rot4.South));
            x += PortraitSize + 6f;

            // Name
            float nameW = rect.width - x - 4f;
            Rect  nameRect = new Rect(x, rect.y, nameW, RowHeight);

            if (!row.enabled)
                GUI.color = Color.gray;

            // Line 1: name
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(x, rect.y + 4f, nameW, 20f),
                row.pawn.NameFullColored.CapitalizeFirst().Resolve());

            // Line 2: reason why disabled, or skills summary
            Text.Font = GameFont.Tiny;
            string infoLine = row.enabled
                ? PawnSkillSummary(row.pawn)
                : row.disabledReason;
            GUI.color = row.enabled ? Color.gray : new Color(1f, 0.5f, 0.5f);
            Widgets.Label(new Rect(x, rect.y + 22f, nameW, 16f), infoLine);

            GUI.color  = Color.white;
            Text.Font  = GameFont.Small;

            // Tooltip on whole row
            if (!row.disabledReason.NullOrEmpty())
                TooltipHandler.TipRegion(rect, row.disabledReason);

            // Click whole row to toggle (if enabled)
            if (row.enabled && Widgets.ButtonInvisible(rect))
            {
                row.selected = !row.selected;
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
            }
        }

        // ── bottom buttons ────────────────────────────────────────────────────
        private void DrawBottomButtons(Rect inRect, float y)
        {
            // Confirm
            string confirmLabel = mode == Mode.Add
                ? "Outposts.Commands.AddPawn.Label".Translate()
                : "Outposts.Commands.Remove.Label".Translate();

            bool anySelected = rows.Any(r => r.selected);

            if (!anySelected) GUI.color = Color.gray;
            Rect confirmRect = new Rect(inRect.width - ButtonW, y, ButtonW, ButtonH);
            if (Widgets.ButtonText(confirmRect, confirmLabel) && anySelected)
                Confirm();
            GUI.color = Color.white;

            // Cancel
            if (Widgets.ButtonText(new Rect(0f, y, ButtonW, ButtonH), "CancelButton".Translate()))
                Close();

            // Select All / None toggles in the middle
            Rect selAllRect  = new Rect(inRect.width / 2f - ButtonW - 4f, y, ButtonW, ButtonH);
            Rect selNoneRect = new Rect(inRect.width / 2f + 4f,           y, ButtonW, ButtonH);

            if (Widgets.ButtonText(selAllRect, "SelectAll".Translate()))
            {
                foreach (var r in rows.Where(r => r.enabled)) r.selected = true;
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
            }
            if (Widgets.ButtonText(selNoneRect, "DeselectAll".Translate()))
            {
                foreach (var r in rows) r.selected = false;
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
            }
        }

        // ── execute the transfer ──────────────────────────────────────────────
        private void Confirm()
        {
            var selected = rows.Where(r => r.selected && r.enabled).Select(r => r.pawn).ToList();
            if (!selected.Any()) return;

            if (mode == Mode.Add)
            {
                foreach (var p in selected)
                    outpost.AddPawn(p);
            }
            else
            {
                foreach (var p in selected)
                    CaravanMaker.MakeCaravan(Gen.YieldSingle(outpost.RemovePawn(p)), p.Faction, outpost.Tile, true);
            }

            SoundDefOf.ExecuteTrade.PlayOneShotOnCamera();
            Close();
        }

        // ── helpers ───────────────────────────────────────────────────────────
        private static string PawnSkillSummary(Pawn pawn)
        {
            if (pawn.skills == null) return pawn.def.LabelCap;
            var top = pawn.skills.skills
                .OrderByDescending(s => s.Level)
                .Take(3)
                .Select(s => s.def.LabelCap + " " + s.Level);
            return string.Join("  ·  ", top);
        }

        // ── inner data class ──────────────────────────────────────────────────
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