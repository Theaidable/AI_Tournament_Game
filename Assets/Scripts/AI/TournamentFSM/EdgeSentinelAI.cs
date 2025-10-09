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

            // Shared combat micro
            var strafe = new Strafe(this);
            var follow = new FollowEnemy(this);
            var dodge = new Dodge(this);
            combat = new Combat(this, strafe, follow, dodge);

            idle = new Idle(this);

            // Anchor just outside center, nudged toward enemy half (positive Z by convention)
            var edgeOffset = new Vector3(0f, 0f, 5f);
            holdEdge = new GuardAtOffset(this, edgeOffset, dodge, strafe);

            // Quick hard-contest of center when quiet
            holdCenter = new HoldCenterAndLookAround(this, dodge);

            // Conditions from framework events (keep names consistent with your example)
            EnemyEnterVision += () => fsm.SetCondition(AICondition.SeesEnemy);
            Death += () => fsm.ChangeState(idle);
            Respawned += () => fsm.SetCondition(AICondition.Spawned);

            // Transitions
            fsm.AddTransition(idle, AICondition.Spawned, holdEdge);
            fsm.AddTransition(holdEdge, AICondition.SeesEnemy, combat);
            fsm.AddTransition(combat, AICondition.MoveToObjective, holdEdge);

            // Small periodic nudge to briefly contest center when no enemies are visible
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
            AllocateStat(StatType.VisionRange, 6);
            AllocateStat(StatType.ProjectileRange, 4);
            AllocateStat(StatType.ReloadSpeed, 4);
        }

        protected override void ExecuteAI() => fsm.Execute();
    }
}
