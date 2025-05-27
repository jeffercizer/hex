using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;
using Godot;
using System.IO;

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
    
    public BuildingEffectType effectType { get; set; }
    public EffectOperation effectOperation { get; set; }
    public float effectMagnitude { get; set; }
    public int priority { get; set; }
    public Action<Building>? applyFunction { get; set; }
    public String functionName { get; set; } = "";

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
                building.productionCost = ApplyOperation(building.productionCost);
            }
            else if (effectType == BuildingEffectType.GoldCost)
            {
                building.goldCost = ApplyOperation(building.goldCost);
            }
            else if (effectType == BuildingEffectType.MaintenanceCost)
            {
                building.maintenanceCost = ApplyOperation(building.maintenanceCost);
            }
            else if (effectType == BuildingEffectType.FoodYield)
            {
                building.yields.food = ApplyOperation(building.yields.food);
            }
            else if (effectType == BuildingEffectType.ProductionYield)
            {
                building.yields.production = ApplyOperation(building.yields.production);
            }
            else if (effectType == BuildingEffectType.GoldYield)
            {
                building.yields.gold = ApplyOperation(building.yields.gold);
            }
            else if (effectType == BuildingEffectType.ScienceYield)
            {
                building.yields.science = ApplyOperation(building.yields.science);
            }
            else if (effectType == BuildingEffectType.CultureYield)
            {
                building.yields.culture = ApplyOperation(building.yields.culture);
            }
            else if (effectType == BuildingEffectType.HappinessYield)
            {
                building.yields.happiness = ApplyOperation(building.yields.happiness);
            }

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
    void ProcessFunctionString(String functionString, Building building)
    {
        Dictionary<String, Action<Building>> effectFunctions = new Dictionary<string, Action<Building>>
        {
            { "WaterSupplyEffect", WaterSupplyEffect },
            { "FarmEffect", FarmEffect },
            { "MineEffect", MineEffect },
            { "LumbermillEffect", LumbermillEffect },
            { "FishingBoatEffect", FishingBoatEffect },
            { "GranaryWarehouseEffect", GranaryWarehouseEffect },
            { "DockWarehouseEffect", DockWarehouseEffect },
            { "StoneCutterWarehouseEffect", StoneCutterWarehouseEffect },
            { "GardenEffect", GardenEffect },
            { "LibraryEffect", LibraryEffect },
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
        
        if(Global.gameManager.game.mainGameBoard.gameHexDict[building.districtHex].terrainType == TerrainType.Coast ||Global.gameManager.game.mainGameBoard.gameHexDict[building.districtHex].featureSet.Contains(FeatureType.River) 
                || Global.gameManager.game.mainGameBoard.gameHexDict[building.districtHex].featureSet.Contains(FeatureType.Wetland))
        {
            GD.Print("water supply effect1 " + Global.gameManager.game.mainGameBoard.gameHexDict[building.districtHex].hex);
            waterHappinessYield = 5.0f;
        }
        else
        {
            foreach(Hex hex in building.districtHex.WrappingNeighbors(Global.gameManager.game.mainGameBoard.left, Global.gameManager.game.mainGameBoard.right, Global.gameManager.game.mainGameBoard.bottom))
            {
                if (Global.gameManager.game.mainGameBoard.gameHexDict[hex].terrainType == TerrainType.Coast || Global.gameManager.game.mainGameBoard.gameHexDict[hex].featureSet.Contains(FeatureType.River) 
                    || Global.gameManager.game.mainGameBoard.gameHexDict[hex].featureSet.Contains(FeatureType.Wetland))
                {
                    GD.Print("water supply effect2 " + Global.gameManager.game.mainGameBoard.gameHexDict[building.districtHex].hex);
                    waterHappinessYield = 5.0f;
                    break;
                }
            }
        }
        building.yields.happiness += waterHappinessYield;
    }
    void FarmEffect(Building building)
    {

    }
    void MineEffect(Building building)
    {

    }
    void LumbermillEffect(Building building)
    {

    }
    void FishingBoatEffect(Building building)
    {

    }
    void GranaryWarehouseEffect(Building building)
    {

        Global.gameManager.game.cityDictionary[Global.gameManager.game.mainGameBoard.gameHexDict[building.districtHex].district.cityID].flatYields.food += 1;
    }
    void DockWarehouseEffect(Building building)
    {
        Global.gameManager.game.cityDictionary[Global.gameManager.game.mainGameBoard.gameHexDict[building.districtHex].district.cityID].coastalYields.food += 1;
    }
    void StoneCutterWarehouseEffect(Building building)
    {
        Global.gameManager.game.cityDictionary[Global.gameManager.game.mainGameBoard.gameHexDict[building.districtHex].district.cityID].roughYields.production += 1;
    }
    void GardenEffect(Building building)
    {
        //garden effect TODO
    }
    void LibraryEffect(Building building)
    {
        //library effect TODO
    }
    void AncientWallEffect(Building building)
    {
        if(!Global.gameManager.game.mainGameBoard.gameHexDict[building.districtHex].district.hasWalls)
        {
            Global.gameManager.game.mainGameBoard.gameHexDict[building.districtHex].district.AddWalls(100.0f);
        }
    }
    void StonehengeEffect(Building building)
    {
        //stonehenge effect TODO
    }
    void CityCenterWallEffect(Building building)
    {
        if(!Global.gameManager.game.mainGameBoard.gameHexDict[building.districtHex].district.hasWalls)
        {
            Global.gameManager.game.mainGameBoard.gameHexDict[building.districtHex].district.maxHealth = 50.0f;
            Global.gameManager.game.mainGameBoard.gameHexDict[building.districtHex].district.health = 50.0f;
            Global.gameManager.game.mainGameBoard.gameHexDict[building.districtHex].district.hasWalls = true;
        }
    }
}
