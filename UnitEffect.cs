using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;
using Godot;
using System.IO;

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
    public UnitEffect(Action<int> applyFunction, int priority)
    {
        this.priority = priority;
        this.applyFunction = applyFunction;
    }

    public UnitEffect(String functionName)
    {
        this.functionName = functionName;
    }
    
    private UnitEffect()
    {
        //used for loading
    }

    public UnitEffectType effectType { get; set; }
    public EffectOperation effectOperation { get; set; }
    public TerrainMoveType terrainMoveType { get; set; }
    public float effectMagnitude { get; set; }
    public int priority { get; set; }
    public Action<int>? applyFunction { get; set; }
    public String functionName { get; set; } = "";


    public bool Apply(int unitID, float combatPower = 0.0f, GameHex abilityTarget = null)
    {
        if (applyFunction != null)
        {
            applyFunction(unitID);
            return true;
        }
        else if (functionName != "")
        {
            if(abilityTarget != null)
            {
                return ProcessFunctionString(functionName, unitID, combatPower, abilityTarget);
            }
            else
            {
                return ProcessFunctionString(functionName, unitID, combatPower);
            }
        }
        else
        {
            if(effectType == UnitEffectType.MovementSpeed)
            {
                Global.gameManager.game.unitDictionary[unitID].movementSpeed = ApplyOperation(Global.gameManager.game.unitDictionary[unitID].movementSpeed);
            }
            else if(effectType == UnitEffectType.SightRange)
            {
                Global.gameManager.game.unitDictionary[unitID].sightRange = ApplyOperation(Global.gameManager.game.unitDictionary[unitID].sightRange);
            }
            else if(effectType == UnitEffectType.SightRange)
            {
                Global.gameManager.game.unitDictionary[unitID].combatStrength = ApplyOperation(Global.gameManager.game.unitDictionary[unitID].combatStrength);
            }
            else if(effectType == UnitEffectType.MovementCosts)
            {
                switch (effectOperation)
                {
                    case EffectOperation.Multiply:
                        Global.gameManager.game.unitDictionary[unitID].movementCosts[terrainMoveType] *= effectMagnitude;
                        break;
                    case EffectOperation.Divide:
                        Global.gameManager.game.unitDictionary[unitID].movementCosts[terrainMoveType] /= effectMagnitude;
                        break;
                    case EffectOperation.Add:
                        Global.gameManager.game.unitDictionary[unitID].movementCosts[terrainMoveType] += effectMagnitude;
                        break;
                    case EffectOperation.Subtract:
                        Global.gameManager.game.unitDictionary[unitID].movementCosts[terrainMoveType] -= effectMagnitude;
                        break;
                }
            }
            else if(effectType == UnitEffectType.SightCosts)
            {
                switch (effectOperation)
                {
                    case EffectOperation.Multiply:
                        Global.gameManager.game.unitDictionary[unitID].sightCosts[terrainMoveType] *= effectMagnitude;
                        break;
                    case EffectOperation.Divide:
                        Global.gameManager.game.unitDictionary[unitID].sightCosts[terrainMoveType] /= effectMagnitude;
                        break;
                    case EffectOperation.Add:
                        Global.gameManager.game.unitDictionary[unitID].sightCosts[terrainMoveType] += effectMagnitude;
                        break;
                    case EffectOperation.Subtract:
                        Global.gameManager.game.unitDictionary[unitID].sightCosts[terrainMoveType] -= effectMagnitude;
                        break;
                }
            }
            return true;
        }
    }
    float ApplyOperation(float property)
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
        return property;
    }
    bool ProcessFunctionString(String functionString, int unitID, float combatPower, GameHex abilityTarget = null)
    {
        if(functionString == "SettleCapitalAbility")
        {
            return SettleCapitalAbility(Global.gameManager.game.unitDictionary[unitID], "CapitalCityName");
        }
        else if(functionString == "SettleCityAbility")
        {
            return SettleCity(Global.gameManager.game.unitDictionary[unitID], "SettledCityName");
        }
        else if(functionString == "ScoutVisionAbility")
        {
            Global.gameManager.game.unitDictionary[unitID].sightRange += 1;
            Global.gameManager.game.unitDictionary[unitID].UpdateVision();
            return true;
        }
        else if(functionString == "RangedAttack")
        {
            if (abilityTarget != null)
            {
                return RangedAttack(Global.gameManager.game.unitDictionary[unitID], combatPower, abilityTarget);
            }
            else
            {
                return false;
            }
        }
        else if(functionString == "EnableEmbarkDisembark")
        {
            EnableEmbarkDisembark(Global.gameManager.game.unitDictionary[unitID]);
            return true;
        }
        else if(functionString == "Fortify")
        {
            Fortify(Global.gameManager.game.unitDictionary[unitID]);
            return true;
        }
        throw new NotImplementedException("The Effect Function: " + functionString + " does not exist, implement it in UnitEffect");
    }
    public bool SettleCapitalAbility(Unit unit, String cityName)
    {
        bool validHex = true;
        foreach (Hex hex in unit.hex.WrappingRange(3, Global.gameManager.game.mainGameBoard.left, Global.gameManager.game.mainGameBoard.right, Global.gameManager.game.mainGameBoard.top, Global.gameManager.game.mainGameBoard.bottom))
        {
            if (Global.gameManager.game.mainGameBoard.gameHexDict[hex].district != null && Global.gameManager.game.mainGameBoard.gameHexDict[hex].district.isCityCenter)
            {
                validHex = false;
                break;
            }
        }
        if (validHex)
        {
            new City(Global.gameManager.game.GetUniqueID(), unit.teamNum, cityName, true, Global.gameManager.game.mainGameBoard.gameHexDict[unit.hex]);
            unit.decreaseHealth(99999.0f);
            return true;
        }
        return false;
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
        bool validHex = true;
        foreach (Hex hex in unit.hex.WrappingRange(3, Global.gameManager.game.mainGameBoard.left, Global.gameManager.game.mainGameBoard.right, Global.gameManager.game.mainGameBoard.top, Global.gameManager.game.mainGameBoard.bottom))
        {
            if (Global.gameManager.game.mainGameBoard.gameHexDict[hex].district != null && Global.gameManager.game.mainGameBoard.gameHexDict[hex].district.isCityCenter)
            {
                validHex = false;
                break;
            }
        }
        if (validHex)
        {
            new City(Global.gameManager.game.GetUniqueID(), 1, cityName, false, Global.gameManager.game.mainGameBoard.gameHexDict[unit.hex]);
            unit.decreaseHealth(99999.0f);
            return true;
        }
        return false;
    }
    public bool RangedAttack(Unit unit, float combatPower, GameHex target)
    {
        return unit.RangedAttackTarget(target, combatPower, Global.gameManager.game.teamManager);
    }
    public bool Fortify(Unit unit)
    {
        GD.PushWarning("Fortify Not Implemented");
        return false;
    }
}
