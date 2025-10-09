using System.Collections.Generic;
using UnityEngine;

namespace GOAP.AI
{
    /// <summary>
    /// This class keeps information about the state of the world
    /// The state of the world is used in the making of the best plan
    /// </summary>
    public class WorldState
    {
        private Dictionary<string, object> state = new Dictionary<string, object>();

        /// <summary>
        /// Sets a state variable to the specified value
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void SetState(string key, object value)
        {
            state[key] = value;
        }

        /// <summary>
        /// Get a state variable of a specific type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns>The State value cast to type T, or default(T) if nothing is found</returns>
        public T GetState<T>(string key)
        {
            if (state.ContainsKey(key))
            {
                return (T)state[key];
            }

            return default(T);
        }

        /// <summary>
        /// Checks if a state variable exists
        /// </summary>
        /// <param name="key"></param>
        /// <returns>True if the sstate variable exists, otherwise it will return false</returns>
        public bool HasState(string key)
        {
            return state.ContainsKey(key);
        }

        /// <summary>
        /// Createsas a deep copy of this world state
        /// </summary>
        /// <returns>A new WorldState wioth the same key-value pairs</returns>
        public WorldState Clone()
        {
            WorldState clone = new WorldState();

            foreach (var kvp in state)
            {
                clone.state[kvp.Key] = kvp.Value;
            }

            return clone;
        }

        /// <summary>
        /// Checks if thbis world state satisfies all conditions in the goal state
        /// </summary>
        /// <param name="goal"></param>
        /// <returns>True if all goal condtions are met, otherwise false</returns>
        public bool Satisfies(WorldState goal)
        {
            foreach (var kvp in goal.state)
            {
                if(state.ContainsKey(kvp.Key) == false || state[kvp.Key].Equals(kvp.Value) == false)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns a string reprsentation of this world state
        /// </summary>
        /// <returns>A formatted string showing all state variables and values</returns>
        public override string ToString()
        {
            string result = "WorldState: ";

            foreach (var kvp in state)
            {
                result += $"{kvp.Key}={kvp.Value}, ";
            }

            return result;
        }
    }

    public static class StateKeys
    {
        // CP / objektiv
        public const string KNOW_CP_POSITION = "KnowCPPosition";
        public const string CONTROL_POINT_POSITION = "ControlPointPosition";
        public const string AT_CP = "AtCP";
        public const string HAS_CP = "HasCP";
        public const string ENEMY_HAS_CP = "EnemyHasCP";
        public const string CP_SECURED = "CPSecured";

        // Kamp
        public const string ENEMY_VISIBLE = "EnemyVisible";
        public const string IN_COMBAT = "InCombat";
        public const string IN_OPT_RANGE = "InOptRange";
        public const string ENEMY_RANGE_ADVANTAGE = "EnemyRangeAdvantage";
        public const string LAST_ENEMY_DIR = "LastEnemyDir";

        // Overlevelse
        public const string INCOMING_DANGER = "IncomingDanger";

        // Formation / patrulje
        public const string ALLY_COUNT_NEARBY = "AllyCountNearby";
        public const string FORMATION_SIZE = "FormationSize";
        public const string PATROL_POINTS_SET = "PatrolPointsSet";
    }
}
