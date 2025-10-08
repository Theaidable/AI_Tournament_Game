using AIGame.Core;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

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
        public IdleAction(BaseAI agent, float cost = 1) : base(agent, "Idle", cost) { }

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

    #region MoveToCPAction
    public class MoveToCPAction : Action
    {
        private Vector3 controlpointPosition;
        private float stopDistance = 2.5f;
        private NavMeshAgent nav;
        private bool destinationSet;

        public MoveToCPAction(BaseAI agent, float cost = 2f) : base(agent, "MoveToCP", cost) 
        {
            //Requires to know the position of the CP
            preconditions.SetState(StateKeys.KNOW_CP_POSITION, true);
        }

        protected override Dictionary<string, object> GetEffects()
        {
            var effects = new Dictionary<string, object>();
            effects[StateKeys.AT_CP] = true;
            return effects;
        }

        public override bool Execute()
        {
            var controlpoint = ControlPoint.Instance;
            
            nav = agent.GetComponent<NavMeshAgent>();
            controlpointPosition = controlpoint.transform.position;

            if(controlpoint == null)
            {
                return false;
            }

            if(nav != null)
            {
                nav.stoppingDistance = stopDistance;

                if(destinationSet == false || (nav.destination - controlpointPosition).sqrMagnitude > 0.1f)
                {
                    nav.SetDestination(controlpointPosition);
                    destinationSet = true;
                }

                return true;
            }

            return false;
        }

        public override bool IsComplete()
        {
            var controlpoint = ControlPoint.Instance;

            if (controlpoint == null)
            {
                return false;
            }

            float distance = Vector3.Distance(agent.transform.position, controlpoint.transform.position);
            
            return distance <= stopDistance;
        }
    }

    #endregion
}
