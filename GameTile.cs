using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;

struct GameTile
{
    public class GameTile
    {
        public GameTile(GameHex gameHex)
        {
            gameHex = gameHex;
            hasBeenSeen = false;
            unitsSeeing = 0;
        }
        GameHex gameHex;
        bool hasBeenSeen;
        int unitsSeeing;
        
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
}

