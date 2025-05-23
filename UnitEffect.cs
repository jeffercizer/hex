using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;
using Godot;

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
    Subtract,
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
    
    public bool Apply(Unit unit, float combatPower = 0.0f, GameHex abilityTarget = null)
    {
        if (applyFunction != null)
        {
            applyFunction(unit);
            return true;
        }
        else if (functionName != "")
        {
            if(abilityTarget != null)
            {
                return ProcessFunctionString(functionName, unit, combatPower, abilityTarget);
            }
            else
            {
                return ProcessFunctionString(functionName, unit, combatPower);
            }
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
            return true;
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
    bool ProcessFunctionString(String functionString, Unit unit, float combatPower, GameHex abilityTarget = null)
    {
        if(functionString == "SettleCapitalAbility")
        {
            return SettleCapitalAbility(unit, "CapitalCityName");
        }
        else if(functionString == "SettleCityAbility")
        {
            return SettleCity(unit, "SettledCityName");
        }
        else if(functionString == "ScoutVisionAbility")
        {
            unit.sightRange += 1;
            unit.UpdateVision();
            return true;
        }
        else if(functionString == "RangedAttack")
        {
            if (abilityTarget != null)
            {
                return RangedAttack(unit, combatPower, abilityTarget);
            }
            else
            {
                return false;
            }
        }
        else if(functionString == "EnableEmbarkDisembark")
        {
            EnableEmbarkDisembark(unit);
            return true;
        }
        else if(functionString == "Fortify")
        {
            Fortify(unit);
            return true;
        }
        throw new NotImplementedException("The Effect Function: " + functionString + " does not exist, implement it in UnitEffect");
    }
    public bool SettleCapitalAbility(Unit unit, String cityName)
    {
        new City(unit.gameHex.gameBoard.game.GetUniqueID(), unit.teamNum, cityName, true, unit.gameHex);
        unit.decreaseHealth(99999.0f);
        return true;
    }
    public void EnableEmbarkDisembark(Unit unit)
    {
        if(unit.movementCosts[TerrainMoveType.Embark] < 0)
        {
            unit.movementCosts[TerrainMoveType.Embark] = 0;
        }
        if(unit.movementCosts[TerrainMoveType.Disembark] < 0)
        {
            unit.movementCosts[TerrainMoveType.Disembark] = 0;
        }
    }
    public bool SettleCity(Unit unit, String cityName)
    {
        new City(unit.gameHex.gameBoard.game.GetUniqueID(), 1, cityName, false, unit.gameHex);
        unit.decreaseHealth(99999.0f);
        return true;
    }
    public bool RangedAttack(Unit unit, float combatPower, GameHex target)
    {
        return unit.RangedAttackTarget(target, combatPower, unit.gameHex.gameBoard.game.teamManager);
    }
    public bool Fortify(Unit unit)
    {
        GD.PushWarning("Fortify Not Implemented");
        return false;
    }
}
