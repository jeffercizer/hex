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
        if (game.TryGetGraphicManager(out GraphicManager manager))
        {
            manager.Update2DUI(UIElement.turnNumber);
            manager.Update2DUI(UIElement.unitDisplay);
        }
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
        if (!game.playerDictionary[teamNum].turnFinished)
        {
            game.playerDictionary[teamNum].OnTurnEnded(currentTurn);
            if (game.mainGameBoard != null & teamNum == 0)
            {
                game.mainGameBoard.OnTurnEnded(currentTurn);
            }
        }
    }

    public List<int> CheckTurnStatus()
    {
        List<int> waitingForPlayers = new List<int>();
        foreach (Player player in game.playerDictionary.Values)
        {
            if(!player.turnFinished)
            {
                waitingForPlayers.Add(player.teamNum);
            }
        }
        return waitingForPlayers;
    }
}
