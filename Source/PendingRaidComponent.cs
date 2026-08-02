using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace RaidsWithinReason
{
    public class PendingRaid : IExposable
    {
        public Faction     faction;
        public Map         map;
        public int         triggerTick;
        public RaidGoalDef forcedGoal;

        public void ExposeData()
        {
            Scribe_References.Look(ref faction,  "faction");
            Scribe_References.Look(ref map,      "map");
            Scribe_Values.Look(ref triggerTick,  "triggerTick");
            Scribe_Defs.Look(ref forcedGoal,     "forcedGoal");
        }
    }

    // GameComponent auto-discovered by RimWorld on game init/load.
    // Holds raids that must fire after a fixed delay (e.g. negotiator killing).
    public class PendingRaidComponent : GameComponent
    {
        private List<PendingRaid>        pending = new List<PendingRaid>();
        private Dictionary<Faction, int> lastOffMapRevengeTick = new Dictionary<Faction, int>();

        public PendingRaidComponent(Game game) : base() { }

        public void Enqueue(Faction faction, Map map, int delayTicks, RaidGoalDef forcedGoal)
        {
            pending.Add(new PendingRaid
            {
                faction     = faction,
                map         = map,
                triggerTick = Find.TickManager.TicksGame + delayTicks,
                forcedGoal  = forcedGoal,
            });
        }

        public bool HasPendingFor(Faction faction, RaidGoalType goalType)
        {
            if (faction == null) return false;
            return pending.Any(p => p.faction == faction && p.forcedGoal?.goalType == goalType);
        }

        public bool IsOffMapRevengeOnCooldown(Faction faction)
        {
            if (faction == null) return false;
            if (!lastOffMapRevengeTick.TryGetValue(faction, out int tick)) return false;
            int cooldown = RWR_Mod.Settings.offMapRevengeCooldownDays * GenDate.TicksPerDay;
            return Find.TickManager.TicksGame - tick < cooldown;
        }

        public void RecordOffMapRevenge(Faction faction)
        {
            if (faction == null) return;
            lastOffMapRevengeTick[faction] = Find.TickManager.TicksGame;
        }

        public override void GameComponentTick()
        {
            if (pending.Count > 0)
            {
                int now = Find.TickManager.TicksGame;
                for (int i = pending.Count - 1; i >= 0; i--)
                {
                    if (now >= pending[i].triggerTick)
                    {
                        FireRaid(pending[i]);
                        pending.RemoveAt(i);
                    }
                }
            }

            // QuestPart ticking — QuestPartTick() is not virtual on QuestPart in 1.6
            foreach (Quest quest in Find.QuestManager.QuestsListForReading)
            {
                if (quest.State != QuestState.Ongoing) continue;
                foreach (QuestPart part in quest.PartsListForReading)
                {
                    if (part is QuestPart_RequireDelivery delivery) delivery.DoTick();
                    else if (part is QuestPart_TimerExpiry timer) timer.DoTick();
                }
            }
        }

        private static void FireRaid(PendingRaid raid)
        {
            if (raid.map == null || raid.faction == null) return;
            NegotiatorUtil.TriggerImmediateRaid(raid.faction, raid.map, raid.forcedGoal);
        }

        private List<Faction> _revengeKeys;
        private List<int>     _revengeValues;

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref pending, "pending", LookMode.Deep);
            Scribe_Collections.Look(ref lastOffMapRevengeTick, "lastOffMapRevengeTick",
                LookMode.Reference, LookMode.Value, ref _revengeKeys, ref _revengeValues);

            pending ??= new List<PendingRaid>();
            lastOffMapRevengeTick ??= new Dictionary<Faction, int>();

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                foreach (Faction stale in lastOffMapRevengeTick.Keys.Where(k => k == null).ToList())
                    lastOffMapRevengeTick.Remove(stale);
            }
        }
    }
}
