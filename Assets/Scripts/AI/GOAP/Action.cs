using AIGame.Core;
using System.Collections.Generic;
using UnityEngine;

namespace GOAP.AI
{
    public abstract class Action
    {
        protected WorldState preconditions = new WorldState();
        protected WorldState effects = new WorldState();

        protected BaseAI agent;

        public string Name { get; protected set; }
        public float Cost { get; protected set; }

        /// <summary>
        /// Constructor for Actions
        /// </summary>
        /// <param name="agent"></param>
        /// <param name="name"></param>
        /// <param name="cost"></param>
        public Action(BaseAI agent, string name, float cost = 1f)
        {
            this.agent = agent;
            this.Name = name;
            this.Cost = cost;
        }

        public virtual bool CanExecute(WorldState worldState)
        {
            return worldState.Satisfies(preconditions);
        }

        public WorldState ApplyEffects(WorldState currentState)
        {
            WorldState newState = currentState.Clone();

            // Apply all effects to the new state
            foreach (var effect in GetEffects())
            {
                newState.SetState(effect.Key, effect.Value);
            }

            return newState;
        }

        protected virtual Dictionary<string, object> GetEffects()
        {
            var effectDict = new Dictionary<string, object>();
            return effectDict;
        }

        public abstract bool Execute();
        public abstract bool IsComplete();
    }

    #region IdleAction
    public class IdleAction : Action
    {
        public IdleAction(BaseAI agent, float cost) : base(agent, "Idle", 1) { }

        public override bool Execute()
        {
            return true;
        }

        public override bool IsComplete()
        {

            return false;
        }
    }
    #endregion
}
