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
        game.teamManager.AddTeam(teamNum, 50);
    }
    public Game game;
    public int teamNum;
    public bool turnFinished;
    public Dictionary<Hex, int> visibleGameHexDict;
    public Dictionary<Hex, bool> seenGameHexDict;
    public List<Unit> unitList;
    public List<City> cityList;
    public Dictionary<Hex, ResourceType> unassignedResources();
    public float scienceTotal;
    public float cultureTotal;
    public float goldTotal;
    public float happinessTotal;
    
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

    public bool RemoveResource(Hex hex)
    {
        foreach(City city in cityList)
        {
            if(city.heldResources.Remove(hex))
            {
                return true;
            }
        }
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
