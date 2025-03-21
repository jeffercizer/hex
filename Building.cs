using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;

[Serializable]
public class Building
{
    public District ourDistrict;
    public List<BuildingEffect> buildingEffects;
    public int baseBuildCost;
    public int buildCost;
    public int baseGoldCost;
    public int goldCost;
    public int baseMaintenanceCost;
    public int maintenanceCost;
    public int baseFoodYield;
    public int foodYield;
    public int baseYield;
    public int production;
    public int baseGoldYield;
    public int goldYield;
    public int baseScienceYield;
    public int scienceYield;
    public int baseCultureYield;
    public int cultureYield;
    public int baseHappinessYield;
    public int happinessYield;

    public Building(String name)
    {
        this.name = name;
        buildingEffects = new();
        //LOAD INFORMATION FROM XML USING NAME
        baseBuildCost = 100;
        baseGoldCost = 50;
        baseMaintenanceCost = 1;
        baseFoodYield = 1;
        baseYieldYield = 2;
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
        baseBuildCost = 100;
        baseGoldCost = 50;
        baseMaintenanceCost = 1;
        baseFoodYield = 1;
        baseYieldYield = 2;
        baseGoldYield = 3;
        baseScienceYield = 4;
        baseCultureYield = 5;
        baseHappinessYield = 6;
    }

    public RecalculateValues()
    {
        //reset all values to base
        baseBuildCost = buildCost;
        baseGoldCost = goldCost;
        baseMaintenanceCost = maintenanceCost;
        baseFoodYield = foodYield;
        baseYieldYield = productionYield;
        baseGoldYield = goldYield;
        baseScienceYield = scienceYield;
        baseCultureYield = cultureYield;
        baseHappinessYield = happinessYield;
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
