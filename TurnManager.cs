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
    }
    GameBoard mainBoard;

    public int currentTurn = 0;

    public void StartNewTurn()
    {
        currentTurn++;
        mainBoard.OnTurnStarted(currentTurn);
    }
    public void EndCurrentTurn()
    {
        mainBoard.OnTurnEnded(currentTurn);
    }
}
