using AIGame.Core;
using System;

[RegisterFactory("HTN Team")]
public class HTNTeamFactory : AgentFactory
{
    /// <summary>
    /// Returnér de agent-typer fabrikken skal spawne.
    /// Bemærk: Frameworket opretter komponenterne – vi skal kun angive typerne.
    /// </summary>
    protected override Type[] GetAgentTypes()
    {
        // Du får: 1 scout, 1 flag carrier, 1 point capturer, 2 snipere.
        // Begge snipere er samme type; vi kan tildele hjørner via inspector eller en lille init-komponent senere.
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
