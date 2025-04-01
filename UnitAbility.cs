using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;
using System.Formats.Asn1;

public class UnitAbility
{
    public String name;
    public UnitEffect effect;
    public float combatPower;
    public int currentUsage;
    public int maxUsagePerTurn;
    public int range;
    
    public UnitAbility(UnitEffect effect, float combatPower = 0.0f, int maxUsagePerTurn = 1, int range = 0)
    {
        name = effect.functionName;
        this.effect = effect;
        this.combatPower = combatPower;
        this.maxUsagePerTurn = maxUsagePerTurn;
        this.range = range;
    }

    public bool ActivateAbility(Unit usingUnit, var abilityInfo = null)
    {
        if(currentUsage < maxUsagePerTurn)
        {
            currentUsage += 1;
            if(abilityInfo != null)
            {
                effect.Apply(usingUnit, abilityInfo);
            }
            else
            {
                effect.Apply(usingUnit);
            }

        }
    }
}
