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
    public int currentCharges;
    public int maxChargesPerTurn; //-1 means no reset
    public int range;
    public TargetSpecification validTargetTypes;
    
    public UnitAbility(UnitEffect effect, float combatPower = 0.0f, int maxUsagePerTurn = 1, int range = 0, validTargetTypes = new TargetSpecification())
    {
        name = effect.functionName;
        this.effect = effect;
        this.combatPower = combatPower;
        this.maxUsagePerTurn = maxUsagePerTurn;
        this.range = range;
        this.validTargetTypes = validTargetTypes;
    }

    public void ResetAbilityUses()
    {
        if(maxChargesPerTurn > -1)
        {
            currentCharges = maxChargesPerTurn;
        }
    }

    public bool ActivateAbility(Unit usingUnit, GameHex abilityTarget = null)
    {
        if(currentCharges > 0)
        {
            currentCharges -= 1;
            if(abilityTarget != null)
            {
                effect.Apply(usingUnit, combatPower, abilityTarget);
            }
            else
            {
                effect.Apply(usingUnit, combatPower);
            }

        }
    }
}
