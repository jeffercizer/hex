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

struct Effect
{
    public Effect(EffectType effectType, EffectOperation effectOperation, float effectMagnitude)
    {
        this.effectType = effectType;
        if(effectType == EffectType.MovementCosts | effectType == EffectType.SightCosts)
        {
            throw new InvalidOperationException("Must provide a TerrainMoveType if adjusting the movecost table");
        }
        this.effectOperation = effectOperation;
        this.effectMagnitude = effectMagnitude;
    }
    public Effect(EffectType effectType, TerrainMoveType terrainMoveType, EffectOperation effectOperation, float effectMagnitude)
    {
        this.effectType = effectType;
        this.effectOperation = effectOperation;
        this.effectMagnitude = effectMagnitude;
        this.terrainMoveType = terrainMoveType;
    }
    public EffectType effectType;
    public EffectOperation effectOperation;
    public TerrainMoveType terrainMoveType;
    public float effectMagnitude;

    public void ApplyEffect(Unit unit)
    {
        if(effectType == EffectType.MovementSpeed)
        {
            ApplyOperation(unit.movementSpeed);
        }
        else if(effectType == EffectType.SightRange)
        {
            ApplyOperation(unit.sightRange);
        }
        else if(effectType == EffectType.SightRange)
        {
            ApplyOperation(unit.combatPower);
        }
        else if(effectType == EffectType.MovementCosts)
        {
            ApplyOperation(unit.MovementCosts[terrainMoveType]);
        }
        else if(effectType == EffectType.SightCosts)
        {
            ApplyOperation(unit.SightCosts[terrainMoveType]);
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
