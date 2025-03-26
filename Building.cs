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
    public float baseBuildCost;
    public float buildCost;
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
        buildingEffects = new();
        //LOAD INFORMATION FROM XML USING NAME
        //So far we have requested
        // 'City Center' 'Farm' 'Mine' 'Hunting Camp' 'Fishing Boat' 'Whaling Ship'
        baseBuildCost = 15;
        baseGoldCost = 50;
        baseMaintenanceCost = 1;
        baseFoodYield = 2;
        baseProductionYield = 2;
        baseGoldYield = 3;
        baseScienceYield = 4;
        baseCultureYield = 5;
        baseHappinessYield = 6;
    }
    public Building(String name, District ourDistrict)
    {
        this.name = name;
        this.ourDistrict = ourDistrict;
        buildingEffects = new();
        //LOAD INFORMATION FROM XML USING NAME
        baseBuildCost = 15;
        baseGoldCost = 50;
        baseMaintenanceCost = 1;
        baseFoodYield = 2;
        baseProductionYield = 2;
        baseGoldYield = 3;
        baseScienceYield = 4;
        baseCultureYield = 5;
        baseHappinessYield = 6;
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

    public void RecalculateYields()
    {
        //reset all Yields to base
        buildCost = baseBuildCost;
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
