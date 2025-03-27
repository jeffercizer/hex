using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;

[Serializable]
public class Building
{
    public String name;
    public District? ourDistrict;
    public List<BuildingEffect> buildingEffects;
    public float baseProductionCost;
    public float productionCost;
    public float baseGoldCost;
    public float goldCost;
    public float baseMaintenanceCost;
    public float maintenanceCost;
    public float baseFoodYield;
    public float foodYield;
    public float baseProductionYield;
    public float productionYield;
    public float baseGoldYield;
    public float goldYield;
    public float baseScienceYield;
    public float scienceYield;
    public float baseCultureYield;
    public float cultureYield;
    public float baseHappinessYield;
    public float happinessYield;

    public Building(String name)
    {
        this.name = name;
        // 'City Center' 'Farm' 'Mine' 'Hunting Camp' 'Fishing Boat' 'Whaling Ship'
        if (Enum.TryParse(name, out BuildingType buildingType) && BuildingLoader.buildingsDict.TryGetValue(buildingType, out BuildingInfo buildingInfo))
        {
            this.productionCost = buildingInfo.BuildCost;
            this.baseProductionCost = buildingInfo.BuildCost;
            
            this.goldCost = buildingInfo.GoldCost;
            this.baseGoldCost = buildingInfo.GoldCost;
            
            this.maintenanceCost = buildingInfo.MaintenanceCost;
            this.baseMaintenanceCost = buildingInfo.MaintenanceCost;
            
            this.foodYield = buildingInfo.FoodYield;
            this.baseFoodYield = buildingInfo.FoodYield;
            
            this.productionYield = buildingInfo.ProductionYield;
            this.baseProductionYield = buildingInfo.ProductionYield;
            
            this.goldYield = buildingInfo.GoldYield;
            this.baseGoldYield = buildingInfo.GoldYield;
            
            this.scienceYield = buildingInfo.ScienceYield;
            this.baseScienceYield = buildingInfo.ScienceYield;
            
            this.cultureYield = buildingInfo.CultureYield;
            this.baseCultureYield = buildingInfo.CultureYield;
            
            this.happinessYield = buildingInfo.HappinessYield;
            this.baseHappinessYield = buildingInfo.HappinessYield;
            
            this.buildingEffects = buildingInfo.Effects;
        }
    }

    public void SwitchTeams()
    {
        RecalculateYields();
    }

    public void DestroyBuilding()
    {
        ourDistrict = null;
    }

    public void AddEffect(BuildingEffect effect)
    {
        buildingEffects.Add(effect);
        ourDistrict.ourCity.RecalculateYields();
    }

    public void RemoveEffect(BuildingEffect effect)
    {
        buildingEffects.Remove(effect);
        ourDistrict.ourCity.RecalculateYields();
    }

    public void RecalculateYields()
    {
        //reset all Yields to base
        productionCost = baseProductionCost;
        goldCost = baseGoldCost;
        maintenanceCost = baseMaintenanceCost;
        foodYield = baseFoodYield;
        productionYield = baseProductionYield;
        goldYield = baseGoldYield;
        scienceYield = baseScienceYield;
        cultureYield = baseCultureYield;
        happinessYield = baseHappinessYield;
        //also order all effects, multiply/divide after add/subtract priority
        //0 means it is applied first 100 means it is applied "last" (highest number last)
        //so multiply/divide effects should be 20 and add/subtract will be 10 to give wiggle room
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
}
