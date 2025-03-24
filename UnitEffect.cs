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
    public UnitEffectType effectType;
    public EffectOperation effectOperation;
    public TerrainMoveType terrainMoveType;
    public float effectMagnitude;
    public int priority;
    public Action<Building>? applyFunction;

    public void ApplyEffect(Unit unit)
    {
        if (applyFunction != null)
        {
            applyFunction(unit);
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
}
