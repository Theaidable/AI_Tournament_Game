using System;
using System.Collections.Generic;
using UnityEngine;
using AIGame.Core;

namespace FiniteStateMachine
{
    // Conditions for vores FSM AI
    public enum AICondition
    {
        None,
        Spawned,
        SeesEnemy,
        MoveToObjective,
        Protect,
        Investigate,
        PowerUp,
    }

    // Base klasse for vores states
    public abstract class AIState
    {
        protected FMS parent;
        public string Name { get; }
        private readonly List<AIState> subStates = new();

        protected AIState(FMS parent, string name, params AIState[] subs)
        {
            this.parent = parent;
            Name = name;
            if (subs != null) subStates.AddRange(subs);
        }

        public virtual void Enter()
        {
            foreach (var s in subStates) s.Enter();
        }

        public virtual void Exit()
        {
            foreach (var s in subStates) s.Exit();
        }

        public virtual void Execute()
        {
            foreach (var s in subStates) s.Execute();
        }
    }

    // Idle state
    public class Idle : AIState
    {
        public Idle(FMS parent) : base(parent, "Idle") { }
    }

    // Base move state
    public abstract class MoveToPosition : AIState
    {
        public event Action DestinationReached;
        protected Vector3 currentDestination;
        protected bool hasReachedDestination;
        protected const float ARRIVAL_THRESHOLD = 0.5f;

        protected MoveToPosition(FMS parent, string name, params AIState[] subs)
            : base(parent, name, subs) { }

        public override void Execute()
        {
            if (!hasReachedDestination)
            {
                if (Vector3.Distance(parent.transform.position, currentDestination) <= ARRIVAL_THRESHOLD)
                {
                    hasReachedDestination = true;
                    DestinationReached?.Invoke();
                }
            }
            base.Execute();
        }
    }

    // Dodge state
    public class Dodge : AIState
    {
        private Ball lastBall;
        public Dodge(FMS parent) : base(parent, "Dodge") { }

        public void OnBallDetected(Ball ball)
        {
            lastBall = ball;
            if (lastBall == null || !parent.CanDodge()) return;

            Vector3 ballDir = (lastBall.transform.position - parent.transform.position).normalized;
            Vector3 dodgeDir = Vector3.Cross(ballDir, Vector3.up).normalized; // sidestep
            parent.StartDodge(dodgeDir);
        }
    }

    // Follow/Attack state
    public class Combat : AIState
    {
        private bool stopped = false;
        public Combat(FMS parent, params AIState[] subs) : base(parent, "Combat", subs) { }

        public override void Execute()
        {
            if (!parent.TryGetTarget(out var target))
            {
                // If engine supports it, try to (re)acquire; else bail to objective
                parent.RefreshOrAcquireTarget();
                if (!parent.TryGetTarget(out target))
                {
                    parent.FSM_SetCondition(AICondition.MoveToObjective);
                    return;
                }
            }

            Vector3 myPos = parent.transform.position;
            Vector3 tgtPos = target.Position;

            float dist = Vector3.Distance(myPos, tgtPos);
            float desired = parent.ProjectileRange;
            float buffer = 0.25f;

            if (dist > desired + buffer)
            {
                Vector3 dir = (tgtPos - myPos).normalized;
                Vector3 stopPos = tgtPos - dir * (desired - 3f);
                parent.MoveTo(stopPos);
                stopped = false;
            }
            else if (!stopped)
            {
                parent.StopMoving();
                stopped = true;
            }

            if (stopped) parent.ThrowBallAt(target);
            base.Execute();
        }
    }

    // Move to objective state
    public class MoveToObjective : MoveToPosition
    {
        public MoveToObjective(FMS parent, params AIState[] subs)
            : base(parent, "MoveToObjective", subs) { }

        public override void Enter()
        {
            hasReachedDestination = false;
            var cp = ControlPoint.Instance;
            if (cp != null)
            {
                currentDestination = cp.transform.position;
                parent.MoveTo(currentDestination);
            }
            base.Enter();
        }
    }

    // Protect objective state
    public class ProtectObjective : AIState
    {
        public ProtectObjective(FMS parent, params AIState[] subs) : base(parent, "Protect", subs) { }

        public override void Enter()
        {
            parent.StopMoving();
            base.Enter();
        }

        public override void Execute()
        {
            var cp = ControlPoint.Instance;
            if (cp != null)
            {
                float dist = Vector3.Distance(parent.transform.position, cp.transform.position);
                if (cp.OTActive || dist > 6f)
                {
                    parent.MoveTo(cp.transform.position);
                }
            }
            base.Execute();
        }
    }

    // Visit power-up state
    public class MoveToPowerUp : MoveToPosition
    {
        private int targetPowerUpId = -1;

        public MoveToPowerUp(FMS parent, params AIState[] subs) : base(parent, "MoveToPowerUp", subs) { }

        public override void Enter()
        {
            hasReachedDestination = false;

            // Pick first visible powerup
            var powerUps = parent.GetVisiblePowerUpsSnapshot();
            if (powerUps.Count > 0)
            {
                targetPowerUpId = powerUps[0].Id;
                currentDestination = powerUps[0].Position;
                parent.MoveTo(currentDestination);
            }
            else
            {
                // Nothing to do
                parent.FSM_SetCondition(AICondition.MoveToObjective);
            }
            base.Enter();
        }

        public override void Execute()
        {
            base.Execute();
            if (hasReachedDestination && targetPowerUpId != -1)
            {
                parent.EatPowerUp(targetPowerUpId);
                parent.FSM_SetCondition(AICondition.MoveToObjective);
            }
        }

        public override void Exit()
        {
            targetPowerUpId = -1;
            base.Exit();
        }
    }

    // Reposition state
    public class InvestigateShot : MoveToPosition
    {
        private Ball lastBall;

        public InvestigateShot(FMS parent) : base(parent, "InvestigateShot") { }

        public void OnBallDetected(Ball ball) => lastBall = ball;

        public override void Enter()
        {
            hasReachedDestination = false;

            if (parent.GetVisibleEnemiesSnapshot().Count == 0 && lastBall != null)
            {
                Vector3 me = parent.transform.position;
                Vector3 dirFromBall = (me - lastBall.transform.position).normalized;
                Vector3 perp = Vector3.Cross(dirFromBall, Vector3.up).normalized;
                currentDestination = me + (perp + dirFromBall) * 8f; // step aside & back
                parent.MoveTo(currentDestination);
            }
            else
            {
                parent.FSM_SetCondition(AICondition.MoveToObjective);
            }
            base.Enter();
        }

        public override void Execute()
        {
            base.Execute();
            if (hasReachedDestination)
            {
                parent.FSM_SetCondition(AICondition.MoveToObjective);
            }
        }
    }


}