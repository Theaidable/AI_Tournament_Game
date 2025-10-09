using AIGame.Core;
using System;

[RegisterFactory("HTN Team")]
public class HTNTeamFactory : AgentFactory
{
    /// <summary>
    /// Return�r de agent-typer fabrikken skal spawne.
    /// Bem�rk: Frameworket opretter komponenterne � vi skal kun angive typerne.
    /// </summary>
    protected override Type[] GetAgentTypes()
    {
        // Du f�r: 1 scout, 1 flag carrier, 1 point capturer, 2 snipere.
        // Begge snipere er samme type; vi kan tildele hj�rner via inspector eller en lille init-komponent senere.
        return new Type[]
        {
            typeof(ScoutAI),
            typeof(FlagCarrierAI),
            typeof(PointCapturerAI),
            typeof(SniperAI),
            typeof(SniperAI),
        };
    }
}
