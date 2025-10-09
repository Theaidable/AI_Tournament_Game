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

            // Shared combat micro
            var strafe = new Strafe(this);
            var follow = new FollowEnemy(this);
            var dodge = new Dodge(this);
            combat = new Combat(this, strafe, follow, dodge);

            idle = new Idle(this);
            holdMid = new HoldCenterAndLookAround(this, dodge, strafe);

            // Conditions
            EnemyEnterVision += () => fsm.SetCondition(AICondition.SeesEnemy);
            Death += () => fsm.ChangeState(idle);
            Respawned += () => fsm.SetCondition(AICondition.Spawned);

            // Transitions
            fsm.AddTransition(idle, AICondition.Spawned, holdMid);
            fsm.AddTransition(holdMid, AICondition.SeesEnemy, combat);
            fsm.AddTransition(combat, AICondition.MoveToObjective, holdMid);

            fsm.ChangeState(holdMid);
        }

        protected override void ConfigureStats()
        {
            // Vision-forward profile; stays calm in center and spots threats fast
            AllocateStat(StatType.VisionRange, 7);
            AllocateStat(StatType.ReloadSpeed, 5);
            AllocateStat(StatType.ProjectileRange, 4);
            AllocateStat(StatType.Speed, 4);
        }

        protected override void ExecuteAI() => fsm.Execute();
    }
}
