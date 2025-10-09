using AIGame.Core;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

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

        private DodgeAction _dodgeAction;
        private bool incomingDanger;

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
                new MoveToCPAction(this),
                (_dodgeAction = new DodgeAction(this)),
                new ShootAction(this)
            };

            //Subscribe to VisionEvents
            EnemyEnterVision += OnEnemySpotted;
            EnemyExitVision += OnEnemyLost;

            //Subscribe to CombatEvents
            BallDetected += OnBallDetected;
            DodgeComplete += OnDodgeFinished; 

            // Subscribe to respawn event
            Respawned += OnRespawned;
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

            var enemies = GetVisibleEnemiesSnapshot();
            bool enemyVisible = enemies.Count > 0;
            ws.SetState(StateKeys.ENEMY_VISIBLE, enemyVisible);

            ws.SetState(StateKeys.INCOMING_DANGER, incomingDanger);

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

        private void OnRespawned()
        {
            _currentPlan = null;
            lastPlanTime = 0f;
        }

        private void OnEnemySpotted()
        {
            RefreshOrAcquireTarget();
            lastPlanTime = 0f;
        }

        private void OnEnemyLost()
        {
            // Store last known position
            if (TryGetTarget(out var target))
            {
                lastKnownEnemyPosition = target.Position;
            }
        }

        private void OnBallDetected(Ball ball)
        {
            incomingDanger = true;
            _dodgeAction?.NotifyIncomingBall(ball);
            lastPlanTime = 0f;
        }

        private void OnDodgeFinished()
        {
            incomingDanger = false;
            FaceTarget(lastKnownEnemyPosition);
            RefreshOrAcquireTarget();
            lastPlanTime = 0f;
        }

        #endregion
    }
}