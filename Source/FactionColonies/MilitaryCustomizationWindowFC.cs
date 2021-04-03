﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI.Group;
using RimWorld.Planet;
using HarmonyLib;
using RimWorld.QuestGen;

namespace FactionColonies
{

	public class MilitaryFireSupport : IExposable, ILoadReferenceable
	{
		public int loadID = -1;
		public string name;
		public float totalCost;
		public int timeRunning;
		public int ticksTillEnd;
		public float accuracy = 15;
		public string fireSupportType;
		public Map map;
		public IntVec3 location;
		public int startupTime;
		public IntVec3 sourceLocation;
		public List<ThingDef> projectiles;

		public MilitaryFireSupport()
		{

		}

		public MilitaryFireSupport(string fireSupportType, Map map, IntVec3 location, int ticksTillEnd, int startupTime, float accuracy, List<ThingDef> projectiles = null)
		{
			this.fireSupportType = fireSupportType;
			this.ticksTillEnd = Find.TickManager.TicksGame + ticksTillEnd + startupTime;
			this.accuracy = accuracy;
			this.map = map;
			this.location = location;
			this.startupTime = startupTime;
			this.sourceLocation = CellFinder.RandomEdgeCell(map);
			this.projectiles = projectiles;
		}

		public string GetUniqueLoadID()
		{
			return "MilitaryFireSupport_" + this.loadID;
		}

		public void setLoadID()
		{
			this.loadID = Find.World.GetComponent<FactionFC>().GetNextMilitaryFireSupportID();
		}

		public float returnAccuracyCostPercentage()
		{
			return (float)Math.Round((Math.Max(0, 15 - accuracy) / 15) * 100);
		}

		public float returnTotalCost()
		{
			float cost = 0;
			foreach (ThingDef def in projectiles)
			{
				cost += def.BaseMarketValue * 1.5f * (1+returnAccuracyCostPercentage()/100);
			}
			this.totalCost = (float)Math.Round(cost);
			return this.totalCost;
		}

		public void delete()
		{
			Find.World.GetComponent<FactionFC>().militaryCustomizationUtil.fireSupportDefs.Remove(this);
		}

		public ThingDef expendProjectile()
		{
			if (projectiles.Count() > 0)
			{
				ThingDef projectile = projectiles[0];
				projectiles.RemoveAt(0);
				return projectile;
			}
			else
			{
				return null;
			}
		}

		public List<ThingDef> returnFireSupportOptions()
		{
			// return list of thingdefs that can be used as fire support
			ThingSetMaker thingSetMaker = new ThingSetMaker_Count();
			ThingSetMakerParams param = new ThingSetMakerParams();
			param.filter = new ThingFilter();
			param.techLevel = Find.World.GetComponent<FactionFC>().techLevel;

			param.filter.SetAllow(DefDatabase<ThingCategoryDef>.GetNamedSilentFail("MortarShells"), true);
			if (DefDatabase<ThingCategoryDef>.GetNamedSilentFail("AmmoShells") != null)
				param.filter.SetAllow(DefDatabase<ThingCategoryDef>.GetNamedSilentFail("AmmoShells"), true);
			List<ThingDef> list = thingSetMaker.AllGeneratableThingsDebug(param).ToList();
			return list;
		}





		public void ExposeData()
		{
			Scribe_Values.Look<int>(ref loadID, "loadID");
			Scribe_Values.Look<string>(ref name, "name");
			Scribe_Values.Look<int>(ref timeRunning, "timeRunning");
			Scribe_Values.Look<int>(ref ticksTillEnd, "ticksTillEnd");
			Scribe_Values.Look<float>(ref accuracy, "accuracy");
			Scribe_Values.Look<string>(ref fireSupportType, "fireSupportType");
			Scribe_References.Look<Map>(ref map, "map");
			Scribe_Values.Look<IntVec3>(ref location, "location");
			Scribe_Values.Look<IntVec3>(ref location, "sourceLocation");
			Scribe_Values.Look<int>(ref startupTime, "startupTime");
			Scribe_Collections.Look<ThingDef>(ref projectiles, "projectiles", LookMode.Def);
		}


	}


	//Mil customization class
	public class MilitaryCustomizationUtil : IExposable
	{
		public List<MilUnitFC> units = new List<MilUnitFC>();
		public List<MilSquadFC> squads = new List<MilSquadFC>();
		public List<MercenarySquadFC> mercenarySquads = new List<MercenarySquadFC>();
		public List<MilitaryFireSupport> fireSupport = new List<MilitaryFireSupport>();
		public List<MilitaryFireSupport> fireSupportDefs = new List<MilitaryFireSupport>();
		public MilUnitFC blankUnit = null;
		public List<Mercenary> deadPawns = new List<Mercenary>();
		public int tickChanged = 0;


		public MilitaryCustomizationUtil()
		{
			//set load stuff here
			if(units == null)
			{
				units = new List<MilUnitFC>();
			}

			if(squads == null)
			{
				squads = new List<MilSquadFC>();
			}

			if(blankUnit == null)
			{
				//blankUnit = new MilUnitFC(true);
			}

			if (mercenarySquads == null)
			{
				mercenarySquads = new List<MercenarySquadFC>();
			}

			if (deadPawns == null)
			{
				deadPawns = new List<Mercenary>();
			}

			if (fireSupportDefs == null)
			{
				fireSupportDefs = new List<MilitaryFireSupport>();
			}
		}

		public void checkMilitaryUtilForErrors()
		{
			if (blankUnit == null)
				blankUnit = new MilUnitFC(true);
			//Log.Message("checking for errors" + Find.TickManager.TicksGame);
			foreach (MilSquadFC squad in squads)
			{
				bool changed = false;
				for (int count = 0; count < 30; count++)
				{
					if (squad.units[count] == null || (units.Contains(squad.units[count]) == false && squad.units[count] != blankUnit))
					{
						squad.units[count] = blankUnit;
						changed = true;
					}
				}

				if (changed)
				{
					foreach (MercenarySquadFC squadMerc in mercenarySquads)
					{
						if (squadMerc.outfit != null && squadMerc.outfit == squad)
						{
							squadMerc.OutfitSquad(squad);
						}
					}
				}
			}
			//Log.Message("1");

			foreach (MercenarySquadFC squad in mercenarySquads)
			{
				if (squad.outfit == null || squads.Contains(squad.outfit) == false)
				{
					squad.StripSquad();
					squad.outfit = null;
				} else
				{
					int settlementMilLevel = 0;
					if (squad.settlement != null)
						settlementMilLevel = squad.settlement.settlementMilitaryLevel;
					if (squad.outfit != null && squad.outfit.equipmentTotalCost > FactionColonies.calculateMilitaryLevelPoints(settlementMilLevel))
					{
						if (squad.settlement != null)
							Messages.Message("The max allowed equipment cost for the squad assigned to " + squad.settlement.name + " has been exceeded. Thus, the settlement's squad has been unassigned.", MessageTypeDefOf.RejectInput);
						squad.outfit = null;
						squad.StripSquad();
					}
				}

			}
			//Log.Message("2");

			if (tickChanged < getLatestChange)
			{
				//Log.Message("tick changed" + Find.TickManager.TicksGame);
				foreach (MercenarySquadFC merc in mercenarySquads)
				{
					if (merc.outfit != null)
					{
						merc.OutfitSquad(merc.outfit);
					} 
				}
			}

		}

		public int getLatestChange
		{
			get
			{
				int latest = 0;
				foreach (MilSquadFC squadFC in squads)
				{
					latest = Math.Max(latest, squadFC.getLatestChanged);
				}
				return latest;
			}
		}

		public MercenarySquadFC returnSquadFromUnit(Pawn unit)
		{
			foreach (MercenarySquadFC squad in mercenarySquads)
			{
				if (squad.AllDeployedMercenaryPawns.Contains(unit))
				{
					return squad;
				}
			}
			Log.Message("Empire - MercenarySquadFC - returnSquadFromUnit - Did not find squad.");
			return null;
		}

		public Mercenary returnMercenaryFromUnit(Pawn unit, MercenarySquadFC squad)
		{
			foreach (Mercenary merc in squad.mercenaries)
			{
				if (merc.pawn == unit)
				{
					return merc;
				}
			}
			return null;
        }

        public IEnumerable<Mercenary> AllMercenaries => mercenarySquads.SelectMany(squad => squad.mercenaries.Concat(squad.animals));

        public IEnumerable<MercenarySquadFC> DeployedSquads => this.mercenarySquads.FindAll(squad => squad.isDeployed);

        public IEnumerable<Pawn> AllMercenaryPawns => this.AllMercenaries.Select(mercenary => mercenary.pawn);

        public void resetSquads()
		{
			squads = new List<MilSquadFC>();
		}

		public void updateUnits()
		{
			foreach (MilUnitFC unit in units)
			{
				unit.updateEquipmentTotalCost();
			}
		}


		public void attemptToAssignSquad(SettlementFC settlement, MilSquadFC squad)
		{
			if (FactionColonies.calculateMilitaryLevelPoints(settlement.settlementMilitaryLevel) >= squad.equipmentTotalCost)
			{
				if (squadExists(settlement))
				{
					//Log.Message("Existing Squad");
					settlement.militarySquad.OutfitSquad(squad);
				}
				else
				{
					//Log.Message("Creating new Squad");
					//create new squad
					createMercenarySquad(settlement);
					settlement.militarySquad.OutfitSquad(squad);
				}

				Messages.Message(squad.name + "'s loadout has been assigned to " + settlement.name, MessageTypeDefOf.TaskCompletion);
			} else
			{
				Messages.Message("That squad exceeds the settlement's max allotted cost", MessageTypeDefOf.RejectInput);
			}
		}

		public MercenarySquadFC createMercenarySquad(SettlementFC settlement, bool isExtra = false)
		{
			MercenarySquadFC squad = new MercenarySquadFC();
			squad.initiateSquad();
			mercenarySquads.Add(squad);
			if (!isExtra)
				settlement.militarySquad = findSquad(squad);
			squad.settlement = settlement;
			squad.isExtraSquad = isExtra;

			//Log.Message(settlement.militarySquad.mercenaries.Count().ToString());
			if (settlement.militarySquad == null)
			{
				Log.Message("Empire - createMercenarySquad fail. Found squad is Null");
			}
			return findSquad(squad);
		}

		public MercenarySquadFC findSquad(MercenarySquadFC squad)
		{
			foreach(MercenarySquadFC mercSquad in mercenarySquads)
			{
				if (squad == mercSquad)
				{
					return mercSquad;
				}
			}
			return null;
		}

		public bool squadExists(SettlementFC settlement)
		{
			if (settlement.militarySquad != null)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		public void changeTick()
		{
			tickChanged = Find.TickManager.TicksGame;
		}

		public void ExposeData()
		{
			Scribe_Collections.Look<MilUnitFC>(ref units, "units", LookMode.Deep);
			Scribe_Collections.Look<MilSquadFC>(ref squads, "squads", LookMode.Deep);
			Scribe_Collections.Look<MercenarySquadFC>(ref mercenarySquads, "mercenarySquads", LookMode.Deep);
			Scribe_Collections.Look<MilitaryFireSupport>(ref fireSupport, "fireSupport", LookMode.Deep);
			Scribe_Collections.Look<MilitaryFireSupport>(ref fireSupportDefs, "fireSupportDefs", LookMode.Deep);
			Scribe_Collections.Look<Mercenary>(ref deadPawns, "deadPawns", LookMode.Deep);

			Scribe_Deep.Look<MilUnitFC>(ref blankUnit, "blankUnit");
			Scribe_Values.Look<int>(ref tickChanged, "tickChanged", 0);
		}
	}





	//Unit Class
	public class MilUnitFC : IExposable, ILoadReferenceable
	{
		public int loadID;
		public string name;
		public Pawn defaultPawn = null;
		public bool isBlank;
		public double equipmentTotalCost;
		public bool isTrader = false;
		public bool isCivilian = false;
		public int tickChanged = -1;
		public PawnKindDef animal;
		public PawnKindDef pawnKind;

		public MilUnitFC()
		{

		}

		public MilUnitFC(bool blank)
		{
			this.loadID = Find.World.GetComponent<FactionFC>().GetNextUnitID();
			this.isBlank = blank;
			this.equipmentTotalCost = 0;
			this.pawnKind = PawnKindDefOf.Colonist;
			generateDefaultPawn();
		}

		public string GetUniqueLoadID()
		{
			return "MilUnitFC_" + this.loadID;
		}
		public void ExposeData()
		{
			Scribe_Values.Look<int>(ref loadID, "loadID");
			Scribe_Values.Look<string>(ref name, "name");
			Scribe_Deep.Look<Pawn>(ref defaultPawn, "defaultPawn");
			Scribe_Values.Look<bool>(ref isBlank, "blank");
			Scribe_Values.Look<double>(ref equipmentTotalCost, "equipmentTotalCost", -1);
			Scribe_Values.Look<bool>(ref isTrader, "isTrader", false);
			Scribe_Values.Look<bool>(ref isCivilian, "isCivilian", false);
			Scribe_Values.Look<int>(ref tickChanged, "tickChanged", 0);
			Scribe_Defs.Look<PawnKindDef>(ref pawnKind, "PawnKind");
			Scribe_Defs.Look<PawnKindDef>(ref animal, "animal");
		}

		public void generateDefaultPawn()
		{
			List<Apparel> apparel = new List<Apparel>();
			List<ThingWithComps> equipment = new List<ThingWithComps>();
			PawnKindDef kindDef = pawnKind;

			if (defaultPawn != null) 
			{
				apparel.AddRange(defaultPawn.apparel.WornApparel);
				equipment.AddRange(defaultPawn.equipment.AllEquipmentListForReading);
			
				Reset:
				foreach (Apparel cloth in defaultPawn.apparel.WornApparel)
				{
					defaultPawn.apparel.Remove(cloth);
					goto Reset;
				}
				foreach (ThingWithComps weapon in defaultPawn.equipment.AllEquipmentListForReading)
				{
					defaultPawn.equipment.Remove(weapon);
					goto Reset;
				}
				defaultPawn.Destroy();
			}
			defaultPawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kindDef, FactionColonies.getPlayerColonyFaction(), PawnGenerationContext.NonPlayer, -1, false, false, false, false, true, true, 0, false, false, false, false, false, false, false, false, 0, null, 0, null, null, null, null, null, null, null, null, null, null, null, null));
			defaultPawn.health.forceIncap = true;
			defaultPawn.mindState.canFleeIndividual = false;
			defaultPawn.apparel.DestroyAll();

			foreach (Apparel clothes in apparel)
			{
				//Log.Message(clothes.Label);
				defaultPawn.apparel.Wear(clothes);
			}
			foreach (ThingWithComps weapon in equipment)
			{
				//Log.Message(weapon.Label);
				equipWeapon(weapon);
			}
		}

		public void changeTick()
		{
			tickChanged = Find.TickManager.TicksGame;
		}

		public void equipWeapon(ThingWithComps weapon)
		{
			changeTick();
			if (isCivilian == false)
			{
				unequipWeapon();
				defaultPawn.equipment.AddEquipment(weapon);

			}
			else
			{
				Messages.Message("You cannot put a weapon on a civilian!", MessageTypeDefOf.RejectInput);
			}
		}

		public void unequipWeapon()
		{
			changeTick();
			defaultPawn.equipment.DestroyAllEquipment();
		}

		public void wearEquipment(Apparel Equipment, bool wear)
		{
			changeTick();
			Reset:
			foreach (ApparelLayerDef layer in Equipment.def.apparel.layers)
			{
				foreach (BodyPartGroupDef part in Equipment.def.apparel.bodyPartGroups)
				{
					foreach (Apparel apparel in defaultPawn.apparel.WornApparel)
					{
						if ((apparel.def.apparel.layers.Contains(layer) == true && apparel.def.apparel.bodyPartGroups.Contains(part)) || (Equipment.def.apparel.layers.Contains(ApparelLayerDefOf.Overhead) && apparel.def.apparel.layers.Contains(ApparelLayerDefOf.Overhead)))
						{
							defaultPawn.apparel.Remove(apparel);
							goto Reset;
						}
					}
				}
			}
			if (wear == false)
			{
				//NOTHING
			}
			else
			{
				defaultPawn.apparel.Wear(Equipment);
			}
		}

		public void removeUnit()
		{
			Find.World.GetComponent<FactionFC>().militaryCustomizationUtil.units.Remove(this);
		}

		public void unequipAllEquipment()
		{
			changeTick();
			defaultPawn.apparel.DestroyAll();
			defaultPawn.equipment.DestroyAllEquipment();
		}

		public double getTotalCost
		{
			get
			{
				updateEquipmentTotalCost();
				return equipmentTotalCost;
			}
		}

		public void updateEquipmentTotalCost()
		{
			if (isBlank == true) 
			{ 
				equipmentTotalCost = 0; 
			}
			else
			{
				double totalCost = 0;

				totalCost += Math.Floor(defaultPawn.def.BaseMarketValue * FactionColonies.militaryRaceCostMultiplier);
				foreach (Thing thing in defaultPawn.apparel.WornApparel)
				{
					//Log.Message(thing.Label);
					totalCost += thing.MarketValue;
				}

				foreach (Thing thing in defaultPawn.equipment.AllEquipmentListForReading)
				{
					totalCost += thing.MarketValue;
				}

				if (animal != null)
				{
					totalCost += Math.Floor(animal.race.BaseMarketValue * FactionColonies.militaryAnimalCostMultiplier);
				}

				equipmentTotalCost = Math.Ceiling(totalCost);
			}
		}

		public void setTrader(bool state)
		{
			changeTick();
			isTrader = state;
			if (state)
			{
				setCivilian(true);
			}
		}

		public void setCivilian(bool state)
		{
			changeTick();
			isCivilian = state;
			if (state)
			{
				unequipWeapon();
			}
			else
			{
				setTrader(false);
			}

		}
	}

	public class MilitaryOrders
	{
		public static int Standby = 1;
		public static int Attack = 2;
		public static int MoveTo = 3;
		public static int RecoverWounded = 4;
		public static int Leave = 5;

	}

	public class MercenarySquadFC : IExposable, ILoadReferenceable
	{
		public int loadID = -1;
		public string name;
		public List<Mercenary> mercenaries = new List<Mercenary>();
		public List<Mercenary> animals = new List<Mercenary>();
		public SettlementFC settlement;
		public bool isTraderCaravan;
		public bool isDeployed;
		public bool isExtraSquad;
		public int order;
		public int timeDeployed;
		public IntVec3 orderLocation;
		public bool hitMap;
		public bool hasDead;
		public MilSquadFC outfit;
		public List<ThingWithComps> UsedWeaponList;
		public List<Apparel> UsedApparelList;
		public int tickChanged = 0;
		public bool hasLord = false;
		public Map map;
        public Quest quest;


		public void ExposeData()
		{
			Scribe_Values.Look<int>(ref loadID, "loadID", -1);
			Scribe_Values.Look<string>(ref name, "name");
			Scribe_Collections.Look<Mercenary>(ref mercenaries, "mercenaries", LookMode.Deep);
			Scribe_Collections.Look<Mercenary>(ref animals, "animals", LookMode.Deep);
			Scribe_Values.Look<bool>(ref isTraderCaravan, "isTraderCaravan", false);
			Scribe_Values.Look<bool>(ref isDeployed, "isDeployed", false);
			Scribe_Values.Look<bool>(ref isExtraSquad, "isExtraSquad", false);
			Scribe_Values.Look<bool>(ref hitMap, "hitMap", false);
			Scribe_References.Look<MilSquadFC>(ref outfit, "outfit", false);
			Scribe_Values.Look<bool>(ref hasDead, "hasDead", false);
			Scribe_Collections.Look<ThingWithComps>(ref UsedWeaponList, "UsedWeaponList", LookMode.Reference);
			Scribe_Collections.Look<Apparel>(ref UsedApparelList, "UsedApparelList", LookMode.Reference);
			Scribe_References.Look<SettlementFC>(ref settlement, "Settlement");
			Scribe_Values.Look<int>(ref tickChanged, "tickChanged");
			Scribe_Values.Look<int>(ref order, "order", -1);
			Scribe_Values.Look<int>(ref timeDeployed, "timeDeployed", -1);
			Scribe_Values.Look<IntVec3>(ref orderLocation, "orderLocation");
			Scribe_Values.Look<bool>(ref hasLord, "hasLord", false);
			Scribe_References.Look<Map>(ref map, "map");
			Scribe_References.Look<Quest>(ref quest, "quest");
        }

        public string GetUniqueLoadID()
		{
			return "MercenarySquadFC_" + this.loadID;
		}

		public MercenarySquadFC()
		{

		}

        public IEnumerable<Mercenary> EquippedMercenaries => mercenaries.FindAll(merc => (merc.pawn.apparel.WornApparel.Any() || merc.pawn.equipment.AllEquipmentListForReading.Any() || merc.animal != null) && merc.deployable == true);
        public IEnumerable<Pawn> EquippedMercenaryPawns => EquippedMercenaries.Select(merc => merc.pawn);

        public IEnumerable<Mercenary> EquippedAnimalMercenaries => animals;
        public IEnumerable<Pawn> EquippedAnimalMercenaryPawns => EquippedAnimalMercenaries.Select(animal => animal.pawn);

        public IEnumerable<Mercenary> AllEquippedMercenaries => EquippedMercenaries.Concat(EquippedAnimalMercenaries);
        public IEnumerable<Pawn> AllEquippedMercenaryPawns => AllEquippedMercenaries.Select(merc => merc.pawn);

        public IEnumerable<Mercenary> DeployedMercenaries => mercenaries.FindAll(merc => merc.pawn.Map != null);
        public IEnumerable<Pawn> DeployedMercenaryPawns => DeployedMercenaries.Select(merc => merc.pawn);

        public IEnumerable<Mercenary> DeployedAnimalMercenaries => animals.FindAll(animal => animal.pawn.Map != null);
        public IEnumerable<Pawn> DeployedAnimalMercenaryPawns => DeployedAnimalMercenaries.Select(animal => animal.pawn);

        public IEnumerable<Mercenary> AllDeployedMercenaries => DeployedMercenaries.Concat(DeployedAnimalMercenaries);
        public IEnumerable<Pawn> AllDeployedMercenaryPawns => AllDeployedMercenaries.Select(merc => merc.pawn);


        public SettlementFC getSettlement
		{
			get
			{
				if (settlement != null)
				{
					return settlement;
				} else
				{
					foreach (SettlementFC settlement in Find.World.GetComponent<FactionFC>().settlements)
					{
						if (settlement.militarySquad != null && settlement.militarySquad == this)
						{
							this.settlement = settlement;
							return settlement;
						}
					}

					return null;
				}
			}
		}

		public void changeTick()
		{
			tickChanged = Find.TickManager.TicksGame;
		}

		public void initiateSquad()
		{
			mercenaries = new List<Mercenary>();
			UsedApparelList = new List<Apparel>();
			UsedWeaponList = new List<ThingWithComps>();

			if (outfit == null) 
			{
				for (int k = 0; k < 30; k++)
				{
					Mercenary pawn = new Mercenary(true);
					createNewPawn(ref pawn, null);
					mercenaries.Add(pawn);
				}
			}
			else
			{
				for (int k = 0; k < 30; k++)
				{
					Mercenary pawn = new Mercenary(true);
					createNewPawn(ref pawn, outfit.units[k].pawnKind);
					mercenaries.Add(pawn);
				}
			}
			//Log.Message("count : " + mercenaries.Count().ToString());
			//this.debugMercenarySquad();
			if (loadID == -1)
			{
				this.loadID = Find.World.GetComponent<FactionFC>().GetNextMercenarySquadID();
			}

			if (outfit != null)
			{
				OutfitSquad(outfit);
			}

		}

		public void resetNeeds()
		{
			foreach (Pawn merc in AllEquippedMercenaryPawns)
			{
				if (merc.health == null)
					merc.health = new Pawn_HealthTracker(merc);
				HealthUtility.HealNonPermanentInjuriesAndRestoreLegs(merc);
				if (merc.needs == null)
					merc.needs = new Pawn_NeedsTracker(merc);
				if (merc.needs.food == null)
					merc.needs.food = new Need_Food(merc);
				if (merc.needs.rest == null)
					merc.needs.rest = new Need_Rest(merc);
				if (!merc.AnimalOrWildMan() && merc.needs.joy == null)
					merc.needs.joy = new Need_Joy(merc);
				merc.needs.food.CurLevel = merc.needs.food.MaxLevel;
				merc.needs.rest.CurLevel = merc.needs.rest.MaxLevel;
				if (!merc.AnimalOrWildMan())
				{
					merc.needs.joy.CurLevel = merc.needs.joy.MaxLevel;
					merc.needs.mood.thoughts.memories.TryGainMemory(DefDatabase<ThoughtDef>.GetNamed("FC_Mercenary"));
				}
				
			}
		}

		public void removeDroppedEquipment()
		{
			while (DroppedApparel.Any())
			{
				Apparel apparel = DroppedApparel[0];
				UsedApparelList.Remove(DroppedApparel[0]);
				if (apparel != null && apparel.Destroyed == false)
				{
					apparel.Destroy();
				}
			}

			while (DroppedWeapons.Any())
			{
				ThingWithComps weapon = DroppedWeapons[0];
				UsedWeaponList.Remove(DroppedWeapons[0]);
				if (weapon != null && weapon.Destroyed == false)
				{
					weapon.Destroy();
				}

			}
		}

		public void createNewAnimal(ref Mercenary merc, PawnKindDef race)
		{
			Pawn newPawn;
			newPawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(race, FactionColonies.getPlayerColonyFaction(), PawnGenerationContext.NonPlayer, -1, false, false, false, false, false, true, 0, false, false, false, false, false, false, false, false, 0, null, 0, null, null, null, null, null, null, null, null, null, null, null, null));
			//merc = (Mercenary)newPawn;
			
			merc.squad = this;
			merc.settlement = settlement;
			//Log.Message(newPawn.Name + "   State: Dead - " + newPawn.health.Dead + "    Apparel Count: " + newPawn.apparel.WornApparel.Count());
			merc.pawn = newPawn;
			
		}

		public void createNewPawn(ref Mercenary merc, PawnKindDef race)
		{
			if (merc.pawn != null)
			{
				//pawn.ParentHolder.remov
				if (merc.pawn.health != null && merc.pawn.health.Dead == true)
				{
					//Log.Message("Passing old pawn to dead mercenaries");
					//PassPawnToDeadMercenaries(pawn);
				}
				else
				{

				}
			}

			PawnKindDef raceChoice;
			

			if(race == null)
			{
				raceChoice = PawnKindDefOf.Colonist;
			} else
			{
				raceChoice = race;
			}


			Pawn newPawn;
			newPawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(raceChoice, FactionColonies.getPlayerColonyFaction(), PawnGenerationContext.NonPlayer, -1, false, false, false, false, false, true, 0, false, false, false, false, false, false, false, false, 0, null, 0, null, null, null, null, null, null, null, null, null, null, null, null));
			newPawn.apparel.DestroyAll();
			newPawn.equipment.DestroyAllEquipment();
			//merc = (Mercenary)newPawn;
			merc.squad = this;
			merc.settlement = settlement;
			//Log.Message(newPawn.Name + "   State: Dead - " + newPawn.health.Dead + "    Apparel Count: " + newPawn.apparel.WornApparel.Count());
			merc.pawn = newPawn;
            Find.WorldPawns.PassToWorld(newPawn);
		}

		public void updateSquadStats(int level)
		{
			foreach (Mercenary merc in mercenaries)
			{
				merc.pawn.skills.GetSkill(SkillDefOf.Shooting).Level = Math.Min(level * 2, 20);
				merc.pawn.skills.GetSkill(SkillDefOf.Melee).Level = Math.Min(level * 2, 20);
				merc.pawn.skills.GetSkill(SkillDefOf.Medicine).Level = Math.Min(level * 1, 20);
			}
		}

		public void PassPawnToDeadMercenaries(Mercenary merc)
		{
			//If ever add past dead pawns, use this code
			MilitaryCustomizationUtil util = Find.World.GetComponent<FactionFC>().militaryCustomizationUtil;
			Mercenary pwn = new Mercenary(true);
			if (merc.animal != null)
			{
				Mercenary animal = new Mercenary(true);
				animal = merc.animal;
				//util.deadPawns.Add(animal);
			}
			pwn = merc;

			//util.deadPawns.Add(pwn);
			Mercenary pawn2 = new Mercenary(true);
			createNewPawn(ref pawn2, merc.pawn.kindDef);
			mercenaries.Replace(merc, pawn2);



		}

		public void HealPawn(Mercenary merc)
		{
			merc.pawn.health.Reset();
		}

		public void StripSquad()
		{
			for(int count = 0; count < 30; count++)
			{
				StripPawn(mercenaries[count]);
			}
		}

		public void OutfitSquad(MilSquadFC outfit)
		{
			FactionFC faction = Find.World.GetComponent<FactionFC>();
			int count = 0;
			this.outfit = outfit;
			UsedWeaponList = new List<ThingWithComps>();
			UsedApparelList = new List<Apparel>();
			animals = new List<Mercenary>();
			foreach (MilUnitFC loadout in outfit.units)
			{ 
				if (mercenaries[count].pawn == null || mercenaries[count].pawn.kindDef != loadout.pawnKind || mercenaries[count].pawn.Dead == true)
				{
					Mercenary pawn = new Mercenary(true);
					createNewPawn(ref pawn, loadout.pawnKind);
					mercenaries.Replace(mercenaries[count], pawn);
				}
				StripPawn(mercenaries[count]);
				HealPawn(mercenaries[count]);
				if (loadout != null)
				{
					//mercenaries[count];
					//StripPawn(mercenaries[count]);
					EquipPawn(mercenaries[count], loadout);
					if (loadout.animal != null)
					{
						Mercenary animal = new Mercenary(true);
						createNewAnimal(ref animal, loadout.animal);
						animal.handler = mercenaries[count];
						mercenaries[count].animal = animal;
						animals.Add(animal);
					}
					mercenaries[count].loadout = loadout;
					if (mercenaries[count].loadout != faction.militaryCustomizationUtil.blankUnit)
						mercenaries[count].deployable = true;
					else
						mercenaries[count].deployable = false;
				} else
				{
				}

				if (mercenaries[count].pawn.equipment.AllEquipmentListForReading != null)
				{
					UsedWeaponList.AddRange(mercenaries[count].pawn.equipment.AllEquipmentListForReading);

					//add single check at start of load and mark variable
				}

				if (mercenaries[count].pawn.apparel.WornApparel != null)
				{
					UsedApparelList.AddRange(mercenaries[count].pawn.apparel.WornApparel);
				}

				count++;
			}

			//debugMercenarySquad();
		}



		public void StripPawn(Mercenary merc)
		{
			merc.pawn.apparel.DestroyAll();
			merc.pawn.equipment.DestroyAllEquipment();
		}

		public void EquipPawn(Mercenary merc, MilUnitFC loadout)
		{
			foreach (Apparel clothes in loadout.defaultPawn.apparel.WornApparel)
			{
				if (clothes.def.MadeFromStuff)
				{
					Thing thing = ThingMaker.MakeThing(clothes.def, clothes.Stuff);
					thing.SetColor(Color.white);
					merc.pawn.apparel.Wear(thing as Apparel);
				} else
				{
					Thing thing = ThingMaker.MakeThing(clothes.def, clothes.Stuff);
					thing.SetColor(Color.white);
					merc.pawn.apparel.Wear(thing as Apparel);
				}
			}
			foreach (ThingWithComps weapon in loadout.defaultPawn.equipment.AllEquipmentListForReading)
			{
				if (weapon.def.MadeFromStuff)
				{
					merc.pawn.equipment.AddEquipment(ThingMaker.MakeThing(weapon.def, weapon.Stuff) as ThingWithComps);
				} else
				{
					merc.pawn.equipment.AddEquipment(ThingMaker.MakeThing(weapon.def) as ThingWithComps);
				}

				if(FactionColonies.checkForMod("CETeam.CombatExtended"))
				{
					//Log.Message("mod detected");
					//CE is loaded
					foreach (ThingComp comp in merc.pawn.AllComps)
					{
						if (comp.GetType().ToString() == "CombatExtended.CompInventory")
						{
							Type typ = FactionColonies.returnUnknownTypeFromName("CombatExtended.LoadoutPropertiesExtension");

							//Method not static, so create instance of object and define the parameters to the method.
							var obj = Activator.CreateInstance(typ);
							object[] paramArgu = new object[] { merc.pawn.equipment.Primary, comp, 1 };

							Traverse.Create(obj).Method("TryGenerateAmmoFor", paramArgu).GetValue();
							Traverse.Create(obj).Method("LoadWeaponWithRandAmmo", new object[] { merc.pawn.equipment.Primary }).GetValue();
							

						}
					}

				}
			}

		}

		public List<ThingWithComps> DroppedWeapons 
		{
			get
			{
				List<ThingWithComps> tmpList = new List<ThingWithComps>();

				foreach (ThingWithComps weapon in UsedWeaponList)
				{
					if (weapon.ParentHolder is Pawn_EquipmentTracker)
					{
						if ((((Pawn_EquipmentTracker)weapon.ParentHolder).pawn.Faction == FactionColonies.getPlayerColonyFaction() || ((Pawn_EquipmentTracker)weapon.ParentHolder).pawn.Faction == Find.FactionManager.OfPlayer)  && ((Pawn_EquipmentTracker)weapon.ParentHolder).pawn.Dead == false)
						{

						} else
						{
							tmpList.Add(weapon);
						}
					}
					else
					{
						tmpList.Add(weapon);
					}

				}

				return tmpList;
			}
		}

		public List<Apparel> DroppedApparel
		{
			get
			{
				List<Apparel> tmpList = new List<Apparel>();

				foreach (Apparel apparel in UsedApparelList)
				{
					//Log.Message(apparel.ParentHolder.ToString());
					//Log.Message(apparel.ParentHolder.ParentHolder.ToString());
					if (apparel.ParentHolder is Pawn_ApparelTracker)
					{
						if ((((Pawn_ApparelTracker)apparel.ParentHolder).pawn.Faction == FactionColonies.getPlayerColonyFaction() || ((Pawn_ApparelTracker)apparel.ParentHolder).pawn.Faction == Find.FactionManager.OfPlayer) && ((Pawn_ApparelTracker)apparel.ParentHolder).pawn.Dead == false)
						{

						}
						else
						{
							tmpList.Add(apparel);
						}
					}
					else
					{
						tmpList.Add(apparel);
					}

				}

				return tmpList;
			}
		}



		public void debugMercenarySquad()
		{
			foreach (Mercenary merc in mercenaries)
			{
				Log.Message(merc.pawn.ToString());
				Log.Message(merc.pawn.health.Dead.ToString());
				Log.Message(merc.pawn.apparel.WornApparelCount.ToString());
				Log.Message(merc.pawn.equipment.AllEquipmentListForReading.Count().ToString());
				//Log.Message(pawn.Name + "   State: Dead - " + pawn.health.Dead + "    Apparel Count: " + pawn.apparel.WornApparel.Count());
			}
		}

		public Mercenary returnPawn(Pawn pawn)
		{
			foreach (Mercenary merc in mercenaries)
			{
				if (merc.pawn == pawn)
				{
					return merc;
				}
			}
			return null;
		}

        public void DeployTo(Map map, PawnsArrivalModeDef arrivalMode, IntVec3? location = null)
        {
            // TODO: move here
            FactionFC fc = Find.World.GetComponent<FactionFC>();

            /*SlateRef<MercenarySquadFC> squadRef = new SlateRef<MercenarySquadFC>("$squad");
            SlateRef<IEnumerable<Pawn>> pawnsRef = new SlateRef<IEnumerable<Pawn>>("$pawns");

            QuestScriptDef questScript = new QuestScriptDef
            {
                defaultHidden = true,
                autoAccept = true,
                isRootSpecial = true,
                defName = null,
            };

            QuestNode_Sequence seq = new QuestNode_Sequence();
            questScript.root = seq;

            QuestNode_DeploySquad deploySquad = new QuestNode_DeploySquad
            {
                squad = squadRef,
                arrivalMode = new SlateRef<PawnsArrivalModeDef>("$arrivalMode"),
                map = new SlateRef<Map>("$map"),
                location = new SlateRef<IntVec3?>("$location")
            };
            seq.nodes.Add(deploySquad);

            QuestNode_SetAllApparelLocked apparelLocked = new QuestNode_SetAllApparelLocked()
            {
                pawns = pawnsRef
            };
            seq.nodes.Add(apparelLocked);

            QuestNode_Signal signal = new QuestNode_Signal()
            {
                inSignal = "undeploy",
                node = new QuestNode_End()
            };
            seq.nodes.Add(signal);
            */
            QuestScriptDef questScript = DefDatabase<QuestScriptDef>.GetNamed("FC_SquadDeploy");          

            Slate slate = new Slate();

            slate.Set("pawns", this.AllEquippedMercenaryPawns);
            slate.Set("arrivalMode", arrivalMode);
            slate.Set("faction", FactionColonies.getPlayerColonyFaction());
            slate.Set("map", map);
            slate.Set("squad", this);
            slate.Set("location", location);

            Quest generatedQuest = QuestUtility.GenerateQuestAndMakeAvailable(questScript, slate);
            this.quest = generatedQuest;
        }

        public void Undeploy()
        {
            if (this.quest != null)
            {
                // Quest should do all the cleanup
                quest.End(QuestEndOutcome.Unknown);
                this.quest = null;
            }
        }
    }

	//Squad Class
	public class MilSquadFC : IExposable, ILoadReferenceable
	{
		public int loadID = -1;
		public string name;
		public List<MilUnitFC> units = new List<MilUnitFC>();
		public double equipmentTotalCost;
		public bool isTraderCaravan;
		public bool isCivilian;
		public int tickChanged = 0;

		public MilSquadFC()
		{
		}

		public MilSquadFC(bool newSquad)
		{
			if (newSquad)
			{
				setLoadID();
			}
		}

		public void ExposeData()
		{
			Scribe_Values.Look<int>(ref loadID, "loadID", -1);
			Scribe_Values.Look<string>(ref name, "name");
			Scribe_Collections.Look<MilUnitFC>(ref units, "units", LookMode.Reference);
			Scribe_Values.Look<double>(ref equipmentTotalCost, "equipmentTotalCost", -1);
			Scribe_Values.Look<bool>(ref isTraderCaravan, "isTraderCaravan", false);
			Scribe_Values.Look<bool>(ref isCivilian, "isCivilian", false);
			Scribe_Values.Look<int>(ref tickChanged, "tickChanged", 0);
		}

		public void setLoadID()
		{
			this.loadID = Find.World.GetComponent<FactionFC>().GetNextSquadID();
		}

		public int updateEquipmentTotalCost()
		{
			double totalCost = 0;
			foreach (MilUnitFC unit in units)
			{
				totalCost += unit.getTotalCost;
			}
			equipmentTotalCost = totalCost;
			return (int)equipmentTotalCost;
		}
		public void newSquad()
		{
			units = new List<MilUnitFC>();
			for (int sq = 0; sq < 30; sq++)
			{
				units.Add(Find.World.GetComponent<FactionFC>().militaryCustomizationUtil.blankUnit);
			}
			updateEquipmentTotalCost();
		}

		public void ChangeTick()
		{
			tickChanged = Find.TickManager.TicksGame;
		}

		public int getLatestChanged
		{
			get
			{
				int latestChange;
				latestChange = tickChanged;
				foreach (MilUnitFC unit in units)
				{
					latestChange = Math.Max(unit.tickChanged, latestChange);
				}

				return latestChange;
			}
		}

		public void deleteSquad()
		{
			Find.World.GetComponent<FactionFC>().militaryCustomizationUtil.squads.Remove(this);
			
		}

		public string GetUniqueLoadID()
		{
			return "MilSquadFC_" + this.loadID;
		}

		public void setTraderCaravan(bool state)
		{
			ChangeTick();
			isTraderCaravan = state;
			if (state)
			{
				bool hasTrader = false;
				int hasTraderCount = 0;
				foreach (MilUnitFC unit in units)
				{
					if (unit.isTrader)
					{
						hasTrader = true;
						hasTraderCount++;
					}
				}

				if (hasTrader == false)
				{
					Messages.Message("There must be a trader in the squad to be a trader caravan!", MessageTypeDefOf.RejectInput);
					setTraderCaravan(false);
					return;
				}
				if (hasTraderCount > 1)
				{
					Messages.Message("There cannot be more than one trader in the caravan!", MessageTypeDefOf.RejectInput);
					setTraderCaravan(false);
					return;
				}
				setCivilian(true);
			} else
			{
				setCivilian(false);
			}
		}

		public void setCivilian(bool state)
		{
			ChangeTick();
			isCivilian = state;
			if (state)
			{
			}
			else
			{
			}

		}
	}
	
	class militaryCustomizationWindowFC : Window
    {
		int tab = 1;
		string selectedText = "";
		MilUnitFC selectedUnit = null;
		MilSquadFC selectedSquad = null;
		MilitaryFireSupport selectedSupport = null;
		SettlementFC settlementPointReference = null;
		FactionFC faction;
		MilitaryCustomizationUtil util;
		public float scroll = 0;
		//public int maxScroll = 0;
		public float settlementMaxScroll = 0;
		public float fireSupportMaxScroll = 0;
		public int settlementHeight = 0;
		public int settlementYSpacing = 0;
		public int settlementWindowHeight = 500;


		public override Vector2 InitialSize
		{
			get
			{
				return new Vector2(838f, 600);
			}
		}


		public militaryCustomizationWindowFC()
		{
			this.forcePause = false;
			this.draggable = true;
			this.doCloseX = true;
			this.preventCameraMotion = false;
			this.tab = 1;
			this.selectedText = "";

			settlementHeight = 120;
			settlementYSpacing = 5;
			this.util = Find.World.GetComponent<FactionFC>().militaryCustomizationUtil;
			this.faction = Find.World.GetComponent<FactionFC>();

		

		}

		public override void PreOpen()
		{
			base.PreOpen();
			scroll = 0;
			settlementMaxScroll = (Find.World.GetComponent<FactionFC>().settlements.Count() * (settlementYSpacing + settlementHeight) - settlementWindowHeight);
		}

		public override void PostClose()
		{
			base.PostClose();
			util.checkMilitaryUtilForErrors();
		}

		public override void WindowUpdate()
		{
			base.WindowUpdate();
		}


		public override void DoWindowContents(Rect inRect)
		{


			switch (tab)
			{
				case 0:
					DrawTabDesignate(inRect);
					break;
				case 1:
					DrawTabAssign(inRect);
					break;
				case 2:
					DrawTabSquad(inRect);
					break;
				case 3:
					DrawTabUnit(inRect);
					break;
				case 4:
					DrawTabFireSupport(inRect);
					break;
			}

			DrawHeaderTabs(inRect);

			//Widgets.ThingIcon(new Rect(50, 50, 60, 60), util.defaultPawn);
		}

		public void DrawHeaderTabs(Rect inRect)
		{
			Rect milDesigination = new Rect(0, 0, 0, 35);
			Rect milSetSquad = new Rect(milDesigination.x + milDesigination.width, milDesigination.y, 187, milDesigination.height);
			Rect milCreateSquad = new Rect(milSetSquad.x + milSetSquad.width, milDesigination.y, 187, milDesigination.height);
			Rect milCreateUnit = new Rect(milCreateSquad.x + milCreateSquad.width, milDesigination.y, 187, milDesigination.height);
			Rect milCreateFiresupport = new Rect(milCreateUnit.x + milCreateUnit.width, milDesigination.y, 187, milDesigination.height);
			Rect helpButton = new Rect(760, 0, 30, 30);

			
			if (Widgets.ButtonImage(helpButton, TexLoad.questionmark))
			{
				string header = "Help! What is this for?";
				string description = "Need Help with this menu? Go to this youtube video: https://youtu.be/lvWb1rMMsq8";
				Find.WindowStack.Add(new DescWindowFc(description, header));
			}

			if (Widgets.ButtonTextSubtle(milDesigination, "Military Designations"))
			{
				tab = 0;
				util.checkMilitaryUtilForErrors();
			}
			if (Widgets.ButtonTextSubtle(milSetSquad, "Designate Squads"))
			{
				tab = 1;
				scroll = 0;
				util.checkMilitaryUtilForErrors();
			}
			if (Widgets.ButtonTextSubtle(milCreateSquad, "Create Squads"))
			{
				tab = 2;
				selectedText = "Select A Squad";
				if(selectedSquad != null)
				{
					selectedText = selectedSquad.name;
					selectedSquad.updateEquipmentTotalCost();
				}
				if (util.blankUnit == null)
				{
					util.blankUnit = new MilUnitFC(true);
				}
				util.checkMilitaryUtilForErrors();

			}
			if (Widgets.ButtonTextSubtle(milCreateUnit, "Create Units"))
			{
				tab = 3;
				selectedText = "Select A Unit";
				if (selectedUnit != null)
				{
					selectedText = selectedUnit.name;
				}
				util.checkMilitaryUtilForErrors();
			}
			if (Widgets.ButtonTextSubtle(milCreateFiresupport, "Create Fire Support"))
			{
				tab = 4;
				selectedText = "Select a fire support";
				
				util.checkMilitaryUtilForErrors();
			}
		}

		public void DrawTabDesignate(Rect inRect)
		{

		}

		public void DrawTabAssign(Rect inRect)
		{
			Rect SettlementBox = new Rect(5, 45, 535, settlementHeight);
			Rect SettlementName = new Rect(SettlementBox.x + 5, SettlementBox.y + 5, 250, 25);
			Rect MilitaryLevel = new Rect(SettlementName.x, SettlementName.y + 30, 250, 25);
			Rect AssignedSquad = new Rect(MilitaryLevel.x, MilitaryLevel.y + 30, 250, 25);
			Rect isBusy = new Rect(AssignedSquad.x, AssignedSquad.y + 30, 250, 25);

			Rect buttonSetSquad = new Rect(SettlementBox.x + SettlementBox.width - 265, SettlementBox.y + 5, 100, 25);
			Rect buttonViewSquad = new Rect(buttonSetSquad.x, buttonSetSquad.y + 3 + buttonSetSquad.height, buttonSetSquad.width, buttonSetSquad.height);
			Rect buttonDeploySquad = new Rect(buttonViewSquad.x, buttonViewSquad.y + 3 + buttonViewSquad.height, buttonSetSquad.width, buttonSetSquad.height);
			Rect buttonResetPawns = new Rect(buttonDeploySquad.x, buttonDeploySquad.y + 3 + buttonDeploySquad.height, buttonSetSquad.width, buttonSetSquad.height);
			Rect buttonOrderFireSupport = new Rect(buttonSetSquad.x + 125 + 5, SettlementBox.y + 5, 125, 25);




			//set text anchor and font
			GameFont fontBefore = Text.Font;
			TextAnchor anchorBefore = Text.Anchor;



			int count = 0;
			foreach (SettlementFC settlement in Find.World.GetComponent<FactionFC>().settlements)
			{
				if (true)//count * (settlementYSpacing+settlementHeight) + scroll >= 0 && count * (settlementYSpacing + settlementHeight) + scroll <= settlementWindowHeight)
				{
					Text.Font = GameFont.Small;

					Widgets.DrawMenuSection(new Rect(SettlementBox.x, SettlementBox.y + (SettlementBox.height + settlementYSpacing) * count + scroll, SettlementBox.width, SettlementBox.height));

					//click on settlement name
					if(Widgets.ButtonTextSubtle(new Rect(SettlementName.x, SettlementName.y + (SettlementBox.height + settlementYSpacing) * count + scroll, SettlementName.width, SettlementName.height), settlement.name))
					{
						Find.WindowStack.Add(new SettlementWindowFc(settlement));
					}
					Widgets.Label(new Rect(MilitaryLevel.x, MilitaryLevel.y + (SettlementBox.height + settlementYSpacing) * count + scroll, MilitaryLevel.width, MilitaryLevel.height*2), "Mil Level: " + settlement.settlementMilitaryLevel + " - Max Squad Cost: " + FactionColonies.calculateMilitaryLevelPoints(settlement.settlementMilitaryLevel));
					if(settlement.militarySquad != null)
					{
						if (settlement.militarySquad.outfit != null)
						{
							Widgets.Label(new Rect(AssignedSquad.x, AssignedSquad.y + (SettlementBox.height + settlementYSpacing) * count + scroll, AssignedSquad.width, AssignedSquad.height), "Assigned Squad: " + settlement.militarySquad.outfit.name);//settlement.militarySquad.name);
						} else
						{
							Widgets.Label(new Rect(AssignedSquad.x, AssignedSquad.y + (SettlementBox.height + settlementYSpacing) * count + scroll, AssignedSquad.width, AssignedSquad.height), "No assigned Squad");//settlement.militarySquad.name);
						}
					} else
					{
						Widgets.Label(new Rect(AssignedSquad.x, AssignedSquad.y + (SettlementBox.height + settlementYSpacing) * count + scroll, AssignedSquad.width, AssignedSquad.height), "No assigned Squad");
					}


					Widgets.Label(new Rect(isBusy.x, isBusy.y + (SettlementBox.height + settlementYSpacing) * count + scroll, isBusy.width, isBusy.height), "Available: " + (!settlement.IsMilitaryBusySilent));

					Text.Font = GameFont.Tiny;

					//Set Squad Button
					if (Widgets.ButtonText(new Rect(buttonSetSquad.x, buttonSetSquad.y + (SettlementBox.height + settlementYSpacing) * count + scroll, buttonSetSquad.width, buttonSetSquad.height), "Set Squad"))
					{
						//check null
						if (util.squads == null)
						{
							util.resetSquads();
						}

						List<FloatMenuOption> Squads = new List<FloatMenuOption>();


						//Create list of selectable units
						foreach (MilSquadFC squad in util.squads)
						{
							Squads.Add(new FloatMenuOption(squad.name + " - Total Equipment Cost: " + squad.equipmentTotalCost, delegate {
								//Unit is selected
								util.attemptToAssignSquad(settlement, squad);
							}));
						}
						if (Squads.Count == 0)
						{
							Squads.Add(new FloatMenuOption("No Available Squads", delegate { }));
						}
						FloatMenu selection = new FloatMenu(Squads);
						Find.WindowStack.Add(selection);

					}

					//View Squad
					if (Widgets.ButtonText(new Rect(buttonViewSquad.x, buttonViewSquad.y + (SettlementBox.height + settlementYSpacing) * count + scroll, buttonViewSquad.width, buttonViewSquad.height), "View Squad"))
					{
						Messages.Message("This is currently not implemented.", MessageTypeDefOf.RejectInput);
					}


					//Deploy Squad
					if (Widgets.ButtonText(new Rect(buttonDeploySquad.x, buttonDeploySquad.y + (SettlementBox.height + settlementYSpacing) * count + scroll, buttonDeploySquad.width, buttonDeploySquad.height), "Deploy Squad"))
					{
						if (!(settlement.isMilitaryBusy()) && settlement.isMilitarySquadValid())
						{

							List<FloatMenuOption> options = new List<FloatMenuOption>();
							options.Add(new FloatMenuOption("Walk into map", delegate { FactionColonies.CallinAlliedForces(settlement, false); Find.WindowStack.currentlyDrawnWindow.Close(); }));
							//check if medieval only
							bool medievalOnly = LoadedModManager.GetMod<FactionColoniesMod>().GetSettings<FactionColonies>().medievalTechOnly;
							if (!medievalOnly && DefDatabase<ResearchProjectDef>.GetNamed("TransportPod", false).IsFinished)
							{
								options.Add(new FloatMenuOption("Drop-Pod", delegate { FactionColonies.CallinAlliedForces(settlement, true); Find.WindowStack.currentlyDrawnWindow.Close(); }));

							}
							Find.WindowStack.Add(new FloatMenu(options));
							
						} else if (settlement.isMilitaryBusy() && settlement.isMilitarySquadValid() && faction.hasPolicy(FCPolicyDefOf.militaristic))
						{
							if ((faction.traitMilitaristicTickLastUsedExtraSquad + GenDate.TicksPerDay * 5) <= Find.TickManager.TicksGame)
							{
								int cost = (int)Math.Round(settlement.militarySquad.outfit.updateEquipmentTotalCost() * .2);
								List<FloatMenuOption> options = new List<FloatMenuOption>();

								options.Add(new FloatMenuOption("Deploy Secondary Squad - $" + cost + " silver", delegate
								{
									if (PaymentUtil.getSilver() >= cost)
									{
										List<FloatMenuOption> deployment = new List<FloatMenuOption>();

										deployment.Add(new FloatMenuOption("Walk into map", delegate
										{
											FactionColonies.CallinAlliedForces(settlement, false, cost);
											Find.WindowStack.currentlyDrawnWindow.Close();
										}));
									//check if medieval only
									bool medievalOnly = LoadedModManager.GetMod<FactionColoniesMod>().GetSettings<FactionColonies>().medievalTechOnly;
										if (!medievalOnly && DefDatabase<ResearchProjectDef>.GetNamed("TransportPod", false).IsFinished)
										{
											deployment.Add(new FloatMenuOption("Drop-Pod", delegate
											{
												FactionColonies.CallinAlliedForces(settlement, true, cost);
												Find.WindowStack.currentlyDrawnWindow.Close();
											}));

										}
										Find.WindowStack.Add(new FloatMenu(deployment));
									}
									else
									{
										Messages.Message("NotEnoughSilverToDeploySquad".Translate(), MessageTypeDefOf.RejectInput);
									}
								}));

								Find.WindowStack.Add(new FloatMenu(options));
							} else
							{
								Messages.Message("XDaysToRedeploy".Translate(Math.Round(((faction.traitMilitaristicTickLastUsedExtraSquad + GenDate.TicksPerDay * 5) - Find.TickManager.TicksGame).TicksToDays(), 1)), MessageTypeDefOf.RejectInput);
							}
						}

					}

					//Reset Squad
					if (Widgets.ButtonText(new Rect(buttonResetPawns.x, buttonResetPawns.y + (SettlementBox.height + settlementYSpacing) * count + scroll, buttonResetPawns.width, buttonResetPawns.height), "Reset Pawns"))
					{
						FloatMenuOption confirm = new FloatMenuOption("Are you sure? Click to confirm", delegate
						{
							if (settlement.militarySquad != null)
							{
								Messages.Message("Pawns have been regenerated for the squad", MessageTypeDefOf.NeutralEvent);
								settlement.militarySquad.initiateSquad();
							} else
							{
								Messages.Message("There is no pawns to reset. Assign a squad first.", MessageTypeDefOf.RejectInput);
							}
						});

						List<FloatMenuOption> list = new List<FloatMenuOption>();
						list.Add(confirm);
						Find.WindowStack.Add(new FloatMenu(list));

					}

					//Order Fire Support
					if (Widgets.ButtonText(new Rect(buttonOrderFireSupport.x, buttonOrderFireSupport.y + (SettlementBox.height + settlementYSpacing) * count + scroll, buttonOrderFireSupport.width, buttonOrderFireSupport.height), "Order Fire Support"))
					{
						List<FloatMenuOption> list = new List<FloatMenuOption>();


						foreach (MilitaryFireSupport support in util.fireSupportDefs)
						{
							float cost = support.returnTotalCost();
							FloatMenuOption option = new FloatMenuOption(support.name + " - $" + cost, delegate 
							{
								if (support.returnTotalCost() <= FactionColonies.calculateMilitaryLevelPoints(settlement.settlementMilitaryLevel))
								{
									if (settlement.buildings.Contains(BuildingFCDefOf.artilleryOutpost))
									{
										if (settlement.artilleryTimer <= Find.TickManager.TicksGame)
										{
											if (PaymentUtil.getSilver() >= cost)
											{
												FactionColonies.FireSupport(settlement, support);
												Find.WindowStack.TryRemove(typeof(militaryCustomizationWindowFC));

											}
											else
											{
												Messages.Message("You lack the required amount of silver to use that firesupport option!", MessageTypeDefOf.RejectInput);
											}
										}
										else
										{
											Messages.Message("That firesupport option is on cooldown for another " + GenDate.ToStringTicksToDays(settlement.artilleryTimer - Find.TickManager.TicksGame), MessageTypeDefOf.RejectInput);
										}
									}
									else
									{
										Messages.Message("The settlement requires an artillery outpost to be built to use that firesupport option", MessageTypeDefOf.RejectInput);
									}
								} 
								else
								{
									Messages.Message("The settlement requires a higher military level to use that fire support!", MessageTypeDefOf.RejectInput);
								}
							});
							list.Add(option);
						}

						if (list.Count() == 0)
						{
							list.Add(new FloatMenuOption("No fire supports currently made. Make one", delegate { }));
						}

						FloatMenu menu = new FloatMenu(list);
						Find.WindowStack.Add(menu);
					}

					count++;
				}
			}


			//Reset Text anchor and font
			Text.Font = fontBefore;
			Text.Anchor = anchorBefore;


			if (Event.current.type == EventType.ScrollWheel)
			{

				scrollWindow(Event.current.delta.y, settlementMaxScroll);
			}


		}

		public void DrawTabSquad(Rect inRect)
		{

			//set text anchor and font
			GameFont fontBefore = Text.Font;
			TextAnchor anchorBefore = Text.Anchor;


			Rect SelectionBar = new Rect(5, 45, 200, 30);
			Rect nameTextField = new Rect(5, 90, 250, 30);
			Rect isTrader = new Rect(5, 130, 130, 30);

			Rect UnitStandBase = new Rect(140, 200, 50, 30);
			Rect EquipmentTotalCost = new Rect(350, 50, 450, 40);
			Rect ResetButton = new Rect(700, 100, 100, 30);
			Rect DeleteButton = new Rect(ResetButton.x, ResetButton.y + ResetButton.height + 5, ResetButton.width, ResetButton.height);
			Rect PointRefButton = new Rect(DeleteButton.x, DeleteButton.y + DeleteButton.height + 5, DeleteButton.width, DeleteButton.height);


			//If squad is not selected
			if (Widgets.CustomButtonText(ref SelectionBar, selectedText, Color.gray, Color.white, Color.black))
			{

				//check null
				if (util.squads == null)
				{
					util.resetSquads();
				}

				List<FloatMenuOption> Squads = new List<FloatMenuOption>();

				//Option to create new unit
				Squads.Add(new FloatMenuOption("Create New Squad", delegate {
					MilSquadFC newSquad = new MilSquadFC(true);
					newSquad.name = "New Squad " + (util.squads.Count() + 1);
					selectedText = newSquad.name;
					selectedSquad = newSquad;
					selectedSquad.newSquad();
					util.squads.Add(newSquad);

				}));

				//Create list of selectable units
				foreach (MilSquadFC squad in util.squads)
				{
					Squads.Add(new FloatMenuOption(squad.name, delegate {
						//Unit is selected
						selectedText = squad.name;
						selectedSquad = squad;
						selectedSquad.updateEquipmentTotalCost();
					}));
				}
				FloatMenu selection = new FloatMenu(Squads);
				Find.WindowStack.Add(selection);
			}



			//if squad is selected
			if (selectedSquad != null)
			{

				Text.Anchor = TextAnchor.MiddleLeft;
				Text.Font = GameFont.Small;




				if (settlementPointReference != null)
				{
					Widgets.Label(EquipmentTotalCost, "Total Squad Equipment Cost: " + selectedSquad.equipmentTotalCost.ToString() + " / " + FactionColonies.calculateMilitaryLevelPoints(settlementPointReference.settlementMilitaryLevel) + " (Max Cost)");
				} else
				{
					Widgets.Label(EquipmentTotalCost, "Total Squad Equipment Cost: " + selectedSquad.equipmentTotalCost.ToString() + " / " + "No Reference");
				}
				Text.Font = GameFont.Tiny;
				Text.Anchor = TextAnchor.UpperCenter;


				Widgets.CheckboxLabeled(isTrader, "is Trader Caravan", ref selectedSquad.isTraderCaravan);
				selectedSquad.setTraderCaravan(selectedSquad.isTraderCaravan);



				//Unit Name
				selectedSquad.name = Widgets.TextField(nameTextField, selectedSquad.name);

				if(Widgets.ButtonText(ResetButton, "Reset to Default"))
				{
					selectedSquad.newSquad();
				}

				if(Widgets.ButtonText(DeleteButton, "Delete Squad"))
				{
					selectedSquad.deleteSquad();
					util.checkMilitaryUtilForErrors();
					selectedSquad = null;
					selectedText = "Select A Squad";

					//Reset Text anchor and font
					Text.Font = fontBefore;
					Text.Anchor = anchorBefore;
					return;
				}

				if(Widgets.ButtonText(PointRefButton, "Set Point Ref"))
				{
					List<FloatMenuOption> settlementList = new List<FloatMenuOption>();

					foreach (SettlementFC settlement in Find.World.GetComponent<FactionFC>().settlements)
					{
						settlementList.Add(new FloatMenuOption(settlement.name + " - Military Level : " + settlement.settlementMilitaryLevel, delegate
						{
							//set points
							settlementPointReference = settlement;
						}));
					}

					if (settlementList.Count() == 0)
					{
						settlementList.Add(new FloatMenuOption("No Valid Settlements", null));
					}
						FloatMenu floatMenu = new FloatMenu(settlementList);
						floatMenu.vanishIfMouseDistant = true;
						Find.WindowStack.Add(floatMenu);
					
				}

				//for (int k = 0; k < 30; k++)
				//{
				//	Widgets.ButtonImage(new Rect(UnitStandBase.x + (k * 15), UnitStandBase.y + ((k % 5) * 70), 50, 20), texLoad.unitCircle);
				//}


				for (int k = 0; k < 30; k++)
				{
					if(Widgets.ButtonImage(new Rect(UnitStandBase.x + ((k%6) * 80),  UnitStandBase.y + (k - k%6)/5 * 70, 50, 20), TexLoad.unitCircle))
					{
						int click = k;
						List<FloatMenuOption> Units = new List<FloatMenuOption>();

						//Option to clear unit slot
						Units.Add(new FloatMenuOption("Clear Unit Slot", delegate {
							//Log.Message(selectedSquad.units.Count().ToString());
							//Log.Message(click.ToString());
							selectedSquad.units[click] = new MilUnitFC(true);
							selectedSquad.updateEquipmentTotalCost();
							selectedSquad.ChangeTick();

						}));

						//Create list of selectable units
						foreach (MilUnitFC unit in util.units)
						{
							Units.Add(new FloatMenuOption(unit.name + " - Cost: " + unit.equipmentTotalCost, delegate {
								//Unit is selected
								selectedSquad.units[click] = unit;
								selectedSquad.updateEquipmentTotalCost();
								selectedSquad.ChangeTick();

							}));
						}
						FloatMenu selection = new FloatMenu(Units);
						Find.WindowStack.Add(selection);
					}

					if (selectedSquad.units[k].isBlank == false) 
					{
						if (selectedSquad.units.ElementAt(k).animal != null)
						{
							Widgets.ButtonImage(new Rect(UnitStandBase.x + 15 + ((k % 6) * 80), UnitStandBase.y - 45 + (k - k % 6) / 5 * 70, 60, 60), selectedSquad.units.ElementAt(k).animal.race.uiIcon);
						}
						Widgets.ThingIcon(new Rect(UnitStandBase.x - 5 + ((k % 6) * 80), UnitStandBase.y - 45 + (k - k % 6) / 5 * 70, 60, 60), selectedSquad.units.ElementAt(k).defaultPawn);
						if (selectedSquad.units.ElementAt(k).defaultPawn.equipment.AllEquipmentListForReading.Count > 0)
						{
							Widgets.ThingIcon(new Rect(UnitStandBase.x - 5 + ((k % 6) * 80), UnitStandBase.y - 15 + (k - k % 6) / 5 * 70, 40, 40), selectedSquad.units.ElementAt(k).defaultPawn.equipment.AllEquipmentListForReading[0]);
						}
						Widgets.Label(new Rect(UnitStandBase.x - 15 + ((k % 6) * 80), UnitStandBase.y - 65 + (k - k % 6) / 5 * 70, 80, 60), selectedSquad.units.ElementAt(k).name);
					}

				}






				//Reset Text anchor and font
				Text.Font = fontBefore;
				Text.Anchor = anchorBefore;
			}

			//Reset Text anchor and font
			Text.Font = fontBefore;
			Text.Anchor = anchorBefore;
		}






		public void DrawTabUnit(Rect inRect)
		{
			Rect SelectionBar = new Rect(5, 45, 200, 30);
			Rect nameTextField = new Rect(5, 90, 250, 30);
			Rect isCivilian = new Rect(5, 130, 100, 30);
			Rect isTrader = new Rect(isCivilian.x, isCivilian.y + isCivilian.height + 5, isCivilian.width, isCivilian.height);

			Rect unitIcon = new Rect(560, 235, 120, 120);

			Rect ApparelHead = new Rect(600, 140, 50, 50);
			Rect ApparelTorsoSkin = new Rect(700, 170, 50, 50);
			Rect ApparelBelt = new Rect(700, 240, 50, 50);
			Rect ApparelLegs = new Rect(700, 310, 50, 50);

			Rect AnimalCompanion = new Rect(500, 160, 50, 50);
			Rect ApparelTorsoShell = new Rect(500, 230, 50, 50);
			Rect ApparelTorsoMiddle = new Rect(500, 310, 50, 50);
			Rect EquipmentWeapon = new Rect(440, 230, 50, 50);

			Rect ApparelWornItems = new Rect(440, 385, 330, 175);
			Rect EquipmentTotalCost = new Rect(450, 50, 350, 40);

			Rect ResetButton = new Rect(700, 50, 100, 30);
			Rect DeleteButton = new Rect(ResetButton.x, ResetButton.y + ResetButton.height + 5, ResetButton.width, ResetButton.height);
			Rect RollNewPawn= new Rect(DeleteButton.x, DeleteButton.y + DeleteButton.height + 5, DeleteButton.width, DeleteButton.height);
			Rect ChangeRace = new Rect(325, ResetButton.y, RollNewPawn.width, RollNewPawn.height);
			//Rect ApparelTorso



			//If unit is not selected

			if (Widgets.CustomButtonText(ref SelectionBar, selectedText, Color.gray, Color.white, Color.black))
			{
				List<FloatMenuOption> Units = new List<FloatMenuOption>();

				//Option to create new unit
				Units.Add(new FloatMenuOption("Create New Unit", delegate {
					MilUnitFC newUnit = new MilUnitFC(false);
					newUnit.name = "New Unit " + (util.units.Count() + 1);
					selectedText = newUnit.name;
					selectedUnit = newUnit;
					util.units.Add(newUnit);

				}));

				//Create list of selectable units
				foreach (MilUnitFC unit in util.units)
				{
					if (unit.defaultPawn.equipment.Primary != null)
					{
						Units.Add(new FloatMenuOption(unit.name, delegate
						{
							//Unit is selected
							selectedText = unit.name;
							selectedUnit = unit;
						}, unit.defaultPawn.equipment.Primary.def));
					} else
					{
						Units.Add(new FloatMenuOption(unit.name, delegate
						{
							//Unit is selected
							selectedText = unit.name;
							selectedUnit = unit;
						}));
					}
				}
				FloatMenu selection = new FloatMenu(Units);
				Find.WindowStack.Add(selection);
			}

			//Worn Items
			Widgets.DrawMenuSection(ApparelWornItems);

			//set text anchor and font
			GameFont fontBefore = Text.Font;
			TextAnchor anchorBefore = Text.Anchor;

			Text.Font = GameFont.Tiny;
			Text.Anchor = TextAnchor.UpperCenter;

			//if unit is not selected
			Widgets.Label(new Rect(new Vector2(ApparelHead.x, ApparelHead.y - 15), ApparelHead.size), "Head");
			Widgets.DrawMenuSection(ApparelHead);
			Widgets.Label(new Rect(new Vector2(ApparelTorsoSkin.x, ApparelTorsoSkin.y - 15), ApparelTorsoSkin.size), "Shirt");
			Widgets.DrawMenuSection(ApparelTorsoSkin);
			Widgets.Label(new Rect(new Vector2(ApparelTorsoMiddle.x, ApparelTorsoMiddle.y - 15), ApparelTorsoMiddle.size), "Chest");
			Widgets.DrawMenuSection(ApparelTorsoMiddle);
			Widgets.Label(new Rect(new Vector2(ApparelTorsoShell.x, ApparelTorsoShell.y - 15), ApparelTorsoShell.size), "Over");
			Widgets.DrawMenuSection(ApparelTorsoShell);
			Widgets.Label(new Rect(new Vector2(ApparelBelt.x, ApparelBelt.y - 15), ApparelBelt.size), "Belt");
			Widgets.DrawMenuSection(ApparelBelt);
			Widgets.Label(new Rect(new Vector2(ApparelLegs.x, ApparelLegs.y - 15), ApparelLegs.size), "Pants");
			Widgets.DrawMenuSection(ApparelLegs);
			Widgets.Label(new Rect(new Vector2(EquipmentWeapon.x, EquipmentWeapon.y - 15), EquipmentWeapon.size), "Weapon");
			Widgets.DrawMenuSection(EquipmentWeapon);
			Widgets.Label(new Rect(new Vector2(AnimalCompanion.x, AnimalCompanion.y - 15), AnimalCompanion.size), "Animal");
			Widgets.DrawMenuSection(AnimalCompanion);

			//Reset Text anchor and font
			Text.Font = fontBefore;
			Text.Anchor = anchorBefore;

			//if unit is selected
			if (selectedUnit != null)
			{

				Text.Font = GameFont.Tiny;
				Text.Anchor = TextAnchor.UpperCenter;


				if (Widgets.ButtonText(ResetButton, "Reset to Default"))
				{
					selectedUnit.unequipAllEquipment();
				}

				if (Widgets.ButtonText(DeleteButton, "Delete Unit"))
				{
					selectedUnit.removeUnit();
					util.checkMilitaryUtilForErrors();
					selectedUnit = null;
					selectedText = "Select A Unit";

					//Reset Text anchor and font
					Text.Font = fontBefore;
					Text.Anchor = anchorBefore;
					return;
				}

				if (Widgets.ButtonText(RollNewPawn, "Roll New Pawn"))
				{
					selectedUnit.generateDefaultPawn();
				}

				if (Widgets.ButtonText(ChangeRace, "Change Race"))
				{
					List<string> races = new List<string>();
					List<FloatMenuOption> options = new List<FloatMenuOption>();
					
					foreach (PawnKindDef def in DefDatabase<PawnKindDef>.AllDefsListForReading)
					{
						if (def.race.race.intelligence == Intelligence.Humanlike & races.Contains(def.race.label) == false && def.race.BaseMarketValue != 0 )
						{
							if (def.race.label == "Human" && def.LabelCap != "Colonist")
							{

							}
							else
							{

								races.Add(def.race.label);
								options.Add(new FloatMenuOption(def.race.label.CapitalizeFirst() + " - Cost: " + Math.Floor(def.race.BaseMarketValue * FactionColonies.militaryRaceCostMultiplier), delegate
								{
									selectedUnit.pawnKind = def;
									selectedUnit.generateDefaultPawn();
									selectedUnit.changeTick();
								}));

							}
						}
					}

					if (options.Count() == 0)
					{
						options.Add(new FloatMenuOption("No available races", null));
					}
					options.Sort(FactionColonies.CompareFloatMenuOption);
					FloatMenu menu = new FloatMenu(options);
					Find.WindowStack.Add(menu);
				}


				//Unit Name
				selectedUnit.name = Widgets.TextField(nameTextField, selectedUnit.name);

				Widgets.CheckboxLabeled(isCivilian, "is Civilian", ref selectedUnit.isCivilian);
				Widgets.CheckboxLabeled(isTrader, "is Trader", ref selectedUnit.isTrader);
				selectedUnit.setTrader(selectedUnit.isTrader);
				selectedUnit.setCivilian(selectedUnit.isCivilian);


				//Reset Text anchor and font
				Text.Font = fontBefore;
				Text.Anchor = anchorBefore;
				//Draw Pawn
				if (selectedUnit.defaultPawn != null)
				{
					Widgets.ThingIcon(unitIcon, selectedUnit.defaultPawn);
				}


				//Animal Companion
				if (Widgets.ButtonInvisible(AnimalCompanion))
				{
					List<FloatMenuOption> list = new List<FloatMenuOption>();


					foreach (PawnKindDef animal in DefDatabase<PawnKindDef>.AllDefs)
					{
						if (animal.RaceProps.IsFlesh == true && animal.race.race.Animal == true && animal.race.tradeTags != null && !animal.race.tradeTags.Contains("AnimalMonster") && !animal.race.tradeTags.Contains("AnimalGenetic") && !animal.race.tradeTags.Contains("AnimalAlpha"))
						{
							list.Add(new FloatMenuOption(animal.LabelCap + " - Cost: " + Math.Floor(animal.race.BaseMarketValue * FactionColonies.militaryAnimalCostMultiplier), delegate {
								//Do add animal code here
								selectedUnit.animal = animal;
							}, animal.race.uiIcon, Color.white));
						}
					}
					list.Sort(FactionColonies.CompareFloatMenuOption);

					list.Insert(0, new FloatMenuOption("Unequip", delegate
					{
						//unequip here
						selectedUnit.animal = null;
					}));
					FloatMenu menu = new FloatMenu(list);
					Find.WindowStack.Add(menu);
				}




				//Weapon Equipment
				if (Widgets.ButtonInvisible(EquipmentWeapon))
				{
					List<FloatMenuOption> list = new List<FloatMenuOption>();


					foreach (ThingDef thing in DefDatabase<ThingDef>.AllDefs)
					{
						if (thing.IsWeapon == true && thing.BaseMarketValue != 0 && FactionColonies.canCraftItem(thing))
						{
							if (true)                    //CHANGE THIS
							{
								list.Add(new FloatMenuOption(thing.LabelCap + " - Cost: " + thing.BaseMarketValue, delegate
								{

									if (thing.MadeFromStuff == true)
									{
										//If made from stuff
										List<FloatMenuOption> stuffList = new List<FloatMenuOption>();
										foreach (ThingDef stuff in DefDatabase<ThingDef>.AllDefs)
										{
											if (stuff.IsStuff == true && thing.stuffCategories.SharesElementWith(stuff.stuffProps.categories) == true)
											{
												stuffList.Add(new FloatMenuOption(stuff.LabelCap + " - Total Value: " + (StatWorker_MarketValue.CalculatedBaseMarketValue(thing, stuff)), delegate
												{

													selectedUnit.equipWeapon(ThingMaker.MakeThing(thing, stuff) as ThingWithComps);
												}));
											}
										}
										stuffList.Sort(FactionColonies.CompareFloatMenuOption);
										FloatMenu stuffWindow = new FloatMenu(stuffList);
										Find.WindowStack.Add(stuffWindow);


									}
									else
									{
										//If not made from stuff

										selectedUnit.equipWeapon(ThingMaker.MakeThing(thing) as ThingWithComps);
									}



								}, thing));

							}
						}
					}
					list.Sort(FactionColonies.CompareFloatMenuOption);

					list.Insert(0, new FloatMenuOption("Unequip", delegate
					{
						selectedUnit.unequipWeapon();
					}));

					FloatMenu menu = new FloatMenu(list);

					Find.WindowStack.Add(menu);
				}

				//headgear Slot
				if (Widgets.ButtonInvisible(ApparelHead))
				{
					List<FloatMenuOption> headgearList = new List<FloatMenuOption>();


					foreach(ThingDef thing in DefDatabase<ThingDef>.AllDefs)
					{
						if (thing.IsApparel == true) {
							if (thing.apparel.layers.Contains(ApparelLayerDefOf.Overhead) == true && FactionColonies.canCraftItem(thing))
							{
								headgearList.Add(new FloatMenuOption(thing.LabelCap + " - Cost: " + thing.BaseMarketValue, delegate 
								{

									if (thing.MadeFromStuff == true)
									{
										//If made from stuff
										List<FloatMenuOption> stuffList = new List<FloatMenuOption>();
										foreach (ThingDef stuff in DefDatabase<ThingDef>.AllDefs)
										{
											if (stuff.IsStuff == true &&  thing.stuffCategories.SharesElementWith(stuff.stuffProps.categories) == true)
											{
												stuffList.Add(new FloatMenuOption(stuff.LabelCap + " - Total Value: " + (StatWorker_MarketValue.CalculatedBaseMarketValue(thing,stuff)), delegate
												{

													selectedUnit.wearEquipment(ThingMaker.MakeThing(thing, stuff) as Apparel, true);
												}));
											}
										}
										stuffList.Sort(FactionColonies.CompareFloatMenuOption);
										FloatMenu stuffWindow = new FloatMenu(stuffList);
										Find.WindowStack.Add(stuffWindow);


									}
									else
									{
										//If not made from stuff

										selectedUnit.wearEquipment(ThingMaker.MakeThing(thing) as Apparel, true);
									}



								}, thing));

							}
						}
					}
					headgearList.Sort(FactionColonies.CompareFloatMenuOption);

					headgearList.Insert(0, new FloatMenuOption("Unequip", delegate
					{
						//Remove old
						foreach (Apparel apparel in selectedUnit.defaultPawn.apparel.WornApparel)
						{
							if (apparel.def.apparel.layers.Contains(ApparelLayerDefOf.Overhead) == true)
							{
								selectedUnit.defaultPawn.apparel.Remove(apparel);
								break;
							}
						}
					}));

					FloatMenu menu = new FloatMenu(headgearList);

					Find.WindowStack.Add(menu);
				}



				//Torso Shell Slot
				if (Widgets.ButtonInvisible(ApparelTorsoShell))
				{
					List<FloatMenuOption> list = new List<FloatMenuOption>();


					foreach (ThingDef thing in DefDatabase<ThingDef>.AllDefs)
					{
						if (thing.IsApparel == true)
						{
							if (thing.apparel.layers.Contains(ApparelLayerDefOf.Shell) == true && thing.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Torso) == true && FactionColonies.canCraftItem(thing))                    //CHANGE THIS
							{
								list.Add(new FloatMenuOption(thing.LabelCap + " - Cost: " + thing.BaseMarketValue, delegate
								{

									if (thing.MadeFromStuff == true)
									{
										//If made from stuff
										List<FloatMenuOption> stuffList = new List<FloatMenuOption>();
										foreach (ThingDef stuff in DefDatabase<ThingDef>.AllDefs)
										{
											if (stuff.IsStuff == true && thing.stuffCategories.SharesElementWith(stuff.stuffProps.categories) == true)
											{
												stuffList.Add(new FloatMenuOption(stuff.LabelCap + " - Total Value: " + (StatWorker_MarketValue.CalculatedBaseMarketValue(thing, stuff)), delegate
												{

													selectedUnit.wearEquipment(ThingMaker.MakeThing(thing, stuff) as Apparel, true);
												}));
											}
										}
										stuffList.Sort(FactionColonies.CompareFloatMenuOption);
										FloatMenu stuffWindow = new FloatMenu(stuffList);
										Find.WindowStack.Add(stuffWindow);


									}
									else
									{
										//If not made from stuff

										selectedUnit.wearEquipment(ThingMaker.MakeThing(thing) as Apparel, true);
									}



								}, thing));

							}
						}
					}
					list.Sort(FactionColonies.CompareFloatMenuOption);

					list.Insert(0, new FloatMenuOption("Unequip", delegate
					{
						//Remove old
						foreach (Apparel apparel in selectedUnit.defaultPawn.apparel.WornApparel)
						{
							if (apparel.def.apparel.layers.Contains(ApparelLayerDefOf.Shell) == true && apparel.def.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Torso) == true)                    //CHANGE THIS
							{
								selectedUnit.defaultPawn.apparel.Remove(apparel);
								break;
							}
						}
					}));
					FloatMenu menu = new FloatMenu(list);

					Find.WindowStack.Add(menu);
				}



				//Torso Middle Slot
				if (Widgets.ButtonInvisible(ApparelTorsoMiddle))
				{
					List<FloatMenuOption> list = new List<FloatMenuOption>();


					foreach (ThingDef thing in DefDatabase<ThingDef>.AllDefs)
					{
						if (thing.IsApparel == true)
						{
							if (thing.apparel.layers.Contains(ApparelLayerDefOf.Middle) == true && thing.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Torso) == true && FactionColonies.canCraftItem(thing))                    //CHANGE THIS
							{
								list.Add(new FloatMenuOption(thing.LabelCap + " - Cost: " + thing.BaseMarketValue, delegate
								{

									if (thing.MadeFromStuff == true)
									{
										//If made from stuff
										List<FloatMenuOption> stuffList = new List<FloatMenuOption>();
										foreach (ThingDef stuff in DefDatabase<ThingDef>.AllDefs)
										{
											if (stuff.IsStuff == true && thing.stuffCategories.SharesElementWith(stuff.stuffProps.categories) == true)
											{
												stuffList.Add(new FloatMenuOption(stuff.LabelCap + " - Total Value: " + (StatWorker_MarketValue.CalculatedBaseMarketValue(thing, stuff)), delegate
												{

													selectedUnit.wearEquipment(ThingMaker.MakeThing(thing, stuff) as Apparel, true);
												}));
											}
										}
										stuffList.Sort(FactionColonies.CompareFloatMenuOption);
										FloatMenu stuffWindow = new FloatMenu(stuffList);
										Find.WindowStack.Add(stuffWindow);


									}
									else
									{
										//If not made from stuff

										selectedUnit.wearEquipment(ThingMaker.MakeThing(thing) as Apparel, true);
									}



								}, thing));

							}
						}
					}
					list.Sort(FactionColonies.CompareFloatMenuOption);

					list.Insert(0, new FloatMenuOption("Unequip", delegate
					{
						//Remove old
						foreach (Apparel apparel in selectedUnit.defaultPawn.apparel.WornApparel)
						{
							if (apparel.def.apparel.layers.Contains(ApparelLayerDefOf.Middle) == true && apparel.def.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Torso) == true)                    //CHANGE THIS
							{
								selectedUnit.defaultPawn.apparel.Remove(apparel);
								break;
							}
						}
					}));
					FloatMenu menu = new FloatMenu(list);

					Find.WindowStack.Add(menu);
				}



				//Torso Skin Slot
				if (Widgets.ButtonInvisible(ApparelTorsoSkin))
				{
					List<FloatMenuOption> list = new List<FloatMenuOption>();


					foreach (ThingDef thing in DefDatabase<ThingDef>.AllDefs)
					{
						if (thing.IsApparel == true)
						{
							if (thing.apparel.layers.Contains(ApparelLayerDefOf.OnSkin) == true && thing.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Torso) == true && FactionColonies.canCraftItem(thing))                    //CHANGE THIS
							{
								list.Add(new FloatMenuOption(thing.LabelCap + " - Cost: " + thing.BaseMarketValue, delegate
								{

									if (thing.MadeFromStuff == true)
									{
										//If made from stuff
										List<FloatMenuOption> stuffList = new List<FloatMenuOption>();
										foreach (ThingDef stuff in DefDatabase<ThingDef>.AllDefs)
										{
											if (stuff.IsStuff == true && thing.stuffCategories.SharesElementWith(stuff.stuffProps.categories) == true)
											{
												stuffList.Add(new FloatMenuOption(stuff.LabelCap + " - Total Value: " + (StatWorker_MarketValue.CalculatedBaseMarketValue(thing, stuff)), delegate
												{

													selectedUnit.wearEquipment(ThingMaker.MakeThing(thing, stuff) as Apparel, true);
												}));
											}
										}
										stuffList.Sort(FactionColonies.CompareFloatMenuOption);
										FloatMenu stuffWindow = new FloatMenu(stuffList);
										Find.WindowStack.Add(stuffWindow);


									}
									else
									{
										//If not made from stuff

										selectedUnit.wearEquipment(ThingMaker.MakeThing(thing) as Apparel, true);
									}



								}, thing));

							}
						}
					}
					list.Sort(FactionColonies.CompareFloatMenuOption);


					list.Insert(0, new FloatMenuOption("Unequip", delegate
					{
						//Remove old
						foreach (Apparel apparel in selectedUnit.defaultPawn.apparel.WornApparel)
						{
							if (apparel.def.apparel.layers.Contains(ApparelLayerDefOf.OnSkin) == true && apparel.def.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Torso) == true)                    //CHANGE THIS
							{
								selectedUnit.defaultPawn.apparel.Remove(apparel);
								break;
							}
						}
					}));
					FloatMenu menu = new FloatMenu(list);

					Find.WindowStack.Add(menu);
				}



				//Pants Slot
				if (Widgets.ButtonInvisible(ApparelLegs))
				{
					List<FloatMenuOption> list = new List<FloatMenuOption>();

					foreach (ThingDef thing in DefDatabase<ThingDef>.AllDefs)
					{
						if (thing.IsApparel == true)
						{
							if (thing.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Legs) == true && thing.apparel.layers.Contains(ApparelLayerDefOf.OnSkin) && FactionColonies.canCraftItem(thing))                    //CHANGE THIS
							{
								list.Add(new FloatMenuOption(thing.LabelCap + " - Cost: " + thing.BaseMarketValue, delegate
								{

									if (thing.MadeFromStuff == true)
									{
										//If made from stuff
										List<FloatMenuOption> stuffList = new List<FloatMenuOption>();
										foreach (ThingDef stuff in DefDatabase<ThingDef>.AllDefs)
										{
											if (stuff.IsStuff == true && thing.stuffCategories.SharesElementWith(stuff.stuffProps.categories) == true)
											{
												stuffList.Add(new FloatMenuOption(stuff.LabelCap + " - Total Value: " + (StatWorker_MarketValue.CalculatedBaseMarketValue(thing, stuff)), delegate
												{

													selectedUnit.wearEquipment(ThingMaker.MakeThing(thing, stuff) as Apparel, true);
												}));
											}
										}
										stuffList.Sort(FactionColonies.CompareFloatMenuOption);
										FloatMenu stuffWindow = new FloatMenu(stuffList);
										Find.WindowStack.Add(stuffWindow);


									}
									else
									{
										//If not made from stuff

										selectedUnit.wearEquipment(ThingMaker.MakeThing(thing) as Apparel, true);
									}



								}, thing));

							}
						}
					}
					list.Sort(FactionColonies.CompareFloatMenuOption);

					list.Insert(0, new FloatMenuOption("Unequip", delegate
					{
						//Remove old
						foreach (Apparel apparel in selectedUnit.defaultPawn.apparel.WornApparel)
						{
							if (apparel.def.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Legs) == true && apparel.def.apparel.layers.Contains(ApparelLayerDefOf.OnSkin))                    //CHANGE THIS
							{
								selectedUnit.defaultPawn.apparel.Remove(apparel);
								break;
							}
						}
					}));
					FloatMenu menu = new FloatMenu(list);

					Find.WindowStack.Add(menu);
				}



				//Apparel Belt Slot
				if (Widgets.ButtonInvisible(ApparelBelt))
				{
					List<FloatMenuOption> list = new List<FloatMenuOption>();

					foreach (ThingDef thing in DefDatabase<ThingDef>.AllDefs)
					{
						if (thing.IsApparel == true)
						{
							if (thing.apparel.layers.Contains(ApparelLayerDefOf.Belt) == true && FactionColonies.canCraftItem(thing))
							{
								list.Add(new FloatMenuOption(thing.LabelCap + " - Cost: " + thing.BaseMarketValue, delegate
								{

									if (thing.MadeFromStuff == true)
									{
										//If made from stuff
										List<FloatMenuOption> stuffList = new List<FloatMenuOption>();
										foreach (ThingDef stuff in DefDatabase<ThingDef>.AllDefs)
										{
											if (stuff.IsStuff == true && thing.stuffCategories.SharesElementWith(stuff.stuffProps.categories) == true)
											{
												stuffList.Add(new FloatMenuOption(stuff.LabelCap + " - Total Value: " + (StatWorker_MarketValue.CalculatedBaseMarketValue(thing, stuff)), delegate
												{

									

													selectedUnit.wearEquipment(ThingMaker.MakeThing(thing, stuff) as Apparel, true);
												}));
											}
										}
										stuffList.Sort(FactionColonies.CompareFloatMenuOption);
										FloatMenu stuffWindow = new FloatMenu(stuffList);
										Find.WindowStack.Add(stuffWindow);


									}
									else
									{
										//If not made from stuff
										//Remove old equipment
										foreach (Apparel apparel in selectedUnit.defaultPawn.apparel.WornApparel)
										{
											if (apparel.def.apparel.layers.Contains(ApparelLayerDefOf.Belt) == true)
											{
												selectedUnit.defaultPawn.apparel.Remove(apparel);
												break;
											}
										}

										selectedUnit.wearEquipment(ThingMaker.MakeThing(thing) as Apparel, true);
									}



								}, thing));

							}
						}
					}
					list.Sort(FactionColonies.CompareFloatMenuOption);

					list.Insert(0, new FloatMenuOption("Unequip", delegate
					{
						//Remove old
						foreach (Apparel apparel in selectedUnit.defaultPawn.apparel.WornApparel)
						{
							if (apparel.def.apparel.layers.Contains(ApparelLayerDefOf.Belt) == true)
							{
								selectedUnit.defaultPawn.apparel.Remove(apparel);
								break;
							}
						}
					}));
					FloatMenu menu = new FloatMenu(list);

					Find.WindowStack.Add(menu);
				}





				//worn items
				float totalCost = 0;
				int i = 0;

				totalCost += (float)Math.Floor(selectedUnit.defaultPawn.def.BaseMarketValue * FactionColonies.militaryRaceCostMultiplier);

				foreach (Thing thing in selectedUnit.defaultPawn.apparel.WornApparel.Concat(selectedUnit.defaultPawn.equipment.AllEquipmentListForReading))
				{
					Rect tmp = new Rect(ApparelWornItems.x, ApparelWornItems.y + i * 25, ApparelWornItems.width, 25);
					i++;

					totalCost += thing.MarketValue;

					if (Widgets.CustomButtonText(ref tmp, thing.LabelCap + " Cost: " + thing.MarketValue, Color.white, Color.black, Color.black))
					{
						Find.WindowStack.Add(new Dialog_InfoCard(thing));
					}
				}

				if (selectedUnit.animal != null)
				{
					Widgets.ButtonImage(AnimalCompanion, selectedUnit.animal.race.uiIcon);
					totalCost += (float)Math.Floor(selectedUnit.animal.race.BaseMarketValue * FactionColonies.militaryAnimalCostMultiplier);
				}

				foreach (Thing thing in selectedUnit.defaultPawn.apparel.WornApparel)
				{
					//Log.Message(thing.Label);


					if (thing.def.apparel.layers.Contains(ApparelLayerDefOf.Overhead) == true)
					{
						Widgets.ButtonImage(ApparelHead, thing.def.uiIcon);

					} 
					if (thing.def.apparel.layers.Contains(ApparelLayerDefOf.Belt) == true)
					{
						Widgets.ButtonImage(ApparelBelt, thing.def.uiIcon);
					} 
					if (thing.def.apparel.layers.Contains(ApparelLayerDefOf.Shell) && thing.def.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Torso))
					{
						Widgets.ButtonImage(ApparelTorsoShell, thing.def.uiIcon);
					} 
					if (thing.def.apparel.layers.Contains(ApparelLayerDefOf.Middle) && thing.def.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Torso))
					{
						Widgets.ButtonImage(ApparelTorsoMiddle, thing.def.uiIcon);
					}
					if (thing.def.apparel.layers.Contains(ApparelLayerDefOf.OnSkin) && thing.def.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Torso))
					{
						Widgets.ButtonImage(ApparelTorsoSkin, thing.def.uiIcon);
					}
					if (thing.def.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Legs) && thing.def.apparel.layers.Contains(ApparelLayerDefOf.OnSkin))
					{
						Widgets.ButtonImage(ApparelLegs, thing.def.uiIcon);
					}
				}

				foreach(Thing thing in selectedUnit.defaultPawn.equipment.AllEquipmentListForReading)
				{

					Widgets.ButtonImage(EquipmentWeapon, thing.def.uiIcon);
				}
				totalCost = (float)Math.Ceiling(totalCost);
				Widgets.Label(EquipmentTotalCost, "Total Equipment Cost: " + totalCost);



			}



		}


		public void DrawTabFireSupport(Rect inRect)
		{

			//set text anchor and font
			GameFont fontBefore = Text.Font;
			TextAnchor anchorBefore = Text.Anchor;


			float projectileBoxHeight = 30;
			Rect SelectionBar = new Rect(5, 45, 200, 30);
			Rect nameTextField = new Rect(5, 90, 250, 30);
			Rect floatRangeAccuracyLabel = new Rect(nameTextField.x, nameTextField.y + nameTextField.height + 5, nameTextField.width, (float)(nameTextField.height*1.5));
			Rect floatRangeAccuracy = new Rect(floatRangeAccuracyLabel.x, floatRangeAccuracyLabel.y + floatRangeAccuracyLabel.height + 5, floatRangeAccuracyLabel.width, floatRangeAccuracyLabel.height);


			Rect UnitStandBase = new Rect(140, 200, 50, 30);
			Rect TotalCost = new Rect(325, 50, 450, 20);
			Rect numberProjectiles = new Rect(TotalCost.x, TotalCost.y + TotalCost.height + 5, TotalCost.width, TotalCost.height);
			Rect duration = new Rect(numberProjectiles.x, numberProjectiles.y + numberProjectiles.height + 5, numberProjectiles.width, numberProjectiles.height);

			Rect ResetButton = new Rect(700-2, 100, 100, 30);
			Rect DeleteButton = new Rect(ResetButton.x, ResetButton.y + ResetButton.height + 5, ResetButton.width, ResetButton.height);
			Rect PointRefButton = new Rect(DeleteButton.x, DeleteButton.y + DeleteButton.height + 5, DeleteButton.width, DeleteButton.height);


			//Up here to make sure it goes behind other layers
			if (selectedSupport != null)
			{
				DrawFireSupportBox(10, 230, 30);
			}

			Widgets.DrawMenuSection(new Rect(0, 0, 800, 225));

			//If firesupport is not selected
			if (Widgets.CustomButtonText(ref SelectionBar, selectedText, Color.gray, Color.white, Color.black))
			{


				List<FloatMenuOption> supports = new List<FloatMenuOption>();

				//Option to create new firesupport
				supports.Add(new FloatMenuOption("Create New Fire Support", delegate {
					MilitaryFireSupport newFireSupport = new MilitaryFireSupport();
					newFireSupport.name = "New Fire Support " + (util.fireSupportDefs.Count() + 1);
					newFireSupport.setLoadID();
					newFireSupport.projectiles = new List<ThingDef>();
					selectedText = newFireSupport.name;
					selectedSupport = newFireSupport;
					util.fireSupportDefs.Add(newFireSupport);

				}));

				//Create list of selectable firesupports
				foreach (MilitaryFireSupport support in util.fireSupportDefs)
				{
					supports.Add(new FloatMenuOption(support.name, delegate {
						//Unit is selected
						selectedText = support.name;
						selectedSupport = support;
					}));
				}
				FloatMenu selection = new FloatMenu(supports);
				Find.WindowStack.Add(selection);
			}



			//if firesupport is selected
			if (selectedSupport != null)
			{
				//Need to adjust
				fireSupportMaxScroll = selectedSupport.projectiles.Count() * projectileBoxHeight - 10 * projectileBoxHeight;

				Text.Anchor = TextAnchor.MiddleLeft;
				Text.Font = GameFont.Small;




				if (settlementPointReference != null)
				{
					Widgets.Label(TotalCost, "Total Fire Support Silver Cost: " + selectedSupport.returnTotalCost() + " / " + FactionColonies.calculateMilitaryLevelPoints(settlementPointReference.settlementMilitaryLevel) + " (Max Cost)");
				}
				else
				{
					Widgets.Label(TotalCost, "Total Fire Support Silver Cost: " + selectedSupport.returnTotalCost() + " / " + "No Reference");
				}
				Widgets.Label(numberProjectiles, "Number of Projectiles: " + selectedSupport.projectiles.Count().ToString());
				Widgets.Label(duration, "Duration of fire support: " + Math.Round((double)selectedSupport.projectiles.Count() * .25, 2).ToString() + " seconds");
				Widgets.Label(floatRangeAccuracyLabel, selectedSupport.accuracy + " = Accuracy of fire support (In tiles radius): Affecting cost by : " + selectedSupport.returnAccuracyCostPercentage() + "%"  );
				selectedSupport.accuracy = Widgets.HorizontalSlider(floatRangeAccuracy, selectedSupport.accuracy, Math.Max(3, (15 - Find.World.GetComponent<FactionFC>().returnHighestMilitaryLevel())), 30, roundTo: 1);
				Text.Font = GameFont.Tiny;
				Text.Anchor = TextAnchor.UpperCenter;



				//Unit Name
				selectedSupport.name = Widgets.TextField(nameTextField, selectedSupport.name);

				if (Widgets.ButtonText(ResetButton, "Reset to Default"))
				{
					selectedSupport.projectiles = new List<ThingDef>();
				}

				if (Widgets.ButtonText(DeleteButton, "Delete Support"))
				{
					selectedSupport.delete();
					util.checkMilitaryUtilForErrors();
					selectedSupport = null;
					selectedText = "Select A Fire Support";

					//Reset Text anchor and font
					Text.Font = fontBefore;
					Text.Anchor = anchorBefore;
					return;
				}

				if (Widgets.ButtonText(PointRefButton, "Set Point Ref"))
				{
					List<FloatMenuOption> settlementList = new List<FloatMenuOption>();

					foreach (SettlementFC settlement in Find.World.GetComponent<FactionFC>().settlements)
					{
						settlementList.Add(new FloatMenuOption(settlement.name + " - Military Level : " + settlement.settlementMilitaryLevel, delegate
						{
							//set points
							settlementPointReference = settlement;
						}));
					}

					if (settlementList.Count() == 0)
					{
						settlementList.Add(new FloatMenuOption("No Valid Settlements", null));
					}
					FloatMenu floatMenu = new FloatMenu(settlementList);
					floatMenu.vanishIfMouseDistant = true;
					Find.WindowStack.Add(floatMenu);

				}

				






				//Reset Text anchor and font
				Text.Font = fontBefore;
				Text.Anchor = anchorBefore;
			}

			if (Event.current.type == EventType.ScrollWheel)
			{

				scrollWindow(Event.current.delta.y, fireSupportMaxScroll);
			}

			//Reset Text anchor and font
			Text.Font = fontBefore;
			Text.Anchor = anchorBefore;
		}

		private void scrollWindow(float num, float maxScroll)
		{
			if (scroll - num * 5 < -1 * maxScroll)
			{
				scroll = -1f * maxScroll;
			}
			else if (scroll - num * 5 > 0)
			{
				scroll = 0;
			}
			else
			{
				scroll -= (int)Event.current.delta.y * 5;
			}
			Event.current.Use();

			//Log.Message(scroll.ToString());
		}

		public Rect deriveRectRow(Rect rect, float x, float y = 0, float width = 0, float height = 0)
		{
			float inputWidth;
			float inputHeight;
			if (width == 0)
			{
				inputWidth = rect.width;
			}
			else
			{
				inputWidth = width;
			}
			if (height == 0)
			{
				inputHeight = rect.height;
			}
			else
			{
				inputHeight = height;
			}

			Rect newRect = new Rect(rect.x + rect.width + x, rect.y + y, inputWidth, inputHeight);
			//Log.Message(newRect.width.ToString());
			return newRect;
		}
		public void DrawFireSupportBox(float x, float y, float rowHeight)
		{
			//Set Text anchor and font
			GameFont fontBefore = Text.Font;
			TextAnchor anchorBefore = Text.Anchor;
			Text.Anchor = TextAnchor.MiddleCenter;
			Text.Font = GameFont.Small;




			for (int i = 0; i <= selectedSupport.projectiles.Count(); i++)
			{
				//Declare Rects
				Rect text = new Rect(x + 2, y + 2 + i*rowHeight + scroll, rowHeight - 4, rowHeight - 4);
				Rect cost = deriveRectRow(text, 2, 0, 150);
				Rect icon = deriveRectRow(cost, 2, 0, 250);
				//Rect name = deriveRectRow(icon, 2, 0, 150);
				Rect options = deriveRectRow(icon, 2, 0, 74);
				Rect upArrow = deriveRectRow(options, 12, 0, rowHeight - 4, rowHeight - 4);
				Rect downArrow = deriveRectRow(upArrow, 4);
				Rect delete = deriveRectRow(downArrow, 12);
				//Create outside box last to encapsulate entirety
				Rect box = new Rect(x, y + i * rowHeight + scroll, delete.x + delete.width + 4 - x, rowHeight);



				Widgets.DrawHighlight(box);
				Widgets.DrawMenuSection(box);

				if (i == selectedSupport.projectiles.Count())
				{ //If on last row
					Text.Anchor = TextAnchor.MiddleCenter;
					Widgets.Label(text, i.ToString());
					if(Widgets.ButtonTextSubtle(icon, "Add new projectile"))
					{ //if creating new projectile
						List<FloatMenuOption> thingOptions = new List<FloatMenuOption>();
						foreach (ThingDef def in selectedSupport.returnFireSupportOptions())
						{
							thingOptions.Add(new FloatMenuOption(def.LabelCap + " - " + Math.Round(def.BaseMarketValue * 1.5, 2).ToString(), delegate 
							{
								selectedSupport.projectiles.Add(def);
							}, def));
						}
						if (thingOptions.Count() == 0)
						{
							thingOptions.Add(new FloatMenuOption("No available projectiles found", delegate { }));
						}
						Find.WindowStack.Add(new FloatMenu(thingOptions));
					}
				}
				else
				{ //if on row with projectile
					Text.Anchor = TextAnchor.MiddleCenter;
					Widgets.Label(text, i.ToString());
					if(Widgets.ButtonTextSubtle(icon, ""))
					{
						List<FloatMenuOption> thingOptions = new List<FloatMenuOption>();
						foreach (ThingDef def in selectedSupport.returnFireSupportOptions())
						{
							int k = i;
							thingOptions.Add(new FloatMenuOption(def.LabelCap + " - " + Math.Round(def.BaseMarketValue * 1.5, 2).ToString(), delegate
							{
								selectedSupport.projectiles[k] = def;
							}, def));
						}
						if (thingOptions.Count() == 0)
						{
							thingOptions.Add(new FloatMenuOption("No available projectiles found", delegate { }));
						}
						Find.WindowStack.Add(new FloatMenu(thingOptions));
					}
					Text.Anchor = TextAnchor.MiddleCenter;
					Widgets.Label(cost, "$ " + (Math.Round(selectedSupport.projectiles[i].BaseMarketValue * 1.5, 2)).ToString());//ADD in future mod setting for firesupport cost

					Widgets.DefLabelWithIcon(icon, selectedSupport.projectiles[i]);
					if (Widgets.ButtonTextSubtle(options, "Options"))
					{ //If clicked options button
						int k = i;
						List<FloatMenuOption> listOptions = new List<FloatMenuOption>();
						listOptions.Add(new FloatMenuOption("Insert Projectile Above Slot", delegate 
						{
							List<FloatMenuOption> thingOptions = new List<FloatMenuOption>();
							foreach (ThingDef def in selectedSupport.returnFireSupportOptions())
							{
								thingOptions.Add(new FloatMenuOption(def.LabelCap + " - " + Math.Round(def.BaseMarketValue*1.5, 2).ToString(), delegate
								{
									Log.Message("insert at " + k.ToString());
									selectedSupport.projectiles.Insert(k, def);
								}, def));
							}
							if (thingOptions.Count() == 0)
							{
								thingOptions.Add(new FloatMenuOption("No available projectiles found", delegate { }));
							}
							Find.WindowStack.Add(new FloatMenu(thingOptions));
						}));
						listOptions.Add(new FloatMenuOption("Duplicate", delegate
						{
							ThingDef tempDef = selectedSupport.projectiles[k];
							List<FloatMenuOption> thingOptions = new List<FloatMenuOption>();

							thingOptions.Add(new FloatMenuOption("1x", delegate
								{
									for (int l = 0; l < 1; l++)
									{
										if (k == selectedSupport.projectiles.Count() - 1)
										{
											selectedSupport.projectiles.Add(tempDef);
										}
										else
										{
											selectedSupport.projectiles.Insert(k + 1, tempDef);
										}
									}
								}));
							thingOptions.Add(new FloatMenuOption("5x", delegate
							{
								for (int l = 0; l < 5; l++)
								{
									if (k == selectedSupport.projectiles.Count() - 1)
									{
										selectedSupport.projectiles.Add(tempDef);
									}
									else
									{
										selectedSupport.projectiles.Insert(k + 1, tempDef);
									}
								}
							}));
							thingOptions.Add(new FloatMenuOption("10x", delegate
							{
								for (int l = 0; l < 10; l++)
								{
									if (k == selectedSupport.projectiles.Count() - 1)
									{
										selectedSupport.projectiles.Add(tempDef);
									}
									else
									{
										selectedSupport.projectiles.Insert(k + 1, tempDef);
									}
								}
							}));
							thingOptions.Add(new FloatMenuOption("20x", delegate
							{
								for (int l = 0; l < 20; l++)
								{
									if (k == selectedSupport.projectiles.Count() - 1)
									{
										selectedSupport.projectiles.Add(tempDef);
									}
									else
									{
										selectedSupport.projectiles.Insert(k + 1, tempDef);
									}
								}
							}));
							thingOptions.Add(new FloatMenuOption("50x", delegate
							{
								for (int l = 0; l < 50; l++)
								{
									if (k == selectedSupport.projectiles.Count() - 1)
									{
										selectedSupport.projectiles.Add(tempDef);
									}
									else
									{
										selectedSupport.projectiles.Insert(k + 1, tempDef);
									}
								}
							}));
							Find.WindowStack.Add(new FloatMenu(thingOptions));
						}));
						Find.WindowStack.Add(new FloatMenu(listOptions));
						
					}
					if (Widgets.ButtonTextSubtle(upArrow, ""))
					{ //if click up arrow button
						if (i != 0)
						{
							ThingDef temp = selectedSupport.projectiles[i];
							selectedSupport.projectiles[i] = selectedSupport.projectiles[i - 1];
							selectedSupport.projectiles[i - 1] = temp;
						}
					}
					Text.Anchor = TextAnchor.MiddleCenter;
					Widgets.Label(upArrow, "^");
					if (Widgets.ButtonTextSubtle(downArrow, ""))
					{ //if click down arrow button
						if (i != selectedSupport.projectiles.Count() - 1)
						{
							ThingDef temp = selectedSupport.projectiles[i];
							selectedSupport.projectiles[i] = selectedSupport.projectiles[i + 1];
							selectedSupport.projectiles[i + 1] = temp;
						}
					}
					Text.Anchor = TextAnchor.MiddleCenter;
					Widgets.Label(downArrow, "v");
					if (Widgets.ButtonTextSubtle(delete, ""))
					{ //if click delete  button
						selectedSupport.projectiles.RemoveAt(i);
					}
					Text.Anchor = TextAnchor.MiddleCenter;
					Widgets.Label(delete, "X");

				}


			}



			//Reset Text anchor and font
			Text.Font = fontBefore;
			Text.Anchor = anchorBefore;
		}


	}

}
