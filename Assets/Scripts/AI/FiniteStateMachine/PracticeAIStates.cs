using AIGame.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Practice.AI
{
    public abstract class PracticeAIState
    {
        /// <summary>
        /// The owning AI instance.
        /// </summary>
        protected PracticeFSMAI parent;

        /// <summary>
        /// Display name for debugging.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Optional nested substates that run with this state.
        /// </summary>
        protected List<PracticeAIState> subStates;

        /// <summary>
        /// Creates a new state.
        /// </summary>
        /// <param name="parent">Owning AI.</param>
        /// <param name="name">Debug name.</param>
        /// <param name="substates">Optional substates.</param>
        public PracticeAIState(PracticeFSMAI parent, string name, params PracticeAIState[] substates)
        {
            this.parent = parent;
            this.Name = name;
            this.subStates = substates?.ToList();
        }

        /// <summary>
        /// Called when entering this state.
        /// Invokes <see cref="Enter"/> on all substates.
        /// </summary>
        public virtual void Enter()
        {
            if (subStates != null)
            {
                foreach (var s in subStates) s.Enter();
            }
        }

        /// <summary>
        /// Called when exiting this state.
        /// Invokes <see cref="Exit"/> on all substates.
        /// </summary>
        public virtual void Exit()
        {
            if (subStates != null)
            {
                foreach (var s in subStates) s.Exit();
            }
        }

        /// <summary>
        /// Called every update while this state is active.
        /// Invokes <see cref="Execute"/> on all substates.
        /// </summary>
        public virtual void Execute()
        {
            if (subStates != null)
            {
                foreach (var s in subStates) s.Execute();
            }
        }
    }

    #region IdleState
    /// <summary>
    /// A no-op idle state.
    /// </summary>
    public class Idle : PracticeAIState
    {
        /// <summary>
        /// Creates an idle state.
        /// </summary>
        /// <param name="parent">Owning AI.</param>
        public Idle(PracticeFSMAI parent) : base(parent, "Idle") { }
    }
    #endregion

    #region MoveToPosition
    /// <summary>
    /// Base state for moving to a world position, raising an event on arrival.
    /// </summary>
    public abstract class MoveToPosition : PracticeAIState
    {
        /// <summary>
        /// Raised exactly once when the destination is reached.
        /// </summary>
        public event Action DestinationReached;

        /// <summary>
        /// Current target world position.
        /// </summary>
        protected Vector3 currentDestination;

        /// <summary>
        /// True after arrival is detected.
        /// </summary>
        protected bool hasReachedDestination = false;

        /// <summary>
        /// Distance threshold to consider arrival.
        /// </summary>
        protected const float ARRIVAL_THRESHOLD = 0.5f;

        /// <summary>
        /// Creates a move state.
        /// </summary>
        /// <param name="parent">Owning AI.</param>
        /// <param name="name">Debug name.</param>
        /// <param name="substates">Optional substates.</param>
        public MoveToPosition(PracticeFSMAI parent, string name, params PracticeAIState[] substates)
            : base(parent, name, substates) { }

        /// <inheritdoc/>
        public override void Enter()
        {
            hasReachedDestination = false;
            base.Enter();
        }

        /// <inheritdoc/>
        public override void Execute()
        {
            // CHANGE: Removed any direct NavMeshAgent checks per patch.
            // Arrival is determined purely by world-space distance + helper movement methods.
            if (parent.IsAlive == false)
            {
                base.Execute();
                return;
            }

            if (hasReachedDestination == false)
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
    #endregion

    #region MoveToObjective (Arrowhead formation)
    /// <summary>
    /// Moves near the current match objective.
    /// </summary>
    public class MoveToObjective : MoveToPosition
    {
        /// <summary>
        /// Creates the state.
        /// </summary>
        /// <param name="parent">Owning AI.</param>
        /// <param name="substates">Optional substates.</param>
        public MoveToObjective(PracticeFSMAI parent, params PracticeAIState[] substates)
            : base(parent, "MoveToObjective", substates) { }

        /// <inheritdoc/>
        public override void Enter()
        {
            hasReachedDestination = false;

            Vector3 obj = GameManager.Instance.Objective.transform.position;

            currentDestination = parent.GetEngageFormationSlot(obj);

            parent.MoveTo(currentDestination);

            base.Enter();
        }

        //------ LOOK HERE IF ANY PROBLEMS -> DELETE THIS TO GO BACK -----
        public override void Execute()
        {
            Vector3 obj = GameManager.Instance.Objective.transform.position;

            Vector3 desired = parent.GetEngageFormationSlot(obj);

            if ((desired - currentDestination).sqrMagnitude > 0.25f)
            {
                currentDestination = desired;
                parent.MoveTo(currentDestination);
            }

            base.Execute();
        }
    }
    #endregion

    #region Combat
    /// <summary>
    /// State for engaging visible enemies.
    /// </summary>
    public class Combat : PracticeAIState
    {
        /// <summary>
        /// Raised when there are no visible enemies.
        /// </summary>
        public event Action NoMoreEnemies;

        /// <summary>
        /// Creates the state.
        /// </summary>
        /// <param name="parent">Owning AI.</param>
        /// <param name="substates">Optional substates.</param>
        public Combat(PracticeFSMAI parent, params PracticeAIState[] substates)
            : base(parent, "Combat", substates) { }

        /// <inheritdoc/>
        public override void Enter()
        {
            // CHANGE: Replace direct NavMeshAgent isStopped with helper.
            parent.StopMoving();

            if (parent.GetVisibleEnemiesSnapshot().Count == 0)
            {
                return;
            }

            parent.RefreshOrAcquireTarget();

            // CHANGE: We already called StopMoving(); no direct agent access needed here.
            base.Enter();
        }

        /// <inheritdoc/>
        public override void Execute()
        {
            var seen = parent.GetVisibleEnemiesSnapshot();

            if (seen.Count == 0)
            {
                NoMoreEnemies?.Invoke();
                return;
            }

            // Track motion to detect recent dodges (for scoring)
            parent.TrackEnemyMotion(seen);

            // Choose a priority target (recently dodged + cluster potential)
            var pick = parent.ChoosePriorityTarget(seen);

            if (pick.Id != 0)
            {
                parent.TrySetTargetById(pick.Id);

                // improved strafe + fire
                parent.StrafeAround(pick.Position);
                float d = Vector3.Distance(parent.transform.position, pick.Position);
                if (d <= parent.ProjectileRange) parent.ThrowBallAt(pick);
            }
            else
            {
                NoMoreEnemies?.Invoke();
            }

            base.Execute();
        }

        /// <inheritdoc/>
        public override void Exit()
        {
            parent.RemoveTarget();
            base.Exit();
        }
    }
    #endregion

    #region ProtectObjective
    /// <summary>
    /// Patrols around the objective by moving to random nearby offsets.
    /// </summary>
    public class ProtectObjective : PracticeAIState
    {
        /// <summary>
        /// Current patrol destination.
        /// </summary>
        private Vector3 currentDestination;

        /// <summary>
        /// Arrival threshold for patrol hops.
        /// </summary>
        private const float ARRIVAL_THRESHOLD = 0.5f;

        /// <summary>
        /// Whether a destination has been set.
        /// </summary>
        private bool hasDestination = false;

        /// <summary>
        /// Creates the state.
        /// </summary>
        /// <param name="parent">Owning AI.</param>
        /// <param name="substates">Optional substates.</param>
        public ProtectObjective(PracticeFSMAI parent, params PracticeAIState[] substates)
            : base(parent, "ProtectObjective", substates) { }

        /// <inheritdoc/>
        public override void Execute()
        {
            Vector3 cp = GameManager.Instance.Objective.transform.position;
            Vector3 desired = parent.GetProtectFormationSlot(cp);

            // CHANGE: Removed NavMeshAgent.pathPending/remainingDistance checks.
            // We rely on world-distance + helper MoveTo().
            bool needNew =
                !hasDestination ||
                Vector3.Distance(currentDestination, desired) > 0.5f ||
                Vector3.Distance(parent.transform.position, currentDestination) <= ARRIVAL_THRESHOLD;

            if (needNew)
            {
                currentDestination = desired;
                parent.MoveTo(currentDestination);
                hasDestination = true;
            }

            base.Execute();
        }

        /// <inheritdoc/>
        public override void Exit()
        {
            hasDestination = false;
            base.Exit();
        }
    }

    #endregion

    #region Strafe

    /// <summary>
    /// Strafes left/right around current position.
    /// </summary>
    public class Strafe : PracticeAIState
    {
        /// <summary>
        /// Current strafe direction flag.
        /// </summary>
        private bool movingRight = true; // kept for potential future toggles

        /// <summary>
        /// Creates the state.
        /// </summary>
        /// <param name="parent">Owning AI.</param>
        public Strafe(PracticeFSMAI parent) : base(parent, "Strafe") { }

        /// <inheritdoc/>
        public override void Execute()
        {
            if (parent.TryGetTarget(out var t))
            {
                parent.StrafeAround(t.Position); // keeps facing + oscillates sideways
            }
            base.Execute();
        }
    }

    #endregion

    #region Dodge
    /// <summary>
    /// Performs a dodge when a hostile ball is detected nearby.
    /// </summary>
    public class Dodge : PracticeAIState
    {
        /// <summary>
        /// Last detected hostile ball.
        /// </summary>
        private Ball trackedBall;

        private readonly Dictionary<int, Vector3> lastBallPos = new();
        private const float HIT_RADIUS = 0.75f;   // tune to collider
        private const float LOOKAHEAD = 1.2f;     // seconds ahead to consider

        /// <summary>
        /// Creates the state.
        /// </summary>
        /// <param name="parent">Owning AI.</param>
        public Dodge(PracticeFSMAI parent) : base(parent, "Dodge") { }

        /// <inheritdoc/>
        public override void Execute()
        {
            if (trackedBall == null)
            {
                base.Execute();
                return;
            }

            if (parent.CanDodge() == false)
            {
                trackedBall = null;
                base.Execute();
                return;
            }

            int id = trackedBall.GetHashCode();
            Vector3 p = trackedBall.transform.position;

            Vector3 v = Vector3.zero;
            if (lastBallPos.TryGetValue(id, out var prev))
            {
                float dt = Mathf.Max(Time.deltaTime, 0.0001f);
                v = (p - prev) / dt;
            }

            lastBallPos[id] = p;

            // predict closest approach within LOOKAHEAD
            Vector3 r0 = p - parent.transform.position;
            float vv = Vector3.Dot(v, v);
            float tStar = vv > 1e-3f ? Mathf.Clamp(-Vector3.Dot(r0, v) / vv, 0f, LOOKAHEAD) : 0f;
            float dMin = (r0 + v * tStar).magnitude;

            if (dMin < HIT_RADIUS)
            {
                Vector3 dir = v.sqrMagnitude > 0.01f ? v.normalized : (p - parent.transform.position).normalized;
                Vector3 dodgeDir = Vector3.Cross(dir, Vector3.up).normalized;
                parent.StartDodge(dodgeDir);
                trackedBall = null; // consume
            }

            base.Execute();
        }

        /// <summary>
        /// AI hook for ball sightings.
        /// </summary>
        /// <param name="ball">Detected ball.</param>
        public void OnBallDetected(Ball ball)
        {
            trackedBall = ball;
        }
    }

    #endregion

    #region FollowEnemy
    /// <summary>
    /// Follows a visible enemy and stops at preferred engagement range.
    /// </summary>
    public class FollowEnemy : PracticeAIState
    {
        /// <summary>
        /// Whether the agent has issued a stop after entering range.
        /// </summary>
        private bool stopped = false;

        /// <summary>
        /// Creates the state.
        /// </summary>
        /// <param name="parent">Owning AI.</param>
        /// <param name="substates">Optional substates.</param>
        public FollowEnemy(PracticeFSMAI parent, params PracticeAIState[] substates)
            : base(parent, "Follow", substates) { }

        /// <inheritdoc/>
        public override void Enter()
        {
            parent.RefreshOrAcquireTarget();
            base.Enter();
        }

        /// <inheritdoc/>
        public override void Execute()
        {
            if (parent.TryGetTarget(out var target) == false)
            {
                return;
            }

            Vector3 myPos = parent.transform.position;
            Vector3 tgtPos = target.Position;

            float dist = Vector3.Distance(myPos, tgtPos);
            float desired = parent.ProjectileRange;
            float buffer = 0.25f;

            if (dist > desired + buffer)
            {
                Vector3 dir = (tgtPos - myPos).normalized;
                Vector3 stopPos = tgtPos - dir * (desired - 3f); // preserves original tweak
                parent.MoveTo(stopPos);
                stopped = false;
            }
            else if (stopped == false)
            {
                parent.StopMoving();
                stopped = true;
            }

            base.Execute();
        }
    }
    #endregion
}
