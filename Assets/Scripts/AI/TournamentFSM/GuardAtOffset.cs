using AIGame.Core;
using AIGame.Examples.FSM;
using UnityEngine;

namespace AIGame.TournamentFSM
{
    // Anchors the agent at (CP + offset). Uses MoveToPosition’s arrival logic.
    public class GuardAtOffset : MoveToPosition
    {
        private readonly Vector3 localOffset;
        private readonly float reCenterInterval = 0.75f;   // small nudge cadence
        private readonly float reCenterThreshold = 1.0f;   // re-center if we drift > 1m
        private float nextReCenterAt;

        public GuardAtOffset(FinitStateAI parent, Vector3 offset, params AIState[] subStates)
            : base(parent, "GuardAtOffset", subStates)
        {
            localOffset = offset;
        }

        private Vector3 DesiredAnchor()
        {
            var cp = GameManager.Instance.Objective.transform.position; // CP center
            return cp + localOffset;
        }

        public override void Enter()
        {
            hasReachedDestination = false;
            currentDestination = DesiredAnchor();
            parent.MoveTo(currentDestination);
            nextReCenterAt = Time.time + reCenterInterval * Random.Range(0.9f, 1.1f); // de-sync agents
            base.Enter();
        }

        public override void Execute()
        {
            // Light, periodic re-center to keep position tight without fighting combat substates
            if (Time.time >= nextReCenterAt)
            {
                var desired = DesiredAnchor();
                if (Vector3.Distance(parent.transform.position, desired) > reCenterThreshold && !parent.IsPathPending())
                {
                    currentDestination = desired;
                    parent.MoveTo(currentDestination);
                    hasReachedDestination = false;
                }
                nextReCenterAt = Time.time + reCenterInterval;
            }

            base.Execute(); // arrival checks + run substates
        }
    }
}
