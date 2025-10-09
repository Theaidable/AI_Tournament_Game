using AIGame.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Practice.AI
{
    /// <summary>
    /// SpearheadFSM AI implementation.
    /// Fast distraction unit that rushes THROUGH the control point and,
    /// the moment enemies are visible, starts continuously circling them
    /// to draw focus and disrupt aim.
    /// Inherits from PracticeFSMAI so we can reuse your helpers (MoveTo, StrafeTo, FaceTarget, etc.)
    /// and keep consistency with PracticeAIState's expected parent type.
    /// </summary>
    public class SpearheadFSMAI : PracticeFSMAI
    {
        private PracticeFSM fsm;
        private Idle idle;

        // Sub-state for dodge behavior (lightweight variant)
        private SpearheadDodge spearDodge;

        // Role tunables
        private const float RUSH_OVERSHOOT = 10.0f; // how far past CP we run during the rush
        private float orbitPhase;                    // can be used for per-agent phase, if needed

        #region ConfigureStats
        /// <summary>
        /// Prioritize speed/vision for a distraction role.
        /// </summary>
        protected override void ConfigureStats()
        {
            AllocateStat(StatType.Speed, 8);
            AllocateStat(StatType.VisionRange, 8);
            AllocateStat(StatType.ProjectileRange, 0);
            AllocateStat(StatType.ReloadSpeed, 0);
            AllocateStat(StatType.DodgeCooldown, 4);
        }
        #endregion

        #region StartAI
        /// <summary>
        /// Build the spearhead FSM: Rush -> CircleEnemy -> Rush (loop).
        /// </summary>
        protected override void StartAI()
        {
            fsm = new PracticeFSM();

            idle = new Idle(this);
            spearDodge = new SpearheadDodge(this);

            var rush = new RushThroughObjective(this, spearDodge);
            var harass = new CircleEnemy(this, spearDodge);               // CHANGE: continuous orbit state

            // Wire state-to-state events
            rush.ReachedHarassZone += () => fsm.SetCondition(AICondition.SeesEnemy);
            harass.NoMoreEnemies += () => fsm.SetCondition(AICondition.MoveToObjective);

            // FSM transitions
            fsm.AddTransition(idle, AICondition.Spawned, rush);
            fsm.AddTransition(rush, AICondition.SeesEnemy, harass);
            fsm.AddTransition(harass, AICondition.MoveToObjective, rush);

            // Entry state
            fsm.ChangeState(rush);

            // Engine hooks
            BallDetected += (ball) => spearDodge.OnBallDetected(ball);
            Respawned += () => fsm.SetCondition(AICondition.Spawned);

            // CHANGE: instant trigger on enemy vision so we switch from rush to circle immediately
            EnemyEnterVision += () => fsm.SetCondition(AICondition.SeesEnemy);

            orbitPhase = UnityEngine.Random.value * Mathf.PI * 2f;
        }
        #endregion

        #region ExecuteAI
        /// <summary>
        /// Per-frame execution: delegate to FSM.
        /// </summary>
        protected override void ExecuteAI()
        {
            fsm.Execute();
        }
        #endregion

        #region Inner States

        /// <summary>
        /// Sub-state: predicts ball impact and triggers a dodge for this role.
        /// Minimal variant; relies on BaseAI helpers.
        /// </summary>
        private class SpearheadDodge : PracticeAIState
        {
            private Ball trackedBall;
            private readonly Dictionary<int, Vector3> lastBallPos = new();

            private const float HIT_RADIUS = 5.75f;
            private const float LOOKAHEAD = 3.0f;

            public SpearheadDodge(PracticeFSMAI parent) : base(parent, "SpearheadDodge") { }

            public override void Execute()
            {
                if (trackedBall == null) return;

                if (parent.CanDodge() == false)
                {
                    trackedBall = null;
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
            }

            public void OnBallDetected(Ball ball) => trackedBall = ball;
        }

        /// <summary>
        /// Rush to CP and intentionally run THROUGH it (overshoot) to force enemy re-aim.
        /// Transitions to circle as soon as any enemy is visible.
        /// </summary>
        private class RushThroughObjective : PracticeAIState
        {
            public event Action ReachedHarassZone;

            private Vector3 currentDest;
            private bool setOnce;

            public RushThroughObjective(PracticeFSMAI parent, params PracticeAIState[] substates)
                : base(parent, "RushThroughObjective", substates) { }

            public override void Enter()
            {
                setOnce = false;
            }

            public override void Execute()
            {
                Vector3 cp = GameManager.Instance.Objective.transform.position;

                Vector3 dir = (cp - parent.transform.position);
                if (dir.sqrMagnitude < 0.001f) dir = parent.transform.forward;
                dir.Normalize();

                Vector3 through = cp + dir * RUSH_OVERSHOOT;

                // CHANGE: if any enemy is visible, cancel movement and signal transition immediately
                var seen = parent.GetVisibleEnemiesSnapshot();
                if (seen.Count > 0)
                {
                    parent.StopMoving();          // cancel current path right now
                    ReachedHarassZone?.Invoke();  // FSM will switch next tick
                    parent.FaceTarget(cp);
                    return;                       // IMPORTANT: do not issue new MoveTo after this
                }

                // Continue rush if no enemies are visible
                if (!setOnce || (through - currentDest).sqrMagnitude > 0.5f)
                {
                    currentDest = through;
                    parent.MoveTo(currentDest);
                    setOnce = true;
                }

                parent.FaceTarget(cp);
            }
        }

        /// <summary>
        /// Continuously circles the nearest visible enemy at a fixed radius.
        /// Cancels any previous path on Enter so orbit takes over immediately.
        /// Falls back to Rush if no enemies are visible.
        /// </summary>
        private class CircleEnemy : PracticeAIState
        {
            public event Action NoMoreEnemies;

            // CHANGE: orbit tuning — tweak to taste
            private const float ORBIT_RADIUS = 10.0f;    // meters
            private const float ANGULAR_SPEED = 1.6f;    // radians/second (~90°/s)
            private const float THROW_IN_RANGE = 0.85f;   // % of projectile range

            // Stable local frame around the chosen target (world-up plane)
            private Vector3 basisRadial;   // unit vector from center (enemy) to orbit
            private Vector3 basisTangent;  // unit vector perpendicular in the horizontal plane
            private float angle;         // current angle along the orbit (radians)
            private int targetId;      // lock target while orbiting

            public CircleEnemy(PracticeFSMAI parent, params PracticeAIState[] substates)
                : base(parent, "CircleEnemy", substates) { }

            public override void Enter()
            {
                parent.StopMoving();  // CHANGE: cancel any rush path

                // Pick nearest visible enemy and lock it
                var seen = parent.GetVisibleEnemiesSnapshot();
                if (seen.Count == 0)
                {
                    NoMoreEnemies?.Invoke();
                    return;
                }

                PerceivedAgent best = seen[0];
                float bestD = float.MaxValue;
                for (int i = 0; i < seen.Count; i++)
                {
                    float d = (seen[i].Position - parent.transform.position).sqrMagnitude;
                    if (d < bestD) { bestD = d; best = seen[i]; }
                }

                targetId = best.Id;
                parent.TrySetTargetById(targetId);

                // Build a stable horizontal basis around the target
                Vector3 center = best.Position;
                Vector3 offset = parent.transform.position - center;
                offset.y = 0f;

                if (offset.sqrMagnitude < 1e-3f)
                    offset = parent.transform.forward; // fallback if we spawn on top

                basisRadial = offset.normalized;
                basisTangent = Vector3.Cross(Vector3.up, basisRadial).normalized;

                // Initialize angle from current placement along the orbit basis
                float x = Vector3.Dot(offset.normalized, basisRadial);
                float y = Vector3.Dot(offset.normalized, basisTangent);
                angle = Mathf.Atan2(y, x);

                base.Enter();
            }

            public override void Execute()
            {
                // If no enemies are currently visible, drop back to rush
                var seen = parent.GetVisibleEnemiesSnapshot();
                if (seen.Count == 0)
                {
                    NoMoreEnemies?.Invoke();
                    return;
                }

                // Find the locked target; if it's gone (dead/out of vision), leave
                PerceivedAgent? locked = null;
                for (int i = 0; i < seen.Count; i++)
                {
                    if (seen[i].Id == targetId) { locked = seen[i]; break; }
                }
                if (locked == null)
                {
                    NoMoreEnemies?.Invoke();
                    return;
                }

                Vector3 center = locked.Value.Position;

                // Advance angle at constant angular speed (world-up plane)
                angle += ANGULAR_SPEED * Time.deltaTime;

                // Desired orbit position using the stable basis
                Vector3 desired = center
                                  + (basisRadial * Mathf.Cos(angle) + basisTangent * Mathf.Sin(angle))
                                  * ORBIT_RADIUS;

                // Optional guarantee: if any legacy path tries to steer us, cancel it
                // parent.StopMoving();

                // Move tangentially and face the target
                parent.FaceTarget(center);
                parent.StrafeTo(desired);

                // Opportunistic throw only, no hard chase
                float dist = Vector3.Distance(parent.transform.position, center);
                if (dist <= parent.ProjectileRange * THROW_IN_RANGE)
                {
                    parent.ThrowBallAt(locked.Value);
                }
            }

            public override void Exit()
            {
                // Let the next state decide how to move; nothing special to clean up here.
                base.Exit();
            }
        }

        #endregion
    }
}
