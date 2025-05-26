using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;
using Godot;

[Serializable]
public class Building
{
    public String name;
    public int id;
    public String buildingType;
    public Hex districtHex;
    public List<BuildingEffect> buildingEffects;
    public float baseProductionCost;
    public float productionCost;
    public float baseGoldCost;
    public float goldCost;
    public float baseMaintenanceCost;
    public float maintenanceCost;
    public Yields baseYields;
    public Yields yields;

    public Building(String buildingType, Hex districtHex)
    {
        this.districtHex = districtHex;
        this.buildingType = buildingType;
        this.name = buildingType;
        // 'City Center' 'Farm' 'Mine' 'Hunting Camp' 'Fishing Boat' 'Whaling Ship'
        if (BuildingLoader.buildingsDict.TryGetValue(buildingType, out BuildingInfo buildingInfo))
        {
            this.productionCost = buildingInfo.ProductionCost;
            this.baseProductionCost = buildingInfo.ProductionCost;
            
            this.goldCost = buildingInfo.GoldCost;
            this.baseGoldCost = buildingInfo.GoldCost;
            
            this.maintenanceCost = buildingInfo.MaintenanceCost;
            this.baseMaintenanceCost = buildingInfo.MaintenanceCost;
            
            this.yields = buildingInfo.yields;
            this.baseYields = buildingInfo.yields;
            
            this.buildingEffects = new();
            foreach (String effectName in buildingInfo.Effects)
            {
                buildingEffects.Add(new BuildingEffect(effectName));
            }
        }
        if(BuildingLoader.buildingsDict[buildingType].Wonder)
        {
            Global.gameManager.game.builtWonders.Add(buildingType);
        }
        id = Global.gameManager.game.GetUniqueID();
        if (Global.gameManager.game.TryGetGraphicManager(out GraphicManager manager)) manager.NewBuilding(this);
    }

    public void SwitchTeams()
    {
        RecalculateYields();
    }

    public void DestroyBuilding()
    {
        districtHex = new Hex(0,0,0);
        if(BuildingLoader.buildingsDict[buildingType].Wonder)
        {
            Global.gameManager.game.builtWonders.Remove(buildingType);
        }
    }

    public void AddEffect(BuildingEffect effect)
    {
        buildingEffects.Add(effect);
        Global.gameManager.game.cityDictionary[Global.gameManager.game.mainGameBoard.gameHexDict[districtHex].district.cityID].RecalculateYields();
    }

    public void RemoveEffect(BuildingEffect effect)
    {
        buildingEffects.Remove(effect);
        Global.gameManager.game.cityDictionary[Global.gameManager.game.mainGameBoard.gameHexDict[districtHex].district.cityID].RecalculateYields();
    }

    public void PrepareYieldRecalculate()
    {
        yields = baseYields;
        PriorityQueue<BuildingEffect, int> orderedEffects = new();
        foreach(BuildingEffect effect1 in buildingEffects)
        {
            orderedEffects.Enqueue(effect1, effect1.priority);
        }
        BuildingEffect effect;
        int priority;
        while(orderedEffects.TryDequeue(out effect, out priority))
        {
            effect.ApplyEffect(this);
        }
    }

    public void RecalculateYields()
    {
        Global.gameManager.game.mainGameBoard.gameHexDict[districtHex].yields += yields;
    }
}
