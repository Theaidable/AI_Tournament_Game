using AIGame.Core;
using AIGame.Examples.FSM;
using UnityEngine;

namespace AIGame.TournamentFSM
{
    // Holds near CP center with tiny scan movement to keep awareness high.
    public class HoldCenterAndLookAround : MoveToPosition
    {
        private float nextScanAt;
        private readonly float scanInterval = 0.6f;  // small cadence for micro-movement
        private readonly float strafeDistance = 0.75f;

        public HoldCenterAndLookAround(FinitStateAI parent, params AIState[] subStates)
            : base(parent, "HoldCenter", subStates)
        {
        }

        private Vector3 PickCenterPoint()
        {
            // Slight jitter so multiple mids don’t stack perfectly
            var cp = GameManager.Instance.Objective.transform.position;
            var jitter = UnityEngine.Random.insideUnitCircle * 1.25f;
            return cp + new Vector3(jitter.x, 0f, jitter.y);
        }

        public override void Enter()
        {
            hasReachedDestination = false;
            currentDestination = PickCenterPoint();
            parent.MoveTo(currentDestination);
            nextScanAt = Time.time + scanInterval * Random.Range(0.9f, 1.1f);
            base.Enter();
        }

        public override void Execute()
        {
            // After arrival, add tiny strafes/turns to avoid tunnel vision
            if (hasReachedDestination && Time.time >= nextScanAt)
            {
                // Micro-strafe left/right
                var dir = (Random.value > 0.5f ? parent.transform.right : -parent.transform.right);
                parent.StrafeTo(parent.transform.position + dir * strafeDistance);

                nextScanAt = Time.time + scanInterval;
            }

            base.Execute();
        }
    }
}
