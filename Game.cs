using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;

struct Game
{
    public Game(TeamManager teamManager)
    {
        this.teamManager = teamManager;
        //this.turnManager = turnManager;
    }
    public Game(GameBoard mainGameBoard, Dictionary<int, Player> playerDictionary, TeamManager teamManager)
    {
        this.mainGameBoard.Add(mainGameBoard);
        this.playerDictionary = playerDictionary;
        this.teamManager = teamManager;
        //this.turnManager = turnManager;
    }
    public List<GameBoard> mainGameBoard = new();
    public Dictionary<int, Player> playerDictionary = new();
    public TeamManager teamManager;
    //public TurnManager turnManager;
}
