using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;

struct TurnManager
{
    public TurnManager(GameBoard mainBoard)
    {
      this.mainBoard = mainBoard;
      OnNewTurn += mainBoard.OnTurnStarted;
    }
    GameBoard mainBoard;
    public delegate void NewTurnEvent(int turnNumber);
    public event NewTurnEvent OnNewTurn;

    private int currentTurn = 0;

    public void StartNextTurn()
    {
        currentTurn++;
        Console.WriteLine($"Turn {currentTurn} started");
        OnNewTurn?.Invoke(currentTurn);
    }
}
