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
            ProcessFunctionString(functionName, building);
        }
        else
        {
            if (effectType == BuildingEffectType.ProductionCost)
            {
                ApplyOperation(ref building.yields.production);
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
                ApplyOperation(ref building.yields.food);
            }
            else if (effectType == BuildingEffectType.ProductionYield)
            {
                ApplyOperation(ref building.yields.production);
            }
            else if (effectType == BuildingEffectType.GoldYield)
            {
                ApplyOperation(ref building.yields.gold);
            }
            else if (effectType == BuildingEffectType.ScienceYield)
            {
                ApplyOperation(ref building.yields.science);
            }
            else if (effectType == BuildingEffectType.CultureYield)
            {
                ApplyOperation(ref building.yields.culture);
            }
            else if (effectType == BuildingEffectType.HappinessYield)
            {
                ApplyOperation(ref building.yields.happiness);
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
        Dictionary<String, Action<Building>> effectFunctions = new Dictionary<string, Action<Building>>
        {
            { "WaterSupplyEffect", WaterSupplyEffect },
            { "GranaryWarehouseEffect", GranaryWarehouseEffect },
            { "StoneCutterWarehouseEffect", StoneCutterWarehouseEffect },
            { "AncientWallEffect", AncientWallEffect },
            { "StonehengeEffect", StonehengeEffect },
            { "CityCenterWallEffect", CityCenterWallEffect }
        };
        
        if (effectFunctions.TryGetValue(functionString, out Action<Building> effectFunction))
        {
            effectFunction(building);
        }
        else
        {
            throw new ArgumentException($"Function '{functionString}' not recognized in BuildingEffect from Buildings file.");
        }
    }
    void WaterSupplyEffect(Building building)
    {
        float waterHappinessYield = 0.0f;
        if(building.district.gameHex.terrainType == TerrainType.Coast ||building.district.gameHex.featureSet.Contains(FeatureType.River) 
                || building.district.gameHex.featureSet.Contains(FeatureType.Wetland))
        {
            waterHappinessYield = 10.0f;
        }
        else
        {
            foreach(Hex hex in building.district.gameHex.hex.WrappingNeighbors(building.district.gameHex.gameBoard.left, building.district.gameHex.gameBoard.right))
            {
                if (building.district.gameHex.gameBoard.gameHexDict[hex].terrainType == TerrainType.Coast || building.district.gameHex.gameBoard.gameHexDict[hex].featureSet.Contains(FeatureType.River) 
                    || building.district.gameHex.gameBoard.gameHexDict[hex].featureSet.Contains(FeatureType.Wetland))
                {
                    waterHappinessYield = 10.0f;
                    break;
                }
            }
        }
        building.yields.happiness += waterHappinessYield;
    }
    void GranaryWarehouseEffect(Building building)
    {
        building.district.city.flatYields.food += 1;
    }
    void StoneCutterWarehouseEffect(Building building)
    {
        building.district.city.roughYields.production += 1;
    }
    void AncientWallEffect(Building building)
    {
        if(!building.district.hasWalls)
        {
            building.district.AddWalls(100.0f);
        }
    }
    void StonehengeEffect(Building building)
    {
        //idk whatever stonehenge would do
    }
    void CityCenterWallEffect(Building building)
    {
        if(!building.district.hasWalls)
        {
            building.district.maxHealth = 50.0f;
            building.district.currentHealth = 50.0f;
            building.district.hasWalls = true;
        }
    }
}
