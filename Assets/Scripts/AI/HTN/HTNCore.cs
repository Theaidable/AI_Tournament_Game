using System;
using System.Collections.Generic;

public abstract class HTNTask
{
    public string Name { get; }
    protected HTNTask(string name) => Name = name;
}

public sealed class PrimitiveTask : HTNTask
{
    public Func<WorldState, bool> Preconditions;
    public Func<WorldState, TaskStatus> Execute;
    public Action<WorldState> ApplyEffects; // opdater lokal model efter succes

    public PrimitiveTask(string name) : base(name) { }
}

public sealed class Method
{
    public Func<WorldState, bool> Applicability; // hvornår denne metode er passende
    public List<HTNTask> Subtasks = new();
}

public sealed class CompoundTask : HTNTask
{
    public List<Method> Methods = new();
    public CompoundTask(string name) : base(name) { }
}

public sealed class HTNPlan
{
    private readonly Queue<PrimitiveTask> _steps = new();
    public bool IsEmpty => _steps.Count == 0;
    public void Enqueue(PrimitiveTask t) => _steps.Enqueue(t);
    public PrimitiveTask Current => _steps.Peek();
    public void Pop() => _steps.Dequeue();
}

public sealed class HTNPlanner
{
    // Depth-first dekomposition fra et toplevel CompoundTask
    public bool TryPlan(WorldState ws, CompoundTask root, out HTNPlan plan)
    {
        plan = new HTNPlan();
        var tmp = new HTNPlan();
        if (Decompose(ws.Clone(), root, tmp))
        {
            plan = tmp;
            return true;
        }
        return false;
    }

    private bool Decompose(WorldState ws, HTNTask task, HTNPlan acc)
    {
        if (task is PrimitiveTask pt)
        {
            if (pt.Preconditions?.Invoke(ws) == false) return false;
            // Optimistisk: anvend effekter på plan-tidspunkt (simuleret)
            pt.ApplyEffects?.Invoke(ws);
            acc.Enqueue(pt);
            return true;
        }

        var ct = (CompoundTask)task;
        foreach (var m in ct.Methods)
        {
            if (m.Applicability != null && !m.Applicability(ws)) continue;

            var wsBranch = ws.Clone();
            var accBranch = new HTNPlan();
            bool ok = true;

            foreach (var st in m.Subtasks)
            {
                if (!Decompose(wsBranch, st, accBranch)) { ok = false; break; }
            }
            if (ok)
            {
                // merge accBranch ind i acc
                while (!accBranch.IsEmpty)
                {
                    acc.Enqueue(accBranch.Current);
                    accBranch.Pop();
                }
                return true;
            }
        }
        return false;
    }
}
