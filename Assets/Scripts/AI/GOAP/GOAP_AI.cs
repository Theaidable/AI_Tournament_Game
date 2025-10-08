using UnityEngine;
using AIGame.Core;

namespace GOAP.AI
{
    /// <summary>
    /// GOAP_AI AI implementation.
    /// Goal Oriented Action Planner AI
    /// Main Infantry unit that takes control of CP and protects it
    /// </summary>
    public class GOAP_AI : BaseAI
    {
        private Vector3 lastKnownEnemyPosition = Vector3.zero;
        private float lastSeenTime = 0f;

        /// <summary>
        /// Configure the agent's stats
        /// </summary>
        protected override void ConfigureStats()
        {
            AllocateStat(StatType.Speed, 3);
            AllocateStat(StatType.VisionRange, 9);
            AllocateStat(StatType.ProjectileRange, 8);
            AllocateStat(StatType.ReloadSpeed, 0);
            AllocateStat(StatType.DodgeCooldown, 0);
        }

        /// <summary>
        /// Give agent a name
        /// </summary>
        /// <returns></returns>
        protected override string SetName()
        {
            for (int i = 0; i < 5; i++)
            {
                string unitName = $"Jægersoldat_{i}";
            }

            return "";
        }

        /// <summary>
        /// Called once when the agent starts.
        /// </summary>
        protected override void StartAI()
        {
            //Subscribe to VisionEvents
            EnemyEnterVision += OnEnemySpotted;         // Enemy comes into view
            EnemyExitVision += OnEnemyLost;             // Enemy leaves view
            FriendlyEnterVision += OnAllySpotted;       // Ally comes into view
            FriendlyExitVision += OnAllyLost;           // Ally leaves view

            BallDetected += OnIncomingBall;             // Ball heading towards you
            VisibleFriendlyDeath += OnAllyDied;         // Ally was killed
            VisibleEnemyDeath += OnEnemyDied;           // Enemy was killed
            DodgeComplete += OnDodgeFinished;           // Dodge maneuver finished
        }

        /// <summary>
        /// Called every frame to make decisions.
        /// </summary>
        protected override void ExecuteAI()
        {
            // TODO: Implement your AI decision-making logic here
        }


        #region Events

        #region VisionEvents
        private void OnEnemySpotted()
        {
            RefreshOrAcquireTarget();
        }

        private void OnEnemyLost()
        {
            // Store last known position
            if (TryGetTarget(out var target))
            {
                lastKnownEnemyPosition = target.Position;
                lastSeenTime = Time.time;
            }
        }

        private void OnAllySpotted()
        {

        }

        private void OnAllyLost()
        {

        }

        #endregion

        #region CombatEvents
        private void OnIncomingBall(Ball ball)
        {
            if (CanDodge() == true)
            {
                Vector3 ballDirection = (ball.transform.position - transform.position).normalized;
                Vector3 dodgeDirection = Vector3.Cross(ballDirection, Vector3.up).normalized;
                StartDodge(dodgeDirection);
            }
        }

        private void OnAllyDied()
        {

        }

        private void OnEnemyDied()
        {

        }

        private void OnDodgeFinished()
        {

        }
        #endregion

        #endregion
    }
}