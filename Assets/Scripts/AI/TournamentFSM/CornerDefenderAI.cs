using AIGame.Examples.FSM;
using UnityEngine;
using static AIGame.Core.BaseAI;

namespace AIGame.TournamentFSM
{
    // Shared logic for CP-corner defenders: park at a fixed offset on our side and hold.
    public abstract class CornerDefenderAI : FinitStateAI
    {
        private FSM fsm;
        private AIState idle, combat, holdCorner;

        // Subclasses define their corner offset relative to CP (world space).
        protected abstract Vector3 CornerOffset { get; }

        protected override void StartAI()
        {
            fsm = new FSM();

            var strafe = new Strafe(this);
            var follow = new FollowEnemy(this);
            var dodge = new Dodge(this);
            var combat = new Combat(this, strafe, follow, dodge);
            idle = new Idle(this);

            AIState shortSearch = null;
            shortSearch = new ShortSearch(this,
                onTimeout: () => fsm.SetCondition(AICondition.MoveToObjective),
                2.0f, dodge, strafe);

            BallDetected += ball => dodge.OnBallDetected(ball);
            EnemyEnterVision += () => fsm.SetCondition(AICondition.SeesEnemy);
            Death += () => fsm.ChangeState(idle);
            Respawned += () => fsm.SetCondition(AICondition.Spawned);

            combat.NoMoreEnemies += () => fsm.ChangeState(shortSearch);

            holdCorner = new GuardAtOffset(this, CornerOffset, dodge);

            fsm.AddTransition(idle, AICondition.Spawned, holdCorner);
            fsm.AddTransition(holdCorner, AICondition.SeesEnemy, combat);
            fsm.AddTransition(combat, AICondition.MoveToObjective, holdCorner);
            fsm.AddTransition(shortSearch, AICondition.SeesEnemy, combat);
            fsm.AddTransition(shortSearch, AICondition.MoveToObjective, holdCorner);

            fsm.ChangeState(holdCorner);
        }

        protected override void ConfigureStats()
        {
            // Slightly more defensive profile; holds angle and trades.
            AllocateStat(StatType.ProjectileRange, 7);
            AllocateStat(StatType.ReloadSpeed, 2);
            AllocateStat(StatType.VisionRange, 8);
            AllocateStat(StatType.Speed, 3);
        }

        protected override void ExecuteAI() => fsm.Execute();
    }

    // Corner on our base-left side (relative to standard world axes).
    public class CornerDefenderLeft : CornerDefenderAI
    {
        // Negative X and Negative Z puts us back-left from CP (towards our base by convention).
        protected override Vector3 CornerOffset => new Vector3(-4f, 0f, -4f);
    }

    // Corner on our base-right side.
    public class CornerDefenderRight : CornerDefenderAI
    {
        protected override Vector3 CornerOffset => new Vector3(4f, 0f, -4f);
    }
}
