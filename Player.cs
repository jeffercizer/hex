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
    public Player(Game game, int teamNum, Dictionary<Hex, int> visibleGameHexDict, Dictionary<Hex, bool> seenGameHexDict)
    {
        this.game = game;
        this.teamNum = teamNum;
        this.visibleGameHexDict = visibleGameHexDict;
        this.seenGameHexDict = seenGameHexDict;
    }
    public Game game;
    public int teamNum;
    public Dictionary<Hex, int> visibleGameHexDict;
    public Dictionary<Hex, bool> seenGameHexDict;
}
