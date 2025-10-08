using System.Collections.Generic;
using UnityEngine;

namespace GOAP.AI
{
    public class Plan
    {
        public List<Action> Actions { get; private set; }
        public float TotalCost { get; private set; }

        /// <summary>
        /// Constructor for creating a plan
        /// </summary>
        public Plan()
        {
            Actions = new List<Action>();
            TotalCost = 0f;
        }

        /// <summary>
        /// Add an action to the plan
        /// </summary>
        /// <param name="action"></param>
        public void AddAction(Action action)
        {
            Actions.Add(action);
            TotalCost += action.Cost;
        }

        /// <summary>
        /// Checks if there is any actions
        /// </summary>
        /// <returns>True if there is no actions, otherwise false</returns>
        public bool IsEmpty()
        {
            return Actions.Count == 0;
        }

        /// <summary>
        /// Get the current action
        /// </summary>
        /// <returns>The current action if there is a action to run</returns>
        public Action GetCurrentAction()
        {
            return Actions.Count > 0 ? Actions[0] : null;
        }

        /// <summary>
        /// Remove a current action
        /// </summary>
        public void RemoveCurrentAction()
        {
            if (Actions.Count > 0)
            {
                Actions.RemoveAt(0);
            }
        }

        /// <summary>
        /// Returns a string reprsentation of the plan
        /// </summary>
        /// <returns>A formatted string showing the plan to the reach the goal</returns>
        public override string ToString()
        {
            string result = $"Plan (Cost: {TotalCost}): ";
            foreach (var action in Actions)
            {
                result += action.Name + " -> ";
            }
            return result + "GOAL";
        }
    }

    public class Planner
    {

    }
}
