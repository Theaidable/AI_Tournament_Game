using AIGame.Core;
using UnityEngine;

public class PointCapturerAI : BaseAI
{
    private HTNPlanner _planner;
    private HTNPlan _plan;
    private CompoundTask _root;

    protected override void ConfigureStats()
    {
        AllocateStat(StatType.Speed, 5);
        AllocateStat(StatType.VisionRange, 6);
        AllocateStat(StatType.ProjectileRange, 5);
        AllocateStat(StatType.ReloadSpeed, 4);
        AllocateStat(StatType.DodgeCooldown, 0);
    }

    protected override void StartAI()
    {
        _planner = new HTNPlanner();
        BuildDomain();
    }

    void BuildDomain()
    {
        _root = new CompoundTask("CPRoot");

        var m = new Method { Applicability = _ => true };
        m.Subtasks.Add(Primitives.MoveToControlPoint(this, 2.0f));

        // Hold kant af CP og engager fjender
        m.Subtasks.Add(Primitives.HoldPosition(
            this,
            ws => GameManager.Instance.Objective.transform.position,
            1.0f));

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
