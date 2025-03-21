using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;

[Serializable]
public class Player
{
    public Player(Game game, int teamNum)
    {
        this.game = game;
        this.teamNum = teamNum;
    }
    public Player(Game game, int teamNum, Dictionary<Hex, int> visibleGameHexDict, Dictionary<Hex, bool> seenGameHexDict, List<Unit> unitList, List<City> cityList)
    {
        this.game = game;
        this.teamNum = teamNum;
        this.visibleGameHexDict = visibleGameHexDict;
        this.seenGameHexDict = seenGameHexDict;
        this.unitList = unitList;
        this.cityList = cityList;
    }
    public Game game;
    public int teamNum;
    public Dictionary<Hex, int> visibleGameHexDict;
    public Dictionary<Hex, bool> seenGameHexDict;
    public List<Unit> unitList;
    public List<City> cityList;
    
    public void OnTurnStarted(int turnNumber)
    {
        foreach (Unit unit in unitList)
        {
            unit.OnTurnStarted(turnNumber);
        }
        Console.WriteLine($"Player{teamNum}: Started turn {turnNumber}.");
    }

    public void OnTurnEnded(int turnNumber)
    {
        foreach (Unit unit in unitList)
        {
            unit.OnTurnEnded(turnNumber);
        }
        Console.WriteLine($"Player{teamNum}: Ended turn {turnNumber}.");
    }
}
