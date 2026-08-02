using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace RaidsWithinReason
{
    public class FactionRWROverride : IExposable
    {
        public string factionDefName = string.Empty;

        // When overrideNegotiate is true, allowNegotiate is used instead of the global chance path.
        public bool overrideNegotiate;
        public bool allowNegotiate = true;

        // When overrideGoals is true, allowGoals is used instead of the global chaotic/goal path.
        public bool overrideGoals;
        public bool allowGoals = true;

        public void ExposeData()
        {
            Scribe_Values.Look(ref factionDefName,    "factionDefName",    string.Empty);
            Scribe_Values.Look(ref overrideNegotiate, "overrideNegotiate", false);
            Scribe_Values.Look(ref allowNegotiate,    "allowNegotiate",    true);
            Scribe_Values.Look(ref overrideGoals,     "overrideGoals",     false);
            Scribe_Values.Look(ref allowGoals,        "allowGoals",        true);
        }
    }

    public class RWR_Settings : ModSettings
    {
        public bool  enableGoalLetters      = true;
        public bool  enableRetreatOnSuccess = true;
        public float chaoticRaidChance      = 0f;
        public float negotiationChance      = 0.45f;
        public int   negotiatorCooldownDays = 10;

        // Off-map wipe retaliation (quest sites, caravan camps).
        public bool enableOffMapRevenge        = true;
        public int  offMapRevengeCooldownDays  = 5;

        // Per-faction overrides.
        public List<FactionRWROverride> factionOverrides = new List<FactionRWROverride>();

        // Full phrase library per goal type (seeded once with built-in defaults; player can add/remove).
        public List<string> customLootPhrases         = new List<string>();
        public List<string> customCapturePhrases      = new List<string>();
        public List<string> customDestroyPhrases      = new List<string>();
        public List<string> customRevengePhrases      = new List<string>();
        public List<string> customRetaliationPhrases  = new List<string>();

        // True after built-in defaults have been copied into the lists above (survives save).
        public bool vocabularySeededWithDefaults;

        // UI state (not saved).
        [Unsaved] private Vector2 factionScroll;
        [Unsaved] private Vector2 vocabScroll;
        [Unsaved] private string  newPhraseBuffer = string.Empty;
        [Unsaved] private int     newPhraseGoalIndex;
        [Unsaved] private int     settingsTab;

        private static readonly string[] GoalTypeLabels =
        {
            "Loot", "Capture", "Destroy", "Revenge", "Retaliation"
        };

        public bool TryGetFactionOverride(string factionDefName, out FactionRWROverride ov)
        {
            ov = factionOverrides?.FirstOrDefault(o => o.factionDefName == factionDefName);
            return ov != null;
        }

        public FactionRWROverride GetOrCreateOverride(string factionDefName)
        {
            if (TryGetFactionOverride(factionDefName, out var existing))
                return existing;

            var created = new FactionRWROverride { factionDefName = factionDefName };
            factionOverrides.Add(created);
            return created;
        }

        public List<string> GetCustomPhrases(RaidGoalType goalType)
        {
            EnsureVocabularySeeded();
            return goalType switch
            {
                RaidGoalType.Loot            => customLootPhrases        ??= new List<string>(),
                RaidGoalType.Capture         => customCapturePhrases     ??= new List<string>(),
                RaidGoalType.Destroy         => customDestroyPhrases     ??= new List<string>(),
                RaidGoalType.Revenge         => customRevengePhrases     ??= new List<string>(),
                RaidGoalType.Retaliation     => customRetaliationPhrases ??= new List<string>(),
                RaidGoalType.ReleasePrisoner => customCapturePhrases     ??= new List<string>(),
                _                            => customLootPhrases        ??= new List<string>(),
            };
        }

        /// <summary>
        /// One-time copy of built-in vocabulary into the editable lists so defaults
        /// appear in settings and can be removed or extended by the player.
        /// </summary>
        public void EnsureVocabularySeeded()
        {
            if (vocabularySeededWithDefaults) return;

            customLootPhrases        ??= new List<string>();
            customCapturePhrases     ??= new List<string>();
            customDestroyPhrases     ??= new List<string>();
            customRevengePhrases     ??= new List<string>();
            customRetaliationPhrases ??= new List<string>();

            SeedList(customLootPhrases,        RaidGoalType.Loot);
            SeedList(customCapturePhrases,     RaidGoalType.Capture);
            SeedList(customCapturePhrases,     RaidGoalType.ReleasePrisoner);
            SeedList(customDestroyPhrases,     RaidGoalType.Destroy);
            SeedList(customRevengePhrases,     RaidGoalType.Revenge);
            SeedList(customRetaliationPhrases, RaidGoalType.Retaliation);

            vocabularySeededWithDefaults = true;
        }

        private static void SeedList(List<string> list, RaidGoalType goalType)
        {
            foreach (string phrase in RWR_Vocabulary.GetDefaultPhrases(goalType))
            {
                if (!list.Contains(phrase))
                    list.Add(phrase);
            }
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref enableGoalLetters,           "enableGoalLetters",           true);
            Scribe_Values.Look(ref enableRetreatOnSuccess,      "enableRetreatOnSuccess",      true);
            Scribe_Values.Look(ref chaoticRaidChance,           "chaoticRaidChance",           0f);
            Scribe_Values.Look(ref negotiationChance,           "negotiationChance",           0.45f);
            Scribe_Values.Look(ref negotiatorCooldownDays,      "negotiatorCooldownDays",      10);
            Scribe_Values.Look(ref enableOffMapRevenge,         "enableOffMapRevenge",         true);
            Scribe_Values.Look(ref offMapRevengeCooldownDays,   "offMapRevengeCooldownDays",   5);
            Scribe_Values.Look(ref vocabularySeededWithDefaults,"vocabularySeededWithDefaults", false);

            Scribe_Collections.Look(ref factionOverrides,         "factionOverrides",         LookMode.Deep);
            Scribe_Collections.Look(ref customLootPhrases,        "customLootPhrases",        LookMode.Value);
            Scribe_Collections.Look(ref customCapturePhrases,     "customCapturePhrases",     LookMode.Value);
            Scribe_Collections.Look(ref customDestroyPhrases,     "customDestroyPhrases",     LookMode.Value);
            Scribe_Collections.Look(ref customRevengePhrases,     "customRevengePhrases",     LookMode.Value);
            Scribe_Collections.Look(ref customRetaliationPhrases, "customRetaliationPhrases", LookMode.Value);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                factionOverrides         ??= new List<FactionRWROverride>();
                customLootPhrases        ??= new List<string>();
                customCapturePhrases     ??= new List<string>();
                customDestroyPhrases     ??= new List<string>();
                customRevengePhrases     ??= new List<string>();
                customRetaliationPhrases ??= new List<string>();
                factionOverrides.RemoveAll(o => o == null || o.factionDefName.NullOrEmpty());

                // Fresh installs / old settings: fill lists with built-in phrases once.
                // Also migrate older saves that only had player-added customs (flag false,
                // non-empty lists) by appending any missing defaults without wiping customs.
                if (!vocabularySeededWithDefaults)
                    EnsureVocabularySeeded();
            }
        }

        public void DoWindowContents(Rect inRect)
        {
            var tabs = new List<TabRecord>
            {
                new TabRecord("RWR_Tab_General".Translate(),    () => settingsTab = 0, settingsTab == 0),
                new TabRecord("RWR_Tab_Factions".Translate(),   () => settingsTab = 1, settingsTab == 1),
                new TabRecord("RWR_Tab_Vocabulary".Translate(), () => settingsTab = 2, settingsTab == 2),
            };
            TabDrawer.DrawTabs(new Rect(inRect.x, inRect.y + 5f, inRect.width, 30f), tabs);

            var content = new Rect(inRect.x, inRect.y + 35f, inRect.width, inRect.height - 35f);
            switch (settingsTab)
            {
                case 0: DrawGeneralTab(content); break;
                case 1: DrawFactionsTab(content); break;
                case 2: DrawVocabularyTab(content); break;
            }
        }

        private void DrawGeneralTab(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.CheckboxLabeled(
                "RWR_Setting_EnableGoalLetters".Translate(),
                ref enableGoalLetters);

            listing.CheckboxLabeled(
                "RWR_Setting_EnableRetreatOnSuccess".Translate(),
                ref enableRetreatOnSuccess);

            listing.Gap(6f);

            listing.Label("RWR_Setting_ChaoticRaidChance".Translate(chaoticRaidChance.ToStringPercent()));
            chaoticRaidChance = listing.Slider(chaoticRaidChance, 0f, 1f);
            listing.Gap(4f);

            listing.Label("RWR_Setting_NegotiationChance".Translate(negotiationChance.ToStringPercent()));
            negotiationChance = listing.Slider(negotiationChance, 0f, 1f);
            listing.Gap(4f);

            listing.Label("RWR_Setting_NegotiatorCooldownDays".Translate(negotiatorCooldownDays));
            negotiatorCooldownDays = Mathf.RoundToInt(listing.Slider(negotiatorCooldownDays, 0f, 60f));
            listing.Gap(10f);

            listing.CheckboxLabeled(
                "RWR_Setting_EnableOffMapRevenge".Translate(),
                ref enableOffMapRevenge,
                "RWR_Setting_EnableOffMapRevengeTip".Translate());

            if (enableOffMapRevenge)
            {
                listing.Label("RWR_Setting_OffMapRevengeCooldownDays".Translate(offMapRevengeCooldownDays));
                offMapRevengeCooldownDays = Mathf.RoundToInt(listing.Slider(offMapRevengeCooldownDays, 1f, 30f));
            }

            listing.End();
        }

        private void DrawFactionsTab(Rect inRect)
        {
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 40f),
                (string)"RWR_Setting_FactionOverridesHelp".Translate());

            var viewRect = new Rect(0f, 0f, inRect.width - 20f, 0f);

            if (Current.ProgramState != ProgramState.Playing || Find.FactionManager == null)
            {
                viewRect.height = 36f;
                Widgets.BeginScrollView(
                    new Rect(inRect.x, inRect.y + 44f, inRect.width, inRect.height - 44f),
                    ref factionScroll, viewRect);
                Widgets.Label(new Rect(0f, 0f, viewRect.width, 36f),
                    (string)"RWR_Setting_FactionOverridesNeedGame".Translate());
                Widgets.EndScrollView();
                return;
            }

            var factions = Find.FactionManager.AllFactionsVisible
                .Where(f => f?.def != null && f.def.humanlikeFaction && !f.IsPlayer && !f.def.hidden)
                .OrderBy(f => f.Name)
                .ToList();

            const float rowHeight = 52f;
            viewRect.height = factions.Count * rowHeight + 8f;

            var outRect = new Rect(inRect.x, inRect.y + 44f, inRect.width, inRect.height - 44f);
            Widgets.BeginScrollView(outRect, ref factionScroll, viewRect);

            float y = 0f;
            foreach (Faction faction in factions)
            {
                var row = new Rect(0f, y, viewRect.width, rowHeight - 4f);
                if ((int)(y / rowHeight) % 2 == 0)
                    Widgets.DrawHighlight(row);

                Widgets.Label(new Rect(4f, y + 4f, viewRect.width * 0.35f, 22f), faction.Name);

                var ov = GetOrCreateOverride(faction.def.defName);

                bool negOverride  = ov.overrideNegotiate;
                bool negAllow     = ov.allowNegotiate;
                bool goalOverride = ov.overrideGoals;
                bool goalAllow    = ov.allowGoals;

                float col = viewRect.width * 0.38f;
                Widgets.CheckboxLabeled(
                    new Rect(col, y + 2f, viewRect.width * 0.30f, 22f),
                    "RWR_Setting_OverrideNegotiate".Translate(),
                    ref negOverride);
                if (negOverride)
                {
                    Widgets.CheckboxLabeled(
                        new Rect(col + viewRect.width * 0.30f, y + 2f, viewRect.width * 0.28f, 22f),
                        "RWR_Setting_AllowNegotiate".Translate(),
                        ref negAllow);
                }

                Widgets.CheckboxLabeled(
                    new Rect(col, y + 26f, viewRect.width * 0.30f, 22f),
                    "RWR_Setting_OverrideGoals".Translate(),
                    ref goalOverride);
                if (goalOverride)
                {
                    Widgets.CheckboxLabeled(
                        new Rect(col + viewRect.width * 0.30f, y + 26f, viewRect.width * 0.28f, 22f),
                        "RWR_Setting_AllowGoals".Translate(),
                        ref goalAllow);
                }

                ov.overrideNegotiate = negOverride;
                ov.allowNegotiate    = negAllow;
                ov.overrideGoals     = goalOverride;
                ov.allowGoals        = goalAllow;

                y += rowHeight;
            }

            Widgets.EndScrollView();

            // Keep saves clean: drop rows that are fully default.
            factionOverrides.RemoveAll(o => !o.overrideNegotiate && !o.overrideGoals);
        }

        private void DrawVocabularyTab(Rect inRect)
        {
            EnsureVocabularySeeded();

            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 36f),
                (string)"RWR_Setting_VocabularyHelp".Translate());

            float y = inRect.y + 40f;

            Widgets.Label(new Rect(inRect.x, y, 100f, 28f), (string)"RWR_Setting_VocabGoalType".Translate());
            if (Widgets.ButtonText(new Rect(inRect.x + 110f, y, 140f, 28f), GoalTypeLabels[newPhraseGoalIndex]))
            {
                var opts = new List<FloatMenuOption>();
                for (int i = 0; i < GoalTypeLabels.Length; i++)
                {
                    int idx = i;
                    opts.Add(new FloatMenuOption(GoalTypeLabels[idx], () => newPhraseGoalIndex = idx));
                }
                Find.WindowStack.Add(new FloatMenu(opts));
            }

            newPhraseBuffer = Widgets.TextField(
                new Rect(inRect.x + 260f, y, inRect.width - 360f, 28f), newPhraseBuffer);

            if (Widgets.ButtonText(new Rect(inRect.x + inRect.width - 90f, y, 85f, 28f),
                    "RWR_Setting_VocabAdd".Translate())
                && !newPhraseBuffer.NullOrEmpty())
            {
                var list = GetCustomPhrases(IndexToGoalType(newPhraseGoalIndex));
                string trimmed = newPhraseBuffer.Trim();
                if (!list.Contains(trimmed))
                    list.Add(trimmed);
                newPhraseBuffer = string.Empty;
            }

            y += 36f;

            var allPhrases = new List<(RaidGoalType type, int index, string text)>();
            void Collect(RaidGoalType t, List<string> list)
            {
                if (list == null) return;
                for (int i = 0; i < list.Count; i++)
                    allPhrases.Add((t, i, list[i]));
            }
            Collect(RaidGoalType.Loot, customLootPhrases);
            Collect(RaidGoalType.Capture, customCapturePhrases);
            Collect(RaidGoalType.Destroy, customDestroyPhrases);
            Collect(RaidGoalType.Revenge, customRevengePhrases);
            Collect(RaidGoalType.Retaliation, customRetaliationPhrases);

            var outRect = new Rect(inRect.x, y, inRect.width, inRect.yMax - y);
            var viewRect = new Rect(0f, 0f, outRect.width - 20f, Mathf.Max(allPhrases.Count * 28f + 4f, 40f));
            Widgets.BeginScrollView(outRect, ref vocabScroll, viewRect);

            if (allPhrases.Count == 0)
            {
                Widgets.Label(new Rect(0f, 0f, viewRect.width, 28f),
                    (string)"RWR_Setting_VocabEmpty".Translate());
            }
            else
            {
                float rowY = 0f;
                for (int i = 0; i < allPhrases.Count; i++)
                {
                    var (type, index, text) = allPhrases[i];
                    Widgets.Label(new Rect(0f, rowY, 90f, 26f), type.ToString());
                    Widgets.Label(new Rect(96f, rowY, viewRect.width - 180f, 26f), text);
                    if (Widgets.ButtonText(new Rect(viewRect.width - 78f, rowY, 70f, 26f),
                            "RWR_Setting_VocabRemove".Translate()))
                    {
                        GetCustomPhrases(type).RemoveAt(index);
                        break;
                    }
                    rowY += 28f;
                }
            }

            Widgets.EndScrollView();
        }

        private static RaidGoalType IndexToGoalType(int index) => index switch
        {
            0 => RaidGoalType.Loot,
            1 => RaidGoalType.Capture,
            2 => RaidGoalType.Destroy,
            3 => RaidGoalType.Revenge,
            4 => RaidGoalType.Retaliation,
            _ => RaidGoalType.Loot,
        };
    }

    public class RWR_Mod : Mod
    {
        public static RWR_Settings Settings { get; private set; }

        public RWR_Mod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<RWR_Settings>();
        }

        public override string SettingsCategory() => "RWR_SettingsCategory".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Settings.DoWindowContents(inRect);
            base.DoSettingsWindowContents(inRect);
        }
    }
}
