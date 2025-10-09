using System;
using AIGame.Core;

namespace AIGame.TournamentFSM
{
    // Factory that spawns a mixed team of our Tournament FSM roles.
    [RegisterFactory("Tournament FSM")]
    public class RoleBasedTournamentFactory : AgentFactory
    {
        // Order defines 5v5 lineup: 1 edge pusher, 2 mid anchors, 2 corner defenders.
        protected override Type[] GetAgentTypes()
        {
            return new Type[]
            {
                typeof(EdgeSentinelAI),
                typeof(MidWatchAI),
                typeof(MidWatchAI),
                typeof(CornerDefenderLeft),
                typeof(CornerDefenderRight),
            };
        }
    }
}
