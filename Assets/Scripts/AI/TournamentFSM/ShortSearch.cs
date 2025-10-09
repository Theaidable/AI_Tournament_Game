using AIGame.Core;
using AIGame.Examples.FSM;
using System;
using UnityEngine;

namespace AIGame.TournamentFSM
{
    // Brief in-place scan after losing enemies. If nothing is found, we signal timeout.
    public class ShortSearch : AIState
    {
        private readonly float scanDuration;
        private readonly float microInterval = 0.5f;
        private float endAt, nextMicroAt;
        private readonly Action onTimeout;

        public ShortSearch(FinitStateAI parent, Action onTimeout, float scanSeconds = 1.0f, params AIState[] subStates)
            : base(parent, "ShortSearch", subStates)
        {
            this.onTimeout = onTimeout;
            this.scanDuration = scanSeconds;
        }

        public override void Enter()
        {
            endAt = Time.time + scanDuration;
            nextMicroAt = 0f; // do a micro move asap
            base.Enter();
        }

        public override void Execute()
        {
            // Small strafes to change facing and re-acquire targets
            if (Time.time >= nextMicroAt)
            {
                var dir = (UnityEngine.Random.value > 0.5f ? parent.transform.right : -parent.transform.right);
                parent.StrafeTo(parent.transform.position + dir * 0.75f);
                nextMicroAt = Time.time + microInterval;
            }

            // Give up after a short scan
            if (Time.time >= endAt)
            {
                onTimeout?.Invoke();
                return; // let the AI switch state this frame
            }

            base.Execute();
        }
    }
}
