using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RaidsWithinReason
{
    /// <summary>
    /// Picks flavour text for raid goals and negotiation demands.
    /// Player settings hold the full phrase library (seeded with built-in defaults).
    /// </summary>
    public static class RWR_Vocabulary
    {
        private static readonly Dictionary<RaidGoalType, string[]> BuiltinKeys =
            new Dictionary<RaidGoalType, string[]>
            {
                [RaidGoalType.Loot] = new[]
                {
                    "RWR_Vocab_Loot_0",
                    "RWR_Vocab_Loot_1",
                    "RWR_Vocab_Loot_2",
                    "RWR_Vocab_Loot_3",
                },
                [RaidGoalType.Capture] = new[]
                {
                    "RWR_Vocab_Capture_0",
                    "RWR_Vocab_Capture_1",
                    "RWR_Vocab_Capture_2",
                    "RWR_Vocab_Capture_3",
                },
                [RaidGoalType.Destroy] = new[]
                {
                    "RWR_Vocab_Destroy_0",
                    "RWR_Vocab_Destroy_1",
                    "RWR_Vocab_Destroy_2",
                    "RWR_Vocab_Destroy_3",
                },
                [RaidGoalType.Revenge] = new[]
                {
                    "RWR_Vocab_Revenge_0",
                    "RWR_Vocab_Revenge_1",
                    "RWR_Vocab_Revenge_2",
                },
                [RaidGoalType.ReleasePrisoner] = new[]
                {
                    "RWR_Vocab_Release_0",
                    "RWR_Vocab_Release_1",
                },
                [RaidGoalType.Retaliation] = new[]
                {
                    "RWR_Vocab_Retaliation_0",
                    "RWR_Vocab_Retaliation_1",
                },
            };

        /// <summary>Resolved default phrases for a goal type (current language).</summary>
        public static List<string> GetDefaultPhrases(RaidGoalType goalType)
        {
            var result = new List<string>();
            if (!BuiltinKeys.TryGetValue(goalType, out string[] keys))
                return result;

            foreach (string key in keys)
            {
                string text = key.Translate().Resolve();
                if (!text.NullOrEmpty())
                    result.Add(text);
            }
            return result;
        }

        public static IEnumerable<RaidGoalType> SeededGoalTypes => new[]
        {
            RaidGoalType.Loot,
            RaidGoalType.Capture,
            RaidGoalType.Destroy,
            RaidGoalType.Revenge,
            RaidGoalType.Retaliation,
            RaidGoalType.ReleasePrisoner,
        };

        public static string PickGoalReason(RaidGoalType goalType, string fallback = null)
        {
            var pool = new List<string>();

            if (RWR_Mod.Settings != null)
            {
                RWR_Mod.Settings.EnsureVocabularySeeded();
                foreach (string phrase in RWR_Mod.Settings.GetCustomPhrases(goalType))
                {
                    if (!phrase.NullOrEmpty())
                        pool.Add(phrase);
                }
            }

            // Fallback if the player cleared the list or settings aren't ready yet.
            if (pool.Count == 0)
                pool.AddRange(GetDefaultPhrases(goalType));

            if (pool.Count == 0)
                return fallback ?? string.Empty;

            return pool.RandomElement();
        }

        public static string PickDemandReason(RaidGoalType linkedGoalType, string fallback = null)
        {
            return PickGoalReason(linkedGoalType, fallback);
        }
    }
}
