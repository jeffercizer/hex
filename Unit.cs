using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;

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

struct Unit
{
    public Unit(String name, Dictionary<TerrainMoveType, float> movementCosts, GameHex currentGameHex)
    {
        this.name = name;
        this.movementCosts = movementCosts;
        this.currentGameHex = currentGameHex;
    }

    public Unit(String name, Dictionary<TerrainMoveType, float> movementCosts, GameHex currentGameHex, float movementSpeed, int teamNum)
    {
        this.name = name;
        this.movementCosts = movementCosts;
        this.currentGameHex = currentGameHex;
        this.movementSpeed = movementSpeed;
        this.teamNum = teamNum;
    }

    public String name;
    public Dictionary<TerrainMoveType, float> movementCosts;
    public GameHex currentGameHex;
    public float movementSpeed = 2.0f;
    public float remainingMovement = 2.0f;
    public int teamNum = 1;

    public void OnTurnStarted(int turnNumber)
    {
        remainingMovement = movementSpeed;
        Console.WriteLine($"Unit ({name}): Started turn {turnNumber}.");
    }

    public void OnTurnEnded(int turnNumber)
    {
        Console.WriteLine($"Unit ({name}): Ended turn {turnNumber}.");
    }

    public bool SetGameHex(GameHex newGameHex)
    {
        currentGameHex = newGameHex;
        return true;
    }

    public bool MoveToGameHex(GameHex targetGameHex)
    {
        if(targetGameHex.unitsList.Any())
        {
            return false;
        }
        moveCost = TravelCost(currentGameHex.hex, targetGameHex.hex, movementCosts, movementSpeed, movementSpeed-remainingMovement);
        if(moveCost <= remainingMovement)
        {
            remainingMovement -= moveCost;
            currentGameHex.unitsList.Remove(this);
            currentGameHex = targetGameHex;
            currentGameHex.unitsList.Add(this);
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool MoveTowards(GameHex targetGameHex)
    {
        Dictionary<Hex, Hex> path = mainBoard.PathFind(currentGameHex, targetGameHex, movementCosts, movementSpeed);
        foreach (GameHex target in path)
        {
            if(!MoveToGameHex(target))
            {
                return false
            }
        }
    }
    
    public float TravelCost(Hex first, Hex second, Dictionary<TerrainMoveType, float> movementCosts, float unitMovementSpeed, float costSoFar)
    {
        //cost for river, embark, disembark are custom (0 = end turn to enter, 1/2/3/4 = normal cost)\\
        GameHex firstHex;
        GameHex secondHex;
        if (!gameHexDict.TryGetValue(first, out firstHex)) //if firstHex is somehow off the table return max
        {
            return 111111;
        }
        if (!gameHexDict.TryGetValue(second, out secondHex)) //if secondHex is off the table return max
        {
            return 333333;
        }
        float moveCost = 222222; //default value should be set
        if (firstHex.terrainType == TerrainType.Coast || firstHex.terrainType == TerrainType.Ocean) //first hex is on water
        {
            if (secondHex.terrainType == TerrainType.Coast || secondHex.terrainType == TerrainType.Ocean) //second hex is on coast so we pay the normal cost
            {
                moveCost = movementCosts[(TerrainMoveType)secondHex.terrainType];
                foreach (FeatureType feature in secondHex.featureSet)
                {
                    if (feature == FeatureType.Coral)
                    {
                        moveCost += movementCosts[TerrainMoveType.Coral];
                    }
                }
                moveCost = movementCosts[TerrainMoveType.Coast];
            }
            else //second hex is on land so we are disembarking
            {
                if (movementCosts[TerrainMoveType.Disembark] == 0) //we must use all remaining movement to disembark
                {
                    moveCost = (costSoFar % unitMovementSpeed == 0) ? unitMovementSpeed : costSoFar % unitMovementSpeed;
                }
                else //otherwise treat it like a normal land move
                {
                    moveCost = movementCosts[(TerrainMoveType)secondHex.terrainType];
                    foreach (FeatureType feature in secondHex.featureSet)
                    {
                        if (feature == FeatureType.Road)
                        {
                            moveCost = movementCosts[TerrainMoveType.Road];
                            break;
                        }
                        if (feature == FeatureType.River && movementCosts[TerrainMoveType.River] == 0) //if river apply river penalty
                        {
                            moveCost = (costSoFar % unitMovementSpeed == 0) ? unitMovementSpeed : costSoFar % unitMovementSpeed;
                        }
                        if (feature == FeatureType.Forest) //if there is a forest add movement penalty
                        {
                            moveCost += movementCosts[TerrainMoveType.Forest];
                        }
                    }
                }
            }
        }
        else //first hex is on land
        {
            if (secondHex.terrainType == TerrainType.Coast || secondHex.terrainType == TerrainType.Ocean) //second hex is on water
            {
                //embark costs all remaining movement and requires at least 1 so costSoFar % unitMovementSpeed = cost or if == 0 then = unitMovementSpeed
                if (movementCosts[TerrainMoveType.Embark] == 0)
                {
                    moveCost = (costSoFar % unitMovementSpeed == 0) ? unitMovementSpeed : costSoFar % unitMovementSpeed;
                }
                else//if we have a non-0 embark speed work like normal water
                {
                    moveCost = movementCosts[(TerrainMoveType)secondHex.terrainType];
                    foreach (FeatureType feature in secondHex.featureSet)
                    {
                        if (feature == FeatureType.Coral)
                        {
                            moveCost += movementCosts[TerrainMoveType.Coral];
                        }
                    }
                    moveCost = movementCosts[TerrainMoveType.Coast];
                }
            }
            else //second hex is on land
            {
                moveCost = movementCosts[(TerrainMoveType)secondHex.terrainType];
                foreach (FeatureType feature in secondHex.featureSet)
                {
                    if (feature == FeatureType.Road)
                    {
                        moveCost = movementCosts[TerrainMoveType.Road];
                        break;
                    }
                    if (feature == FeatureType.River && movementCosts[TerrainMoveType.River] == 0) //if river apply river penalty
                    {
                        moveCost = (costSoFar % unitMovementSpeed == 0) ? unitMovementSpeed : costSoFar % unitMovementSpeed;
                    }
                    if (feature == FeatureType.Forest) //if there is a forest add movement penalty
                    {
                        moveCost += movementCosts[TerrainMoveType.Forest];
                    }
                }
            }
        }
        foreach (Unit unit in secondHex.unitsList)
        {
            moveCost += 555555;
            break;
            // if (unit.teamNum != this.teamNum | stacking) //add back if we allow units of the same team to stack or for war movement attack
            // {
            //     break;
            // }
        }
        return moveCost;
    }



    private int AstarHeuristic(Hex start, Hex end)
    {
        return start.WrapDistance(end);
    }

    public List<Hex> PathFind(Hex start, Hex end, Dictionary<TerrainMoveType, float> movementCosts, float unitMovementSpeed)
    {
        PriorityQueue<Hex, float> frontier = new();
        frontier.Enqueue(start, 0);
        Dictionary<Hex, float> cost_so_far = new();
        Dictionary<Hex, Hex> came_from = new();
        came_from[start] = start;
        cost_so_far[start] = 0;
    
        while (frontier.TryDequeue(out Hex current, out float priority))
        {
            if (current.Equals(end))
            {
                List<Hex> path = new List<Hex>();
                while (!current.Equals(start))
                {
                    path.Add(current);
                    current = came_from[current];
                }
                path.Add(start);
                path.Reverse();
                return path;
            }
    
            foreach (Hex next in current.WrappingNeighbors())
            {
                float new_cost = cost_so_far[current] + TravelCost(current, next, movementCosts, unitMovementSpeed, cost_so_far[current]);
                //if cost_so_far doesn't have next as a key yet or the new cost is lower than the lowest cost of this node previously
                if (!cost_so_far.ContainsKey(next) || new_cost < cost_so_far[next])
                {
                    cost_so_far[next] = new_cost;
                    float new_priority = new_cost + AstarHeuristic(end, next);
                    frontier.Enqueue(next, new_priority);
                    came_from[next] = current;
                }
            }
        }

        //if the end is unreachable return an empty path
        return new List<Hex>();
    }


struct UnitTests
{   
    static public void TestSimpleMountainPathFinding(bool printGameBoard)
    {
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
                    gameHexDict.Add(new Hex(q, r, -q-r), new GameHex(new Hex(q, r, -q-r), TerrainType.Mountain, TerrainTemperature.Grassland, new HashSet<FeatureType>()));
                }
                else
                {
                    gameHexDict.Add(new Hex(q, r, -q-r), new GameHex(new Hex(q, r, -q-r), TerrainType.Flat, TerrainTemperature.Grassland, new HashSet<FeatureType>()));
                }
            }
        }
        GameBoard mainBoard = new GameBoard(gameHexDict, top, bottom, left, right);
        Dictionary<TerrainMoveType,float> scoutMovementCosts = new Dictionary<TerrainMoveType, float>{
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
        float scoutMovementSpeed = 2.0f;

        Hex start = new Hex(1, 1, -2);
        Hex end = new Hex(12, 6, -18);
        List<Hex> path = mainBoard.PathFind(start, end, scoutMovementCosts, scoutMovementSpeed);
        Hex cur = end;
        if (printGameBoard)
        {
            mainBoard.PrintGameBoard();
        }
        Tests.EqualHex("TestMountainPathFinding", path[17], new Hex(12, 6, -18));
        Tests.EqualHex("TestMountainPathFinding", path[16], new Hex(11, 6, -17));
        Tests.EqualHex("TestMountainPathFinding", path[15], new Hex(10, 6, -16));
        Tests.EqualHex("TestMountainPathFinding", path[14], new Hex(9, 6, -15));
        Tests.EqualHex("TestMountainPathFinding", path[13], new Hex(8, 6, -14));
        Tests.EqualHex("TestMountainPathFinding", path[12], new Hex(7, 6, -13));
        Tests.EqualHex("TestMountainPathFinding", path[11], new Hex(6, 6, -12));
        Tests.EqualHex("TestMountainPathFinding", path[10], new Hex(5, 6, -11));
        Tests.EqualHex("TestMountainPathFinding", path[9], new Hex(4, 6, -10));
        Tests.EqualHex("TestMountainPathFinding", path[8], new Hex(3, 6, -9));
        Tests.EqualHex("TestMountainPathFinding", path[7], new Hex(2, 6, -8));
        Tests.EqualHex("TestMountainPathFinding", path[6], new Hex(1, 6, -7));
        Tests.EqualHex("TestMountainPathFinding", path[5], new Hex(0, 6, -6));
        Tests.EqualHex("TestMountainPathFinding", path[4], new Hex(0, 5, -5));
        Tests.EqualHex("TestMountainPathFinding", path[3], new Hex(1, 4, -5));
        Tests.EqualHex("TestMountainPathFinding", path[2], new Hex(1, 3, -4));
        Tests.EqualHex("TestMountainPathFinding", path[1], new Hex(1, 2, -3));
        Tests.EqualHex("TestMountainPathFinding", path[0], new Hex(1, 1, -2));
    }
    
    static public void TestNeutralUnitObstaclePathFinding(bool printGameBoard)
    {
        int top = 0;
        int bottom = 10;
        int left = 0;
        int right = 30;
        String name = "TestNeutralUnitObstaclePathFinding";
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
        Dictionary<TerrainMoveType,float> scoutMovementCosts = new Dictionary<TerrainMoveType, float>{
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
        float scoutMovementSpeed = 2.0f;
        
        Unit testUnit = new Unit("testUnit", movementCosts, null);
        Unit testUnit2 = new Unit("testUnit2", movementCosts, null);
        //Unit testUnit3 = new Unit("testUnit2", movementCosts, null);
        Unit testUnit4 = new Unit("testUnit2", movementCosts, null);
        Unit testUnit5 = new Unit("testUnit2", movementCosts, null);
        Unit testUnit6 = new Unit("testUnit2", movementCosts, null);
        Unit testUnit7 = new Unit("testUnit2", movementCosts, null);
        Hex target = new Hex(1, 1, -2);
        Hex target2 = new Hex(2, 0, -2);
        //Hex target3 = new Hex(1, 0, -1);
        Hex target4 = new Hex(0, 1, -1);
        Hex target5= new Hex(0, 2, -2);
        Hex target6 = new Hex(1, 2, -3);
        Hex target7= new Hex(2, 1, -3);
        mainBoard.gameHexDict[target].SpawnUnit(testUnit, true, true)
        mainBoard.gameHexDict[target2].SpawnUnit(testUnit2, true, true)
        mainBoard.gameHexDict[target4].SpawnUnit(testUnit4, true, true)
        mainBoard.gameHexDict[target5].SpawnUnit(testUnit5, true, true)
        mainBoard.gameHexDict[target6].SpawnUnit(testUnit6, true, true)
        mainBoard.gameHexDict[target7].SpawnUnit(testUnit7, true, true)

        Hex start = new Hex(1, 1, -2);
        Hex end = new Hex(3, 1, -4);
        List<Hex> path = mainBoard.PathFind(start, end, scoutMovementCosts, scoutMovementSpeed);
        Hex cur = end;
        if (printGameBoard)
        {
            mainBoard.PrintGameBoard();
        }
        Tests.EqualHex(name, path[3], new Hex(3, 1, -4));
        Tests.EqualHex(name, path[2], new Hex(2, 0, -2));
        Tests.EqualHex(name, path[1], new Hex(1, 0, -1));
        Tests.EqualHex(name, path[0], new Hex(1, 1, -2));
        
    }

    static public void TestSimpleRoughPathFinding(bool printGameBoard)
    {
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
                    gameHexDict.Add(new Hex(q, r, -q-r), new GameHex(new Hex(q, r, -q-r), TerrainType.Rough, TerrainTemperature.Grassland, new HashSet<FeatureType>()));
                }
                else
                {
                    gameHexDict.Add(new Hex(q, r, -q-r), new GameHex(new Hex(q, r, -q-r), TerrainType.Flat, TerrainTemperature.Grassland, new HashSet<FeatureType>()));
                }
            }
        }
        GameBoard mainBoard = new GameBoard(gameHexDict, top, bottom, left, right);
        if (printGameBoard)
        {
            mainBoard.PrintGameBoard();
        }
        Dictionary<TerrainMoveType,float> scoutMovementCosts = new Dictionary<TerrainMoveType, float>{
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
        float scoutMovementSpeed = 2.0f;

        Hex start = new Hex(2, 4, -6);
        Hex end = new Hex(1, 6, -7);
        List<Hex> path = mainBoard.PathFind(start, end, scoutMovementCosts, scoutMovementSpeed);
        Tests.EqualHex("TestRoughPathFinding", path[2], new Hex(1, 6, -7));
        Tests.EqualHex("TestRoughPathFinding", path[1], new Hex(2, 5, -7));
        Tests.EqualHex("TestRoughPathFinding", path[0], new Hex(2, 4, -6));
    }

    static public void TestSimpleUnitMovement(bool printGameBoard)
    {
        int top = 0;
        int bottom = 10;
        int left = 0;
        int right = 30;
        String name = "";
        Dictionary<Hex, GameHex> gameHexDict = new();
        for (int r = top; r <= bottom; r++){
            int r_offset = r>>1; //same as (int)Math.Floor(r/2.0f)
            for (int q = left - r_offset; q <= right - r_offset; q++){
                if(r==0 || r == bottom || q == left - r_offset || q == right - r_offset || (r == bottom/2 && q > left - r_offset + 2 && q < right - r_offset - 2))
                {
                    gameHexDict.Add(new Hex(q, r, -q-r), new GameHex(new Hex(q, r, -q-r), TerrainType.Rough, TerrainTemperature.Grassland, new HashSet<FeatureType>()));
                }
                else
                {
                    gameHexDict.Add(new Hex(q, r, -q-r), new GameHex(new Hex(q, r, -q-r), TerrainType.Flat, TerrainTemperature.Grassland, new HashSet<FeatureType>()));
                }
            }
        }
        GameBoard mainBoard = new GameBoard(gameHexDict, top, bottom, left, right);
        if (printGameBoard)
        {
            mainBoard.PrintGameBoard();
        }
        Dictionary<TerrainMoveType,float> scoutMovementCosts = new Dictionary<TerrainMoveType, float>{
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
        float scoutMovementSpeed = 2.0f;

        Hex start = new Hex(2, 4, -6);
        Hex end = new Hex(1, 6, -7);
        List<Hex> path = mainBoard.PathFind(start, end, scoutMovementCosts, scoutMovementSpeed);
        Tests.EqualHex(name, path[2], new Hex(1, 6, -7));
        Tests.EqualHex(name, path[1], new Hex(2, 5, -7));
        Tests.EqualHex(name, path[0], new Hex(2, 4, -6));
        
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
        Unit testUnit = new Unit("testUnit", movementCosts, null);
        if (!mainBoard.gameHexDict[start].SpawnUnit(testUnit, true, true))
        {
            Tests.Complain(name);
        }
        if(testUnit.MoveTowards(end))
        {
            Tests.Complain(name);
        }
        testUnit.OnTurnEnded();
        testUnit.OnTurnStarted();
        if(!testUnit.MoveTowards(end))
        {
            Tests.Complain(name);
        }
        Tests.EqualHex(name, testUnit., new Hex(1, 6, -7))
    }

    static public void TestSimpleEmbarkDisembarkPathFinding(bool printGameBoard)
    {
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
                    gameHexDict.Add(new Hex(q, r, -q-r), new GameHex(new Hex(q, r, -q-r), TerrainType.Coast, TerrainTemperature.Grassland, new HashSet<FeatureType>()));
                }
                else
                {
                    gameHexDict.Add(new Hex(q, r, -q-r), new GameHex(new Hex(q, r, -q-r), TerrainType.Flat, TerrainTemperature.Grassland, new HashSet<FeatureType>()));
                }
            }
        }
        GameBoard mainBoard = new GameBoard(gameHexDict, top, bottom, left, right);
        if (printGameBoard)
        {
            mainBoard.PrintGameBoard();
        }
        Dictionary<TerrainMoveType,float> scoutMovementCosts = new Dictionary<TerrainMoveType, float>{
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
        float scoutMovementSpeed = 2.0f;

        Hex start = new Hex(2, 4, -6);
        Hex end = new Hex(4, 6, -10);
        List<Hex> path = mainBoard.PathFind(start, end, scoutMovementCosts, scoutMovementSpeed);
        Hex cur = end;

        Tests.EqualHex("TestSimpleEmbarkDisembarkPathFinding", path[4], new Hex(4, 6, -10));
        Tests.EqualHex("TestSimpleEmbarkDisembarkPathFinding", path[3], new Hex(4, 5, -9));
        Tests.EqualHex("TestSimpleEmbarkDisembarkPathFinding", path[2], new Hex(3, 5, -8));
        Tests.EqualHex("TestSimpleEmbarkDisembarkPathFinding", path[1], new Hex(3, 4, -7));
        Tests.EqualHex("TestSimpleEmbarkDisembarkPathFinding", path[0], new Hex(2, 4, -6));
    }

    static public void TestSimpleRoadPathFinding(bool printGameBoard)
    {
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
                    gameHexDict.Add(new Hex(q, r, -q-r), new GameHex(new Hex(q, r, -q-r), TerrainType.Flat, TerrainTemperature.Grassland, new HashSet<FeatureType>(){FeatureType.Road}));
                }
                else
                {
                    gameHexDict.Add(new Hex(q, r, -q-r), new GameHex(new Hex(q, r, -q-r), TerrainType.Flat, TerrainTemperature.Grassland, new HashSet<FeatureType>()));
                }
            }
        }
        GameBoard mainBoard = new GameBoard(gameHexDict, top, bottom, left, right);
        if (printGameBoard)
        {
            mainBoard.PrintGameBoard();
        }
        Dictionary<TerrainMoveType,float> scoutMovementCosts = new Dictionary<TerrainMoveType, float>{
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
        float scoutMovementSpeed = 2.0f;

        Hex start = new Hex(2, 4, -6);
        Hex end = new Hex(4, 6, -10);
        List<Hex> path = mainBoard.PathFind(start, end, scoutMovementCosts, scoutMovementSpeed);
        Hex cur = end;
       
        //print start node for testing
        while (!path[cur].Equals(new Hex(-1, -1, 2)))
        {
            Console.WriteLine(cur.q + ", " + cur.r);
            cur = path[cur];
        }
        Console.WriteLine(cur.q + ", " + cur.r);
        cur = end;

        Tests.EqualHex("TestSimpleEmbarkDisembarkPathFinding", path[4], new Hex(4, 6, -10));
        Tests.EqualHex("TestSimpleEmbarkDisembarkPathFinding", path[3], new Hex(4, 5, -9));
        Tests.EqualHex("TestSimpleEmbarkDisembarkPathFinding", path[2], new Hex(3, 5, -8));
        Tests.EqualHex("TestSimpleEmbarkDisembarkPathFinding", path[1], new Hex(3, 4, -7));
        Tests.EqualHex("TestSimpleEmbarkDisembarkPathFinding", path[0], new Hex(2, 4, -6));
    }



    // static public void TestLineofSightFlat(bool printGameBoard)
    // {
    //     String name = "TestLineofSightFlat";
    //     int top = 0;
    //     int bottom = 10;
    //     int left = 0;
    //     int right = 30;
    //     Dictionary<Hex, GameHex> gameHexDict = new();
    //     for (int r = top; r <= bottom; r++){
    //         int r_offset = r>>1; //same as (int)Math.Floor(r/2.0f)
    //         for (int q = left - r_offset; q <= right - r_offset; q++){
    //             if(r==0 || r == bottom || q == left - r_offset || q == right - r_offset || (r == bottom/2 && q > left - r_offset + 2 && q < right - r_offset - 2))
    //             {
    //                 gameHexDict.Add(new Hex(q, r, -q-r), new GameHex(new Hex(q, r, -q-r), TerrainType.Flat, TerrainTemperature.Grassland, new HashSet<FeatureType>(){}));
    //             }
    //             else
    //             {
    //                 gameHexDict.Add(new Hex(q, r, -q-r), new GameHex(new Hex(q, r, -q-r), TerrainType.Flat, TerrainTemperature.Grassland, new HashSet<FeatureType>()));
    //             }
    //         }
    //     }
    //     GameBoard mainBoard = new GameBoard(gameHexDict, top, bottom, left, right);
    //     if (printGameBoard)
    //     {
    //         mainBoard.PrintGameBoard();
    //     }
        
    //     float scoutSightRange = 3.0f;

    //     Hex start = new Hex(2, 4, -6);
    //     Hex[] visible = {new Hex(2, 4, -6), }
    //     //Hex end = new Hex(4, 6, -10);
    //     //Dictionary<Hex, Hex> path = mainBoard.PathFind(start, end, scoutMovementCosts, scoutMovementSpeed);
    //     //Hex cur = end;
       
    //     foreach (Hex visibleHex in visible)
    //     {
    //         if(!visibleHexDictionary.contains(visibleHex))
    //         {
    //             Tests.Complain(name);
    //         }
    //     }
    //     Tests.EqualHex("TestSimpleEmbarkDisembarkPathFinding", cur, new Hex(4, 6, -10));
    //     cur = path[cur];
    //     Tests.EqualHex("TestSimpleEmbarkDisembarkPathFinding", cur, new Hex(4, 5, -9));
    //     cur = path[cur];
    //     Tests.EqualHex("TestSimpleEmbarkDisembarkPathFinding", cur, new Hex(3, 5, -8));
    //     cur = path[cur];
    //     Tests.EqualHex("TestSimpleEmbarkDisembarkPathFinding", cur, new Hex(3, 4, -7));
    //     cur = path[cur];
    //     Tests.EqualHex("TestSimpleEmbarkDisembarkPathFinding", cur, new Hex(2, 4, -6));
    // }
    
    static public void TestAll()
    {
        GameBoardTests.TestSimpleMountainPathFinding(false);
        GameBoardTests.TestSimpleRoughPathFinding(false);
        GameBoardTests.TestSimpleEmbarkDisembarkPathFinding(false);
        GameBoardTests.TestSimpleRoadPathFinding(true);
    }

}
