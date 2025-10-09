using AIGame.Core;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FlagCarrierAI : BaseAI
{
    private HTNPlanner _planner;
    private HTNPlan _plan;
    private CompoundTask _root;

    private readonly List<Vector3> _searchHints = new();
    private int _hintIdx = 0;

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
        BuildSearchHints();
        BuildDomain();
    }

    void BuildSearchHints()
    {
        var cp = GameManager.Instance.Objective.transform.position;
        _searchHints.Clear();
        _searchHints.Add(cp + new Vector3(14, 0, 14));
        _searchHints.Add(cp + new Vector3(-14, 0, 14));
        _searchHints.Add(cp + new Vector3(-14, 0, -14));
        _searchHints.Add(cp + new Vector3(14, 0, -14));
    }

    Vector3 NextHint() => _searchHints[_hintIdx++ % _searchHints.Count];

    void BuildDomain()
    {
        _root = new CompoundTask("FlagCarrierRoot");

        // 1) Patruljér efter flag/indsigt
        var mSearch = new Method { Applicability = _ => true };
        mSearch.Subtasks.Add(Primitives.MoveTo(_ => NextHint(), this, 1.5f));
        mSearch.Subtasks.Add(Primitives.LookAround(this, 0.6f));

        // 2) Hvis fjender er synlige, engager dem lidt defensivt
        mSearch.Subtasks.Add(Primitives.EngageEnemies(this, 12f));

        // 3) Falback “retning hjem” – kan senere udskiftes med rigtig HasEnemyFlag/score-logik
        mSearch.Subtasks.Add(Primitives.MoveTo(ws => GetMyTeamBasePositionSafe(), this, 1.5f));

        _root.Methods.Add(mSearch);
    }

    protected override void ExecuteAI()
    {
        var ws = AssessWorld();
        if (_plan == null || _plan.IsEmpty || !_planner.TryPlan(ws, _root, out _plan))
            _planner.TryPlan(ws, _root, out _plan);

        if (_plan == null || _plan.IsEmpty) return;

        var step = _plan.Current;
        var status = step.Execute(ws);
        if (status == TaskStatus.Success) _plan.Pop();
    }

    Vector3 GetMyTeamBasePositionSafe()
    {
        // Har du en helper i BaseAI, så brug den i stedet:
        // return GetMyTeamBasePosition();
        // Ellers placer "basen" som et punkt bag CP (midlertidigt):
        var cp = GameManager.Instance.Objective.transform.position;
        return cp + new Vector3(-20, 0, 0);
    }

    WorldState AssessWorld()
    {
        var ws = new WorldState
        {
            MyPos = transform.position
        };
        return ws;
    }
}
