using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;

public enum BuildingEffectType
{
    ProductionCost,
    GoldCost,
    MaintenanceCost,
    FoodYield,
    ProductionYield,
    GoldYield,
    ScienceYield,
    CultureYield,
    HappinessYield    
}

[Serializable]
public class BuildingEffect
{
    public BuildingEffect(BuildingEffectType effectType, EffectOperation effectOperation, float effectMagnitude, int priority, Action<object> applyFunction)
    {
        this.effectType = effectType;
        this.effectOperation = effectOperation;
        this.effectMagnitude = effectMagnitude;
        this.priority = priority;
    }
    public BuildingEffect(Action<Building> applyFunction, int priority)
    {
        //default values
        this.effectType = BuildingEffectType.ProductionCost;
        this.effectOperation = EffectOperation.Add;
        this.effectMagnitude = 0.0f;
        //real values
        this.priority = priority;
        this.applyFunction = applyFunction;
    }

    public BuildingEffect(String functionName)
    {
        this.functionName = functionName;
    }
    
    public BuildingEffectType effectType;
    public EffectOperation effectOperation;
    public float effectMagnitude;
    public int priority;
    public Action<Building>? applyFunction;
    public String functionName = "";

    public void ApplyEffect(Building building)
    {
        if (applyFunction != null)
        {
            applyFunction(building);
        }
        else if (functionName != "")
        {
            ProcessFunctionString(functionName, unit);
        }
        else
        {
            if (effectType == BuildingEffectType.ProductionCost)
            {
                ApplyOperation(ref building.productionCost);
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
    void ProcessFunctionString(String functionString, Building building)
    {
        if(functionString == "")
        {
            
        }
        else if(functionString == "")
        {

        }
    }
}
