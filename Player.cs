using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;

struct Player
{
    public Player(Game game, int teamNum)
    {
        this.game = game;
        this.teamNum = teamNum;
    }
    public Game game;
    public int teamNum;
}
