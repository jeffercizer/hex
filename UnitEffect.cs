using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;

public enum UnitEffectType
{
    MovementSpeed,
    MovementCosts,
    SightRange,
    SightCosts,
    CombatStrength
}

public enum EffectOperation
{
    Multiply,
    Divide,
    Add,
    Subtract
}

[Serializable]
public class UnitEffect
{
    //priority is 0-100 (100 most important)
    public UnitEffect(UnitEffectType effectType, EffectOperation effectOperation, float effectMagnitude, int priority)
    {
        this.effectType = effectType;
        if(effectType == UnitEffectType.MovementCosts | effectType == UnitEffectType.SightCosts)
        {
            throw new InvalidOperationException("Must provide a TerrainMoveType if adjusting the movecost table");
        }
        this.effectOperation = effectOperation;
        this.effectMagnitude = effectMagnitude;
        this.priority = priority;
    }
    public UnitEffect(Action<Unit> applyFunction, int priority)
    {
        this.priority = priority;
        this.applyFunction = applyFunction;
    }

    public UnitEffect(String functionName)
    {
        this.functionName = functionName;
    }

    public UnitEffectType effectType;
    public EffectOperation effectOperation;
    public TerrainMoveType terrainMoveType;
    public float effectMagnitude;
    public int priority;
    public Action<Unit>? applyFunction;
    public String functionName = "";
    
    public void Apply(Unit unit)
    {
        if (applyFunction != null)
        {
            applyFunction(unit);
        }
        else if (functionName != "")
        {
            ProcessFunctionString(functionName, unit);
        }
        else
        {
            if(effectType == UnitEffectType.MovementSpeed)
            {
                ApplyOperation(ref unit.movementSpeed);
            }
            else if(effectType == UnitEffectType.SightRange)
            {
                ApplyOperation(ref unit.sightRange);
            }
            else if(effectType == UnitEffectType.SightRange)
            {
                ApplyOperation(ref unit.combatStrength);
            }
            else if(effectType == UnitEffectType.MovementCosts)
            {
                switch (effectOperation)
                {
                    case EffectOperation.Multiply:
                        unit.movementCosts[terrainMoveType] *= effectMagnitude;
                        break;
                    case EffectOperation.Divide:
                        unit.movementCosts[terrainMoveType] /= effectMagnitude;
                        break;
                    case EffectOperation.Add:
                        unit.movementCosts[terrainMoveType] += effectMagnitude;
                        break;
                    case EffectOperation.Subtract:
                        unit.movementCosts[terrainMoveType] -= effectMagnitude;
                        break;
                }
            }
            else if(effectType == UnitEffectType.SightCosts)
            {
                switch (effectOperation)
                {
                    case EffectOperation.Multiply:
                        unit.sightCosts[terrainMoveType] *= effectMagnitude;
                        break;
                    case EffectOperation.Divide:
                        unit.sightCosts[terrainMoveType] /= effectMagnitude;
                        break;
                    case EffectOperation.Add:
                        unit.sightCosts[terrainMoveType] += effectMagnitude;
                        break;
                    case EffectOperation.Subtract:
                        unit.sightCosts[terrainMoveType] -= effectMagnitude;
                        break;
                }
            }
        }
    }
    void ApplyOperation(ref float property)
    {
        switch (effectOperation)
        {
            case EffectOperation.Multiply:
                property *= effectMagnitude;
                break;
            case EffectOperation.Divide:
                property /= effectMagnitude;
                break;
            case EffectOperation.Add:
                property += effectMagnitude;
                break;
            case EffectOperation.Subtract:
                property -= effectMagnitude;
                break;
        }
    }
    void ProcessFunctionString(String functionString, Unit unit)
    {
        if(functionString == "SettleCityAbility")
        {
            SettleCity(unit, "SettledCityName");
        }
        else if(functionString == "ScoutVisionAbility")
        {
            unit.sightRange += 1;
            unit.UpdateVision();
        }
        else if(functionString == "RangedAttack")
        {
            RangedAttack(unit);
        }
    }
    public bool SettleCity(Unit unit, String cityName)
    {
        new City(unit.currentGameHex.gameBoard.game.GetUniqueID(), 1, cityName, unit.currentGameHex);
        unit.decreaseCurrentHealth(99999.0f);
        return true;
    }
    public bool RangedAttack(Unit unit)
    {
        return false;
    }
}
