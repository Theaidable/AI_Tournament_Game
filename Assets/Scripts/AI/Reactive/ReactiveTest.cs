using UnityEngine;
using UnityEngine.AI;
using AIGame.Core;

namespace ReactiveTest
{
    /// <summary>
    /// ReactiveTest AI implementation.
    /// TODO: Describe your AI strategy here.
    /// </summary>
    public class ReactiveTest : BaseAI
    {
        //Fields
        private int maxAttempts = 30;
        private float wanderRadius = 200f;
        private const float ARRIVAL_THRESHOLD = 0.5f;
        private Vector3 currentDestination;

        /// <summary>
        /// Configure the agent's stats (speed, health, etc.).
        /// </summary>
        protected override void ConfigureStats()
        {
            // TODO: Configure your agent's stats
            AllocateStat(StatType.Speed, 5);
            AllocateStat(StatType.VisionRange, 5);
            AllocateStat(StatType.ProjectileRange, 4);
            AllocateStat(StatType.ReloadSpeed, 3);
            AllocateStat(StatType.DodgeCooldown, 3);
        }

        /// <summary>
        /// Called once when the agent starts.
        /// Use this for initialization.
        /// </summary>
        protected override void StartAI()
        {
            EnemyEnterVision += OnEnemySpotted;
            BallDetected += OnIncomingBall;
        }

        /// <summary>
        /// Called every frame to make decisions.
        /// Implement your AI logic here.
        /// </summary>
        protected override void ExecuteAI()
        {
            if (!IsAlive) return;

            // Deduktion: Dodge

            // Fight: Er der et target/enemy, angrib.
            if (HasTarget && TryGetTarget(out var target))
            {
                float distance = Vector3.Distance(transform.position, target.Position);
                if (distance <= ProjectileRange)
                {
                    StopMoving();
                    ThrowBallAt(target);
                }
                else
                {
                    MoveTo(target.Position);
                }
                return;
            }

            // Forsøg at finde/genfinde target
            RefreshOrAcquireTarget();
            if (HasTarget) return;

            // Induktion: Power-ups, kun samle op hvis der er "safe"
            var enemies = GetVisibleEnemiesSnapshot();
            var powerUps = GetVisiblePowerUpsSnapshot();
            bool safe = enemies.Count == 0 || DistanceToClosest(enemies) > 10f;

            if (safe && powerUps.Count > 0)
            {
                var pu = powerUps[0];
                MoveTo(pu.Position);

                if (Vector3.Distance(transform.position, pu.Position) < 2f)
                    {
                        TryConsumePowerup(pu.Id);
                    }
                return;
            }

            // Objektive, gå mod objektive for at cappe.
            var cp = ControlPoint.Instance;
            if (cp != null)
            {
                if (cp.OTActive || Vector3.Distance(transform.position, cp.transform.position) > 8f)
                {
                    MoveTo(cp.transform.position);
                    return;
                }
            }

            // Patruljer eller "wander"
            //Wander();
        }

        private void OnEnemySpotted()
        {
            // Når den ser en fjende så prøver den at få fjenden som target
            RefreshOrAcquireTarget() ;
        }

        private void OnIncomingBall(Ball ball)
        {
            if (CanDodge() && ball != null)
            {
                Vector3 ballDir = (ball.transform.position - transform.position).normalized;
                Vector3 dodgeDir = Vector3.Cross(ballDir, Vector3.up).normalized;
                StartDodge(dodgeDir);
            }
        }

        private float DistanceToClosest(System.Collections.Generic.IReadOnlyList<PerceivedAgent> agents)
        {
            float best = float.MaxValue;
            Vector3 me = transform.position;
            for(int i = 0; i < agents.Count; i++)
            {
                float d = Vector3.Distance(me, agents[i].Position);
                if (d < best) best = d;
            }
            return best;
        }

        private Vector3 PickRandomDestination()
        {
            Vector3 currentPosition = transform.position;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                Vector3 randomDirection = Random.insideUnitSphere * wanderRadius + currentPosition;
                if (UnityEngine.AI.NavMesh.SamplePosition(randomDirection, out var hit, wanderRadius, UnityEngine.AI.NavMesh.AllAreas))
                {
                    var path = new UnityEngine.AI.NavMeshPath();
                    if (UnityEngine.AI.NavMesh.CalculatePath(currentPosition, hit.position, UnityEngine.AI.NavMesh.AllAreas, path)
                        && path.status == UnityEngine.AI.NavMeshPathStatus.PathComplete)
                        return hit.position;
                }
            }
            return currentPosition;
        }

        //private bool HasReachedDestination()
        //{
        //    if (NavMeshAgent.remainingDistance <= ARRIVAL_THRESHOLD) return true;
        //    if (!NavMeshAgent.pathPending && !NavMeshAgent.hasPath &&
        //        Vector3.Distance(transform.position, currentDestination) <= ARRIVAL_THRESHOLD) return true;
        //    return false;
        //}

        //private void Wander()
        //{
        //    if (HasReachedDestination())
        //        currentDestination = PickRandomDestination();

        //    MoveTo(currentDestination); // BaseAI-metode
        //}
    }
}