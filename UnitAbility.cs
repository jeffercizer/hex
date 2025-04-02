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
    
    public UnitAbility(UnitEffect effect, float combatPower = 0.0f, int maxChargesPerTurn = 1, int range = 0, TargetSpecification validTargetTypes = null)
    {
        name = effect.functionName;
        this.effect = effect;
        this.combatPower = combatPower;
        this.maxChargesPerTurn = maxChargesPerTurn;
        this.currentCharges = maxChargesPerTurn;
        this.range = range;
        if(validTargetTypes == null)
        {
            validTargetTypes = new TargetSpecification();
        }
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
                return effect.Apply(usingUnit, combatPower, abilityTarget);
            }
            else
            {
                return effect.Apply(usingUnit, combatPower);
            }
        }
        return false;
    }

    // public List<Hex> ValidAbilityTargets(Unit unit)
    // {
    //     foreach(Hex hex in unit.gameHex.hex.WrappingRange(range, unit.gameHex.gameBoard.left, unit.gameHex.gameBoard.right, unit.gameHex.gameBoard.top, unit.gameHex.gameBoard.bottom))
    //     {
    //         IsValidTarget(UnitType? unitType, UnitClass? unitClass, BuildingType? buildingType, TerrainType? terrainType, bool isEnemy = false, bool isAlly = false)
    //         //TODO
    //     }
    // }
}
