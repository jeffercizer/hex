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

enum TerrainMoveType
{
    Flat,
    Rough,
    Mountain,
    Coast,
    Ocean,
    Forest,
    River,
    Road,
    
    Coral,
    Embark,
    Disembark
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

struct GameHex
{
    public GameHex(Hex hex, GameBoard ourGameBoard, TerrainType terrainType, TerrainTemperature terrainTemp, HashSet<FeatureType> featureSet, List<Unity> unitsList)
    {
        this.hex = hex;
        this.ourGameBoard = ourGameBoard;
        this.terrainType = terrainType;
        this.terrainTemp = terrainTemp;
        this.featureSet = featureSet;
        this.unitsList = unitsList;
    }

    public readonly Hex hex;
    public readonly GameBoard ourGameBoard;
    public TerrainType terrainType;
    public TerrainTemperature terrainTemp;
    public HashSet<FeatureType> featureSet;
    public List<Unit> unitsList = new List<Unit>();

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

    //if stackable is true allow multiple units to stack
    //if flexible is true look for adjacent spaces to place
    public bool SpawnUnit(Unit newUnit, bool stackable, bool flexible)
    {
        if(!stackable & unitsList.Any()) //if they cant stack and their are units
        {
            if (flexible)
            {
                for (int i = 0; i < 6; i++) //ask all our neighbors if they have space
                {
                    if(WrappingNeighbor(i, ourGameBoard.left, ourGameBoard.right).SpawnUnit(newUnit, stackable, false))
                    {
                        return true;
                    }
                }
                //if we still havent found a spot give up
                return false;
            }
        }
        else //if they cant stack and there aren't units or they can stack and units are/aren't there
        {
            unitsList.Add(newUnit);
            newUnit.SetGameHex(this);
        }
        return true;
    }


}

struct GameHexTests
{
    static public void TestAll()
    {
        GameHexTests.TestSpawnUnitSingle(false);
        GameHexTests.TestSpawnUnitMultipleStacking(false);
        GameHexTests.TestSpawnUnitMultipleNoStackingFlexible(false);
        GameHexTests.TestSpawnUnitMultipleNoStackingNoFlexible(false);
    }
    
    static public void TestSpawnUnitSingle(bool printGameBoard)
    {
        String name = "TestSpawnUnitSingle";
        int top = 0;
        int bottom = 10;
        int left = 0;
        int right = 30;
        Dictionary<Hex, GameHex> gameHexDict = new();
        for (int r = top; r <= bottom; r++){
            int r_offset = r>>1; //same as (int)Math.Floor(r/2.0f)
            for (int q = left - r_offset; q <= right - r_offset; q++){
                if(r==0 || r == bottom || q == left - r_offset || q == right - r_offset || (r == bottom/2 && q > left - r_offset + 2 && q < right - r_offset - 2))
                {
                    gameHexDict.Add(new Hex(q, r, -q-r), new GameHex(new Hex(q, r, -q-r), TerrainType.Flat, TerrainTemperature.Grassland, new HashSet<FeatureType>()));
                }
                else
                {
                    gameHexDict.Add(new Hex(q, r, -q-r), new GameHex(new Hex(q, r, -q-r), TerrainType.Flat, TerrainTemperature.Grassland, new HashSet<FeatureType>()));
                }
            }
        }
        GameBoard mainBoard = new GameBoard(gameHexDict, top, bottom, left, right);

        Hex target = new Hex(1, 1, -2);
        Dictionary<TerrainMoveType, float> movementCosts = {
            { TerrainMoveType.Flat, 1 },
            { TerrainMoveType.Rough, 2 },
            { TerrainMoveType.Mountain, 9999 },
            { TerrainMoveType.Coast, 1 },
            { TerrainMoveType.Ocean, 1 },
            { TerrainMoveType.Forest, 1 },
            { TerrainMoveType.River, 0 },
            { TerrainMoveType.Road, 0.5f },
            { TerrainMoveType.Embark, 0 },
            { TerrainMoveType.Disembark, 0 },
        };
        Unit testUnit = new Unit("testUnit", movementCosts, this);
        if (!mainBoard.gameHexDict[target].SpawnUnit(testUnit, true, true))
        {
            Tests.Complain(name);
        }
        else if (!mainBoard.gameHexDict[target].unitsList.Contains(testUnit))
        {
            Tests.Complain(name);
        }
        if (printGameBoard)
        {
            mainBoard.PrintGameBoard();
        }
    }

    static public void TestSpawnUnitMultipleStacking(bool printGameBoard)
    {
        String name = "TestSpawnUnitMultipleStacking";
        int top = 0;
        int bottom = 10;
        int left = 0;
        int right = 30;
        Dictionary<Hex, GameHex> gameHexDict = new();
        for (int r = top; r <= bottom; r++){
            int r_offset = r>>1; //same as (int)Math.Floor(r/2.0f)
            for (int q = left - r_offset; q <= right - r_offset; q++){
                if(r==0 || r == bottom || q == left - r_offset || q == right - r_offset || (r == bottom/2 && q > left - r_offset + 2 && q < right - r_offset - 2))
                {
                    gameHexDict.Add(new Hex(q, r, -q-r), new GameHex(new Hex(q, r, -q-r), TerrainType.Flat, TerrainTemperature.Grassland, new HashSet<FeatureType>()));
                }
                else
                {
                    gameHexDict.Add(new Hex(q, r, -q-r), new GameHex(new Hex(q, r, -q-r), TerrainType.Flat, TerrainTemperature.Grassland, new HashSet<FeatureType>()));
                }
            }
        }
        GameBoard mainBoard = new GameBoard(gameHexDict, top, bottom, left, right);

        Dictionary<TerrainMoveType, float> movementCosts = {
            { TerrainMoveType.Flat, 1 },
            { TerrainMoveType.Rough, 2 },
            { TerrainMoveType.Mountain, 9999 },
            { TerrainMoveType.Coast, 1 },
            { TerrainMoveType.Ocean, 1 },
            { TerrainMoveType.Forest, 1 },
            { TerrainMoveType.River, 0 },
            { TerrainMoveType.Road, 0.5f },
            { TerrainMoveType.Embark, 0 },
            { TerrainMoveType.Disembark, 0 },
        };
        Unit testUnit = new Unit("testUnit", movementCosts, this);
        Unit testUnit2 = new Unit("testUnit2", movementCosts, this);
        Hex target = new Hex(1, 1, -2);
        if (!mainBoard.gameHexDict[target].SpawnUnit(testUnit, true, true) | !mainBoard.gameHexDict[target].SpawnUnit(testUnit2, true, true))
        {
            Tests.Complain(name);
        }
        if (!mainBoard.gameHexDict[target].unitsList.Contains(testUnit) | !mainBoard.gameHexDict[target].unitsList.Contains(testUnit2))
        {
            Tests.Complain(name);
        }
        if (printGameBoard)
        {
            mainBoard.PrintGameBoard();
        }
    }

    static public void TestSpawnUnitMultipleNoStackingFlexible(bool printGameBoard)
    {
        String name = "TestSpawnUnitMultipleNoStackingFlexible";
        int top = 0;
        int bottom = 10;
        int left = 0;
        int right = 30;
        Dictionary<Hex, GameHex> gameHexDict = new();
        for (int r = top; r <= bottom; r++){
            int r_offset = r>>1; //same as (int)Math.Floor(r/2.0f)
            for (int q = left - r_offset; q <= right - r_offset; q++){
                if(r==0 || r == bottom || q == left - r_offset || q == right - r_offset || (r == bottom/2 && q > left - r_offset + 2 && q < right - r_offset - 2))
                {
                    gameHexDict.Add(new Hex(q, r, -q-r), new GameHex(new Hex(q, r, -q-r), TerrainType.Flat, TerrainTemperature.Grassland, new HashSet<FeatureType>()));
                }
                else
                {
                    gameHexDict.Add(new Hex(q, r, -q-r), new GameHex(new Hex(q, r, -q-r), TerrainType.Flat, TerrainTemperature.Grassland, new HashSet<FeatureType>()));
                }
            }
        }
        GameBoard mainBoard = new GameBoard(gameHexDict, top, bottom, left, right);

        Dictionary<TerrainMoveType, float> movementCosts = {
            { TerrainMoveType.Flat, 1 },
            { TerrainMoveType.Rough, 2 },
            { TerrainMoveType.Mountain, 9999 },
            { TerrainMoveType.Coast, 1 },
            { TerrainMoveType.Ocean, 1 },
            { TerrainMoveType.Forest, 1 },
            { TerrainMoveType.River, 0 },
            { TerrainMoveType.Road, 0.5f },
            { TerrainMoveType.Embark, 0 },
            { TerrainMoveType.Disembark, 0 },
        };
        Unit testUnit = new Unit("testUnit", movementCosts, this);
        Unit testUnit2 = new Unit("testUnit2", movementCosts, this);
        Hex target = new Hex(1, 1, -2);
        Hex flexTarget = target.WrappingNeighbor(0);
        if (!mainBoard.gameHexDict[target].SpawnUnit(testUnit, false, true) | !mainBoard.gameHexDict[target].SpawnUnit(testUnit2, false, true))
        {
            Tests.Complain(name);
        }
        if (!mainBoard.gameHexDict[target].unitsList.Contains(testUnit) | !mainBoard.gameHexDict[flexTarget].unitsList.Contains(testUnit2))
        {
            Tests.Complain(name);
        }
        if (printGameBoard)
        {
            mainBoard.PrintGameBoard();
        }
    }
    
    static public void TestSpawnUnitSevenNoStackingFlexible(bool printGameBoard)
    {
        String name = "TestSpawnUnitSevenNoStackingFlexible";
        int top = 0;
        int bottom = 10;
        int left = 0;
        int right = 30;
        Dictionary<Hex, GameHex> gameHexDict = new();
        for (int r = top; r <= bottom; r++){
            int r_offset = r>>1; //same as (int)Math.Floor(r/2.0f)
            for (int q = left - r_offset; q <= right - r_offset; q++){
                if(r==0 || r == bottom || q == left - r_offset || q == right - r_offset || (r == bottom/2 && q > left - r_offset + 2 && q < right - r_offset - 2))
                {
                    gameHexDict.Add(new Hex(q, r, -q-r), new GameHex(new Hex(q, r, -q-r), TerrainType.Flat, TerrainTemperature.Grassland, new HashSet<FeatureType>()));
                }
                else
                {
                    gameHexDict.Add(new Hex(q, r, -q-r), new GameHex(new Hex(q, r, -q-r), TerrainType.Flat, TerrainTemperature.Grassland, new HashSet<FeatureType>()));
                }
            }
        }
        GameBoard mainBoard = new GameBoard(gameHexDict, top, bottom, left, right);

        Dictionary<TerrainMoveType, float> movementCosts = {
            { TerrainMoveType.Flat, 1 },
            { TerrainMoveType.Rough, 2 },
            { TerrainMoveType.Mountain, 9999 },
            { TerrainMoveType.Coast, 1 },
            { TerrainMoveType.Ocean, 1 },
            { TerrainMoveType.Forest, 1 },
            { TerrainMoveType.River, 0 },
            { TerrainMoveType.Road, 0.5f },
            { TerrainMoveType.Embark, 0 },
            { TerrainMoveType.Disembark, 0 },
        };
        Unit testUnit = new Unit("testUnit", movementCosts, this);
        Unit testUnit2 = new Unit("testUnit2", movementCosts, this);
        Unit testUnit3 = new Unit("testUnit3", movementCosts, this);
        Unit testUnit4 = new Unit("testUnit4", movementCosts, this);
        Unit testUnit5 = new Unit("testUnit5", movementCosts, this);
        Unit testUnit6 = new Unit("testUnit6", movementCosts, this);
        Unit testUnit7 = new Unit("testUnit7", movementCosts, this);
        Hex target = new Hex(1, 1, -2);
        Hex flexTarget2 = target.WrappingNeighbor(0);
        Hex flexTarget3 = target.WrappingNeighbor(1);
        Hex flexTarget4 = target.WrappingNeighbor(2);
        Hex flexTarget5 = target.WrappingNeighbor(3);
        Hex flexTarget6 = target.WrappingNeighbor(4);
        Hex flexTarget7 = target.WrappingNeighbor(5);
        if (!mainBoard.gameHexDict[target].SpawnUnit(testUnit, false, true) | !mainBoard.gameHexDict[flexTarget2].SpawnUnit(testUnit2, false, true) | !mainBoard.gameHexDict[flexTarget3].SpawnUnit(testUnit3, false, true) | !mainBoard.gameHexDict[flexTarget4].SpawnUnit(testUnit4, false, true) |!mainBoard.gameHexDict[flexTarget5].SpawnUnit(testUnit5, false, true) | !mainBoard.gameHexDict[flexTarget6].SpawnUnit(testUnit6, false, true) | !mainBoard.gameHexDict[flexTarget7].SpawnUnit(testUnit7, false, true))
        {
            Tests.Complain(name);
        }
        if (!mainBoard.gameHexDict[target].unitsList.Contains(testUnit) | !mainBoard.gameHexDict[flexTarget2].unitsList.Contains(testUnit2) | !mainBoard.gameHexDict[flexTarget3].unitsList.Contains(testUnit3) | !mainBoard.gameHexDict[flexTarget4].unitsList.Contains(testUnit4) | !mainBoard.gameHexDict[testUnit5].unitsList.Contains(testUnit5) | !mainBoard.gameHexDict[flexTarget6].unitsList.Contains(testUnit6) | !mainBoard.gameHexDict[flexTarget7].unitsList.Contains(testUnit7))
        {
            Tests.Complain(name);
        }
        if (printGameBoard)
        {
            mainBoard.PrintGameBoard();
        }
    }

    static public void TestSpawnUnitMultipleNoStackingNoFlexible(bool printGameBoard)
    {
        String name = "TestSpawnUnitMultipleNoStackingNoFlexible";
        int top = 0;
        int bottom = 10;
        int left = 0;
        int right = 30;
        Dictionary<Hex, GameHex> gameHexDict = new();
        for (int r = top; r <= bottom; r++){
            int r_offset = r>>1; //same as (int)Math.Floor(r/2.0f)
            for (int q = left - r_offset; q <= right - r_offset; q++){
                if(r==0 || r == bottom || q == left - r_offset || q == right - r_offset || (r == bottom/2 && q > left - r_offset + 2 && q < right - r_offset - 2))
                {
                    gameHexDict.Add(new Hex(q, r, -q-r), new GameHex(new Hex(q, r, -q-r), TerrainType.Flat, TerrainTemperature.Grassland, new HashSet<FeatureType>()));
                }
                else
                {
                    gameHexDict.Add(new Hex(q, r, -q-r), new GameHex(new Hex(q, r, -q-r), TerrainType.Flat, TerrainTemperature.Grassland, new HashSet<FeatureType>()));
                }
            }
        }
        GameBoard mainBoard = new GameBoard(gameHexDict, top, bottom, left, right);

        Dictionary<TerrainMoveType, float> movementCosts = {
            { TerrainMoveType.Flat, 1 },
            { TerrainMoveType.Rough, 2 },
            { TerrainMoveType.Mountain, 9999 },
            { TerrainMoveType.Coast, 1 },
            { TerrainMoveType.Ocean, 1 },
            { TerrainMoveType.Forest, 1 },
            { TerrainMoveType.River, 0 },
            { TerrainMoveType.Road, 0.5f },
            { TerrainMoveType.Embark, 0 },
            { TerrainMoveType.Disembark, 0 },
        };
        Unit testUnit = new Unit("testUnit", movementCosts, this);
        Unit testUnit2 = new Unit("testUnit2", movementCosts, this);
        Hex target = new Hex(1, 1, -2);
        Hex flexTarget = target.WrappingNeighbor(0);
        if (!mainBoard.gameHexDict[target].SpawnUnit(testUnit, false, false) | mainBoard.gameHexDict[target].SpawnUnit(testUnit2, false, false))
        {
            Tests.Complain(name);
        }
        if (!mainBoard.gameHexDict[target].unitsList.Contains(testUnit) | mainBoard.gameHexDict[target].unitsList.Contains(testUnit2) | mainBoard.gameHexDict[flexTarget].unitsList.Contains(testUnit2))
        {
            Tests.Complain(name);
        }
        if (printGameBoard)
        {
            mainBoard.PrintGameBoard();
        }
    }
    
    static public void Complain(String name)
    {
        Console.WriteLine("FAIL " + name);
    }
}

struct GameHexMain
{
    static public void Main()
    {
        Tests.TestAll();
    }
}
