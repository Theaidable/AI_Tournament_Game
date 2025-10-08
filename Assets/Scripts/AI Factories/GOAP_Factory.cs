using System;
using AIGame.Core;
using UnityEngine;

namespace GOAP.AI.Factory
{
    /// <summary>
    /// Factory that spawns GOAP_Factory agents.
    /// Creates a full team of agents using custom AI behaviour.
    /// </summary>
    [RegisterFactory("GOAP FACTORY")]
    public class GOAP_Factory : AgentFactory
    {
        /// <summary>
        /// Returns the agent types this factory wants to spawn.
        /// </summary>
        /// <returns>An array containing the AI types to spawn.</returns>
        protected override Type[] GetAgentTypes()
        {
            return new Type[] { typeof(GOAP_AI) };
        }
    }
}