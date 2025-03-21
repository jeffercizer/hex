using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;

[Serializable]
public class TurnManager
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
        foreach (Player player in game.playerDictionary.Values)
        {
            player.OnTurnStarted(currentTurn);
        }
        foreach (GameBoard gameBoard in game.mainGameBoard)
        {
            gameBoard.OnTurnStarted(currentTurn);
        }
    }
    public void EndCurrentTurn(int teamNum)
    {
        foreach (Player player in game.playerDictionary.Values)
        {
            player.OnTurnEnded(currentTurn);
        }
        foreach (GameBoard gameBoard in game.mainGameBoard)
        {
            gameBoard.OnTurnStarted(currentTurn);
        }
    }
}
