using System;
using System.Collections.Generic;
using UnityEngine;
using AIGame.Core;

namespace FiniteStateMachine
{
    /// Your agent (spawned by "FSM Test" factory)
    public class FMS : BaseAI
    {
        // --- Tiny generic state machine ---
        private class StateMachine
        {
            public AIState CurrentState { get; private set; }
            private readonly Dictionary<(AIState, AICondition), AIState> transitions = new();
            private AICondition currentCondition = AICondition.None;

            public void AddTransition(AIState from, AICondition c, AIState to) => transitions[(from, c)] = to;
            public void SetCondition(AICondition c) => currentCondition = c;

            public void ChangeState(AIState next)
            {
                CurrentState?.Exit();
                CurrentState = next;
                CurrentState?.Enter();
            }

            public void Execute()
            {
                if (CurrentState != null &&
                    transitions.TryGetValue((CurrentState, currentCondition), out var next))
                {
                    ChangeState(next);
                }
                currentCondition = AICondition.None; // consume condition
                CurrentState?.Execute();
            }
        }

        // Expose a safe way for states to signal conditions
        internal void FSM_SetCondition(AICondition c) => fsm.SetCondition(c);

        // --- States & FSM ---
        private StateMachine fsm;
        private Idle idle;
        private Dodge dodge;
        private Combat combat;
        private MoveToObjective moveToObjective;
        private ProtectObjective protectObjective;
        private MoveToPowerUp moveToPowerUp;
        private InvestigateShot investigate;

        // Stats
        protected override void ConfigureStats()
        {
            AllocateStat(StatType.Speed, 5);
            AllocateStat(StatType.VisionRange, 5);
            AllocateStat(StatType.ProjectileRange, 4);
            AllocateStat(StatType.ReloadSpeed, 3);
            AllocateStat(StatType.DodgeCooldown, 3);
        }

        protected override void StartAI()
        {
            fsm = new StateMachine();

            // Create states
            idle = new Idle(this);
            dodge = new Dodge(this);
            combat = new Combat(this, dodge);
            moveToObjective = new MoveToObjective(this, dodge);
            protectObjective = new ProtectObjective(this, dodge);
            moveToPowerUp = new MoveToPowerUp(this, dodge);
            investigate = new InvestigateShot(this);

            // Wire events
            moveToObjective.DestinationReached += () => fsm.SetCondition(AICondition.Protect);
            EnemyEnterVision += () => fsm.SetCondition(AICondition.SeesEnemy);
            BallDetected += (ball) =>
            {
                dodge.OnBallDetected(ball);
                if (GetVisibleEnemiesSnapshot().Count == 0) { investigate.OnBallDetected(ball); fsm.SetCondition(AICondition.Investigate); }
            };
            Death += () => fsm.ChangeState(idle);
            Respawned += () => fsm.SetCondition(AICondition.Spawned);

            // Transitions
            fsm.AddTransition(idle, AICondition.Spawned, moveToObjective);
            fsm.AddTransition(moveToObjective, AICondition.SeesEnemy, combat);
            fsm.AddTransition(protectObjective, AICondition.SeesEnemy, combat);
            fsm.AddTransition(combat, AICondition.MoveToObjective, moveToObjective);

            fsm.AddTransition(moveToObjective, AICondition.Protect, protectObjective);

            fsm.AddTransition(moveToObjective, AICondition.PowerUp, moveToPowerUp);
            fsm.AddTransition(protectObjective, AICondition.PowerUp, moveToPowerUp);
            fsm.AddTransition(moveToPowerUp, AICondition.SeesEnemy, combat);
            fsm.AddTransition(moveToPowerUp, AICondition.MoveToObjective, moveToObjective);

            fsm.AddTransition(moveToObjective, AICondition.Investigate, investigate);
            fsm.AddTransition(protectObjective, AICondition.Investigate, investigate);
            fsm.AddTransition(investigate, AICondition.MoveToObjective, moveToObjective);

            // Initial state
            fsm.ChangeState(moveToObjective);
        }

        protected override void ExecuteAI()
        {
            if (!IsAlive) return;

            // High-level “triggers” we evaluate every tick

            // 1) If in combat but no visible enemies -> back to objective
            if (fsm.CurrentState == combat && GetVisibleEnemiesSnapshot().Count == 0)
                fsm.SetCondition(AICondition.MoveToObjective);

            // 2) If safe powerup is visible -> go eat it
            var enemies = GetVisibleEnemiesSnapshot();
            bool safe = enemies.Count == 0 || DistanceToClosestEnemy(enemies) > 10f;
            if (safe && GetVisiblePowerUpsSnapshot().Count > 0 &&
                (fsm.CurrentState == moveToObjective || fsm.CurrentState == protectObjective))
            {
                fsm.SetCondition(AICondition.PowerUp);
            }

            fsm.Execute();
        }

        public bool EatPowerUp(int id) => TryConsumePowerup(id);

        private float DistanceToClosestEnemy(IReadOnlyList<PerceivedAgent> snapshot)
        {
            float best = float.PositiveInfinity;
            Vector3 me = transform.position;
            foreach (var e in snapshot)
            {
                float d = Vector3.Distance(me, e.Position);
                if (d < best) best = d;
            }
            return best;
        }
    }
}
