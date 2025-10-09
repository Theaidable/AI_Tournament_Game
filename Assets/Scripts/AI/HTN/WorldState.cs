// En let wrapper der samler det, agenten "ved" nu + team-intel fra blackboard
using UnityEngine;
using System.Collections.Generic;

public class WorldState
{
    public Vector3 MyPos;
    public Vector3 TeamBasePos;
    public bool ControlPointOwnedByTeam;
    public Vector3 ControlPointPos;
    public bool CanSeeEnemy;
    public Vector3? LastSeenEnemyPos;
    public bool CanSeeEnemyFlag;
    public Vector3? EnemyFlagPos;
    public bool HasEnemyFlag;

    // Team intel (læses/skrives via Blackboard)
    public List<Vector3> TeamSpottedEnemies = new();
    public Vector3? TeamKnownEnemyFlagPos;

    public WorldState Clone()
    {
        var c = (WorldState)MemberwiseClone();
        c.TeamSpottedEnemies = new List<Vector3>(TeamSpottedEnemies);
        return c;
    }
}
