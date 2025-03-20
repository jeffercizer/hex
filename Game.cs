using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;

struct Game
{
    public Game(TeamManager teamManager, TurnManager turnManager)
    {
        this.teamManager = teamManager;
        this.turnManager = turnManager;
    }
    public Game(GameBoard mainGameBoard, Dictionary<int, Player> playerDictionary, TeamManager teamManager, TurnManager turnManager)
    {
        this.teamManager = teamManager;
        this.turnManager = turnManager;
    }
    public GameBoard mainGameBoard;
    public Dictionary<int, Player> playerDictionary;
    public TeamManager teamManager;
    public TurnManager turnManager;
}
