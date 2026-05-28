using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace GOAP
{
    public class ActionStandGuard : Action
    {
        [SerializeField] float boredomMin = 20f;
        [SerializeField] float boredomMax = 45f;

        [SerializeField] float rotateAngle = 0f;
        [SerializeField] float rotateSpeed = 30f;
        [SerializeField] float rotateHoldTime = 1f;

        float boredomTime = 0f;

        Vector3 guardPosition;
        Vector3 guardDirection;

        private void Start()
        {
            guardPosition = transform.position;
            guardDirection = transform.forward;
        }

        public override bool ExecuteAction()
        {
            if (boredomTime < 0 && (agent.transform.position - guardPosition).magnitude <= 0.25f)
            {
                boredomTime = Time.time + Mathf.Lerp(boredomMin, boredomMax, UnityEngine.Random.value);
            }

            if (boredomTime > 0)
            {
                agent.transform.rotation = Quaternion.RotateTowards(agent.transform.rotation, Quaternion.LookRotation(guardDirection), Time.fixedDeltaTime * agent.angularSpeed);

                if (boredomTime < Time.time)
                {
                    return true;
                }
            }

            return false;
        }

        public override void PostExecute()
        {
        }

        public override void PreExecute()
        {
            brain.State = AgentState.Goto;
            agent.GetComponent<AgentLocomotion>().SetDestination(guardPosition);
            boredomTime = -1f;
        }

        public void UpdateGuardPosition(Vector3 guardPosition, Vector3 guardDirection)
        {
            this.guardPosition = guardPosition;
            this.guardDirection = guardDirection;
        }
    }

}
