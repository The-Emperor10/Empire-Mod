using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld.Planet;
using RimWorld;
using UnityEngine;
using Verse;
using HarmonyLib;
using Verse.AI;
using Verse.AI.Group;
using System.IO;
using RimWorld.QuestGen;

namespace FactionColonies
{
    public class QuestNode_DeploySquad : QuestNode
    {
        public SlateRef<IntVec3?> location;
        public SlateRef<MercenarySquadFC> squad;
        public SlateRef<Map> map;
        public SlateRef<PawnsArrivalModeDef> arrivalMode;
        public SlateRef<string> inSignal;

        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;

            MercenarySquadFC mercs = squad.GetValue(slate);
            mercs.resetNeeds();
            mercs.updateSquadStats(mercs.settlement.settlementMilitaryLevel);

            Map spawnMap = map.GetValue(slate);

            if (mercs == null)
                throw new NullReferenceException("Empire - Squad is null. Report this.");

            if (spawnMap == null)
                throw new NullReferenceException("Empire - Map is null. Report this.");

            if (mercs.isDeployed)
                throw new InvalidOperationException(
                    "Empire - Attempted to deploy an already deployed squad, report this.");

            QuestPart_DeploySquad deploySquad = new QuestPart_DeploySquad
            {
                arrivalMode = arrivalMode.GetValue(slate),
                location = location.GetValue(slate),
                map = spawnMap,
                squad = mercs,
                // This is how vanilla handles inSignal by default
                inSignal = this.inSignal.GetValue(slate) ??
                           QuestGenUtility.HardcodedSignalWithQuestID(slate.Get<string>("inSignal"))
            };

            QuestPart_ExtraFaction extraFaction = new QuestPart_ExtraFaction
            {
                affectedPawns = mercs.AllEquippedMercenaryPawns.ToList(),
                extraFaction = new ExtraFaction(FactionColonies.getPlayerColonyFaction(), ExtraFactionType.HomeFaction),
                areHelpers = false,
            };

            QuestPart_Leave leave = new QuestPart_Leave
            {
                pawns = mercs.AllEquippedMercenaryPawns.ToList(),
                leaveOnCleanup = true,
                sendStandardLetter = false
            };

            QuestGen.quest.AddPart(deploySquad);
            QuestGen.quest.AddPart(extraFaction);
            QuestGen.quest.AddPart(leave);
        }

        protected override bool TestRunInt(Slate slate)
        {
            MercenarySquadFC mercs = squad.GetValue(slate);
            return mercs != null && !mercs.isDeployed && mercs.settlement.IsMilitaryValid;
        }
    }

    public class QuestPart_DeploySquad : QuestPart
    {
        public IntVec3? location;
        public MercenarySquadFC squad;
        public Map map;
        public PawnsArrivalModeDef arrivalMode;
        public string inSignal;

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag != this.inSignal)
                return;

            IncidentParms parms = new IncidentParms
            {
                spawnCenter = location ?? IntVec3.Invalid,
                target = map
            };

            squad.resetNeeds();
            squad.updateSquadStats(squad.settlement.settlementMilitaryLevel);

            squad.settlement.SendMilitary(Find.CurrentMap.Index, Find.World.info.name, SettlementFC.MilitaryJob.Deploy,
                1, null);
            squad.isDeployed = true;
            squad.timeDeployed = Find.TickManager.TicksGame;
            squad.map = map;

            foreach (var i in squad.AllEquippedMercenaryPawns)
            {
                i.SetFaction(Find.FactionManager.OfPlayer);
            }

            if (!arrivalMode.Worker.TryResolveRaidSpawnCenter(parms))
                throw new Exception("Empire - Failed to resolve spawn centers");

            arrivalMode.Worker.Arrive(squad.AllEquippedMercenaryPawns.ToList(), parms);

            squad.CreateLord();
            squad.SetOrder(MilitaryOrder.Standby, location);

            foreach (Pawn pawn in squad.AllEquippedMercenaryPawns)
            {
                if (pawn.playerSettings != null)
                    pawn.playerSettings.hostilityResponse = HostilityResponseMode.Attack;
            }

            if (!Find.WindowStack.IsOpen<EmpireUIMercenaryCommandMenu>())
                Find.WindowStack.Add(new EmpireUIMercenaryCommandMenu());
        }

        public override void Cleanup()
        {
            base.Cleanup();
            FactionFC fc = Find.World.GetComponent<FactionFC>();

            foreach (var i in squad.AllEquippedMercenaryPawns)
            {
                i.SetFaction(FactionColonies.getPlayerColonyFaction());
            }

            squad.isDeployed = false;

            squad.DeleteLord();
            squad.quest = null;
            squad.map = null;

            if (!fc.militaryCustomizationUtil.DeployedSquads.Any())
                Find.WindowStack.TryRemove(typeof(EmpireUIMercenaryCommandMenu));
        }

        public override void Notify_PawnKilled(Pawn pawn, DamageInfo? dinfo)
        {
            base.Notify_PawnKilled(pawn, dinfo);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref inSignal, "inSignal");
            Scribe_Values.Look(ref location, "location");
            Scribe_Defs.Look(ref arrivalMode, "arrivalMode");
            Scribe_References.Look(ref map, "map");
            Scribe_References.Look(ref squad, "squad");
        }

    }
}