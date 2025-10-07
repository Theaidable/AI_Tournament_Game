using System;
using AIGame.Core;
using UnityEngine;

namespace ReactiveAIFactory
{
    /// <summary>
    /// Factory that spawns ReactiveAIFactory agents.
    /// Creates a full team of agents using custom AI behaviour.
    /// </summary>
    
    [RegisterFactory("ReactiveTest AI")]
    public class ReactiveAIFactory : AgentFactory
    {
        /// <summary>
        /// Returns the agent types this factory wants to spawn.
        /// </summary>
        /// <returns>An array containing the AI types to spawn.</returns>
        protected override System.Type[] GetAgentTypes()
        {
            return new System.Type[] { typeof(ReactiveTest.ReactiveTest) };
        }
    }
}