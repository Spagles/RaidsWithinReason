using RimWorld;
using Verse;

namespace RaidsWithinReason
{
    /// <summary>
    /// Central eligibility checks for negotiation and goal assignment.
    /// Respects global settings and per-faction overrides.
    /// </summary>
    public static class RWR_FactionRules
    {
        public static bool IsHumanlikeHostile(Faction faction)
        {
            if (faction == null || faction.IsPlayer || faction.defeated) return false;
            if (!faction.def.humanlikeFaction) return false;
            if (!faction.HostileTo(Faction.OfPlayer)) return false;
            return true;
        }

        public static bool ShouldSendNegotiator(Faction faction)
        {
            if (!IsHumanlikeHostile(faction)) return false;

            if (RWR_Mod.Settings.TryGetFactionOverride(faction.def.defName, out var ov)
                && ov.overrideNegotiate)
                return ov.allowNegotiate;

            return true; // global chance is applied separately
        }

        public static bool ShouldUseGoals(Faction faction)
        {
            if (faction == null || faction.IsPlayer || faction.defeated) return false;
            if (!faction.def.humanlikeFaction) return false;
            if (!faction.HostileTo(Faction.OfPlayer)) return false;

            if (RWR_Mod.Settings.TryGetFactionOverride(faction.def.defName, out var ov)
                && ov.overrideGoals)
                return ov.allowGoals;

            return true;
        }

        /// <summary>
        /// Whether this raid incident should be eligible for RWR interception / goals.
        /// Friendly aid workers are excluded.
        /// </summary>
        public static bool IsEnemyRaidWorker(IncidentWorker worker)
        {
            return worker is IncidentWorker_RaidEnemy;
        }

        public static bool IsMilitaryAid(IncidentParms parms)
        {
            return parms != null && parms.raidArrivalModeForQuickMilitaryAid;
        }
    }
}
