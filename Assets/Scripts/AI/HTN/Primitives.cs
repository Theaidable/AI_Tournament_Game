using AIGame.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Primitives
{
    // Simpel MoveTo – målopnåelse måles på transform.position
    public static PrimitiveTask MoveTo(Func<WorldState, Vector3> target, BaseAI ai, float stopDist = 1.0f)
    {
        var t = new PrimitiveTask("MoveTo");
        t.Preconditions = _ => true;
        t.Execute = ws =>
        {
            var p = target(ws);
            ai.MoveTo(p);
            return Vector3.Distance(ai.transform.position, p) <= stopDist
                ? TaskStatus.Success
                : TaskStatus.Running;
        };
        return t;
    }

    // Kort “kig rundt” – kan fx trigge scan/rotation hvis din BaseAI har noget tilsvarende
    public static PrimitiveTask LookAround(BaseAI ai, float seconds = 0.5f)
    {
        float start = -1f;
        var t = new PrimitiveTask("LookAround");
        t.Preconditions = _ => true;
        t.Execute = ws =>
        {
            if (start < 0f) start = Time.time;
            // Hvis du har en scan/rotate-funktion, kan du kalde den her:
            // ai.Scan();
            return (Time.time - start) >= seconds ? TaskStatus.Success : TaskStatus.Running;
        };
        return t;
    }

    // Rapporter simple observationer til et delt blackboard (valgfrit – behold kun hvis du bruger Blackboard)
    public static PrimitiveTask ReportIntelToTeam(BaseAI ai, Func<WorldState, (IEnumerable<Vector3> enemies, Vector3? flag)> gather)
    {
        var t = new PrimitiveTask("ReportIntel");
        t.Preconditions = _ => true;
        t.Execute = ws =>
        {
            var (enemies, flag) = gather(ws);

            var bb = Blackboard.GetShared(ai);
            var list = bb.HasKey(BlackboardKeys.TEAM_SPOTTED_ENEMIES)
                ? bb.GetValue<List<Vector3>>(BlackboardKeys.TEAM_SPOTTED_ENEMIES)
                : new List<Vector3>();
            list.AddRange(enemies);
            bb.SetValue(BlackboardKeys.TEAM_SPOTTED_ENEMIES, list);

            if (flag.HasValue)
                bb.SetValue(BlackboardKeys.TEAM_FLAG_POS, flag.Value);

            return TaskStatus.Success;
        };
        return t;
    }

    // Hold en position og engager fjender hvis de ses
    public static PrimitiveTask HoldPosition(BaseAI ai, Func<WorldState, Vector3> anchor, float radius = 0.75f)
    {
        var t = new PrimitiveTask("HoldPosition");
        t.Preconditions = _ => true;
        t.Execute = ws =>
        {
            var p = anchor(ws);
            if (Vector3.Distance(ai.transform.position, p) > radius)
                ai.MoveTo(p);

            var visibles = ai.GetVisibleEnemiesSnapshot();
            if (visibles != null && visibles.Count > 0)
            {
                var me = ai.transform.position;
                var closest = visibles.OrderBy(e => Vector3.Distance(e.Position, me)).First();
                ai.ThrowBallAt(closest); // vigtigt: kast mod target-objektet, ikke en Vector3
            }
            return TaskStatus.Running; // stående node
        };
        return t;
    }

    // Aggressiv engagering – går ind til en afstand og kaster mod nærmeste
    public static PrimitiveTask EngageEnemies(BaseAI ai, float keepDist = 10f)
    {
        var t = new PrimitiveTask("EngageEnemies");
        t.Preconditions = ws =>
        {
            var v = ai.GetVisibleEnemiesSnapshot();
            return v != null && v.Count > 0;
        };

        t.Execute = ws =>
        {
            var visibles = ai.GetVisibleEnemiesSnapshot();
            if (visibles == null || visibles.Count == 0)
                return TaskStatus.Success; // Intet at gøre

            var me = ai.transform.position;
            var closest = visibles.OrderBy(e => Vector3.Distance(e.Position, me)).First();

            float d = Vector3.Distance(me, closest.Position);
            if (d > keepDist)
                ai.MoveTo(closest.Position);

            ai.ThrowBallAt(closest);
            return TaskStatus.Running;
        };
        return t;
    }

    // Hjælper: Move til Control Point (CP) – CP tages fra GameManager/Objective
    public static PrimitiveTask MoveToControlPoint(BaseAI ai, float stopDist = 2.0f)
    {
        var t = new PrimitiveTask("MoveToCP");
        t.Preconditions = _ => GameManager.Instance != null && GameManager.Instance.Objective != null;
        t.Execute = ws =>
        {
            var cp = GameManager.Instance.Objective.transform.position;
            ai.MoveTo(cp);
            return Vector3.Distance(ai.transform.position, cp) <= stopDist
                ? TaskStatus.Success
                : TaskStatus.Running;
        };
        return t;
    }
}
