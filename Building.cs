using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;

[Serializable]
public class Building
{
    public String name;
    public BuildingType buildingType;
    public District? district;
    public List<BuildingEffect> buildingEffects;
    public float baseProductionCost;
    public float productionCost;
    public float baseGoldCost;
    public float goldCost;
    public float baseMaintenanceCost;
    public float maintenanceCost;
    public Yields baseYields;
    public Yields yields;

    public Building(BuildingType buildingType)
    {
        this.buildingType = buildingType;
        this.name = BuildingLoader.buildingNames[buildingType];
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
        district.gameHex.gameBoard.game.builtWonder.Add(buildingType);
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
            district.gameHex.gameBoard.game.builtWonder.Remove(buildingType);
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
        yields = baseYields;
    }

    public void RecalculateYields()
    {
        district.gameHex.yields += yields;
    }
}
