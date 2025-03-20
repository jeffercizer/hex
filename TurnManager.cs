using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;

struct TurnManager
{
    public TurnManager(Game game)
    {
      this.game = game;
    }
    public Game game;

    public int currentTurn = 0;

    public void StartNewTurn()
    {
        currentTurn++;
        foreach (Player player in game.playerDictionary)
        {
            player.OnTurnStarted(currentTurn)
        }
        game.mainGameBoard.OnTurnStarted(currentTurn);
    }
    public void EndCurrentTurn(int teamNum)
    {
        foreach (Player player in game.playerDictionary)
        {
            player.OnTurnEnded(currentTurn)
        }
        game.mainGameBoard.OnTurnEnded(currentTurn);
    }
}
