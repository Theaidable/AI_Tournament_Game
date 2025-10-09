using AIGame.Examples.FSM;
using UnityEngine;
using static AIGame.Core.BaseAI;

namespace AIGame.TournamentFSM
{
    // Center anchor: holds near CP middle and snaps into combat quickly.
    public class MidWatchAI : FinitStateAI
    {
        private FSM fsm;
        private AIState idle, combat, holdMid;

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

            holdMid = new HoldCenterAndLookAround(this, dodge, strafe);

            fsm.AddTransition(idle, AICondition.Spawned, holdMid);
            fsm.AddTransition(holdMid, AICondition.SeesEnemy, combat);
            fsm.AddTransition(combat, AICondition.MoveToObjective, holdMid);
            fsm.AddTransition(shortSearch, AICondition.SeesEnemy, combat);
            fsm.AddTransition(shortSearch, AICondition.MoveToObjective, holdMid);

            fsm.ChangeState(holdMid);
        }

        protected override void ConfigureStats()
        {
            // Vision-forward profile; stays calm in center and spots threats fast
            AllocateStat(StatType.VisionRange, 7);
            AllocateStat(StatType.ReloadSpeed, 3);
            AllocateStat(StatType.ProjectileRange, 6);
            AllocateStat(StatType.Speed, 4);
        }

        protected override void ExecuteAI() => fsm.Execute();
    }
}
