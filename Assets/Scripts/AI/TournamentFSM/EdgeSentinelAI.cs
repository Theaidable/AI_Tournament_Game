using AIGame.Examples.FSM;
using UnityEngine;
using static AIGame.Core.BaseAI;

namespace AIGame.TournamentFSM
{
    // Edge pusher: holds CP rim toward enemy side; occasionally contests center if quiet.
    public class EdgeSentinelAI : FinitStateAI
    {
        private FSM fsm;
        private AIState idle, combat, holdEdge, holdCenter;

        protected override void StartAI()
        {
            fsm = new FSM();

            // micro
            var strafe = new Strafe(this);
            var follow = new FollowEnemy(this);
            var dodge = new Dodge(this);
            var combat = new Combat(this, strafe, follow, dodge);
            idle = new Idle(this);

            AIState shortSearch = null;
            shortSearch = new ShortSearch(this,
                onTimeout: () => fsm.SetCondition(AICondition.MoveToObjective),
                2.0f, dodge, strafe);

            // wire sensors -> states
            BallDetected += ball => dodge.OnBallDetected(ball);
            EnemyEnterVision += () => fsm.SetCondition(AICondition.SeesEnemy);
            Death += () => fsm.ChangeState(idle);
            Respawned += () => fsm.SetCondition(AICondition.Spawned);

            // combat exit -> move back to objective/anchor
            combat.NoMoreEnemies += () => fsm.ChangeState(shortSearch);

            // anchors
            var edgeOffset = new Vector3(0f, 0f, 5f);
            holdEdge = new GuardAtOffset(this, edgeOffset, dodge, strafe);
            holdCenter = new HoldCenterAndLookAround(this, dodge);

            // transitions
            fsm.AddTransition(idle, AICondition.Spawned, holdEdge);
            fsm.AddTransition(holdEdge, AICondition.SeesEnemy, combat);
            fsm.AddTransition(combat, AICondition.MoveToObjective, holdEdge);
            fsm.AddTransition(shortSearch, AICondition.SeesEnemy, combat);
            fsm.AddTransition(shortSearch, AICondition.MoveToObjective, holdEdge);

            // periodic brief contest if quiet
            InvokeRepeating(nameof(MaybeContestCenter), 8f, 10f);

            fsm.ChangeState(holdEdge);
        }

        private void MaybeContestCenter()
        {
            // Don’t abandon combat; only drift in if we currently don’t see threats
            if (!PerceptionHasVisibleEnemies())
            {
                fsm.ChangeState(holdCenter);
                Invoke(nameof(ReturnToEdge), 2.5f);
            }
        }

        private void ReturnToEdge() => fsm.ChangeState(holdEdge);

        // Helper to check vision without coupling to specific perception lists
        private bool PerceptionHasVisibleEnemies()
        {
            var enemies = GetVisibleEnemiesSnapshot();
            return enemies != null && enemies.Count > 0;
        }

        protected override void ConfigureStats()
        {
            // Slight speed/vision bias to encourage safe pokes forward
            AllocateStat(StatType.Speed, 6);
            AllocateStat(StatType.VisionRange, 7);
            AllocateStat(StatType.ProjectileRange, 6);
            AllocateStat(StatType.ReloadSpeed, 1);
        }

        protected override void ExecuteAI() => fsm.Execute();
    }
}
