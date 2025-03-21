using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;

[Serializable]
public class Game
{
    public Game(TeamManager teamManager)
    {
        this.teamManager = teamManager;
        this.playerDictionary = new();
    }
    public Game(Dictionary<int, Player> playerDictionary, TeamManager teamManager)
    {
        this.playerDictionary = playerDictionary;
        this.teamManager = teamManager;
    }

    public void AssignGameBoard(GameBoard mainGameBoard)
    {
        this.mainGameBoard = mainGameBoard;
    }

    public void AssignTurnManager(TurnManager turnManager)
    {
        this.turnManager = turnManager;
    }
    public GameBoard? mainGameBoard;
    public Dictionary<int, Player> playerDictionary;
    public TeamManager teamManager;
    public TurnManager? turnManager;
}
