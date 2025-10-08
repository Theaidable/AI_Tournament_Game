using AIGame.Core;
using AIGame.Examples.GoalOriented;
using System.Collections.Generic;
using UnityEngine;

namespace GOAP.AI
{
    /// <summary>
    /// GOAP_AI AI implementation.
    /// Goal Oriented Action Planner AI
    /// Main Infantry unit that takes control of CP and protects it
    /// </summary>
    public class GOAP_AI : BaseAI
    {
        private Planner _planner;
        private Plan _currentPlan;

        private List<Action> _availableActions;
        private WorldState _currentGoal;

        private float lastPlanTime = 0f;
        private const float REPLAN_INTERVAL = 2f;

        private Vector3 cpPosition = Vector3.zero;
        private const float CP_RADIUS = 2.5f;
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
        /// Called once when the agent starts.
        /// </summary>
        protected override void StartAI()
        {
            //Planner + mål
            _planner = new Planner();
            _currentGoal = new WorldState();

            _currentGoal.SetState(StateKeys.AT_CP, true);

            //Actions
            _availableActions = new List<Action>
            {
                new IdleAction(this),
                new MoveToCPAction(this)
            };

            //Subscribe to VisionEvents
            EnemyEnterVision += OnEnemySpotted;         // Enemy comes into view
            EnemyExitVision += OnEnemyLost;             // Enemy leaves view
            FriendlyEnterVision += OnAllySpotted;       // Ally comes into view
            FriendlyExitVision += OnAllyLost;           // Ally leaves view

            //Subscribe to CombatEvents
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
            if (IsAlive == false)
            {
                return;
            }

            // Start planning immediately
            if (_currentPlan == null || _currentPlan.IsEmpty() || Time.time - lastPlanTime > REPLAN_INTERVAL)
            {
                CreateNewPlan();
                lastPlanTime = Time.time;
            }

            // Execute current action
            if (_currentPlan != null && !_currentPlan.IsEmpty())
            {
                ExecuteCurrentAction();
            }
        }

        private void CreateNewPlan()
        {
            WorldState currentWorldState = AssessCurrentWorldState();
            _currentPlan = _planner.CreatePlan(currentWorldState, _currentGoal, _availableActions);
        }

        private WorldState AssessCurrentWorldState()
        {
            var ws = new WorldState();

            var cp = ControlPoint.Instance;

            if(cp != null)
            {
                cpPosition = cp.transform.position;
            }

            bool knowCP = (cpPosition != Vector3.zero);
            ws.SetState(StateKeys.KNOW_CP_POSITION, knowCP);
            ws.SetState(StateKeys.CONTROL_POINT_POSITION, cpPosition);

            bool atCP = knowCP && Vector3.Distance(transform.position, cpPosition) <= CP_RADIUS;
            ws.SetState(StateKeys.AT_CP, atCP);

            return ws;
        }

        private void ExecuteCurrentAction()
        {
            Action currentAction = _currentPlan.GetCurrentAction();

            if (currentAction == null)
            {
                return;
            }

            // Execute the action
            bool actionStarted = currentAction.Execute();

            if (actionStarted == false)
            {
                _currentPlan.RemoveCurrentAction();
                return;
            }

            // Check if action is complete
            if (currentAction.IsComplete() == true)
            {
                _currentPlan.RemoveCurrentAction();
            }
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