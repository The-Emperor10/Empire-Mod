using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace FactionColonies
{
    public class LordJob_SquadStandby : LordJob
    {
        private IntVec3? location;
        public LordJob_SquadStandby(IntVec3? location = null)
        {
            this.location = location;
        }

        public override StateGraph CreateGraph()
        {
            IntVec3 defendLocation;
            if (this.location.HasValue)
            {
                defendLocation = this.location.Value;
            }
            else
            {
                defendLocation = Map.areaManager.Home.TrueCount > 0 ? Map.areaManager.Home.ActiveCells.RandomElement() :
                    Map.Center;
            }
            
            StateGraph stateGraph = new StateGraph();
            LordToil toil = new LordToil_DefendPoint(false);
            LordToilData_DefendPoint data = (LordToilData_DefendPoint)toil.data;
            data.defendPoint = defendLocation;
            data.defendRadius = 28f;
            data.wanderRadius = 8f;
            
            stateGraph.AddToil(toil);
            return stateGraph;
        }
    }

    public class LordJob_SquadAttack : LordJob
    {
        private IntVec3? location;
        public LordJob_SquadAttack(IntVec3? location = null)
        {
            this.location = location;
        }

        public override StateGraph CreateGraph()
        {
            IntVec3 defendLocation;
            if (this.location.HasValue)
            {
                defendLocation = this.location.Value;
            }
            else
            {
                defendLocation = Map.areaManager.Home.TrueCount > 0 ? Map.areaManager.Home.ActiveCells.RandomElement() :
                    Map.Center;
            }
            
            StateGraph graph = new StateGraph();
            if (Map.areaManager.Home.TrueCount > 0)
                defendLocation = Map.areaManager.Home.ActiveCells.RandomElement();
            else
                defendLocation = Map.Center;
            
            LordToil toil = new LordToil_HuntEnemies(defendLocation);
            graph.AddToil(toil);
            return graph;
        }
    }
}