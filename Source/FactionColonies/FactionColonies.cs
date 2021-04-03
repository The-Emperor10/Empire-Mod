﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld.Planet;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using System.Reflection;
using HarmonyLib;

namespace FactionColonies
{

	public class FactionColonies : ModSettings
	{
		public FactionColonies()
		{
		}

		public static void updateChanges()
		{
			FactionFC factionFC = Find.World.GetComponent<FactionFC>();

			MilitaryCustomizationUtil util = factionFC.militaryCustomizationUtil;
			Log.Message("Updating Empire to Latest Version");
			if (factionFC.updateVersion < 0.311)
			{
				factionFC.Bills = new List<BillFC>();
				factionFC.events = new List<FCEvent>();
				verifyTraits();
			}

			if (factionFC.updateVersion < 0.305)
			{
				foreach (MercenarySquadFC squad in util.mercenarySquads)
				{
					squad.animals = new List<Mercenary>();
				}

				util.deadPawns = new List<Mercenary>();
			}

			if (factionFC.updateVersion < 0.304)
			{
				factionFC.nextMercenaryID = 1;
				foreach (MercenarySquadFC squad in util.mercenarySquads)
				{
					squad.initiateSquad();
				}

				util.deadPawns = new List<Mercenary>();
			}


			if (factionFC.updateVersion < 0.302)
			{
				util.fireSupport = new List<MilitaryFireSupport>();

				foreach (SettlementFC settlement in factionFC.settlements)
				{
					settlement.artilleryTimer = 0;
				}
			}

			if (factionFC.updateVersion < 0.300)
			{
				factionFC.militaryCustomizationUtil = new MilitaryCustomizationUtil();

			}

			if (factionFC.updateVersion < 0.301)
			{
				foreach (MilUnitFC unit in util.units)
				{
					unit.pawnKind = PawnKindDefOf.Colonist;
				}

				foreach (MercenarySquadFC squad in util.mercenarySquads)
				{
					if (squad.outfit != null && squad.isDeployed == false)
					{
						squad.OutfitSquad(squad.outfit);
					} else
					{
						squad.UsedApparelList = new List<Apparel>();
					}
				}

			}
			
			//NEW PLACE FOR UPDATE VERSIONS

			if (factionFC.updateVersion < 0.312)
			{
				foreach (SettlementFC settlement in factionFC.settlements)
				{
					settlement.returnMilitary(false);
					factionFC.updateVersion = 0.312;
				}
			}

			if (factionFC.updateVersion < 0.314)
			{

				LoadedModManager.GetMod<FactionColoniesMod>().GetSettings<FactionColonies>().settlementMaxLevel = 10;

				foreach (SettlementFC settlement in factionFC.settlements)
				{
					settlement.power = new ResourceFC(0, ResourceType.Power, settlement);
					settlement.medicine = new ResourceFC(0, ResourceType.Medicine, settlement);
					settlement.research = new ResourceFC(0, ResourceType.Research, settlement);
					settlement.power.isTithe = true;
					settlement.power.isTitheBool = true;
					settlement.research.isTithe = true;
					settlement.research.isTitheBool = true;

					for (int i = 0; i < 4; i++)
					{
						settlement.buildings.Add(BuildingFCDefOf.Empty);
					}
				}

				factionFC.power = new ResourceFC(0, ResourceType.Power);
				factionFC.medicine = new ResourceFC(0, ResourceType.Medicine);
				factionFC.research = new ResourceFC(0, ResourceType.Research);
				factionFC.power.isTithe = true;
				factionFC.power.isTitheBool = true;
				factionFC.research.isTithe = true;
				factionFC.research.isTitheBool = true;
				factionFC.researchPointPool = 0;

				
			}

			if (factionFC.updateVersion < 0.323)
			{
				foreach (SettlementFC settlement in factionFC.settlements)
				{
					settlement.power.isTithe = true;
					settlement.power.isTitheBool = true;
					settlement.research.isTithe = true;
					settlement.research.isTitheBool = true;
				}
				factionFC.power.isTithe = true;
				factionFC.power.isTitheBool = true;
				factionFC.research.isTithe = true;
				factionFC.research.isTitheBool = true;

				factionFC.updateVersion = 0.323;
			}

			if (factionFC.updateVersion < 0.324)
			{
				factionFC.medicine.label = "Medicine";

				factionFC.updateVersion = 0.323;
			}


			if (factionFC.updateVersion < 0.328)
			{
				foreach (SettlementFC settlement in factionFC.settlements)
				{
					settlement.generatePrisonerTable();
				}
			}

			if (factionFC.updateVersion < 0.329)
			{
				foreach (SettlementFC settlement in factionFC.settlements)
				{
					//reset prisoner hp
					foreach (FCPrisoner prisoner in settlement.prisonerList)
					{
						prisoner.healthTracker = new Pawn_HealthTracker(prisoner.prisoner);
						prisoner.healthTracker = prisoner.prisoner.health;
						HealthUtility.HealNonPermanentInjuriesAndRestoreLegs(prisoner.prisoner);
					}
				}
			}
			
			if (factionFC.updateVersion < 0.335)
			{
				foreach (SettlementFC settlement in factionFC.settlements)
				{

					foreach (ResourceType resourceType in ResourceUtils.resourceTypes)
					{
						ResourceFC resource = settlement.getResource(resourceType);
						resource.taxStock = 0;
						resource.taxMinimumToTithe = 99999;
						resource.taxPercentage = 0;
						resource.settlement = settlement;
						resource.filter = new ThingFilter();
						PaymentUtil.resetThingFilter(settlement, resourceType);
						resource.returnLowestCost();

					}

				}
			}
			if (factionFC.updateVersion < 0.337)
			{
				factionFC.factionIcon = TexLoad.factionIcons.First();
				factionFC.factionIconPath = TexLoad.factionIcons.First().name;
			}

			if (factionFC.updateVersion < 0.339)
			{
				factionFC.raceFilter = new ThingFilter();
				factionFC.resetRaceFilter();
			}

			if (factionFC.updateVersion < 0.340) 
			{
				factionFC.factionBackup = new Faction();
				factionFC.factionBackup = FactionColonies.getPlayerColonyFaction();
				if (FactionColonies.getPlayerColonyFaction() != null)
				{
					Log.Message("Faction created");
					factionFC.factionCreated = true;
				}
				factionFC.capitalPlanet = Find.World.info.name;
				factionFC.deleteSettlementQueue = new List<SettlementSoS2Info>();
				factionFC.createSettlementQueue = new List<SettlementSoS2Info>();

				foreach (SettlementFC settlement in factionFC.settlements)
				{

					settlement.planetName = Find.World.info.name;
					settlement.militaryLocationPlanet = Find.World.info.name;



				}

				foreach (FCEvent evt in factionFC.events)
				{
					evt.planetName = Find.World.info.name;
				}

				SoS2HarmonyPatches.ResetFactionLeaders();

				
			}

			if (factionFC.updateVersion < 0.341)
			{
				foreach (SettlementFC settlement in factionFC.settlements)
				{
					settlement.title = getTownTitle(settlement);

				}
			}

			if (factionFC.updateVersion < 0.343)
			{
				factionFC.roadBuilder = new FCRoadBuilder();
				factionFC.roadBuilder.CreateRoadQueue(Find.World.info.name);
				if (FactionColonies.getPlayerColonyFaction() != null)
				{
					Log.Message("Faction created");
					factionFC.factionCreated = true;

				}
			}

			if (factionFC.updateVersion < 0.346)
			{
				factionFC.militaryCustomizationUtil.fireSupportDefs = new List<MilitaryFireSupport>();
			}


			if (factionFC.updateVersion < 0.348)
			{
				foreach (SettlementFC settlement in factionFC.settlements)
				{
					settlement.autoDefend = false;

				}
			}

			if (factionFC.updateVersion < 0.349)
			{
				foreach (SettlementFC settlement in factionFC.settlements)
				{
					if (settlement.planetName == null)
					{
						//Log.Message("Reset planet military location for " + settlement.name);
						settlement.militaryLocationPlanet = Find.World.info.name;
						settlement.planetName = Find.World.info.name;
					}




				}

			}

			if (factionFC.updateVersion < 0.350)
			{
				factionFC.policies = new List<FCPolicy>();
				factionFC.traitMilitaristicTickLastUsedExtraSquad = -1;
				factionFC.traitPacifistTickLastUsedDiplomat = -1;
				factionFC.traitExpansionistTickLastUsedSettlementFeeReduction = -1;
				factionFC.traitExpansionistBoolCanUseSettlementFeeReduction = true;
				factionFC.traitFeudalTickLastUsedMercenary = -1;
				factionFC.traitFeudalBoolCanUseMercenary = true;
				factionFC.tradedAmount = 0;

				factionFC.factionLevel = 1;
				factionFC.factionXPCurrent = 0;
				factionFC.factionXPGoal = 100;

				factionFC.factionTraits = new List<FCPolicy>() { new FCPolicy(FCPolicyDefOf.empty), new FCPolicy(FCPolicyDefOf.empty), new FCPolicy(FCPolicyDefOf.empty), new FCPolicy(FCPolicyDefOf.empty), new FCPolicy(FCPolicyDefOf.empty) };


				foreach (MercenarySquadFC squad in factionFC.militaryCustomizationUtil.mercenarySquads)
				{
					squad.isExtraSquad = false;
				}

			}

			if (factionFC.updateVersion < 0.353)
			{
				

				foreach (MercenarySquadFC squad in factionFC.militaryCustomizationUtil.mercenarySquads)
				{
					if (squad.outfit != null && squad.outfit.equipmentTotalCost != 0)
					{
						squad.OutfitSquad(squad.outfit);
					}
				}
			}

			if (factionFC.updateVersion < 0.354)
			{
				foreach (SettlementFC settlement in factionFC.settlements)
				{
					if (settlement.mapLocation == null)
					{
						bool found = false;
						foreach (Settlement tile in Find.World.worldObjects.Settlements)
						{
							if (tile.Name.ToLower() == settlement.name.ToLower())
							{
								settlement.mapLocation = tile.Tile;
								found = true;
								break;
							}
						}
						if (!found)
						{
							Log.Message("Could not find proper settlement for tile location");
							return;
						}
					}
				}
				factionFC.militaryCustomizationUtil.deadPawns = new List<Mercenary>();
			}

			//CHECK SAVE DATA



			//bool broken = false;
			//Log.Message("Empire - Test Settlements");
			//foreach (WorldObject obj in Find.WorldObjects.AllWorldObjects)
			//{
			//	if (obj.def.defName == "Settlement")
			//	{
			//		//Log.Message(obj.Faction.ToString());
			//
			//		if (obj.Faction != null && obj.Faction.def == null)
			//		{
			//			//Log.Message(obj.Label);
			//			Log.Message("Detected broken save");
			//			broken = true;
			//		}
			//	}
			//}

			//Log.Message("Empire - Test Settlement Save");
			//if ((factionFC.name != "PlayerFaction".Translate() || factionFC.settlements.Count() > 0) && getPlayerColonyFaction() == null || broken == true)
			//{
			//	Log.Message("Old save detected - Adjusting factions to fix possible issues. Note: You will see some until the next load.");
			//	Faction newFaction = createPlayerColonyFaction();
			//	foreach (WorldObject obj in Find.WorldObjects.AllWorldObjects)
			//	{
			//		if (obj.def.defName == "Settlement")
			//		{
			//			//Log.Message(obj.Faction.ToString());
			//
			//			if (obj.Faction.ToString() == factionFC.name)
			//			{
			//				if (obj.Faction == null || obj.Faction.def == null)
			//				{
			//					//Log.Message(obj.Label);
			//					obj.SetFaction(newFaction);
			//					Log.Message("Reseting Faction of Settlement " + obj.LabelCap + ". If this is in error, please report it on the Empire Discord");
			//
			//				}
			//
			//
			//			}
			//		}
			//	}
			//}

			//Log.Message("Empire - Test FactionDef Change");

			//foreach (Faction faction in Find.FactionManager.AllFactions)
			//{
			//	if (faction.Name == factionFC.name && faction.leader != null || faction.def.defName == "PColonySpacer" || faction.def.defName == "PColonyTribal" || faction.def.defName == "PColonyIndustrial")
			//	{
			//		//Log.Message("Found Faction");
			//		if (faction.def.defName != "PColony" || faction.def == null)
			//		{
			//			//Log.Message("Setting new factiondef");
			//			faction.def = DefDatabase<FactionDef>.GetNamed("PColony");
			//			factionFC.updateFaction();
			//		}
			//	}
			//}
			Log.Message("Empire - Setting Null Variables");

			if (factionFC.militaryTargets == null)
			{
				Log.Message("Empire - militaryTargets was Null");
				factionFC.militaryTargets = new List<int>();
			}

			if (factionFC.factionDef == null)
			{
				Log.Message("Empire - factionDef was Null");
				factionFC.updateFaction();
			}

			if (factionFC.factionIconPath == null || factionFC.factionIconPath == "FactionIcons/Base")
			{
				Log.Message("Empire - Faction icon null - resetting");

				factionFC.factionIconPath = "Base";

			}

			if (factionFC.militaryTimeDue == -1)
			{
				Log.Message("Empire - militaryTimeDue was Null");
				factionFC.militaryTimeDue = Find.TickManager.TicksGame + 30000;
			}

			if (factionFC.timeStart == -1)
			{
				Log.Message("Empire - timeStart was Null");
				factionFC.timeStart = Find.TickManager.TicksGame - 600000;
			}

			if (factionFC.militaryCustomizationUtil == null)
			{
				Log.Message("Empire - militaryCustomizationUtil was Null");
				factionFC.militaryCustomizationUtil = new MilitaryCustomizationUtil();
			}

			Log.Message("Empire - Testing Settlements for null variables");
			foreach (SettlementFC settlement in factionFC.settlements)
			{
				if (settlement.loadID == -1)
				{
					settlement.loadID = factionFC.GetNextSettlementFCID();
					Log.Message("Settlement: " + settlement.name + " - Reseting load ID");
				}

				if (settlement.prisoners == null)
				{
					settlement.prisoners = new List<Pawn>();
				}

				foreach (SettlementFC settlement2 in factionFC.settlements)
				{
					if (settlement != settlement2)
					{
						//if not same
						if (settlement.loadID == settlement2.loadID)
						{
							Log.Message("Fixing LoadID of settlement");
							settlement2.loadID = factionFC.GetNextSettlementFCID(); ;
						}
					}
				}
			}

			Log.Message("Empire - Testing for traits with no tie");
			//check for dull traits
			verifyTraits();


			Log.Message("Empire - Testing for invalid capital map");
			//Check for an invalid capital map
			if (Find.WorldObjects.SettlementAt(factionFC.capitalLocation) == null && factionFC.SoSShipCapital == false)
			{
				Messages.Message("Please reset your capital location. If you continue to see this after reseting, please report it.", MessageTypeDefOf.NegativeEvent);
			}
			else
			{
				
				//if (Find.WorldObjects.SettlementAt(Find.World.GetComponent<FactionFC>().capitalLocation).Map != null)
				//{
				//	if (Find.CurrentMap.IsPlayerHome == true)
				//	{
						//	Find.World.GetComponent<FactionFC>().capitalLocation = Find.CurrentMap.Tile;
				//	}
				//}
			}

			if (factionFC.taxMap == null)
			{
				Messages.Message("Your tax map has not been set. Please set it using the faction menu.", MessageTypeDefOf.CautionInput);
			}

			if (factionFC.policies.Count() < 2)
			{
				Find.LetterStack.ReceiveLetter("FCTraits".Translate(), "FCSelectYourTraits".Translate(), LetterDefOf.NeutralEvent);
			}

			Log.Message("Empire - Testing for update change");



			//Add update letter/checker here!!
			if (factionFC.updateVersion < 0.350)
			{
				string str;
				str = "A new update for Empire has been released!  v.0.350\n The following abbreviated changes have occurred:";
				str += "\n\n- New 'Overview' window. Can be opened from the faction window.";
				str += "\n\n- Can now directly control your faction's military units";
				str += "\n\n- New Faction Trait system replacing the old policies system";
				str += "\n- 8 new starting traits. When creating your faction, you get to pick two but cannot change them";
				str += "\n- 7 new unlockable traits";
				str += "\n\n- New faction leveling system";
				str += "\n- Unlock new slots to select traits by leveling your faction";
				str += "\n- Gain XP from the following:";
				str += "\n- + 10xp per settlement created";
				str += "\n- + 5xp per successful raid/enslavement raid";
				str += "\n- + 5xp per settlement captured";
				str += "\n- + 1xp per settlement per tax period";
				//str += "\n- Fixed some other minor bugs and changes";
				//str += "\n- Fixed specific animals from the Witcher Mod, Alpha Animals, Genetic Rim animals from being animal tithes";

				//str += "\n- A bit more randomness added to enemy factions attacking. It is now possible for their attacking force to be 2 below or 2 above their standard attack force size. (This is before combat modifiers)";
				//str += "\n- Bug Fixes";
				//str += "\n";
				str += "\n\n Also, if you need help, go check out this video I made with this url: https://youtu.be/lvWb1rMMsq8";


				str += "\n\nWant to see the full patch notes? Join us on Discord! https://discord.gg/f3zFQqA";

				factionFC.updateVersion = 0.354;
				Find.LetterStack.ReceiveLetter("Empire Mod Update!", str, LetterDefOf.NewQuest);


			}

			
		}

		public static void testLogFunction()
		{
			Log.Message("Test Successful");
		}

		public static void verifyTraits()
		{
			//make new list for factionfc traits
			//loop through events and add traits
			//loop through
			List<FCTraitEffectDef> factionTraits = new List<FCTraitEffectDef>();

			foreach (FCEvent evt in Find.World.GetComponent<FactionFC>().events)
			{
				if (evt.settlementTraitLocations.Count() > 0)
				{
					//ignore
				} else
				{
					factionTraits.AddRange(evt.traits);
				}
			}

			Find.World.GetComponent<FactionFC>().traits = factionTraits;

			//go through each settlement and make new list for each settlement
			//loop through each active event and add settlement traits
			//loop through buildings and add traits

			foreach (SettlementFC settlement in Find.World.GetComponent<FactionFC>().settlements)
			{
				List<FCTraitEffectDef> settlementsTraits = new List<FCTraitEffectDef>();

				foreach (FCEvent evt in Find.World.GetComponent<FactionFC>().events)
				{
					if (evt.settlementTraitLocations.Count() > 0)
					{
						//ignore
						if (evt.settlementTraitLocations.Contains(settlement) == true)
						{
							settlementsTraits.AddRange(evt.traits);
						}
					}
					else
					{
						//factionTraits.AddRange(evt.traits);
					}
				}

				foreach (BuildingFCDef building in settlement.buildings)
				{
					settlementsTraits.AddRange(building.traits);
				}

				settlement.traits = settlementsTraits;
			}



		}

		public static bool checkForMod(string packageID)
		{
			foreach (ModContentPack mod in LoadedModManager.RunningModsListForReading)
			{
				//Log.Message(mod.PackageIdPlayerFacing);
				if (mod.PackageIdPlayerFacing == packageID)
				{
					return true;
				}
			}

			return false;
		}

		public static Type returnUnknownTypeFromName(string name)
		{
			foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
			{
				var type = a.GetType(name);
				if (type != null)
					return type;
			}
			return null;
		}

		public static double calculateMilitaryLevelPoints(int MilitaryLevel)
		{
			double points = 500; //starting points at mil level 0
			for (int i = 1; i <= MilitaryLevel; i++)
			{
				points += (500 * MilitaryLevel);
			}
			return points;
		}

		public static bool canCraftItem(ThingDef thing, bool includeSingleUse = false)
		{
			bool canCraft = true;
			if (thing.recipeMaker != null)
			{
				if (thing.recipeMaker.researchPrerequisites != null)
				{
					foreach (ResearchProjectDef research in thing.recipeMaker.researchPrerequisites)
					{
						if (!(Find.ResearchManager.GetProgress(research) >= research.baseCost))
						{
							//research is not good
							canCraft = false;
						}
					}
				}
				if (thing.recipeMaker.researchPrerequisite != null)
				{
					if (!(Find.ResearchManager.GetProgress(thing.recipeMaker.researchPrerequisite) >= thing.recipeMaker.researchPrerequisite.baseCost))
					{
						//research is not good
						canCraft = false;
					}
				}
			} else
			{
				if (Find.World.GetComponent<FactionFC>().techLevel < thing.techLevel)
				{
					canCraft = false;
				}
			}

			if (thing.thingSetMakerTags != null && thing.thingSetMakerTags.Contains("SingleUseWeapon") && !includeSingleUse)
			{
				canCraft = false;
			}


			return canCraft;
		}
		public static Faction getPlayerColonyFaction()
		{


			return Find.FactionManager.FirstFactionOfDef(DefDatabase<FactionDef>.GetNamed("PColony"));
		}


		//<DevAdd>   Create new seperate function to create a faction
		public static Settlement createPlayerColonySettlement(int tile, bool createWorldObject, string planetName)
		{
			//Log.Message("boop");
			StringBuilder reason = new StringBuilder();
			if (!TileFinder.IsValidTileForNewSettlement(tile, reason))
			{
				//Log.Message("Invalid Tile");
				//Alert Error to User
				Messages.Message(reason.ToString(), MessageTypeDefOf.NegativeEvent);


				return null;
				//create alert with reason
				//AlertsReadout alert = new AlertsReadout()
			}

			//Log.Message("Colony is being created");
			Faction faction = getPlayerColonyFaction();

			FactionFC worldcomp = Find.World.GetComponent<FactionFC>();
			if (worldcomp.settlements.Count() < 1)
			{
				Find.World.GetComponent<FactionFC>().timeStart = Find.TickManager.TicksGame;
			}

			//Log.Message(faction.Name);

			SettlementFC settlementfc;
			Settlement settlement = null;
			if (createWorldObject)
			{
				settlement = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
				settlement.SetFaction(faction);
				settlement.Tile = tile; //TileFinder.RandomSettlementTileFor(faction, false, null);
				settlement.Name = SettlementNameGenerator.GenerateSettlementName(settlement, null);
				Find.WorldObjects.Add(settlement);
				settlementfc = new SettlementFC(settlement.Name, settlement.Tile);
			} else
			{
				settlementfc = new SettlementFC("Settlement", tile);
			}

			//create settlement data for world object
			settlementfc.power.isTithe = true;
			settlementfc.power.isTitheBool = true;
			settlementfc.research.isTithe = true;
			settlementfc.research.isTitheBool = true;
			settlementfc.planetName = planetName;
			if (worldcomp.hasPolicy(FCPolicyDefOf.militaristic))
				settlementfc.constructBuilding(DefDatabase<BuildingFCDef>.GetNamed("barracks"), 0);
			if (worldcomp.hasPolicy(FCPolicyDefOf.authoritarian))
				settlementfc.loyalty = 70;
			if (worldcomp.hasPolicy(FCPolicyDefOf.egalitarian))
				settlementfc.happiness = 60;
			if (worldcomp.hasPolicy(FCPolicyDefOf.expansionist) && settlementfc.settlementLevel == 1)
				settlementfc.upgradeSettlement();

			worldcomp.addSettlement(settlementfc);
			if (createWorldObject)
			{
				worldcomp.roadBuilder.FlagUpdateRoadQueues();
			}
			Find.LetterStack.ReceiveLetter("FCSettlementFormed".Translate(), "TheSettlement".Translate() + " " + settlementfc.name + "HasBeenFormed".Translate() + "!", LetterDefOf.PositiveEvent);

			//Example to grab settlement data from FC
			//Log.Message(settlementfc.ReturnFCSettlement().Name.ToString());



			return settlement;
		}

		[DebugAction("Empire", "Increment Time 5 Days", allowedGameStates = AllowedGameStates.Playing)]
		private static void incrementTimeFiveDays()
		{
			//Log.Message("Debug - Increment Time 5 Days");
			Find.TickManager.DebugSetTicksGame(Find.TickManager.TicksGame + 300000);
		}
		[DebugAction("Empire", "Increment Time 1 Year", allowedGameStates = AllowedGameStates.Playing)]
		private static void incrementTimeOneYear()
		{
			//Log.Message("Debug - Increment Time 5 Days");
			Find.TickManager.DebugSetTicksGame(Find.TickManager.TicksGame + GenDate.TicksPerYear);
		}

		[DebugAction("Empire", "Reset All Military Squad Assignments", allowedGameStates = AllowedGameStates.Playing)]
		private static void resetAllMilitarySquads()
		{
			Log.Message("Debug - Reset All Military Squad Assignments");
			MilitaryCustomizationUtil util = Find.World.GetComponent<FactionFC>().militaryCustomizationUtil;

			foreach(Mercenary merc in util.AllMercenaries)
			{
				merc.pawn.Destroy();
				merc.squad.mercenaries.Remove(merc);
			}

			for (int k = util.mercenarySquads.Count() - 1; k >= 0; k--)
			{
				util.mercenarySquads[k].settlement.militarySquad = null;
				util.mercenarySquads.RemoveAt(k);
			}

			util.checkMilitaryUtilForErrors();
		}



		[DebugAction("Empire", "Make Random Event", allowedGameStates = AllowedGameStates.Playing)]
		private static void makeRandomEvent()
		{
			List<DebugMenuOption> list = new List<DebugMenuOption>();
			foreach (FCEventDef evtDef in DefDatabase<FCEventDef>.AllDefsListForReading)
			{
				if (evtDef.isRandomEvent == true)
					list.Add(new DebugMenuOption(evtDef.label, DebugMenuOptionMode.Action, delegate ()
					{
						FCEvent evt;

						Log.Message("Debug - Make Random Event - " + evtDef.label);
						evt = FCEventMaker.MakeRandomEvent(evtDef, null);
						if (evtDef.activateAtStart == false)
						{
							FCEventMaker.MakeRandomEvent(evtDef, null); Find.World.GetComponent<FactionFC>().addEvent(evt);
						}

						//letter code
						string settlementString = "";
						foreach (SettlementFC loc in evt.settlementTraitLocations)
						{
							if (settlementString == "")
							{
								settlementString = settlementString + loc.name;
							}
							else
							{
								settlementString = settlementString + ", " + loc.name;
							}
						}
						if (settlementString != "")
						{
							Find.LetterStack.ReceiveLetter("Random Event", evtDef.desc + "\n This event is affecting the following settlements: " + settlementString, LetterDefOf.NeutralEvent);
						}
						else
						{

						}
					}
	
					));
			}
			Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));




		}

		[DebugAction("Empire", "Proc MilitaryTimeDue", allowedGameStates = AllowedGameStates.Playing)]
		private static void procMilitaryTimeDue()
		{
			Log.Message("Debug - Proc MilitaryTimeDue");
			Find.World.GetComponent<FactionFC>().militaryTimeDue = Find.TickManager.TicksGame + 1;
		}

		[DebugAction("Empire", "Fix Missing Settlements", allowedGameStates = AllowedGameStates.Playing)]
		private static void checkForMissingSettlements()
		{
			Log.Message("Debug - Proc MilitaryTimeDue");

			FactionFC factionfc = Find.World.GetComponent<FactionFC>();

			foreach (SettlementFC settlement in factionfc.settlementsOnPlanet)
			{
				if (Find.WorldObjects.AnySettlementAt(settlement.mapLocation) == false)
				{
					Settlement worldObj = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
					worldObj.SetFaction(FactionColonies.getPlayerColonyFaction());
					worldObj.Tile = settlement.mapLocation;
					worldObj.Name = settlement.name;
					Find.WorldObjects.Add(worldObj);
				} else
				{
					Settlement obj = Find.WorldObjects.SettlementAt(settlement.mapLocation);
					if (obj.Faction != FactionColonies.getPlayerColonyFaction())
						obj.SetFaction(FactionColonies.getPlayerColonyFaction());
				}
			}
		}
		


		[DebugAction("Empire", "Reset Faction Leaders", allowedGameStates = AllowedGameStates.Playing)]
		private static void resetFactionLeadeers()
		{
			Log.Message("Debug - Reset Faction Leaders");
			SoS2HarmonyPatches.ResetFactionLeaders();
		}

		[DebugAction("Empire", "Attack Player Settlement", allowedGameStates = AllowedGameStates.Playing)]
		private static void attackPlayerSettlement()
		{
			List<DebugMenuOption> list = new List<DebugMenuOption>();
			foreach (SettlementFC settlement in Find.World.GetComponent<FactionFC>().settlements)
			{
				list.Add(new DebugMenuOption(settlement.name, DebugMenuOptionMode.Action, delegate ()
				{
					Log.Message("Debug - Attack Player Settlement - " + settlement.name);
					Faction enemyFaction = Find.FactionManager.RandomEnemyFaction();
					MilitaryUtilFC.attackPlayerSettlement(militaryForce.createMilitaryForceFromFaction(enemyFaction, true), settlement, enemyFaction);
				}
				));
			}
			Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
		}



		[DebugAction("Empire", "Change Settlement Defending Force", allowedGameStates = AllowedGameStates.Playing)]
		private static void ChangeAttackPlayerSettlementMilitaryForce()
		{
			FactionFC worldcomp = Find.World.GetComponent<FactionFC>();
			List<DebugMenuOption> list = new List<DebugMenuOption>();
			foreach (FCEvent evt in worldcomp.events)
			{


				if (evt.def == FCEventDefOf.settlementBeingAttacked)
				{

					list.Add(new DebugMenuOption(worldcomp.returnSettlementByLocation(evt.location, evt.planetName).name, DebugMenuOptionMode.Action, delegate ()
					{
						//when event is selected, select defending force to replace it with

						List<DebugMenuOption> list2 = new List<DebugMenuOption>();
						foreach (SettlementFC settlement in worldcomp.settlements)
						{
							if (settlement.IsMilitaryValid == true && settlement.name != evt.settlementFCDefending.name)
							{

								list2.Add(new DebugMenuOption(settlement.name + " - " + settlement.settlementMilitaryLevel + " - Busy: " + settlement.IsMilitaryBusySilent, DebugMenuOptionMode.Action, delegate ()
								{
									if (settlement.isMilitaryBusy() == false)
									{
										Log.Message("Debug - Change Player Settlement - " + evt.militaryForceDefending.homeSettlement.name + " to " + settlement.name);
										MilitaryUtilFC.changeDefendingMilitaryForce(evt, settlement);
									}
								}
								));
							}
						}
						Find.WindowStack.Add(new Dialog_DebugOptionListLister(list2));

					}
					));
					Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
				}
			}

		}

		[DebugAction("Empire", "Upgrade Player Settlement", allowedGameStates = AllowedGameStates.Playing)]
		private static void UpgradePlayerSettlement()
		{
			List<DebugMenuOption> list = new List<DebugMenuOption>();
			foreach (SettlementFC settlement in Find.World.GetComponent<FactionFC>().settlements)
			{
				list.Add(new DebugMenuOption(settlement.name, DebugMenuOptionMode.Action, delegate ()
				{
					Log.Message("Debug - Upgrade Player Settlement - " + settlement.name);
					settlement.upgradeSettlement();
				}
				));
			}
			Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
		}

		[DebugAction("Empire", "Upgrade Player Settlement x5", allowedGameStates = AllowedGameStates.Playing)]
		private static void UpgradePlayerSettlementx5()
		{
			List<DebugMenuOption> list = new List<DebugMenuOption>();
			foreach (SettlementFC settlement in Find.World.GetComponent<FactionFC>().settlements)
			{
				list.Add(new DebugMenuOption(settlement.name, DebugMenuOptionMode.Action, delegate ()
				{
					Log.Message("Debug - Upgrade Player Settlement x5- " + settlement.name);
					settlement.upgradeSettlement();
					settlement.upgradeSettlement();
					settlement.upgradeSettlement();
					settlement.upgradeSettlement();
					settlement.upgradeSettlement();
				}
				));
			}
			Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
		}


		[DebugAction("Empire", "Test Function", allowedGameStates = AllowedGameStates.Playing)]
		private static void testVariable()
		{
			Log.Message("Debug - Test Function - ");
			Find.World.GetComponent<FactionFC>().roadBuilder.FlagUpdateRoadQueues();
		}

		[DebugAction("Empire", "De-Level Player Settlement", allowedGameStates = AllowedGameStates.Playing)]
		private static void DelevelPlayerSettlement()
		{
			List<DebugMenuOption> list = new List<DebugMenuOption>();
			foreach (SettlementFC settlement in Find.World.GetComponent<FactionFC>().settlements)
			{
				list.Add(new DebugMenuOption(settlement.name, DebugMenuOptionMode.Action, delegate ()
				{
					Log.Message("Debug - Delevel Player Settlement - " + settlement.name);
					settlement.delevelSettlement();
				}
				));
			}
			Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
		}

		[DebugAction("Empire", "Reset Military Squads Cooldowns", allowedGameStates = AllowedGameStates.Playing)]
		private static void ResetMilitarySquads()
		{
			Find.World.GetComponent<FactionFC>().militaryCustomizationUtil.mercenarySquads = new List<MercenarySquadFC>();
			Log.Message("Debug - Reset Military Squad Cooldowns");
			foreach (SettlementFC settlement in Find.World.GetComponent<FactionFC>().settlements)
			{
				settlement.returnMilitary(false);
			}
		}

		[DebugAction("Empire", "Clear Old Bills", allowedGameStates = AllowedGameStates.Playing)]
		private static void clearOldBills()
		{
			Find.World.GetComponent<FactionFC>().OldBills = new List<BillFC>();
		}

		[DebugAction("Empire", "Clear All Events", allowedGameStates = AllowedGameStates.Playing)]
		private static void clearAllEvents()
		{
			Find.World.GetComponent<FactionFC>().events = new List<FCEvent>();
		}

		[DebugAction("Empire", "Clear All Bills", allowedGameStates = AllowedGameStates.Playing)]
		private static void clearAllBills()
		{
			Find.World.GetComponent<FactionFC>().Bills = new List<BillFC>();
		}

		[DebugAction("Empire", "Place 500 Silver", allowedGameStates = AllowedGameStates.PlayingOnMap)]
		private static void placeSilverFC()
		{


			DebugTool tool = null;
			IntVec3 DropPosition;
			Map map;
			tool = new DebugTool("Select Drop Position", delegate ()
			{
				DropPosition = UI.MouseCell();
				map = Find.CurrentMap;


				Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);
				silver.stackCount = 500;
				GenPlace.TryPlaceThing(silver, DropPosition, map, ThingPlaceMode.Near);
			});
			DebugTools.curTool = tool;
		}

		[DebugAction("Empire", "Place 50000 Silver", allowedGameStates = AllowedGameStates.PlayingOnMap)]
		private static void PlaceALotOfSilver()
		{
			DebugTool tool = null;
			IntVec3 DropPosition;
			Map map;
			tool = new DebugTool("Select Drop Position", delegate ()
			{
				DropPosition = UI.MouseCell();
				map = Find.CurrentMap;


				Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);
				silver.stackCount = 50000;
				GenPlace.TryPlaceThing(silver, DropPosition, map, ThingPlaceMode.Near);
			});
			DebugTools.curTool = tool;
		}

        public static void CallinAlliedForces(SettlementFC settlement, bool DropPod)
		{
            if (DropPod)
            {
                DebugTool tool = new DebugTool("Select Deployment Position", delegate
                {
                    DebugTools.curTool = null;
                    settlement.militarySquad.DeployTo(Find.CurrentMap, PawnsArrivalModeDefOf.CenterDrop, UI.MouseCell());
                });
                DebugTools.curTool = tool;
            }
            else
            {
                settlement.militarySquad.DeployTo(Find.CurrentMap, PawnsArrivalModeDefOf.EdgeWalkIn);
            }
		}

		public static void CallinAlliedForces(SettlementFC settlement, bool DropPod, int cost)
		{
			FactionFC factionfc = Find.World.GetComponent<FactionFC>();
			MercenarySquadFC squad = factionfc.militaryCustomizationUtil.createMercenarySquad(settlement, true);
			squad.OutfitSquad(squad.settlement.militarySquad.outfit);
			//Do not use this normally!!!!

			PaymentUtil.paySilver(cost);

			IncidentParms parms = new IncidentParms();
			parms.target = Find.CurrentMap;
			parms.faction = FactionColonies.getPlayerColonyFaction();
			parms.podOpenDelay = 140;
			parms.points = 999;
			parms.raidArrivalModeForQuickMilitaryAid = true;
			parms.raidNeverFleeIndividual = true;
			parms.raidForceOneIncap = true;
			parms.raidArrivalMode = PawnsArrivalModeDefOf.CenterDrop;
			parms.raidStrategy = RaidStrategyDefOf.ImmediateAttackFriendly;
			parms.raidArrivalModeForQuickMilitaryAid = true;

			squad.updateSquadStats(squad.settlement.settlementMilitaryLevel);
			squad.resetNeeds();


			DebugTool tool = null;
			IntVec3 DropPosition;
			tool = new DebugTool("Select Deployment Position", delegate ()
			{
				DropPosition = UI.MouseCell();
				if (DropPod)
				{
					parms.spawnCenter = DropPosition;
					PawnsArrivalModeWorkerUtility.DropInDropPodsNearSpawnCenter(parms, squad.AllEquippedMercenaryPawns.ToList());
				}
				else
				{
					PawnsArrivalModeWorker_EdgeWalkIn worker = new PawnsArrivalModeWorker_EdgeWalkIn();
					worker.TryResolveRaidSpawnCenter(parms);
					worker.Arrive(squad.AllEquippedMercenaryPawns.ToList(), parms);
					//Log.Message(squad.DeployedMercenaries.Count().ToString());


					foreach (Mercenary merc in squad.DeployedMercenaries)
					{

						merc.pawn.mindState.forcedGotoPosition = DropPosition;
						JobGiver_ForcedGoto jobGiver_Standby = new JobGiver_ForcedGoto();
						ThinkResult resultStandby = jobGiver_Standby.TryIssueJobPackage(merc.pawn, new JobIssueParams());
						bool isValidStandby = resultStandby.IsValid;
						if (isValidStandby)
						{
							//Log.Message("valid");
							merc.pawn.jobs.StartJob(resultStandby.Job, JobCondition.InterruptForced);
						}
					}
					foreach (Mercenary merc in squad.DeployedAnimalMercenaries)
					{
						merc.pawn.mindState.forcedGotoPosition = DropPosition;
						JobGiver_ForcedGoto jobGiver_Standby = new JobGiver_ForcedGoto();
						ThinkResult resultStandby = jobGiver_Standby.TryIssueJobPackage(merc.pawn, new JobIssueParams());
						bool isValidStandby = resultStandby.IsValid;
						if (isValidStandby)
						{

							//Log.Message("valid");
							merc.pawn.jobs.StartJob(resultStandby.Job, JobCondition.InterruptForced);
						}
					}
				}

				squad.isDeployed = true;
				squad.order = MilitaryOrders.Standby;
				squad.orderLocation = DropPosition;
				squad.timeDeployed = Find.TickManager.TicksGame;
				Find.LetterStack.ReceiveLetter("Military Deployed", "The Military forces of " + squad.settlement.name + " have been deployed to " + Find.CurrentMap.Parent.LabelCap, LetterDefOf.NeutralEvent, new LookTargets(squad.AllEquippedMercenaryPawns));
				factionfc.traitMilitaristicTickLastUsedExtraSquad = Find.TickManager.TicksGame;

				DebugTools.curTool = null;

			});
			DebugTools.curTool = tool;

			//UI.UIToMapPosition(UI.MousePositionOnUI).ToIntVec3();
		}

		public static void FireSupport(SettlementFC settlement, MilitaryFireSupport support)
		{
			DebugTool tool = null;
			IntVec3 DropPosition;
			tool = new DebugTool("Select Artillery Position", delegate ()
			{
				float cost = support.returnTotalCost();
				if (PaymentUtil.getSilver() > cost)
				{
					PaymentUtil.paySilver((int)Math.Round(cost));
					DropPosition = UI.MouseCell();
					IntVec3 spawnCenter = DropPosition;
					Map map = Find.CurrentMap;
					//Make new list
					List<ThingDef> projectiles = new List<ThingDef>();
					projectiles.AddRange(support.projectiles);
					MilitaryFireSupport fireSupport = new MilitaryFireSupport("fireSupport", map, spawnCenter, projectiles.Count()*15, 600, support.accuracy, projectiles);
					Find.World.GetComponent<FactionFC>().militaryCustomizationUtil.fireSupport.Add(fireSupport);

					Messages.Message(support.name + " will be fired upon shortly on the marked position!", MessageTypeDefOf.ThreatSmall);
					settlement.artilleryTimer = Find.TickManager.TicksGame + 60000;

				} else
				{
					Messages.Message("You do not have enough silver to pay for the strike!", MessageTypeDefOf.RejectInput);
				}


				DebugTools.curTool = null;
			}, delegate
			{
				GenDraw.DrawRadiusRing(UI.MouseCell(), support.accuracy, Color.red, null);
			});
			DebugTools.curTool = tool;
		}


		//[DebugAction("Empire", "Call In Artillery", allowedGameStates = AllowedGameStates.PlayingOnMap)]
		private static void ArtilleryStrike()
		{
			DebugTool tool = null;
			IntVec3 DropPosition;
			tool = new DebugTool("Select Artillery Position", delegate ()
			{
				DropPosition = UI.MouseCell();
				IntVec3 spawnCenter = DropPosition;
				Map map = Find.CurrentMap;
				MilitaryFireSupport fireSupport = new MilitaryFireSupport("lightArtillery", map, spawnCenter, 600, 600, 20);
				Find.World.GetComponent<FactionFC>().militaryCustomizationUtil.fireSupport.Add(fireSupport);

				Messages.Message("An artillery strike will be occuring shortly on the marked position!", MessageTypeDefOf.ThreatSmall);




				DebugTools.curTool = null;
			}, onGUIAction: delegate
			{
				//GenUI.RenderMouseoverBracket();
				//GenDraw.DrawRadiusRing(UI.MouseCell(), 26, Color.yellow, null);
				//GenDraw.DrawRadiusRing(UI.MouseCell(), 20, Color.red, null);
				GenDraw.DrawRadiusRing(UI.MouseCell(), 26, Color.yellow, null);
				GenDraw.DrawRadiusRing(UI.MouseCell(), 20, Color.red, null);
			});
			DebugTools.curTool = tool;
		}

		

		public static void TurretDrop()
		{

		}

		private static void CallInAlliedForcesSelect()
		{
			List<FloatMenuOption> list = new List<FloatMenuOption>();
			foreach (SettlementFC settlement in Find.World.GetComponent<FactionFC>().settlements)
			{
				if (settlement.militarySquad != null)
				{
					list.Add(new FloatMenuOption(settlement.name, delegate ()
					{

						IncidentParms parms = new IncidentParms();
						parms.target = Find.CurrentMap;
						parms.faction = FactionColonies.getPlayerColonyFaction();
						parms.podOpenDelay = 140;
						parms.points = 999;
						parms.raidArrivalModeForQuickMilitaryAid = true;
						parms.raidNeverFleeIndividual = true;
						parms.raidForceOneIncap = true;
						parms.raidArrivalMode = PawnsArrivalModeDefOf.CenterDrop;
						parms.raidStrategy = RaidStrategyDefOf.ImmediateAttackFriendly;
						parms.raidArrivalModeForQuickMilitaryAid = true;

						settlement.militarySquad.updateSquadStats(settlement.settlementMilitaryLevel);


						DebugTool tool = null;
						IntVec3 DropPosition;
						tool = new DebugTool("Select Drop Position", delegate ()
						{
							DropPosition = UI.MouseCell();
							parms.spawnCenter = DropPosition;

							//List<Pawn> list2 = parms.raidStrategy.Worker.SpawnThreats(parms);
							//parms.raidArrivalMode.Worker.Arrive(list2, parms);
							settlement.militarySquad.isDeployed = true;
							settlement.militarySquad.order = MilitaryOrders.Standby;
							settlement.militarySquad.orderLocation = DropPosition;
							settlement.militarySquad.timeDeployed = Find.TickManager.TicksGame;



							PawnsArrivalModeWorkerUtility.DropInDropPodsNearSpawnCenter(parms, settlement.militarySquad.AllEquippedMercenaryPawns.ToList());
							settlement.militarySquad.isDeployed = true;
							DebugTools.curTool = null;
						});
						DebugTools.curTool = tool;

						//UI.UIToMapPosition(UI.MousePositionOnUI).ToIntVec3();

					}
					));
				}
			}
			Find.WindowStack.Add(new FloatMenu(list));
		}


		[DebugAction("Empire", "Call In Allied Forces", allowedGameStates = AllowedGameStates.PlayingOnMap)]
		private static void CallInAlliedForcesDebug()
		{
			CallInAlliedForcesSelect();
		}



		[DebugAction("Empire", "Level Up Faction", allowedGameStates = AllowedGameStates.PlayingOnMap)]
		private static void LevelUpFaction()
		{
			FactionFC faction = Find.World.GetComponent<FactionFC>();
			faction.addExperienceToFactionLevel(faction.factionXPGoal);

		}

		public static bool returnIsResearched(ResearchProjectDef def)
		{
			string name = def.defName;
			return (bool)(Find.ResearchManager.GetProgress(def) == def.baseCost);
		}

		public static void removePlayerSettlement(SettlementFC settlement)
		{
			FactionFC faction = Find.World.GetComponent<FactionFC>();
			faction.settlements.Remove(settlement);
			Messages.Message(TranslatorFormattedStringExtensions.Translate("SettlementRemoved", settlement.name), MessageTypeDefOf.NegativeEvent);

			if (Find.World.info.name == settlement.planetName)
			{
				Find.WorldObjects.Remove(Find.World.worldObjects.SettlementAt(settlement.mapLocation));
			} else
			{
				faction.deleteSettlementQueue.Add(new SettlementSoS2Info(settlement.planetName, settlement.mapLocation));
			}

			//clear military events
			settlement.returnMilitary(false);
			Reset:
			foreach (FCEvent evt in faction.events)
			{
				//military event removal
				if (evt.def == FCEventDefOf.captureEnemySettlement || evt.def == FCEventDefOf.raidEnemySettlement)
				{
					if(evt.militaryForceAttacking.homeSettlement == settlement)
					{
						faction.events.Remove(evt);
						goto Reset;
					}
				}
				if (evt.def == FCEventDefOf.settlementBeingAttacked)
				{
					if (evt.militaryForceDefending.homeSettlement == settlement)
					{
						if (evt.settlementFCDefending == settlement)
						{
							faction.events.Remove(evt);
							goto Reset;
						} else
						{
							//if not defending settlement
							MilitaryUtilFC.changeDefendingMilitaryForce(evt, evt.settlementFCDefending);
						}
					} else
					{
						//if force belongs to other settlement
						evt.militaryForceDefending.homeSettlement.cooldownMilitary();

						faction.events.Remove(evt);
						goto Reset;
					}
				}


				//settlement event removal
				if (evt.def == FCEventDefOf.constructBuilding || evt.def == FCEventDefOf.enactSettlementPolicy || evt.def == FCEventDefOf.upgradeSettlement || evt.def == FCEventDefOf.cooldownMilitary)
				{
					if (evt.source == settlement.mapLocation)
					{
						faction.events.Remove(evt);
						goto Reset;
					}
				}

				if (evt.def.isRandomEvent == true && evt.settlementTraitLocations.Count() > 0)
				{
					if (evt.settlementTraitLocations.Contains(settlement))
					{
						evt.settlementTraitLocations.Remove(settlement);
						if (evt.settlementTraitLocations.Count() == 0)
						{
							faction.events.Remove(evt);
							goto Reset;
						}
					}
				}
			}
		}

		public static int CompareFloatMenuOption(FloatMenuOption x, FloatMenuOption y)
		{
			return String.Compare(x.Label, y.Label);
		}

		public static int CompareBuildingDef(BuildingFCDef x, BuildingFCDef y)
		{
			return string.Compare(x.label, y.label);
		}

		public static int CompareSettlementName(SettlementFC x, SettlementFC y)
		{
			return string.Compare(x.name, y.name);
		}

		public static int CompareSettlementLevel(SettlementFC x, SettlementFC y)
		{
			return y.settlementLevel.CompareTo(x.settlementLevel);
		}

		public static int CompareSettlementMilitaryLevel(SettlementFC x, SettlementFC y)
		{
			return y.settlementMilitaryLevel.CompareTo(x.settlementMilitaryLevel);
		}
		public static int CompareSettlementFreeWorkers(SettlementFC x, SettlementFC y)
		{
			return ((y.workersUltraMax - y.getTotalWorkers()).CompareTo((x.workersUltraMax - x.getTotalWorkers())));
		}
		public static int CompareSettlementUnrest(SettlementFC x, SettlementFC y)
		{
			return x.unrest.CompareTo(y.unrest);
		}
		public static int CompareSettlementLoyalty(SettlementFC x, SettlementFC y)
		{
			return y.loyalty.CompareTo(x.loyalty);
		}
		public static int CompareSettlementHappiness(SettlementFC x, SettlementFC y)
		{
			return y.happiness.CompareTo(x.happiness);
		}
		public static int CompareSettlementProsperity(SettlementFC x, SettlementFC y)
		{
			return y.prosperity.CompareTo(x.prosperity);
		}
		public static int CompareSettlementProfit(SettlementFC x, SettlementFC y)
		{
			return y.getTotalProfit().CompareTo(x.getTotalProfit());
		}


		public static int ReturnTicksToArrive(int currentTile, int destinationTile)
		{
			bool medievalOnly = LoadedModManager.GetMod<FactionColoniesMod>().GetSettings<FactionColonies>().medievalTechOnly;
			ResearchProjectDef def = DefDatabase<ResearchProjectDef>.GetNamed("TransportPod", false);
			int ticksToArrive = -1;


			if (currentTile == -1 || destinationTile == -1)
			{
				if (!medievalOnly && def != null && Find.ResearchManager.GetProgress(def) == def.baseCost)
				{
					//if have research pod tech
					return 30000;
				}

				return 600000;
			}

			using (WorldPath tempPath = Find.WorldPathFinder.FindPath(currentTile, destinationTile, null, null))
			{
				if (tempPath == WorldPath.NotFound)
				{
					ticksToArrive = 600000;
				}
				else
				{
					ticksToArrive = CaravanArrivalTimeEstimator.EstimatedTicksToArrive(currentTile, destinationTile, tempPath, 0f, CaravanTicksPerMoveUtility.GetTicksPerMove(null, null), Find.TickManager.TicksAbs);
				}
			}

			if (!medievalOnly && def != null && Find.ResearchManager.GetProgress(def) == def.baseCost && ticksToArrive > 30000)
			{
				//if have research pod tech
				return 30000;
			}
			return ticksToArrive;
		}

		public static void sendPrisoner(Pawn prisoner, SettlementFC settlement)
		{
			settlement.addPrisoner(prisoner);
			prisoner.DeSpawn();
		}

		public static Faction copyPlayerColonyFaction()
		{
			FactionFC worldcomp = Find.World.GetComponent<FactionFC>();

			worldcomp.setCapital();

			FactionDef facDef = new FactionDef();


			facDef = DefDatabase<FactionDef>.GetNamed("PColony");
			Faction faction = new Faction();
			faction.def = facDef;
			faction.def.techLevel = worldcomp.factionBackup.def.techLevel;
			faction.loadID = Find.UniqueIDsManager.GetNextFactionID();
			faction.colorFromSpectrum = worldcomp.factionBackup.colorFromSpectrum;
			faction.Name = worldcomp.factionBackup.Name;
			faction.centralMelanin = worldcomp.factionBackup.centralMelanin;
			//<DevAdd> Copy player faction relationships  
			foreach (Faction other in Find.FactionManager.AllFactionsListForReading)
			{
				faction.TryMakeInitialRelationsWith(other);
			}
			//faction.GenerateNewLeader();
			faction.TryGenerateNewLeader();

			//Log.Message(Find.FactionManager.AllFactions.Contains(faction).ToString());

			//Find.FactionManager.Add(faction);

			//check if SoS2 is enabled
			if (FactionColonies.checkForMod("kentington.saveourship2"))
			{
				Log.Message("SoS2 running - planet changed");
				//SoS2 is loaded

				Type typ = FactionColonies.returnUnknownTypeFromName("SaveOurShip2.WorldSwitchUtility");
				Type typ2 = FactionColonies.returnUnknownTypeFromName("SaveOurShip2.WorldFactionList");

				var mainclass = Traverse.CreateWithType(typ.ToString());
				var dict = mainclass.Property("PastWorldTracker").Field("WorldFactions").GetValue();

				var planetfactiondict = Traverse.Create(dict);
				var unknownclass = planetfactiondict.Property("Item", new object[]{ Find.World.info.name }).GetValue();

				var factionlist = Traverse.Create(unknownclass);
				var list = factionlist.Field("myFactions").GetValue();
				List<String> modifiedlist = (List<String>)list;
				modifiedlist.Add(faction.GetUniqueLoadID());
				factionlist.Field("myFactions").SetValue(modifiedlist);
				//Log.Message("Added faction to world list");

				foreach (Faction other in Find.FactionManager.AllFactionsVisibleInViewOrder)
				{
					faction.TryMakeInitialRelationsWith(other);
				}
				Find.FactionManager.Add(faction);
			}



			return faction;
		}

		public static void debugMarker(ref int i)
		{
			Log.Message(i.ToString());
			i = i + 1;
		}
		public static Faction createPlayerColonyFaction()
		{
			FactionFC worldcomp = Find.World.GetComponent<FactionFC>();
			//Log.Message("Creating new faction");
			//Set start time for world component to start tracking your faction;
			worldcomp.setCapital();

			//Log.Message("Faction is being created");
			FactionDef facDef = new FactionDef();


			facDef = DefDatabase<FactionDef>.GetNamed("PColony");
			Faction faction = new Faction();
			faction.def = facDef;
			faction.def.techLevel = TechLevel.Undefined;
			faction.loadID = Find.UniqueIDsManager.GetNextFactionID();
			faction.colorFromSpectrum = FactionGenerator.NewRandomColorFromSpectrum(faction);
			faction.Name = "PlayerColony".Translate();
			faction.centralMelanin = Rand.Value;
			//<DevAdd> Copy player faction relationships  
			foreach (Faction other in Find.FactionManager.AllFactionsListForReading)
			{
				faction.TryMakeInitialRelationsWith(other);
			}
			faction.TryGenerateNewLeader();
			worldcomp.factionBackup = faction;
			Find.FactionManager.Add(faction);
			return faction;
		}

		public static void changePlayerColonyFaction(Faction faction)
		{
			faction = createPlayerColonyFaction();
			Log.Message("Faction was updated - " + faction.Name);
		}


		private static List<float> getAttackPoints()
		{
			List<float> list = new List<float>();
			for (int i = -Convert.ToInt32(plusOrMinusRandomAttackValue * 10); i < plusOrMinusRandomAttackValue * 10; i++)
			{
				list.Add((i / 10));
			}
			return list;

		}

		public static float randomAttackModifier()
		{
			float y = (from x in getAttackPoints()
					   select x).RandomElementByWeight((float x) => new SimpleCurve { { new CurvePoint(0f, 1f), true }, { new CurvePoint(plusOrMinusRandomAttackValue, .1f), true } }.Evaluate(Math.Abs(x) - 2));
			return y;

		}


		public static string FloorStat(double stat)
		{
			return Convert.ToString(Math.Floor((stat * 100)) / 100);
		}

		public static FactionColonies Settings()
		{
			return LoadedModManager.GetMod<FactionColoniesMod>().GetSettings<FactionColonies>();
		}

		public static string getTownTitle(SettlementFC settlement)
		{
			string title = "";
			double highest = 0;
			ResourceType? resourceKey = null;
			int level;
			if (settlement.settlementLevel <= 3)
			{
				level = 1;
			} else if (settlement.settlementLevel <= 6)
			{
				level = 2;
			}
			else
			{
				level = 3;
			}

			foreach (ResourceType resourceType in ResourceUtils.resourceTypes)
			{
				ResourceFC resource = settlement.getResource(resourceType);
				if (resource.endProduction > highest)
				{
					highest = resource.endProduction;
					resourceKey = resourceType;
				}
			}

			return ("FCTitle_" + resourceKey + "_" + level).Translate();
		}
		
		public int silverPerResource = 100;
		public static double silverToCreateSettlement = 1000;
		public int timeBetweenTaxes = GenDate.TicksPerTwelfth;
		public static int updateUiTimer = 150;
		public int productionTitheMod = 25;
		public static int productionResearchBase = 100;
		public static int storeReportCount = 4;
		public int workerCost = 100;
		

		public static double unrestBaseGain = 0;
		public static double unrestBaseLost = 1;

		public static double loyaltyBaseGain = 1;
		public static double loyaltyBaseLost = 0;

		public static double happinessBaseGain = 1;
		public static double happinessBaseLost = 0;

		public static double prosperityBaseRecovery = 1;

		public double settlementBaseUpgradeCost = 1000;
		public int settlementMaxLevel = 10;

		public static int randomEventChance = 25;

		public bool medievalTechOnly = false;
		public bool disableHostileMilitaryActions = false;
		public bool disableRandomEvents = false;
		public bool disableForcedPausingDuringEvents = true;

		public int minDaysTillMilitaryAction = 4;
		public int maxDaysTillMilitaryAction = 10;
		public IntRange minMaxDaysTillMilitaryAction = new IntRange(4, 10);

		private static float plusOrMinusRandomAttackValue = 2;
		public static double militaryAnimalCostMultiplier = 1.5;
		public static double militaryRaceCostMultiplier = .15;
		
		public override void ExposeData()
		{

			base.ExposeData();
			Scribe_Values.Look(ref silverPerResource, "silverPerResource");
			Scribe_Values.Look(ref timeBetweenTaxes, "timeBetweenTaxes");
			Scribe_Values.Look(ref productionTitheMod, "productionTitheMod");
			Scribe_Values.Look(ref workerCost, "workerCost");
			Scribe_Values.Look(ref settlementMaxLevel, "settlementMaxLevel");
			Scribe_Values.Look(ref medievalTechOnly, "medievalTechOnly");
			Scribe_Values.Look(ref disableHostileMilitaryActions, "disableHostileMilitaryActions");
			Scribe_Values.Look(ref disableRandomEvents, "disableRandomEvents");
			Scribe_Values.Look(ref minDaysTillMilitaryAction, "minDaysTillMilitaryAction");
			Scribe_Values.Look(ref maxDaysTillMilitaryAction, "maxDaysTillMilitaryAction");
			
			//Log.Message("load");
			//Log.Message(silverPerResource.ToString());

		}


	}

	public class FactionColoniesMod : Mod
	{
		public FactionColonies settings = new FactionColonies();

		public FactionColoniesMod(ModContentPack content) : base(content)
		{
			settings = GetSettings<FactionColonies>();
		}

		string silverPerResource;
		string timeBetweenTaxes;
		string productionTitheMod;
		string workerCost;
		string settlementMaxLevel;
		int daysBetweenTaxes;
		bool medievalTechOnly;
		bool disableHostileMilitaryActions;
		bool disableRandomEvents;
		bool disableForcedPausingDuringEvents;
		int minDaysTillMilitaryAction;
		int maxDaysTillMilitaryAction;
		IntRange minMaxDaysTillMilitaryAction;


		public override void DoSettingsWindowContents(Rect inRect)
		{
			silverPerResource = settings.silverPerResource.ToString();
			timeBetweenTaxes = (settings.timeBetweenTaxes / 60000).ToString();
			productionTitheMod = settings.productionTitheMod.ToString();
			workerCost = settings.workerCost.ToString();
			settlementMaxLevel = settings.settlementMaxLevel.ToString();
			daysBetweenTaxes = (settings.timeBetweenTaxes / 60000);
			medievalTechOnly = (settings.medievalTechOnly);
			disableHostileMilitaryActions = settings.disableHostileMilitaryActions;
			minDaysTillMilitaryAction = settings.minDaysTillMilitaryAction;
			maxDaysTillMilitaryAction = settings.maxDaysTillMilitaryAction;
			minMaxDaysTillMilitaryAction = new IntRange(minDaysTillMilitaryAction, maxDaysTillMilitaryAction);

			settings.minMaxDaysTillMilitaryAction = new IntRange(minDaysTillMilitaryAction,maxDaysTillMilitaryAction);

			Listing_Standard listingStandard = new Listing_Standard();
			listingStandard.Begin(inRect);
			listingStandard.Label("Silver amount gained per resource");
			listingStandard.IntEntry(ref settings.silverPerResource, ref silverPerResource);
			listingStandard.Label("Days between tax time");
			listingStandard.IntEntry(ref daysBetweenTaxes, ref timeBetweenTaxes, 1);
			settings.timeBetweenTaxes = daysBetweenTaxes * 60000;
			listingStandard.Label("Production Tithe Modifier");
			listingStandard.IntEntry(ref settings.productionTitheMod, ref productionTitheMod);
			listingStandard.Label("Cost Per Worker");
			listingStandard.IntEntry(ref settings.workerCost, ref workerCost);
			listingStandard.Label("Max Settlement Level");
			listingStandard.IntEntry(ref settings.settlementMaxLevel, ref settlementMaxLevel);
			listingStandard.CheckboxLabeled("MedievalTechOnly".Translate(), ref settings.medievalTechOnly);
			listingStandard.CheckboxLabeled("Disable Hostile Military Actions", ref settings.disableHostileMilitaryActions);
			listingStandard.CheckboxLabeled("Disable Random Events", ref settings.disableRandomEvents);
			listingStandard.CheckboxLabeled("Disable Forced Pausing During Events", ref settings.disableForcedPausingDuringEvents);
			listingStandard.Label("Min/Max Days Until Military Action (ex. Settlements being attacked)");
			listingStandard.IntRange(ref minMaxDaysTillMilitaryAction, 1, 20);
			settings.minDaysTillMilitaryAction = minMaxDaysTillMilitaryAction.min;
			settings.maxDaysTillMilitaryAction = minMaxDaysTillMilitaryAction.max;

			if(listingStandard.ButtonText("Reset Settings"))
			{
				FactionColonies blank = new FactionColonies();
				settings.silverPerResource = blank.silverPerResource;
				settings.timeBetweenTaxes = blank.timeBetweenTaxes;
				settings.productionTitheMod = blank.productionTitheMod;
				settings.workerCost = blank.workerCost;
				settings.medievalTechOnly = blank.medievalTechOnly;
				settings.settlementMaxLevel = blank.settlementMaxLevel;
				settings.minMaxDaysTillMilitaryAction = blank.minMaxDaysTillMilitaryAction;
				settings.disableRandomEvents = blank.disableRandomEvents;
				settings.disableForcedPausingDuringEvents = blank.disableForcedPausingDuringEvents;
			}

			listingStandard.End();
			base.DoSettingsWindowContents(inRect);
		}

		public override string SettingsCategory()
		{
			return "Empire";
		}

		public override void WriteSettings()
		{
			LoadedModManager.GetMod<FactionColoniesMod>().GetSettings<FactionColonies>().timeBetweenTaxes = daysBetweenTaxes * 60000;
			LoadedModManager.GetMod<FactionColoniesMod>().GetSettings<FactionColonies>().minDaysTillMilitaryAction = LoadedModManager.GetMod<FactionColoniesMod>().GetSettings<FactionColonies>().minMaxDaysTillMilitaryAction.min;
			LoadedModManager.GetMod<FactionColoniesMod>().GetSettings<FactionColonies>().maxDaysTillMilitaryAction = LoadedModManager.GetMod<FactionColoniesMod>().GetSettings<FactionColonies>().minMaxDaysTillMilitaryAction.max;
			base.WriteSettings();

		}

		
	}



}

