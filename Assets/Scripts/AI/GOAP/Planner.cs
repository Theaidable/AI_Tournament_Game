using AIGame.Examples.GoalOriented;
using NUnit.Framework.Internal;
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
        public Plan CreatePlan(WorldState currentState, WorldState goalState, List<Action> availableActions)
        {
            if (currentState.Satisfies(goalState))
            {
                return new Plan();
            }

            // Simple greedy planning - not optimal but fast and demonstrates the concept
            List<Action> planActions = new List<Action>();
            WorldState workingState = currentState.Clone();

            int maxIterations = 10; // Prevent infinite loops
            int iterations = 0;

            while (!workingState.Satisfies(goalState) && iterations < maxIterations)
            {
                iterations++;

                Action bestAction = null;
                float bestScore = float.MinValue;

                // Find the best action to take from current state
                foreach (var action in availableActions)
                {
                    if (action.CanExecute(workingState))
                    {
                        // Score the action based on how much it helps us reach the goal
                        float score = ScoreAction(action, workingState, goalState);

                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestAction = action;
                        }
                    }

                }

                if (bestAction != null)
                {
                    planActions.Add(bestAction);
                    workingState = bestAction.ApplyEffects(workingState);

                }
                else
                {

                    break;
                }
            }

            Plan plan = new Plan();
            foreach (var action in planActions)
            {
                plan.AddAction(action);
            }


            return plan;
        }

        private float ScoreAction(Action action, WorldState currentState, WorldState goalState)
        {
            WorldState resultState = action.ApplyEffects(currentState);

            // Basic scoring: how many goal conditions does this action help achieve?
            float score = 0f;

            // Check if this action gets us closer to the goal
            if (resultState.Satisfies(goalState))
            {
                score += 100f; // High score for reaching the goal
            }

            // Subtract cost to prefer cheaper actions
            score -= action.Cost;

            //Priotetsliste

            if(currentState.GetState<bool>(StateKeys.INCOMING_DANGER) == true)
            {
                if(action is DodgeAction)
                {
                    score += 70;
                }
                else
                {
                    score -= 50;
                }
            }
            else if(currentState.GetState<bool>(StateKeys.ENEMY_VISIBLE) == true)
            {
                if(action is ShootAction)
                {
                    score += 70;
                }
                else
                {
                    score -= 50;
                }
            }
            else if (currentState.GetState<bool>(StateKeys.AT_CP) == false)
            {
                if (action is MoveToCPAction)
                {
                    score += 70;
                }
                else
                {
                    score -= 50;
                }
            }

            return score;
        }
    }
}
