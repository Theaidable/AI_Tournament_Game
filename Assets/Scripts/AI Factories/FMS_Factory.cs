using System;
using AIGame.Core;
using UnityEngine;

namespace FiniteStateMachine
{
    /// <summary>
    /// Factory that spawns NewAgentFactory agents.
    /// Creates a full team of agents using custom AI behaviour.
    /// </summary>
    [RegisterFactory("FSM Test")]
    public class NewAgentFactory : AgentFactory
    {
        /// <summary>
        /// Returns the agent types this factory wants to spawn.
        /// </summary>
        /// <returns>An array containing the AI types to spawn.</returns>
        protected override System.Type[] GetAgentTypes()
        {
            return new System.Type[] { typeof(FiniteStateMachine.FMS) };
        }
    }
}