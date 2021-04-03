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
                throw new InvalidOperationException("Empire - Attempted to deploy an already deployed squad, report this.");

            QuestPart_DeploySquad deploySquad = new QuestPart_DeploySquad
            {
                arrivalMode = arrivalMode.GetValue(slate),
                location = location.GetValue(slate),
                map = spawnMap,
                squad = mercs,
                // This is how vanilla handles inSignal by default
                inSignal = this.inSignal.GetValue(slate) ?? QuestGenUtility.HardcodedSignalWithQuestID(slate.Get<string>("inSignal"))
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

            squad.settlement.SendMilitary(Find.CurrentMap.Index, Find.World.info.name, SettlementFC.MilitaryJob.Deploy, 1, null);
            squad.isDeployed = true;
            squad.timeDeployed = Find.TickManager.TicksGame;

            foreach(var i in squad.AllEquippedMercenaryPawns)
            {
                i.SetFaction(Find.FactionManager.OfPlayer);
            }

            if (!arrivalMode.Worker.TryResolveRaidSpawnCenter(parms))
                throw new Exception("Empire - Failed to resolve spawn centers");

            arrivalMode.Worker.Arrive(squad.AllEquippedMercenaryPawns.ToList(), parms);

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

    public static class MilitaryAI
    {

        public static void SquadAI(ref MercenarySquadFC squad)
        {
            Faction playerFaction = Find.FactionManager.OfPlayer;
            Faction settlementFaction = FactionColonies.getPlayerColonyFaction();

            FactionFC factionfc = Find.World.GetComponent<FactionFC>();
            MilitaryCustomizationUtil militaryCustomizationUtil = factionfc.militaryCustomizationUtil;
            bool deployed = false;


            //Log.Message("1");
            #region cc

            foreach (Mercenary merc in squad.DeployedMercenaries)
            {
                //If pawn is deployed
                    deployed = true;
                    if (!squad.hitMap)
                    {
                        squad.hitMap = true;
                    }

                    if (merc.pawn.health.State == PawnHealthState.Mobile)
                    {
                        JobGiver_AIFightEnemies jobGiver = new JobGiver_AIFightEnemies();
                        ThinkResult result = jobGiver.TryIssueJobPackage(merc.pawn, new JobIssueParams());
                        bool isValid = result.IsValid;


                        if (isValid)
                        {
                            //Log.Message("Success");
                            if (merc.pawn.jobs.curJob == null || ((merc.pawn.jobs.curJob.def == JobDefOf.Goto || merc.pawn.jobs.curJob.def != result.Job.def) && merc.pawn.jobs.curJob.def.defName != "ReloadWeapon" && merc.pawn.jobs.curJob.def.defName != "ReloadTurret" && !merc.pawn.Drafted))
                            {
                                merc.pawn.jobs.StartJob(result.Job, JobCondition.Ongoing);
                                //Log.Message(result.Job.ToString());
                            }
                        }
                        else
                        {
                            //Log.Message("Fail");
                            if (squad.timeDeployed + 30000 >= Find.TickManager.TicksGame)
                            {
                                if (merc.pawn.drafter == null || merc.pawn.Drafted == false)
                                {
                                    if (squad.order == MilitaryOrders.Standby)
                                    {
                                        //Log.Message("Standby");
                                        merc.pawn.mindState.forcedGotoPosition = squad.orderLocation;
                                        JobGiver_ForcedGoto jobGiver_Standby = new JobGiver_ForcedGoto();
                                        ThinkResult resultStandby = jobGiver_Standby.TryIssueJobPackage(merc.pawn, new JobIssueParams());
                                        bool isValidStandby = resultStandby.IsValid;
                                        if (isValidStandby)
                                        {
                                            //Log.Message("valid");
                                            merc.pawn.jobs.StartJob(resultStandby.Job, JobCondition.InterruptForced);


                                        }
                                    }
                                    else
                                    if (squad.order == MilitaryOrders.Attack)
                                    {
                                        //Log.Message("Attack");
                                        //If time is up, leave, else go home
                                        JobGiver_AIGotoNearestHostile jobGiver_Move = new JobGiver_AIGotoNearestHostile();
                                        ThinkResult resultMove = jobGiver_Move.TryIssueJobPackage(merc.pawn, new JobIssueParams());
                                        bool isValidMove = resultMove.IsValid;
                                        //Log.Message(resultMove.ToString());
                                        if (isValidMove)
                                        {
                                            merc.pawn.jobs.StartJob(resultMove.Job, JobCondition.InterruptForced);
                                        }
                                        else
                                        {

                                        }
                                    }
                                    else
                                    if (squad.order == MilitaryOrders.RecoverWounded)
                                    {
                                        JobGiver_RescueNearby jobGiver_Rescue = new JobGiver_RescueNearby();
                                        ThinkResult resultRescue = jobGiver_Rescue.TryIssueJobPackage(merc.pawn, new JobIssueParams());
                                        bool isValidRescue = resultRescue.IsValid;

                                        if (isValidRescue)
                                        {
                                            merc.pawn.jobs.StartJob(resultRescue.Job, JobCondition.InterruptForced);
                                        }
                                    }
                                }
                            }
                        }


                        
                        //end of if pawn is mobile
                    }
                    else 
                    {
                        //if pawn is down or dead
                        if (merc.pawn.health.Dead) //if pawn is on map and is dead
                        {
                            //squad.removeDroppedEquipment();
                            //squad.PassPawnToDeadMercenaries(merc);
                            //squad.hasDead = true;
                        } else
                        { //If alive but downed
                            if (merc.pawn.drafter != null)
                                merc.pawn.drafter.Drafted = false;
                            if (merc.pawn.Faction != settlementFaction)
                                merc.pawn.SetFaction(settlementFaction);

                        }
                    }
            }
            #endregion cc

            //Log.Message("2");
            foreach (Mercenary animal in squad.DeployedAnimalMercenaries)
            {
                //if on map
                deployed = true;
                if (animal.pawn.health.State == PawnHealthState.Mobile)
                {
                    animal.pawn.mindState.duty = new PawnDuty();
                    animal.pawn.mindState.duty.def = DutyDefOf.Defend;
                    animal.pawn.mindState.duty.attackDownedIfStarving = false;
                    //animal.pawn.mindState.duty.radius = 2;
                    animal.pawn.mindState.duty.focus = animal.handler.pawn;
                    //If master is not dead
                    JobGiver_AIFightEnemies jobGiver = new JobGiver_AIFightEnemies();
                    ThinkResult result = jobGiver.TryIssueJobPackage(animal.pawn, new JobIssueParams());
                    bool isValid = result.IsValid;
                    if (isValid)
                    {
                        //Log.Message("att");
                        if (animal.pawn.jobs.curJob.def != result.Job.def)
                        {
                            animal.pawn.jobs.StartJob(result.Job, JobCondition.InterruptForced);
                        }
                    }
                    else
                    {
                        animal.pawn.mindState.duty.def = DutyDefOf.Defend;
                        animal.pawn.mindState.duty.radius = 2;
                        animal.pawn.mindState.duty.focus = animal.handler.pawn;
                        //if defend master not valid, follow master
                        JobGiver_AIFollowEscortee jobGiverFollow = new JobGiver_AIFollowEscortee();
                        ThinkResult resultFollow = jobGiverFollow.TryIssueJobPackage(animal.pawn, new JobIssueParams());
                        bool isValidFollow = resultFollow.IsValid;
                        if (isValidFollow)
                        {
                            //Log.Message("foloor");
                            if (animal.pawn.jobs.curJob.def != resultFollow.Job.def)
                            {
                                animal.pawn.jobs.StartJob(resultFollow.Job, JobCondition.Ongoing);
                            }
                        }
                        else
                        {
                            JobGiver_ExitMapBest jobGiver_Rescue = new JobGiver_ExitMapBest();
                            ThinkResult resultLeave = jobGiver_Rescue.TryIssueJobPackage(animal.pawn, new JobIssueParams());
                            bool isValidLeave = resultLeave.IsValid;

                            if (isValidLeave)
                            {
                                animal.pawn.jobs.StartJob(resultLeave.Job, JobCondition.InterruptForced);
                            }
                        }
                    }
                }
            }
        }
    }
}
