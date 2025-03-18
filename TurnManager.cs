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

    private int currentTurn = 0;

    public void StartNextTurn()
    {
        currentTurn++;
        mainBoard.OnTurnStarted();
    }
}
