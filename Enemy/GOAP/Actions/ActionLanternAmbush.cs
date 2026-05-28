using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace GOAP
{
    public class ActionLanternAmbush : ActionLanternGotoNearest
    {
        float nextDestinationCheck = 0f;

        public override bool ExecuteAction()
        {
            var pos = targetLantern.transform.position;

            agent.SetDestination(pos);
            Vector3 vec = pos - agent.transform.position;
            vec.y = 0;
            if (vec.magnitude <= desiredDistance)
            {
                return true;
            }

            nextDestinationCheck -= Time.fixedDeltaTime;
            if (nextDestinationCheck <= 0)
            {
                nextDestinationCheck = 1f;

                var playerPosition = Player.instance.transform.position;
                var dir = playerPosition - pos;
                if ((dir.magnitude <= occupier.AmbushRange && !Physics.Raycast(pos, dir, dir.magnitude, LayerMask.GetMask("Default", "Terrain"), QueryTriggerInteraction.Ignore))
                    || dir.magnitude <= occupier.AmbushMinimumRange)
                {
                    var lantern = occupier.FindEmptyLanternForAmbush(playerPosition);
                    if (lantern != null)
                    {
                        occupier.ReserveLantern(lantern);
                    }
                    else
                    {
                        //Debug.LogWarning("Failed to find empty lantern for ambush!");
                    }
                }
            }

            return false;
        }

        public override void PreExecute()
        {
            brain.State = AgentState.Goto;
            // brain.WorldStateMemory["near_lantern"] = 0;
            nextDestinationCheck = 1f;

            // not supposed to hardcode player but whatever
            var lantern = occupier.FindEmptyLanternForAmbush(Player.instance.transform.position);
            if (lantern != null)
            {
                occupier.ReserveLantern(lantern);
            }
            else
            {
                //Debug.LogWarning("Failed to find empty lantern for ambush!");
                // brain.WorldStateMemory["hibernate"] = 1;
            }
        }

    }

}
