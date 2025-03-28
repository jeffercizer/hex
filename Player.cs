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
    public Queue<ResearchType> queuedResearch;
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

    public Queue<ResearchType> SelectResearch(ResearchType researchType)
    {
        HashSet<String> visited = new();
        Queue<ResearchType> queue = new();

        void TopologicalSort(ResearchType researchType)
        {
            if (visited.Contains(researchType))
                return; 

            visited.Add(researchType);

            if (researchDependencies.ContainsKey(researchType))
            {
                foreach (ResearchType requirement in researchDependencies[researchType])
                {
                    TopologicalSort(requirement);
                }
            }
            queue.Enqueue(ResearchType);
        }

        TopologicalSort(researchType);
        return queue;
    }

    public void OnResearchComplete()
    {
        //activate research effect
        
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
