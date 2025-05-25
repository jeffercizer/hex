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
    public District district;
    public List<BuildingEffect> buildingEffects;
    public float baseProductionCost;
    public float productionCost;
    public float baseGoldCost;
    public float goldCost;
    public float baseMaintenanceCost;
    public float maintenanceCost;
    public Yields baseYields;
    public Yields yields;

    public Building(String buildingType, District district)
    {
        this.district = district;
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
            district.gameHex.gameBoard.game.builtWonders.Add(buildingType);
        }
        id = district.city.gameHex.gameBoard.game.GetUniqueID();
        if (district.city.gameHex.gameBoard.game.TryGetGraphicManager(out GraphicManager manager)) manager.NewBuilding(this);
    }

    public void SwitchTeams()
    {
        RecalculateYields();
    }

    public void DestroyBuilding()
    {
        district = null;
        if(BuildingLoader.buildingsDict[buildingType].Wonder)
        {
            district.gameHex.gameBoard.game.builtWonders.Remove(buildingType);
        }
    }

    public void AddEffect(BuildingEffect effect)
    {
        buildingEffects.Add(effect);
        district.city.RecalculateYields();
    }

    public void RemoveEffect(BuildingEffect effect)
    {
        buildingEffects.Remove(effect);
        district.city.RecalculateYields();
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
        district.gameHex.yields += yields;
    }
}
