using AIGame.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simpel statisk "shared memory" mellem agenter.
/// Bruges af HTN-AI'erne til at gemme og hente team-information.
/// </summary>
public class Blackboard
{
    private static readonly Dictionary<string, object> _shared = new();

    public static Blackboard GetShared(BaseAI ai)
    {
        // Frameworket forventer GetShared(ai), men vi ignorerer ai i denne simple version.
        return new Blackboard();
    }

    public bool HasKey(string key) => _shared.ContainsKey(key);

    public T GetValue<T>(string key)
    {
        if (_shared.TryGetValue(key, out var val) && val is T typed)
            return typed;
        return default;
    }

    public void SetValue(string key, object value)
    {
        _shared[key] = value;
    }
}
