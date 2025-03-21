using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;

enum BuildingEffectType
{
    BuildCost,
    GoldCost,
    MaintenanceCost,
    FoodYield,
    ProductionYield,
    GoldYield,
    ScienceYield,
    CultureYield,
    HappinessYield    
}

enum EffectOperation
{
    Multiply,
    Divide,
    Add,
    Subtract
}

[Serializable]
public class BuildingEffect
{
    public BuildingEffect(BuildingEffectType effectType, EffectOperation effectOperation, float effectMagnitude, int priority, Action<object> applyFunction = null)
    {
        this.effectType = effectType;
        this.effectOperation = effectOperation;
        this.effectMagnitude = effectMagnitude;
        this.priority = priority;
    }
    public BuildingEffect(Action<object> applyFunction, int priority)
    {
        //default values
        this.effectType = BuildingEffectType.BuildCost;
        this.effectOperation = EffectOperation.Add;
        this.effectMagnitude = 0.0f;
        //real values
        this.priority = priority;
        this.applyFunction = applyFunction;
    }
    public EffectType effectType;
    public EffectOperation effectOperation;
    public float effectMagnitude;
    public int priority;
    public Action<object> ApplyFunction;

    public void ApplyEffect(Building building)
    {
        if (applyFunction != null)
        {
            applyFunction(building);
        }
        else
        {
            if (effectType == BuildingEffectType.BuildCost)
            {
                ApplyOperation(ref building.buildCost);
            }
            else if (effectType == BuildingEffectType.GoldCost)
            {
                ApplyOperation(ref building.goldCost);
            }
            else if (effectType == BuildingEffectType.MaintenanceCost)
            {
                ApplyOperation(ref building.maintenanceCost);
            }
            else if (effectType == BuildingEffectType.FoodYield)
            {
                ApplyOperation(ref building.foodYield);
            }
            else if (effectType == BuildingEffectType.ProductionYield)
            {
                ApplyOperation(ref building.productionYield);
            }
            else if (effectType == BuildingEffectType.GoldYield)
            {
                ApplyOperation(ref building.goldYield);
            }
            else if (effectType == BuildingEffectType.ScienceYield)
            {
                ApplyOperation(ref building.scienceYield);
            }
            else if (effectType == BuildingEffectType.CultureYield)
            {
                ApplyOperation(ref building.cultureYield);
            }
            else if (effectType == BuildingEffectType.HappinessYield)
            {
                ApplyOperation(ref building.happinessYield);
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
