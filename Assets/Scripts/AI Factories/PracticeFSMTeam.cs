using System;
using AIGame.Core;
using Practice.AI;
using UnityEngine;

namespace Practice.Factory
{
    /// <summary>
    /// Factory that spawns PracticeFSMTeam agents.
    /// Creates a full team of agents using custom AI behaviour.
    /// </summary>
    [RegisterFactory("Practice FSM")]
 
    public class PracticeFSMTeam : AgentFactory
    {
        /// <summary>
        /// Returns the agent types this factory wants to spawn.
        /// </summary>
        /// <returns>An array containing the AI types to spawn.</returns>
        protected override Type[] GetAgentTypes()
        {
            // CHANGE: Added explicit 'typeof(PracticeFSMAI)' to make it clear this factory spawns our FSM AI.
            // If we later rename the class/namespace, update this type accordingly.
            return new Type[] 
            { 
                typeof(PracticeFSMAI),
                typeof(SpearheadFSMAI),
            };
        }
    }
}
