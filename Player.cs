using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;

[Serializable]
public class Player
{
    public Player(Game game, float goldTotal, int teamNum)
    {
        this.game = game;
        this.teamNum = teamNum;
        this.goldTotal = goldTotal;
        this.visibleGameHexDict = new();
        this.seenGameHexDict = new();
        this.unitList = new();
        this.cityList = new();
        this.unassignedResources = new();
        this.allowedBuildings = new();
        this.allowedUnits = new();
        this.unitResearchEffects = new();
        this.buildingResearchEffects = new();
        this.queuedResearch = new();
        game.teamManager.AddTeam(teamNum, 50);
        OnResearchComplete(ResearchType.Agriculture);
    }
    public Player(Game game, int teamNum, Dictionary<Hex, int> visibleGameHexDict, Dictionary<Hex, bool> seenGameHexDict, List<Unit> unitList, List<City> cityList, float scienceTotal, float cultureTotal, float goldTotal, float happinessTotal)
    {
        this.game = game;
        this.teamNum = teamNum;
        this.visibleGameHexDict = visibleGameHexDict;
        this.seenGameHexDict = seenGameHexDict;
        this.unitList = unitList;
        this.cityList = cityList;
        this.scienceTotal = scienceTotal;
        this.cultureTotal = cultureTotal;
        this.goldTotal = goldTotal;
        this.happinessTotal = happinessTotal;
        this.unassignedResources = new();
        this.allowedBuildings = new();
        this.allowedUnits = new();
        game.teamManager.AddTeam(teamNum, 50);
    }
    public Game game;
    public int teamNum;
    public bool turnFinished;
    public Dictionary<Hex, int> visibleGameHexDict;
    public Dictionary<Hex, bool> seenGameHexDict;
    public List<ResearchQueueType> queuedResearch;
    public Dictionary<ResearchType, ResearchQueueType> partialResearchDictionary;
    public ResearchType currentResearch;
    public List<Unit> unitList;
    public List<City> cityList;
    public List<(UnitEffect, UnitClass)> unitResearchEffects;
    public List<(BuildingEffect, BuildingType)> buildingResearchEffects;
    public HashSet<BuildingType> allowedBuildings;
    public HashSet<UnitType> allowedUnits;
    public Dictionary<Hex, ResourceType> unassignedResources;
    public float scienceTotal;
    public float cultureTotal;
    public float goldTotal;
    public float happinessTotal;
    public float strongestUnitBuilt = 0.0f;
    
    public void OnTurnStarted(int turnNumber)
    {
        turnFinished = false;
        foreach (Unit unit in unitList)
        {
            unit.OnTurnStarted(turnNumber);
        }
        foreach (City city in cityList)
        {
            city.OnTurnStarted(turnNumber);
        }
        if(queuedResearch.Any())
        {
            float cost = queuedResearch[0].researchLeft;
            queuedResearch[0].researchLeft -= scienceTotal;
            scienceTotal -= cost;
            scienceTotal = Math.Max(0.0f, scienceTotal);
            if(queuedResearch[0].researchLeft <= 0)
            {
                OnResearchComplete(queuedResearch[0].researchType);
                queuedResearch.RemoveAt(0);
            }
        }
    }

    public void OnTurnEnded(int turnNumber)
    {
        foreach (Unit unit in unitList)
        {
            unit.OnTurnEnded(turnNumber);
        }
        foreach (City city in cityList)
        {
            city.OnTurnEnded(turnNumber);
        }
        turnFinished = true;
    }

    public List<ResearchQueueType> SelectResearch(ResearchType researchType)
    {
        HashSet<ResearchType> visited = new();
        List<ResearchQueueType> queue = new();
        if(queuedResearch.Any())
        {
            partialResearchDictionary[researchType] = queuedResearch[0];
        }
        void TopologicalSort(ResearchType researchType)
        {
            if (visited.Contains(researchType))
                return; 

            visited.Add(researchType);

            if (ResearchLoader.researchesDict.ContainsKey(researchType))
            {
                foreach (ResearchType requirement in ResearchLoader.researchesDict[researchType].Requirements)
                {
                    TopologicalSort(requirement);
                }
            }
            if(partialResearchDictionary.ContainsKey(researchType))
            {
                queuedResearch.Add(partialResearchDictionary[researchType]);
            }
            else
            {
                queuedResearch.Add(new ResearchQueueType(researchType, ResearchLoader.researchesDict[researchType].Tier, ResearchLoader.researchesDict[researchType].Tier)); //apply cost mod TODO
            }
        }

        TopologicalSort(researchType);
        return queue;
    }

    public void OnResearchComplete(ResearchType researchType)
    {
        foreach(UnitType unitType in ResearchLoader.researchesDict[researchType].UnitUnlocks)
        {
            allowedUnits.Add(unitType);
        }
        foreach(BuildingType buildingType in ResearchLoader.researchesDict[researchType].BuildingUnlocks)
        {
            allowedBuildings.Add(buildingType);
        }
        foreach(String effect in ResearchLoader.researchesDict[researchType].Effects)
        {
            ResearchLoader.ProcessFunctionString(effect, this);
        }
    }

    public bool AddResource(Hex hex, ResourceType resourceType, City targetCity)
    {
        if(targetCity.heldResources.Count < targetCity.maxResourcesHeld)
        {
            targetCity.heldResources.Add(hex, resourceType);
            unassignedResources.Remove(hex);
            return true;
        }
        return false;
    }



    public bool RemoveResource(Hex hex)
    {
        foreach(City city in cityList)
        {
            if(city.heldResources.Keys.Contains(hex))
            {
                ResourceType temp = city.heldResources[hex];
                city.heldResources.Remove(hex);
                unassignedResources.Add(hex, temp);
                return true;
            }
        }
        return false;
    }
    
    
    public bool RemoveLostResource(Hex hex)
    {
        foreach(City city in cityList)
        {
            if(city.heldResources.Remove(hex))
            {
                return true;
            }
        }
        return false;
    }

    public void AddGold(float gold)
    {
        goldTotal += gold;
    }
    public void AddScience(float science)
    {
        scienceTotal += science;
    }
    public void AddCulture(float culture)
    {
        cultureTotal += culture;
    }
    public void AddHappiness(float happiness)
    {
        happinessTotal += happiness;
    }

}

public class ResearchQueueType
{

    public ResearchQueueType(ResearchType researchType, float researchCost, float researchLeft)
    {
        this.researchType = researchType;
        this.researchCost = researchCost;
        this.researchLeft = researchLeft;
    }
    public ResearchType researchType;
    public float researchCost;
    public float researchLeft;
}
