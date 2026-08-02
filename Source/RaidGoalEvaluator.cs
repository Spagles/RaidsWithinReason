using System.Linq;
using RimWorld;
using Verse;

namespace RaidsWithinReason
{
    public static class RaidGoalEvaluator
    {
        public static RaidGoalDef SelectGoal(IncidentParms parms, Faction faction, Map map)
        {
            var candidates = DefDatabase<RaidGoalDef>.AllDefsListForReading;
            if (candidates.Count == 0) return null;

            float wealthScore  = ColonyStateReader.GetWealthScore(map);
            bool  hasColonists = map.mapPawns.FreeColonists.Any();
            bool  hasRooms     = ColonyStateReader.HasAnyRooms(map);

            // Capture = kidnap a colonist (NOT "colony has prisoners").
            // Moderate base weight so it competes with Loot/Destroy without dominating.
            return candidates.MaxByWithFallback(def => def.goalType switch
            {
                RaidGoalType.Loot    => wealthScore + Rand.Range(0f, 0.2f),
                RaidGoalType.Capture => hasColonists ? 0.32f + Rand.Range(0f, 0.18f) : 0f,
                RaidGoalType.Destroy => hasRooms ? 0.45f + Rand.Range(0f, 0.2f) : 0f,
                _                    => 0f, // Revenge / ReleasePrisoner / Retaliation are reactive only
            });
        }
    }
}
