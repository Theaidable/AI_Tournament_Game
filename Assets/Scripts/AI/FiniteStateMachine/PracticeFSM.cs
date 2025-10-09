using System.Collections.Generic;
using UnityEngine;

namespace Practice.AI
{
    /// <summary>
    /// Represents the possible conditions that can trigger a state change.
    /// </summary>
    public enum AICondition
    {
        None,
        Spawned,
        Idle,
        SeesEnemy,
        MoveToObjective,
        Investigate,
        Protect,
        EnemyZone,
        EnemyFlag,
        FriendlyZone,
        FriendlyFlag,
    }

    /// <summary>
    /// Provides a finite state machine implementation for switching between states
    /// </summary>
    public class PracticeFSM
    {
        /// <summary>
        /// The current active state.
        /// </summary>
        protected PracticeAIState currentState;

        /// <summary>
        /// Maps a tuple of (current state, condition) to the next state.
        /// </summary>
        protected Dictionary<(PracticeAIState, AICondition), PracticeAIState> transitions = new();

        /// <summary>
        /// The condition currently set, which may trigger a state change.
        /// </summary>
        protected AICondition currentCondition = AICondition.None;

        /// <summary>
        /// Main update logic for the AI.
        /// Runs once per frame as part of <see cref="BaseAI"/> execution.
        /// </summary>
        public void Execute()
        {
            ProcessTransitions();
            currentCondition = AICondition.None;

            if (currentState != null)
            {
                currentState.Execute();
            }
        }

        /// <summary>
        /// Sets the current condition to be evaluated in the next update cycle.
        /// </summary>
        /// <param name="condition">The new condition value.</param>
        public void SetCondition(AICondition condition)
        {
            currentCondition = condition;
        }

        /// <summary>
        /// Adds a state transition rule.
        /// </summary>
        /// <param name="from">The starting state.</param>
        /// <param name="condition">The condition that triggers the change.</param>
        /// <param name="to">The target state.</param>
        public void AddTransition(PracticeAIState from, AICondition condition, PracticeAIState to)
        {
            // CHANGE: Guard against nulls and accidental self-transitions unless explicitly intended.
            if (from == null || to == null)
            {
                Debug.LogError($"[FSM] Tried to add transition with null state. From: {from}, To: {to}, Cond: {condition}");
                return;
            }

            // NOTE: Self-transitions are allowed in our current setup (e.g. MoveToObjective -> MoveToObjective),
            // so we do NOT block them. We simply overwrite if the key already exists.
            transitions[(from, condition)] = to;
        }

        /// <summary>
        /// Checks if the current state and condition match any registered transition rule.
        /// Changes the state if a match is found.
        /// </summary>
        private void ProcessTransitions()
        {
            // CHANGE: Fast exit if no current state (e.g., before first ChangeState)
            if (currentState == null) return;

            if (transitions.TryGetValue((currentState, currentCondition), out var newState))
            {
                ChangeState(newState);
            }
        }

        /// <summary>
        /// Changes the current state, calling <see cref="PracticeAIState.Exit"/> on the old state
        /// and <see cref="PracticeAIState.Enter"/> on the new state.
        /// </summary>
        /// <param name="newState">The state to switch to.</param>
        public void ChangeState(PracticeAIState newState)
        {
            if (newState == null)
            {
                // CHANGE: Safety log — helps catch setup mistakes when wiring transitions.
                Debug.LogWarning("[FSM] ChangeState called with null newState.");
                return;
            }

            if (currentState != null)
            {
                currentState.Exit();
            }

            currentState = newState;
            currentState.Enter();
        }
    }
}
