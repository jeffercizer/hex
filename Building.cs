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
    public int baseFoodProduction;
    public int foodProduction;
    public int baseProduction;
    public int production;
    public int baseGoldProduction;
    public int goldProduction;
    public int baseScienceProduction;
    public int scienceProduction;
    public int baseCultureProduction;
    public int cultureProduction;
    public int baseHappinessProduction;
    public int happinessProduction;

    public Building()
    {
        buildingEffects = new();
    }
    public Building(String name, District ourDistrict)
    {
        this.name = name;
        this.ourDistrict = ourDistrict;
        //LOAD INFORMATION FROM XML USING NAME
        baseBuildCost = 100;
        baseGoldCost = 50;
        baseMaintenanceCost = 1;
        baseFoodProduction = 1;
        baseProductionProduction = 2;
        baseGoldProduction = 3;
        baseScienceProduction = 4;
        baseCultureProduction = 5;
        baseHappinessProduction = 6;
    }

    public RecalculateValues()
    {
        //reset all values to base
        baseBuildCost = buildCost;
        baseGoldCost = goldCost;
        baseMaintenanceCost = maintenanceCost;
        baseFoodProduction = foodProduction;
        baseProductionProduction = productionProduction;
        baseGoldProduction = goldProduction;
        baseScienceProduction = scienceProduction;
        baseCultureProduction = cultureProduction;
        baseHappinessProduction = happinessProduction;
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
