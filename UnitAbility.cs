using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;
using System.Formats.Asn1;
using Godot;

public class UnitAbility
{
    public String name;
    public UnitEffect effect;
    public Unit usingUnit;
    public float combatPower;
    public int currentCharges;
    public int maxChargesPerTurn; //-1 means no reset... we use the charge then its gone I think is the idea
    public int range;
    public String iconPath;
    public TargetSpecification validTargetTypes;
    
    public UnitAbility(Unit usingUnit, UnitEffect effect, float combatPower = 0.0f, int maxChargesPerTurn = 1, int range = 0, TargetSpecification validTargetTypes = null, String iconPath = "")
    {
        this.usingUnit = usingUnit;
        name = effect.functionName;
        this.effect = effect;
        this.iconPath = iconPath;
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

    public bool ActivateAbility(GameHex abilityTarget = null)
    {
        if(currentCharges > 0)
        {
            currentCharges -= 1;
            if(usingUnit.gameHex.gameBoard.game.TryGetGraphicManager(out GraphicManager manager))
            {
                manager.Update2DUI(UIElement.unitDisplay);
            }
            if (abilityTarget != null)
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
    //         IsValidTarget(UnitType? unitType, UnitClass? unitClass, String? buildingType, TerrainType? terrainType, bool isEnemy = false, bool isAlly = false)
    //         //TODO
    //     }
    // }
}
