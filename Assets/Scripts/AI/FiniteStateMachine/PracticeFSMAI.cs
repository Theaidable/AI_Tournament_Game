using AIGame.Core;
using System.Collections.Generic;
// using System.Linq;  // CHANGE: No longer needed after removing 'All' usage, but we can keep if used elsewhere.
using UnityEngine;

namespace Practice.AI
{
    /// <summary>
    /// PracticeFSM AI implementation.
    /// AI That uses FSM.
    /// </summary>
    public class PracticeFSMAI : BaseAI
    {
        #region Fields

        /// <summary>
        /// Cached reference to the idle state for quick resetting after death.
        /// </summary>
        private Idle idle;

        private PracticeFSM fsm;

        #region Formation
        //Formation / slotting
        private int teamSlot = 0;
        private int teamCount = 1;

        // Arrowhead (relative slots): front, wings, rears
        private static readonly Vector3[] ArrowheadSlots =
        {
            new Vector3( 0f, 0f,  0f),  // tip
            new Vector3(-8f, 0f, 8f),  // left wing
            new Vector3( 8f, 0f, 8f),  // right wing
            new Vector3(-16f, 0f, 16f),  // rear left
            new Vector3( 16f, 0f, 16f)   // rear right
        };

        // Protection: center + four corners
        private static readonly Vector3[] ProtectSlots =
        {
            new Vector3( 0f, 0f,  0f), // Center
            new Vector3( 18f, 0f,  18f), // Left top corner
            new Vector3(-18f, 0f,  18f), // Right top corner
            new Vector3( 18f, 0f, -18f), // Left bottom corner
            new Vector3(-18f, 0f, -18f), // Right bottom corner
        };
        #endregion

        // Strafing
        private float strafePhase;
        private const float STRAFE_RADIUS = 2.2f;
        private const float STRAFE_SPEED = 2.6f;

        // Targeting
        private readonly Dictionary<int, Vector3> lastEnemyPos = new();
        private readonly Dictionary<int, Vector3> lastEnemyVel = new();
        private readonly Dictionary<int, float> lastDodgeTime = new();

        #endregion

        #region ConfigureStats
        /// <summary>
        /// Configure the agent's stats (speed, health, etc.).
        /// </summary>
        protected override void ConfigureStats()
        {
            AllocateStat(StatType.Speed, 3);
            AllocateStat(StatType.VisionRange, 9);
            AllocateStat(StatType.ProjectileRange, 8);
            AllocateStat(StatType.ReloadSpeed, 0);
            AllocateStat(StatType.DodgeCooldown, 0);
        }
        #endregion

        #region StartAI
        /// <summary>
        /// Called once when the agent starts.
        /// Use this for initialization.
        /// </summary>
        protected override void StartAI()
        {
            fsm = new PracticeFSM();

            // Create states
            idle = new Idle(this);
            Strafe strafe = new Strafe(this);
            Dodge dodge = new Dodge(this);
            FollowEnemy follow = new FollowEnemy(this);
            Combat combat = new Combat(this, strafe, follow, dodge);
            MoveToObjective moveToObjective = new MoveToObjective(this, dodge);
            ProtectObjective protectObjective = new ProtectObjective(this, dodge);

            // Create event listeners
            moveToObjective.DestinationReached += () => OnObjectiveReached();
            EnemyEnterVision += () => OnEnemyEnterVision();
            combat.NoMoreEnemies += () => OnNoMoreEnemies();
            BallDetected += (ball) => dodge.OnBallDetected(ball);
            Death += () => OnDeath();
            Respawned += () => OnSpawned();

            // Create state transitions
            fsm.AddTransition(moveToObjective, AICondition.Protect, protectObjective);
            fsm.AddTransition(idle, AICondition.Spawned, moveToObjective);
            fsm.AddTransition(moveToObjective, AICondition.MoveToObjective, moveToObjective);
            fsm.AddTransition(moveToObjective, AICondition.SeesEnemy, combat);
            fsm.AddTransition(combat, AICondition.MoveToObjective, moveToObjective);
            fsm.AddTransition(protectObjective, AICondition.SeesEnemy, combat);

            // Set initial state
            fsm.ChangeState(moveToObjective);

            // Slot + cosmetics
            ComputeTeamSlot();
            strafePhase = Random.value * Mathf.PI * 2f;

            // CHANGE: Removed direct NavMeshAgent access per patch. Avoidance priority cannot be set directly anymore.
            // NavMeshAgent.avoidancePriority = 20 + (Random.Range(0, 5) * 15);
        }
        #endregion

        #region ExecuteAI
        /// <summary>
        /// Called every frame to make decisions.
        /// Implement your AI logic here.
        /// </summary>
        protected override void ExecuteAI()
        {
            fsm.Execute();
        }
        #endregion

        #region OnDisable (cleanup)
        /// <summary>
        /// Ensures state cleanup when object gets disabled (prevents stale event-driven states).
        /// Nice to have, but not necessary after last patch update.
        /// We do NOT call base.OnDisable() because it is not accessible and not needed.
        /// </summary>
        private void OnDisable()
        {
            // CHANGE: Do NOT call base.OnDisable(); it is not accessible and not needed.
            if (fsm != null && idle != null)
            {
                fsm.ChangeState(idle);
            }
        }
        #endregion

        #region OnDeath
        /// <summary>
        /// Called when the AI dies. Resets to idle state.
        /// </summary>
        private void OnDeath()
        {
            fsm.ChangeState(idle);
        }
        #endregion

        #region OnObjectiveReached
        /// <summary>
        /// Called when the objective is reached.
        /// Switches to the protect-objective state.
        /// </summary>
        private void OnObjectiveReached()
        {
            fsm.SetCondition(AICondition.Protect);
        }
        #endregion

        #region OnEnemyEnterVision
        /// <summary>
        /// Called when an enemy enters the AI's vision range.
        /// Switches to combat state.
        /// </summary>
        private void OnEnemyEnterVision()
        {
            fsm.SetCondition(AICondition.SeesEnemy);
        }
        #endregion

        #region OnNoMoreEnemies
        /// <summary>
        /// Called when there are no more visible enemies.
        /// Switches to move-to-objective state.
        /// </summary>
        private void OnNoMoreEnemies()
        {
            fsm.SetCondition(AICondition.MoveToObjective);
        }
        #endregion

        #region OnSpawned
        /// <summary>
        /// Called when the AI respawns.
        /// Switches to spawned state.
        /// </summary>
        private void OnSpawned()
        {
            fsm.SetCondition(AICondition.Spawned);
        }
        #endregion

        #region Helpers

        #region Formation Helpers

        /// <summary>
        /// Computes a local frame (origin/fwd/left) around the objective for formation placement.
        /// </summary>
        private void GetTeamFormationFrame(Vector3 objective, out Vector3 origin, out Vector3 fwd, out Vector3 left)
        {
            // CHANGE: Removed dependency on global 'All' list/centroid.
            // We anchor the frame on the objective and orient it from our current position.
            origin = objective;

            Vector3 dir = (objective - transform.position);
            if (dir.sqrMagnitude < 1e-4f)
                dir = transform.forward;

            fwd = dir.normalized;
            left = Vector3.Cross(Vector3.up, fwd).normalized;
        }

        /// <summary>
        /// Assigns a deterministic slot id without relying on a global team list.
        /// </summary>
        private void ComputeTeamSlot()
        {
            // CHANGE: Previous version used 'All.Where(...).OrderBy(...).IndexOf(this)' which is not available here.
            // We create a stable per-instance slot by hashing the instance id.
            int hash = Mathf.Abs(GetInstanceID());
            teamSlot = hash % Mathf.Max(1, ArrowheadSlots.Length);
            teamCount = ArrowheadSlots.Length; // informational only
        }

        /// <summary>
        /// Returns a world position for travelling towards the objective in an arrowhead pattern.
        /// </summary>
        public Vector3 GetEngageFormationSlot(Vector3 objective)
        {
            // CHANGE: Build slot relative to objective frame (no team centroid).
            GetTeamFormationFrame(objective, out var origin, out var fwd, out var left);

            const float FORMATION_LEAD = 6f; // keep a small forward lead while travelling

            int idx = (teamSlot % ArrowheadSlots.Length);
            var local = ArrowheadSlots[idx];

            float forward = local.z + FORMATION_LEAD;

            return origin + fwd * forward + left * local.x;
        }

        /// <summary>
        /// Returns a world position around the control point to protect it (rotating square pattern).
        /// </summary>
        public Vector3 GetProtectFormationSlot(Vector3 cp)
        {
            GetTeamFormationFrame(cp, out var origin, out var fwd, out var left);

            int idx = (teamSlot % ProtectSlots.Length);
            var local = ProtectSlots[idx];

            Vector2 j = Random.insideUnitCircle * 0.6f;
            Vector3 offset = new Vector3(local.x + j.x, 0f, local.z + j.y);

            // project offset in the shared basis so the square rotates with the frame
            return origin + fwd * offset.z + left * offset.x;
        }
        #endregion

        /// <summary>
        /// Strafes left/right around a focus point using helper movement APIs.
        /// </summary>
        public void StrafeAround(Vector3 focus)
        {
            Vector3 toFocus = (focus - transform.position);
            Vector3 fwd = toFocus.sqrMagnitude > 0.001f ? toFocus.normalized : transform.forward;
            Vector3 side = Vector3.Cross(Vector3.up, fwd);

            float t = Time.time * STRAFE_SPEED + strafePhase;
            float s = Mathf.Sin(t) * STRAFE_RADIUS;

            Vector3 desired = transform.position + side * s;

            FaceTarget(focus);
            StrafeTo(desired);
        }

        /// <summary>
        /// Tracks enemy motion vectors to detect recent dodges (adds a time mark used in scoring).
        /// </summary>
        public void TrackEnemyMotion(IReadOnlyList<PerceivedAgent> seen)
        {
            float now = Time.time;

            foreach (var enemy in seen)
            {
                Vector3 pos = enemy.Position;
                int id = enemy.Id;

                if (lastEnemyPos.TryGetValue(id, out var prev))
                {
                    float dt = Mathf.Max(Time.deltaTime, 0.0001f);
                    Vector3 vel = (pos - prev) / dt;

                    // detect “dodge”: big lateral speed spike compared to last frame
                    if (lastEnemyVel.TryGetValue(id, out var vPrev))
                    {
                        Vector3 dv = vel - vPrev;
                        // heuristic thresholds; tweak as you test
                        if (dv.magnitude > 6.0f)
                        {
                            lastDodgeTime[id] = now; // mark as “just dodged”
                        }
                    }

                    lastEnemyVel[id] = vel;
                }

                lastEnemyPos[id] = pos;
            }
        }

        /// <summary>
        /// Chooses a priority target based on recent dodge bonus, cluster potential, and range bias.
        /// </summary>
        public PerceivedAgent ChoosePriorityTarget(IReadOnlyList<PerceivedAgent> seen)
        {
            if (seen.Count == 0)
            {
                return default;
            }

            Vector3 me = transform.position;
            float now = Time.time;

            PerceivedAgent best = default;
            float bestScore = float.NegativeInfinity;

            foreach (var e in seen)
            {
                Vector3 aim = (e.Position - me);
                float dist = aim.magnitude;
                if (dist < 0.001f) continue;

                // cluster potential: enemies behind target along throw line
                int cluster = 0;

                for (int i = 0; i < seen.Count; i++)
                {
                    var o = seen[i];
                    if (o.Id == e.Id) continue;

                    Vector3 rel = o.Position - e.Position;
                    float along = Vector3.Dot(rel, aim.normalized);
                    float across = (rel - aim.normalized * along).magnitude;

                    if (along > 0.0f && along < 6.0f && across < 1.2f)
                    {
                        cluster++;
                    }
                }

                // recent-dodge bonus
                float rec = 0f;
                if (lastDodgeTime.TryGetValue(e.Id, out var tD))
                {
                    float dt = now - tD;
                    if (dt < 3f)
                    {
                        rec = Mathf.Lerp(1f, 0f, dt / 3f);
                    }
                }

                // distance bias: slightly prefer targets already in range
                float inRange = dist <= ProjectileRange ? 0.5f : -0.25f;

                float score = rec * 3.0f + cluster * 1.5f + inRange;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = e;
                }
            }

            return best;
        }

        #endregion
    }
}
