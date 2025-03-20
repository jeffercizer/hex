using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;

struct GameTile
{
    public GameTile(GameHex gameHex)
    {
        this.gameHex = gameHex;
        this.hasBeenSeen = false;
        this.unitsSeeing = 0;
    }
    public GameHex gameHex;
    public bool hasBeenSeen;
    public int unitsSeeing;
    
    public void RevealTile()
    {
        hasBeenSeen = true;
    }
    
    public bool AddUnitSeeing()
    {
        unitsSeeing++;
        if (unitsSeeing > 0)
        {
            RevealTile();
            return true;
        }
        return false;
    }
    
    public bool RemoveUnitSeeing()
    {
        if (unitsSeeing > 0)
        {
            unitsSeeing--;
            return true;
        }
        return false;
    }
}

