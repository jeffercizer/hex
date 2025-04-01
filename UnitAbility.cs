using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;
using System.Formats.Asn1;

public class UnitAbility
{
    public UnitEffect effect;
    public float combatPower;
    public int currentUsage;
    public int maxUsagePerTurn;
    public int range;
    public UnitAbility(UnitEffect effect, float combatPower = 0.0f, int maxUsagePerTurn = 1, int range = 0)
    {
        this.effect = effect;
        this.combatPower = combatPower;
        this.maxUsagePerTurn = maxUsagePerTurn;
        this.range = range;
    }

    public bool ActivateAbility(Unit usingUnit, var abilityTarget)
    {
        if(currentUsage < maxUsagePerTurn)
        {
            currentUsage += 1;
            effect.Apply(usingUnit);
        }
    }
}
