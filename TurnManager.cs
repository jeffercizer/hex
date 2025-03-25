using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;

[Serializable]
public class TurnManager
{
    public TurnManager()
    {
    }
    public TurnManager(Game game)
    {
      this.game = game;
    }
    public Game? game;

    public int currentTurn = 1;

    public void StartNewTurn()
    {
        currentTurn++;
        foreach (Player player in game.playerDictionary.Values)
        {
            player.OnTurnStarted(currentTurn);
        }
        if(game.mainGameBoard != null)
        {
            game.mainGameBoard.OnTurnStarted(currentTurn);
        }
    }
    public void EndCurrentTurn(int teamNum)
    {
        game.playerDictionary[teamNum].OnTurnEnded(currentTurn);
        if(game.mainGameBoard != null & teamNum == 0)
        {
            game.mainGameBoard.OnTurnEnded(currentTurn);
        }    
    }
}
