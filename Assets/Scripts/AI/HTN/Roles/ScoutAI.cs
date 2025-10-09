using AIGame.Core;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ScoutAI : BaseAI
{
    private HTNPlanner _planner;
    private HTNPlan _plan;
    private CompoundTask _root;

    private readonly List<Vector3> _route = new();

    protected override void ConfigureStats()
    {
        AllocateStat(StatType.Speed, 10);
        AllocateStat(StatType.VisionRange, 10);
        AllocateStat(StatType.ProjectileRange, 0);
        AllocateStat(StatType.ReloadSpeed, 0);
        AllocateStat(StatType.DodgeCooldown, 0);
    }

    protected override void StartAI()
    {
        _planner = new HTNPlanner();
        BuildRoute();
        BuildDomain();
    }

    void BuildRoute()
    {
        var cp = GameManager.Instance.Objective.transform.position;
        _route.Add(cp + new Vector3(12, 0, 12));
        _route.Add(cp + new Vector3(-12, 0, 12));
        _route.Add(cp + new Vector3(-12, 0, -12));
        _route.Add(cp + new Vector3(12, 0, -12));
    }

    void BuildDomain()
    {
        _root = new CompoundTask("ScoutRoot");
        var m = new Method { Applicability = _ => true };

        for (int i = 0; i < _route.Count; i++)
        {
            int local = i;
            m.Subtasks.Add(Primitives.MoveTo(_ => _route[local], this, 1.0f));
            m.Subtasks.Add(Primitives.LookAround(this, 0.6f));

            // (Valgfrit) – del observationer med holdet via blackboard:
            m.Subtasks.Add(Primitives.ReportIntelToTeam(this, ws =>
            {
                var enemies = GetVisibleEnemiesSnapshot()?.Select(e => e.Position) ?? Enumerable.Empty<Vector3>();
                Vector3? flagPos = null; // tilføj når vi har korrekt flag-API
                return (enemies, flagPos);
            }));

            // Hvis scout ser fjender, så kast og smut videre
            m.Subtasks.Add(Primitives.EngageEnemies(this, 10f));
        }

        _root.Methods.Add(m);
    }

    protected override void ExecuteAI()
    {
        var ws = new WorldState { MyPos = transform.position };

        if (_plan == null || _plan.IsEmpty || !_planner.TryPlan(ws, _root, out _plan))
            _planner.TryPlan(ws, _root, out _plan);

        if (_plan == null || _plan.IsEmpty) return;

        var step = _plan.Current;
        var status = step.Execute(ws);
        if (status == TaskStatus.Success) _plan.Pop();
    }
}
