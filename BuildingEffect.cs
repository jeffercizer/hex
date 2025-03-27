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
        if(functionString == "WaterSupplyEffect")
        {
            WaterSupplyEffect(building);
        }
        else if(functionString == "GranaryWarehouseEffect")
        {
            GranaryWarehouseEffect(building);
        }
        else if(functionString == "StoneCutterWarehouseEffect")
        {
            StoneCutterWarehouseEffect(building);
        }
        else if(functionString == "AncientWallEffect")
        {
            AncientWallEffect(building);
        }
    }
    void WaterSupplyEffect(Building building)
    {
        float waterHappinessYield = 0.0f;
        if(building.ourDistrict.ourGameHex.terrainType == TerrainType.Coastal ||building.ourDistrict.ourGameHex.featureSet.Contains(FeatureType.River) 
                || building.ourDistrict.ourGameHex.featureSet.Contains(FeatureType.Wetland))
        {
            waterHappinessYield = 10.0f;
        }
        else
        {
            foreach(Hex hex on building.ourDistrict.ourGameHex.WrappingNeighbors(building.ourDistrict.ourGameHex.gameBoard.left, building.ourDistrict.ourGameHex.gameBoard.right))
            {
                if (building.ourDistrict.ourGameHex.gameBoard.gameHexDict[hex].terrainType == TerrainType.Coastal || building.ourDistrict.ourGameHex.gameBoard.gameHexDict[hex].featureSet.Contains(FeatureType.River) 
                    || building.ourDistrict.ourGameHex.gameBoard.gameHexDict[hex].featureSet.Contains(FeatureType.Wetland))
                {
                    waterHappinessYield = 10.0f;
                    break;
                }
            }
        }
        building.yields.happiness += waterhappinessYield;
    }
    void GranaryWarehouseEffect(Building building)
    {
        building.ourDistrict.ourCity.flatYields.food += 1;
    }
    void StoneCutterWarehouseEffect(Building building)
    {
        building.ourDistrict.ourCity.roughYields.production += 1;
    }
    void AncientWallEffect(Building building)
    {
        if(!building.district.hasWalls)
        {
            building.district.AddWalls(100.0f);
        }
    }
}
