using AIGame.Core;
using UnityEngine;

public class SniperAI : BaseAI
{
    public enum Corner { A, B }
    [SerializeField] private Corner _corner = Corner.A;

    private HTNPlanner _planner;
    private HTNPlan _plan;
    private CompoundTask _root;

    protected override void ConfigureStats()
    {
        AllocateStat(StatType.Speed, 3);
        AllocateStat(StatType.VisionRange, 9);
        AllocateStat(StatType.ProjectileRange, 8);
        AllocateStat(StatType.ReloadSpeed, 0);
        AllocateStat(StatType.DodgeCooldown, 0);
    }

    protected override void StartAI()
    {
        _planner = new HTNPlanner();
        BuildDomain();
    }

    Vector3 CornerAnchor()
    {
        var cp = GameManager.Instance.Objective.transform.position;
        var off = (_corner == Corner.A) ? new Vector3(-6, 0, 6) : new Vector3(6, 0, -6);
        return cp + off;
    }

    void BuildDomain()
    {
        _root = new CompoundTask("SniperRoot");

        var m = new Method { Applicability = _ => true };
        m.Subtasks.Add(Primitives.MoveToControlPoint(this, 2.0f));
        m.Subtasks.Add(Primitives.MoveTo(_ => CornerAnchor(), this, 0.75f));
        m.Subtasks.Add(Primitives.HoldPosition(this, _ => CornerAnchor(), 0.75f));

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
