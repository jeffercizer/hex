using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;

enum TerrainType
{
    Flat,
    Rough,
    Mountain,
    Coast,
    Ocean
}



enum FeatureType
{
    Forest,
    River,
    Road,

    Coral
}

enum TerrainTemperature
{
    Desert,
    Grassland,
    Plains,
    Tundra,
    Artic
}

[Serializable]
public class GameHex
{
    public GameHex(Hex hex, GameBoard ourGameBoard, TerrainType terrainType, TerrainTemperature terrainTemp, HashSet<FeatureType> featureSet)
    {
        this.hex = hex;
        this.ourGameBoard = ourGameBoard;
        this.terrainType = terrainType;
        this.terrainTemp = terrainTemp;
        this.featureSet = featureSet;
    }
    public GameHex(Hex hex, GameBoard ourGameBoard, TerrainType terrainType, TerrainTemperature terrainTemp, HashSet<FeatureType> featureSet, List<Unit> unitsList)
    {
        this.hex = hex;
        this.ourGameBoard = ourGameBoard;
        this.terrainType = terrainType;
        this.terrainTemp = terrainTemp;
        this.featureSet = featureSet;
        this.unitsList = unitsList;
    }
    
    public GameHex(Hex hex, GameBoard ourGameBoard, TerrainType terrainType, TerrainTemperature terrainTemp, HashSet<FeatureType> featureSet, List<Unit> unitsList, District district)
    {
        this.hex = hex;
        this.ourGameBoard = ourGameBoard;
        this.terrainType = terrainType;
        this.terrainTemp = terrainTemp;
        this.featureSet = featureSet;
        this.unitsList = unitsList;
        this.district = district;
    }

    public Hex hex;
    public GameBoard ourGameBoard;
    public TerrainType terrainType;
    public TerrainTemperature terrainTemp;
    public int ownedBy;
    public HashSet<FeatureType> featureSet = new();
    public List<Unit> unitsList = new();
    public District district;

    public void OnTurnStarted(int turnNumber)
    {
        //Console.WriteLine($"GameHex ({hex.q},{hex.r}): Started turn {turnNumber}.");
    }

    public void OnTurnEnded(int turnNumber)
    {
        //Console.WriteLine($"GameHex ({hex.q},{hex.r}): Ended turn {turnNumber}.");
    }
    public bool SetTerrainType(TerrainType newTerrainType)
    {
        this.terrainType = newTerrainType;
        return true;
    }

    public bool AddTerrainFeature(FeatureType newFeature)
    {
        this.featureSet.Add(newFeature);
        return true;
    }

    public void ClaimHex(int teamNum)
    {
        ownedBy = teamNum;
    }

    public bool TryClaimHex(int teamNum)
    {
        if(ownedBy == -1)
        {
            ownedBy = teamNum;
            return true;
        }
        return false;
    }

    //if stackable is true allow multiple units to stack
    //if flexible is true look for adjacent spaces to place
    public bool SpawnUnit(Unit newUnit, bool stackable, bool flexible)
    {
        if(!stackable & unitsList.Any() | newUnit.movementCosts[(TerrainMoveType)terrainType] > 100) //if they cant stack and their are units or the hex is invalid for this unit
        {
            if (flexible)
            {
                for (int i = 0; i < 6; i++) //ask all our neighbors if they have space
                {
                    if(ourGameBoard.gameHexDict[hex.WrappingNeighbor(i, ourGameBoard.left, ourGameBoard.left)].SpawnUnit(newUnit, stackable, flexible))
                    {
                        return true;
                    }
                }
                //if we still havent found a spot give up
            }
            return false;
        }
        else if(newUnit.movementCosts[(TerrainMoveType)terrainType] < 100)//if they cant stack and there aren't units or they can stack and units are/aren't there and the hex is valid for this unit
        {
            unitsList.Add(newUnit);
            newUnit.SetGameHex(this);
            return true;
        }
        return false;
    }


}

// struct GameHexTests
// {    
//     static public void TestSpawnUnitSingle(bool printGameBoard)
//     {
//         String name = "TestSpawnUnitSingle";
//         int top = 0;
//         int bottom = 10;
//         int left = 0;
//         int right = 30;
//         Dictionary<Hex, GameTile> gameTileDict = new();
//         for (int r = top; r <= bottom; r++){
//             int r_offset = r>>1; //same as (int)Math.Floor(r/2.0f)
//             for (int q = left - r_offset; q <= right - r_offset; q++){
//                 if(r==0 || r == bottom || q == left - r_offset || q == right - r_offset || (r == bottom/2 && q > left - r_offset + 2 && q < right - r_offset - 2))
//                 {
//                     gameTileDict.Add(new Hex(q, r, -q-r), new GameTile(new GameHex(new Hex(q, r, -q-r), left, right, TerrainType.Flat, TerrainTemperature.Grassland, new HashSet<FeatureType>())));
//                 }
//                 else
//                 {
//                     gameTileDict.Add(new Hex(q, r, -q-r), new GameTile(new GameHex(new Hex(q, r, -q-r), left, right, TerrainType.Flat, TerrainTemperature.Grassland, new HashSet<FeatureType>())));
//                 }
//             }
//         }
//         gameTileDict.Add(new Hex(-1, -1, 2), new GameTile(new GameHex(new Hex(-1, -1, 2), left, right, TerrainType.Flat, TerrainTemperature.Grassland, new HashSet<FeatureType>())));
//         GameBoard mainBoard = new GameBoard(gameTileDict, top, bottom, left, right);

//         Hex target = new Hex(1, 1, -2);
//         Dictionary<TerrainMoveType, float> movementCosts = new Dictionary<TerrainMoveType, float>
//         {
//             { TerrainMoveType.Flat, 1f },
//             { TerrainMoveType.Rough, 2f },
//             { TerrainMoveType.Mountain, 9999f },
//             { TerrainMoveType.Coast, 1f },
//             { TerrainMoveType.Ocean, 1f },
//             { TerrainMoveType.Forest, 1f },
//             { TerrainMoveType.River, 0f },
//             { TerrainMoveType.Road, 0.5f },
//             { TerrainMoveType.Embark, 0f },
//             { TerrainMoveType.Disembark, 0f }
//         };
//         Unit testUnit = new Unit("testUnit", movementCosts, gameTileDict[new Hex(-1, -1, 2)].gameHex);
//         if (!mainBoard.gameTileDict[target].gameHex.SpawnUnit(testUnit, true, true))
//         {
//             Tests.Complain(name);
//         }
//         if (!mainBoard.gameTileDict[target].gameHex.unitsList.Contains(testUnit))
//         {
//             Tests.Complain(name);
//         }
//         if (!testUnit.currentGameHex.Equals(mainBoard.gameTileDict[target].gameHex))
//         {
//             Tests.Complain(name);
//         }
//         if (printGameBoard)
//         {
//             mainBoard.PrintGameBoard();
//         }
//     }

//     static public void TestSpawnUnitMultipleStacking(bool printGameBoard)
//     {
//         String name = "TestSpawnUnitMultipleStacking";
//         int top = 0;
//         int bottom = 10;
//         int left = 0;
//         int right = 30;
//         Dictionary<Hex, GameTile> gameTileDict = new();
//         for (int r = top; r <= bottom; r++){
//             int r_offset = r>>1; //same as (int)Math.Floor(r/2.0f)
//             for (int q = left - r_offset; q <= right - r_offset; q++){
//                 if(r==0 || r == bottom || q == left - r_offset || q == right - r_offset || (r == bottom/2 && q > left - r_offset + 2 && q < right - r_offset - 2))
//                 {
//                     gameTileDict.Add(new Hex(q, r, -q-r), new GameTile(new GameHex(new Hex(q, r, -q-r), left, right, TerrainType.Flat, TerrainTemperature.Grassland, new HashSet<FeatureType>())));
//                 }
//                 else
//                 {
//                     gameTileDict.Add(new Hex(q, r, -q-r), new GameTile(new GameHex(new Hex(q, r, -q-r), left, right, TerrainType.Flat, TerrainTemperature.Grassland, new HashSet<FeatureType>())));
//                 }
//             }
//         }
//         gameTileDict.Add(new Hex(-1, -1, 2), new GameTile(new GameHex(new Hex(-1, -1, 2), left, right, TerrainType.Flat, TerrainTemperature.Grassland, new HashSet<FeatureType>())));
//         GameBoard mainBoard = new GameBoard(gameTileDict, top, bottom, left, right);

//         Dictionary<TerrainMoveType, float> movementCosts = new Dictionary<TerrainMoveType, float>
//         {
//             { TerrainMoveType.Flat, 1f },
//             { TerrainMoveType.Rough, 2f },
//             { TerrainMoveType.Mountain, 9999f },
//             { TerrainMoveType.Coast, 1f },
//             { TerrainMoveType.Ocean, 1f },
//             { TerrainMoveType.Forest, 1f },
//             { TerrainMoveType.River, 0f },
//             { TerrainMoveType.Road, 0.5f },
//             { TerrainMoveType.Embark, 0f },
//             { TerrainMoveType.Disembark, 0f }
//         };
//         Unit testUnit = new Unit("testUnit", movementCosts, gameTileDict[new Hex(-1, -1, 2)].gameHex);
//         Unit testUnit2 = new Unit("testUnit2", movementCosts, gameTileDict[new Hex(-1, -1, 2)].gameHex);
//         Hex target = new Hex(1, 1, -2);
//         if (!mainBoard.gameTileDict[target].gameHex.SpawnUnit(testUnit, true, true) | !mainBoard.gameTileDict[target].gameHex.SpawnUnit(testUnit2, true, true))
//         {
//             Tests.Complain(name);
//         }
//         if (!mainBoard.gameTileDict[target].gameHex.unitsList.Contains(testUnit) | !mainBoard.gameTileDict[target].gameHex.unitsList.Contains(testUnit2))
//         {
//             Tests.Complain(name);
//         }
//         if (!testUnit.currentGameHex.Equals(mainBoard.gameTileDict[target].gameHex) & !testUnit2.currentGameHex.Equals(mainBoard.gameTileDict[target].gameHex))
//         {
//             Tests.Complain(name);
//         }
//         if (printGameBoard)
//         {
//             mainBoard.PrintGameBoard();
//         }
//     }

//     static public void TestSpawnUnitMultipleNoStackingFlexible(bool printGameBoard)
//     {
//         String name = "TestSpawnUnitMultipleNoStackingFlexible";
//         int top = 0;
//         int bottom = 10;
//         int left = 0;
//         int right = 30;
//         Dictionary<Hex, GameTile> gameTileDict = new();
//         for (int r = top; r <= bottom; r++){
//             int r_offset = r>>1; //same as (int)Math.Floor(r/2.0f)
//             for (int q = left - r_offset; q <= right - r_offset; q++){
//                 if(r==0 || r == bottom || q == left - r_offset || q == right - r_offset || (r == bottom/2 && q > left - r_offset + 2 && q < right - r_offset - 2))
//                 {
//                     gameTileDict.Add(new Hex(q, r, -q-r), new GameTile(new GameHex(new Hex(q, r, -q-r), left, right, TerrainType.Flat, TerrainTemperature.Grassland, new HashSet<FeatureType>())));
//                 }
//                 else
//                 {
//                     gameTileDict.Add(new Hex(q, r, -q-r), new GameTile(new GameHex(new Hex(q, r, -q-r), left, right, TerrainType.Flat, TerrainTemperature.Grassland, new HashSet<FeatureType>())));
//                 }
//             }
//         }
//         gameTileDict.Add(new Hex(-1, -1, 2), new GameTile(new GameHex(new Hex(-1, -1, 2), left, right, TerrainType.Flat, TerrainTemperature.Grassland, new HashSet<FeatureType>())));
//         GameBoard mainBoard = new GameBoard(gameTileDict, top, bottom, left, right);

//         Dictionary<TerrainMoveType, float> movementCosts = new Dictionary<TerrainMoveType, float>
//         {
//             { TerrainMoveType.Flat, 1f },
//             { TerrainMoveType.Rough, 2f },
//             { TerrainMoveType.Mountain, 9999f },
//             { TerrainMoveType.Coast, 1f },
//             { TerrainMoveType.Ocean, 1f },
//             { TerrainMoveType.Forest, 1f },
//             { TerrainMoveType.River, 0f },
//             { TerrainMoveType.Road, 0.5f },
//             { TerrainMoveType.Embark, 0f },
//             { TerrainMoveType.Disembark, 0f }
//         };
//         Unit testUnit = new Unit("testUnit", movementCosts, gameTileDict[new Hex(-1, -1, 2)].gameHex);
//         Unit testUnit2 = new Unit("testUnit2", movementCosts, gameTileDict[new Hex(-1, -1, 2)].gameHex);
//         Hex target = new Hex(1, 1, -2);
//         Hex flexTarget = target.WrappingNeighbor(0, left, right);
//         if (!mainBoard.gameTileDict[target].gameHex.SpawnUnit(testUnit, false, true) | !mainBoard.gameTileDict[target].gameHex.SpawnUnit(testUnit2, false, true))
//         {
//             Tests.Complain(name);
//         }
//         if (!mainBoard.gameTileDict[target].gameHex.unitsList.Contains(testUnit) | !mainBoard.gameTileDict[flexTarget].gameHex.unitsList.Contains(testUnit2))
//         {
//             Tests.Complain(name);
//         }
//         if (!testUnit.currentGameHex.Equals(mainBoard.gameTileDict[target].gameHex))
//         {
//             Tests.Complain(name);
//         }
//         if (!testUnit2.currentGameHex.Equals(mainBoard.gameTileDict[flexTarget].gameHex))
//         {
//             Tests.Complain(name);
//         }
//         if (printGameBoard)
//         {
//             mainBoard.PrintGameBoard();
//         }
//     }
    
//     static public void TestSpawnUnitSevenNoStackingFlexible(bool printGameBoard)
//     {
//         String name = "TestSpawnUnitSevenNoStackingFlexible";
//         int top = 0;
//         int bottom = 10;
//         int left = 0;
//         int right = 30;
//         Dictionary<Hex, GameTile> gameTileDict = new();
//         for (int r = top; r <= bottom; r++){
//             int r_offset = r>>1; //same as (int)Math.Floor(r/2.0f)
//             for (int q = left - r_offset; q <= right - r_offset; q++){
//                 if(r==0 || r == bottom || q == left - r_offset || q == right - r_offset || (r == bottom/2 && q > left - r_offset + 2 && q < right - r_offset - 2))
//                 {
//                     gameTileDict.Add(new Hex(q, r, -q-r), new GameTile(new GameHex(new Hex(q, r, -q-r), left, right, TerrainType.Flat, TerrainTemperature.Grassland, new HashSet<FeatureType>())));
//                 }
//                 else
//                 {
//                     gameTileDict.Add(new Hex(q, r, -q-r), new GameTile(new GameHex(new Hex(q, r, -q-r), left, right, TerrainType.Flat, TerrainTemperature.Grassland, new HashSet<FeatureType>())));
//                 }
//             }
//         }
//         gameTileDict.Add(new Hex(-1, -1, 2), new GameTile(new GameHex(new Hex(-1, -1, 2), left, right, TerrainType.Flat, TerrainTemperature.Grassland, new HashSet<FeatureType>())));
//         GameBoard mainBoard = new GameBoard(gameTileDict, top, bottom, left, right);

//         Dictionary<TerrainMoveType, float> movementCosts = new Dictionary<TerrainMoveType, float>
//         {
//             { TerrainMoveType.Flat, 1f },
//             { TerrainMoveType.Rough, 2f },
//             { TerrainMoveType.Mountain, 9999f },
//             { TerrainMoveType.Coast, 1f },
//             { TerrainMoveType.Ocean, 1f },
//             { TerrainMoveType.Forest, 1f },
//             { TerrainMoveType.River, 0f },
//             { TerrainMoveType.Road, 0.5f },
//             { TerrainMoveType.Embark, 0f },
//             { TerrainMoveType.Disembark, 0f }
//         };
//         Unit testUnit = new Unit("testUnit", movementCosts, gameTileDict[new Hex(-1, -1, 2)].gameHex);
//         Unit testUnit2 = new Unit("testUnit2", movementCosts, gameTileDict[new Hex(-1, -1, 2)].gameHex);
//         Unit testUnit3 = new Unit("testUnit3", movementCosts, gameTileDict[new Hex(-1, -1, 2)].gameHex);
//         Unit testUnit4 = new Unit("testUnit4", movementCosts, gameTileDict[new Hex(-1, -1, 2)].gameHex);
//         Unit testUnit5 = new Unit("testUnit5", movementCosts, gameTileDict[new Hex(-1, -1, 2)].gameHex);
//         Unit testUnit6 = new Unit("testUnit6", movementCosts, gameTileDict[new Hex(-1, -1, 2)].gameHex);
//         Unit testUnit7 = new Unit("testUnit7", movementCosts, gameTileDict[new Hex(-1, -1, 2)].gameHex);
//         Hex target = new Hex(1, 1, -2);
//         Hex flexTarget2 = target.WrappingNeighbor(0, left, right);
//         Hex flexTarget3 = target.WrappingNeighbor(1, left, right);
//         Hex flexTarget4 = target.WrappingNeighbor(2, left, right);
//         Hex flexTarget5 = target.WrappingNeighbor(3, left, right);
//         Hex flexTarget6 = target.WrappingNeighbor(4, left, right);
//         Hex flexTarget7 = target.WrappingNeighbor(5, left, right);
//         if (!mainBoard.gameTileDict[target].gameHex.SpawnUnit(testUnit, false, true) | !mainBoard.gameTileDict[flexTarget2].gameHex.SpawnUnit(testUnit2, false, true) | !mainBoard.gameTileDict[flexTarget3].gameHex.SpawnUnit(testUnit3, false, true) | !mainBoard.gameTileDict[flexTarget4].gameHex.SpawnUnit(testUnit4, false, true) |!mainBoard.gameTileDict[flexTarget5].gameHex.SpawnUnit(testUnit5, false, true) | !mainBoard.gameTileDict[flexTarget6].gameHex.SpawnUnit(testUnit6, false, true) | !mainBoard.gameTileDict[flexTarget7].gameHex.SpawnUnit(testUnit7, false, true))
//         {
//             Tests.Complain(name);
//         }
//         if (!mainBoard.gameTileDict[target].gameHex.unitsList.Contains(testUnit) | !mainBoard.gameTileDict[flexTarget2].gameHex.unitsList.Contains(testUnit2) | !mainBoard.gameTileDict[flexTarget3].gameHex.unitsList.Contains(testUnit3) | !mainBoard.gameTileDict[flexTarget4].gameHex.unitsList.Contains(testUnit4) | !mainBoard.gameTileDict[flexTarget5].gameHex.unitsList.Contains(testUnit5) | !mainBoard.gameTileDict[flexTarget6].gameHex.unitsList.Contains(testUnit6) | !mainBoard.gameTileDict[flexTarget7].gameHex.unitsList.Contains(testUnit7))
//         {
//             Tests.Complain(name);
//         }
//         if (!testUnit.currentGameHex.Equals(mainBoard.gameTileDict[target].gameHex) & !testUnit2.currentGameHex.Equals(mainBoard.gameTileDict[flexTarget2]) & !testUnit3.currentGameHex.Equals(mainBoard.gameTileDict[flexTarget3]) & !testUnit4.currentGameHex.Equals(mainBoard.gameTileDict[flexTarget4]) & !testUnit5.currentGameHex.Equals(mainBoard.gameTileDict[flexTarget5]) & !testUnit6.currentGameHex.Equals(mainBoard.gameTileDict[flexTarget6]) & !testUnit7.currentGameHex.Equals(mainBoard.gameTileDict[flexTarget7]))
//         {
//             Tests.Complain(name);
//         }
//         if (printGameBoard)
//         {
//             mainBoard.PrintGameBoard();
//         }
//     }

//     static public void TestSpawnUnitMultipleNoStackingNoFlexible(bool printGameBoard)
//     {
//         String name = "TestSpawnUnitMultipleNoStackingNoFlexible";
//         int top = 0;
//         int bottom = 10;
//         int left = 0;
//         int right = 30;
//         Dictionary<Hex, GameTile> gameTileDict = new();
//         for (int r = top; r <= bottom; r++){
//             int r_offset = r>>1; //same as (int)Math.Floor(r/2.0f)
//             for (int q = left - r_offset; q <= right - r_offset; q++){
//                 if(r==0 || r == bottom || q == left - r_offset || q == right - r_offset || (r == bottom/2 && q > left - r_offset + 2 && q < right - r_offset - 2))
//                 {
//                     gameTileDict.Add(new Hex(q, r, -q-r), new GameTile(new GameHex(new Hex(q, r, -q-r), left, right, TerrainType.Flat, TerrainTemperature.Grassland, new HashSet<FeatureType>())));
//                 }
//                 else
//                 {
//                     gameTileDict.Add(new Hex(q, r, -q-r), new GameTile(new GameHex(new Hex(q, r, -q-r), left, right, TerrainType.Flat, TerrainTemperature.Grassland, new HashSet<FeatureType>())));
//                 }
//             }
//         }
//         gameTileDict.Add(new Hex(-1, -1, 2), new GameTile(new GameHex(new Hex(-1, -1, 2), left, right, TerrainType.Flat, TerrainTemperature.Grassland, new HashSet<FeatureType>())));
//         GameBoard mainBoard = new GameBoard(gameTileDict, top, bottom, left, right);

//         Dictionary<TerrainMoveType, float> movementCosts = new Dictionary<TerrainMoveType, float>
//         {
//             { TerrainMoveType.Flat, 1f },
//             { TerrainMoveType.Rough, 2f },
//             { TerrainMoveType.Mountain, 9999f },
//             { TerrainMoveType.Coast, 1f },
//             { TerrainMoveType.Ocean, 1f },
//             { TerrainMoveType.Forest, 1f },
//             { TerrainMoveType.River, 0f },
//             { TerrainMoveType.Road, 0.5f },
//             { TerrainMoveType.Embark, 0f },
//             { TerrainMoveType.Disembark, 0f }
//         };
//         Unit testUnit = new Unit("testUnit", movementCosts, gameTileDict[new Hex(-1, -1, 2)].gameHex);
//         Unit testUnit2 = new Unit("testUnit2", movementCosts, gameTileDict[new Hex(-1, -1, 2)].gameHex);
//         Hex target = new Hex(1, 1, -2);
//         Hex flexTarget = target.WrappingNeighbor(0, left, right);
//         if (!mainBoard.gameTileDict[target].gameHex.SpawnUnit(testUnit, false, false) | mainBoard.gameTileDict[target].gameHex.SpawnUnit(testUnit2, false, false))
//         {
//             Tests.Complain(name);
//         }
//         if (!mainBoard.gameTileDict[target].gameHex.unitsList.Contains(testUnit) | mainBoard.gameTileDict[target].gameHex.unitsList.Contains(testUnit2) | mainBoard.gameTileDict[flexTarget].gameHex.unitsList.Contains(testUnit2))
//         {
//             Tests.Complain(name);
//         }
//         if (!testUnit.currentGameHex.Equals(mainBoard.gameTileDict[target].gameHex) & !testUnit2.currentGameHex.Equals(gameTileDict[new Hex(-1, -1, 2)].gameHex))
//         {
//             Tests.Complain(name);
//         }
//         if (printGameBoard)
//         {
//             mainBoard.PrintGameBoard();
//         }
//     }
    
//     static public void TestSpawnUnitInvalidHexFlexible(bool printGameBoard)
//     {
//         String name = "TestSpawnUnitInvalidHexFlexible";
//         int top = 0;
//         int bottom = 10;
//         int left = 0;
//         int right = 30;
//         Dictionary<Hex, GameTile> gameTileDict = new();
//         for (int r = top; r <= bottom; r++){
//             //int r_offset = r>>1; //same as (int)Math.Floor(r/2.0f)
//             for (int q = left; q <= right; q++){
//                 if(r==0 || r == bottom || q == left || q == right || (r == bottom/2 && q > left + 2 && q < right - 2))
//                 {
//                     gameTileDict.Add(new Hex(q, r, -q-r), new GameTile(new GameHex(new Hex(q, r, -q-r), left, right, TerrainType.Mountain, TerrainTemperature.Grassland, new HashSet<FeatureType>())));
//                 }
//                 else
//                 {
//                     gameTileDict.Add(new Hex(q, r, -q-r), new GameTile(new GameHex(new Hex(q, r, -q-r), left, right, TerrainType.Flat, TerrainTemperature.Grassland, new HashSet<FeatureType>())));
//                 }
//             }
//         }
//         gameTileDict.Add(new Hex(-1, -1, 2), new GameTile(new GameHex(new Hex(-1, -1, 2), left, right, TerrainType.Flat, TerrainTemperature.Grassland, new HashSet<FeatureType>())));
//         GameBoard mainBoard = new GameBoard(gameTileDict, top, bottom, left, right);

//         Dictionary<TerrainMoveType, float> movementCosts = new Dictionary<TerrainMoveType, float>
//         {
//             { TerrainMoveType.Flat, 1f },
//             { TerrainMoveType.Rough, 2f },
//             { TerrainMoveType.Mountain, 9999f },
//             { TerrainMoveType.Coast, 1f },
//             { TerrainMoveType.Ocean, 1f },
//             { TerrainMoveType.Forest, 1f },
//             { TerrainMoveType.River, 0f },
//             { TerrainMoveType.Road, 0.5f },
//             { TerrainMoveType.Embark, 0f },
//             { TerrainMoveType.Disembark, 0f }
//         };
//         Unit testUnit = new Unit("testUnit", movementCosts, gameTileDict[new Hex(-1, -1, 2)].gameHex);
//         Hex target = new Hex(0, 5, -7);
//         Hex flexTarget = target.WrappingNeighbor(0, left, right);
//         if (!mainBoard.gameTileDict[target].gameHex.SpawnUnit(testUnit, false, true))
//         {
//             Tests.Complain(name);
//         }
//         if (mainBoard.gameTileDict[target].gameHex.unitsList.Contains(testUnit))
//         {
//             Tests.Complain(name);
//         }
//         if (!mainBoard.gameTileDict[flexTarget].gameHex.unitsList.Contains(testUnit))
//         {
//             Tests.Complain(name);
//         }
//         if (testUnit.currentGameHex.Equals(mainBoard.gameTileDict[target].gameHex))
//         {
//             Tests.Complain(name);
//         }
//         if (printGameBoard)
//         {
//             mainBoard.PrintGameBoard();
//         }
//     }

//     static public void TestSpawnUnitInvalidHexNoFlexible(bool printGameBoard)
//     {
//         String name = "TestSpawnUnitInvalidHexFlexible";
//         int top = 0;
//         int bottom = 10;
//         int left = 0;
//         int right = 30;
//         Dictionary<Hex, GameTile> gameTileDict = new();
//         for (int r = top; r <= bottom; r++){
//             //int r_offset = r>>1; //same as (int)Math.Floor(r/2.0f)
//             for (int q = left; q <= right; q++){
//                 if(r==0 || r == bottom || q == left || q == right || (r == bottom/2 && q > left + 2 && q < right - 2))
//                 {
//                     gameTileDict.Add(new Hex(q, r, -q-r), new GameTile(new GameHex(new Hex(q, r, -q-r), left, right, TerrainType.Mountain, TerrainTemperature.Grassland, new HashSet<FeatureType>())));
//                 }
//                 else
//                 {
//                     gameTileDict.Add(new Hex(q, r, -q-r), new GameTile(new GameHex(new Hex(q, r, -q-r), left, right, TerrainType.Flat, TerrainTemperature.Grassland, new HashSet<FeatureType>())));
//                 }
//             }
//         }
//         gameTileDict.Add(new Hex(-1, -1, 2), new GameTile(new GameHex(new Hex(-1, -1, 2), left, right, TerrainType.Flat, TerrainTemperature.Grassland, new HashSet<FeatureType>())));
//         GameBoard mainBoard = new GameBoard(gameTileDict, top, bottom, left, right);

//         Dictionary<TerrainMoveType, float> movementCosts = new Dictionary<TerrainMoveType, float>
//         {
//             { TerrainMoveType.Flat, 1f },
//             { TerrainMoveType.Rough, 2f },
//             { TerrainMoveType.Mountain, 9999f },
//             { TerrainMoveType.Coast, 1f },
//             { TerrainMoveType.Ocean, 1f },
//             { TerrainMoveType.Forest, 1f },
//             { TerrainMoveType.River, 0f },
//             { TerrainMoveType.Road, 0.5f },
//             { TerrainMoveType.Embark, 0f },
//             { TerrainMoveType.Disembark, 0f }
//         };
//         Unit testUnit = new Unit("testUnit", movementCosts, gameTileDict[new Hex(-1, -1, 2)].gameHex);
//         Hex target = new Hex(0, 5, -7);
//         if (mainBoard.gameTileDict[target].gameHex.SpawnUnit(testUnit, false, false))
//         {
//             Tests.Complain(name);
//         }
//         if (!mainBoard.gameTileDict[target].gameHex.unitsList.Contains(testUnit))
//         {
//             Tests.Complain(name);
//         }
//         if (mainBoard.gameTileDict[target].gameHex.unitsList.Contains(testUnit))
//         {
//             Tests.Complain(name);
//         }
//         if (testUnit.currentGameHex.Equals(mainBoard.gameTileDict[target].gameHex))
//         {
//             Tests.Complain(name);
//         }
//         if (printGameBoard)
//         {
//             mainBoard.PrintGameBoard();
//         }
//     }
    
    
    
//     static public void Complain(String name)
//     {
//         Console.WriteLine("FAIL " + name);
//     }

//     static public void TestAll()
//     {
//         GameHexTests.TestSpawnUnitSingle(false);
//         GameHexTests.TestSpawnUnitMultipleStacking(false);
//         GameHexTests.TestSpawnUnitMultipleNoStackingFlexible(false);
//         GameHexTests.TestSpawnUnitMultipleNoStackingNoFlexible(false);
//         GameHexTests.TestSpawnUnitInvalidHexFlexible(false);
//         GameHexTests.TestSpawnUnitInvalidHexNoFlexible(false);
//     }
// }

// struct GameHexMain
// {
//     static public void Main()
//     {
//         Tests.TestAll();
//     }
// }
