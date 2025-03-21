using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;

enum EffectType
{
    MovementSpeed,
    MovementCosts,
    SightRange,
    SightCosts,
    CombatStrength
}

enum EffectOperation
{
    Multiply,
    Divide,
    Add,
    Subtract
}

[Serializable]
public class Effect
{
    public Effect(EffectType effectType, EffectOperation effectOperation, float effectMagnitude, int priority)
    {
        this.effectType = effectType;
        if(effectType == EffectType.MovementCosts | effectType == EffectType.SightCosts)
        {
            throw new InvalidOperationException("Must provide a TerrainMoveType if adjusting the movecost table");
        }
        this.effectOperation = effectOperation;
        this.effectMagnitude = effectMagnitude;
        this.priority = priority;
    }
    public Effect(EffectType effectType, TerrainMoveType terrainMoveType, EffectOperation effectOperation, float effectMagnitude, int priority)
    {
        this.effectType = effectType;
        this.effectOperation = effectOperation;
        this.effectMagnitude = effectMagnitude;
        this.terrainMoveType = terrainMoveType;
        this.priority = priority;
    }
    public EffectType effectType;
    public EffectOperation effectOperation;
    public TerrainMoveType terrainMoveType;
    public float effectMagnitude;
    public int priority;

    public void ApplyEffect(Unit unit)
    {
        if(effectType == EffectType.MovementSpeed)
        {
            ApplyOperation(ref unit.movementSpeed);
        }
        else if(effectType == EffectType.SightRange)
        {
            ApplyOperation(ref unit.sightRange);
        }
        else if(effectType == EffectType.SightRange)
        {
            ApplyOperation(ref unit.combatStrength);
        }
        else if(effectType == EffectType.MovementCosts)
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
        else if(effectType == EffectType.SightCosts)
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
