using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;

struct GameTile
{
  public class GameTile
  {
      public GameHex gameHex { get; set; } // Represents the tile's core data
      public bool hasBeenSeen { get; set; } // Whether the tile has been revealed at least once by the active players team
      public int unitsSeeing { get; set; } // Number of units currently seeing the tile on the active players team
  }
}

