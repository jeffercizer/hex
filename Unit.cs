using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;

struct Unit
{
    public Unit(String name, Dictionary<TerrainMoveType, float> movementCosts, GameHex currentGameHex)
    {
        this.name = name;
        this.movementCosts = movementCosts;
        this.currentGameHex = currentGameHex;
    }

    public String name;
    public Dictionary<TerrainMoveType, float> movementCosts;
    public GameHex currentGameHex;

    public bool SetGameHex(GameHex newGameHex)
    {
        currentGameHex = newGameHex;
        return true;
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
        return moveCost;
    }



    private int AstarHeuristic(Hex start, Hex end)
    {
        return start.WrapDistance(end);
    }

    public Dictionary<Hex, Hex> PathFind(Hex start, Hex end, Dictionary<TerrainMoveType, float> movementCosts, float unitMovementSpeed)
    {
        PriorityQueue<Hex, float> frontier = new();
        frontier.Enqueue(start, 0);
        Dictionary<Hex, Hex> came_from = new();
        Dictionary<Hex, float> cost_so_far = new();
        came_from[start] = new Hex(-1, -1, 2);
        cost_so_far[start] = 0;

        Hex current;
        float priority;
        while (frontier.TryDequeue(out current, out priority))
        {
            if (current.Equals(end))
            {
                break;
            }
            foreach (Hex next in current.WrappingNeighbors())
            {
                float new_cost = cost_so_far[current] + TravelCost(current, next, movementCosts, unitMovementSpeed, cost_so_far[current]);
                //if cost_so_far doesn't have next as a key yet or the new cost is lower than the lowest cost of this node previously
                if (!cost_so_far.Keys.Contains(next) || new_cost < cost_so_far[next]) 
                {
                    cost_so_far[next] = new_cost;
                    priority = new_cost + AstarHeuristic(end, next);
                    frontier.Enqueue(next, priority);
                    came_from[next] = current;
                    //Console.Write("|"+next.q + "," + next.r + ", " + priority+"|");
                }
            }
        }
        return came_from;
    }

    static public void Main()
    {
        Tests.TestAll();
    }
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
        Dictionary<Hex, Hex> path = mainBoard.PathFind(start, end, scoutMovementCosts, scoutMovementSpeed);
        Hex cur = end;
        if (printGameBoard)
        {
            mainBoard.PrintGameBoard();
        }
        Tests.EqualHex("TestMountainPathFinding", cur, new Hex(12, 6, -18));
        cur = path[cur];
        Tests.EqualHex("TestMountainPathFinding", cur, new Hex(11, 6, -17));
        cur = path[cur];
        Tests.EqualHex("TestMountainPathFinding", cur, new Hex(10, 6, -16));
        cur = path[cur];
        Tests.EqualHex("TestMountainPathFinding", cur, new Hex(9, 6, -15));
        cur = path[cur];
        Tests.EqualHex("TestMountainPathFinding", cur, new Hex(8, 6, -14));
        cur = path[cur];
        Tests.EqualHex("TestMountainPathFinding", cur, new Hex(7, 6, -13));
        cur = path[cur];
        Tests.EqualHex("TestMountainPathFinding", cur, new Hex(6, 6, -12));
        cur = path[cur];
        Tests.EqualHex("TestMountainPathFinding", cur, new Hex(5, 6, -11));
        cur = path[cur];
        Tests.EqualHex("TestMountainPathFinding", cur, new Hex(4, 6, -10));
        cur = path[cur];
        Tests.EqualHex("TestMountainPathFinding", cur, new Hex(3, 6, -9));
        cur = path[cur];
        Tests.EqualHex("TestMountainPathFinding", cur, new Hex(2, 6, -8));
        cur = path[cur];
        Tests.EqualHex("TestMountainPathFinding", cur, new Hex(1, 6, -7));
        cur = path[cur];
        Tests.EqualHex("TestMountainPathFinding", cur, new Hex(0, 6, -6));
        cur = path[cur];
        Tests.EqualHex("TestMountainPathFinding", cur, new Hex(0, 5, -5));
        cur = path[cur];
        Tests.EqualHex("TestMountainPathFinding", cur, new Hex(1, 4, -5));
        cur = path[cur];
        Tests.EqualHex("TestMountainPathFinding", cur, new Hex(1, 3, -4));
        cur = path[cur];
        Tests.EqualHex("TestMountainPathFinding", cur, new Hex(1, 2, -3));
        cur = path[cur];
        Tests.EqualHex("TestMountainPathFinding", cur, new Hex(1, 1, -2));
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
        Dictionary<Hex, Hex> path = mainBoard.PathFind(start, end, scoutMovementCosts, scoutMovementSpeed);
        Hex cur = end;
        Tests.EqualHex("TestRoughPathFinding", cur, new Hex(1, 6, -7));
        cur = path[cur];
        Tests.EqualHex("TestRoughPathFinding", cur, new Hex(2, 5, -7));
        cur = path[cur];
        Tests.EqualHex("TestRoughPathFinding", cur, new Hex(2, 4, -6));
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
        Dictionary<Hex, Hex> path = mainBoard.PathFind(start, end, scoutMovementCosts, scoutMovementSpeed);
        Hex cur = end;

        Tests.EqualHex("TestSimpleEmbarkDisembarkPathFinding", cur, new Hex(4, 6, -10));
        cur = path[cur];
        Tests.EqualHex("TestSimpleEmbarkDisembarkPathFinding", cur, new Hex(4, 5, -9));
        cur = path[cur];
        Tests.EqualHex("TestSimpleEmbarkDisembarkPathFinding", cur, new Hex(3, 5, -8));
        cur = path[cur];
        Tests.EqualHex("TestSimpleEmbarkDisembarkPathFinding", cur, new Hex(3, 4, -7));
        cur = path[cur];
        Tests.EqualHex("TestSimpleEmbarkDisembarkPathFinding", cur, new Hex(2, 4, -6));
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
        Dictionary<Hex, Hex> path = mainBoard.PathFind(start, end, scoutMovementCosts, scoutMovementSpeed);
        Hex cur = end;
       
        //print start node for testing
        while (!path[cur].Equals(new Hex(-1, -1, 2)))
        {
            Console.WriteLine(cur.q + ", " + cur.r);
            cur = path[cur];
        }
        Console.WriteLine(cur.q + ", " + cur.r);
        cur = end;

        Tests.EqualHex("TestSimpleEmbarkDisembarkPathFinding", cur, new Hex(4, 6, -10));
        cur = path[cur];
        Tests.EqualHex("TestSimpleEmbarkDisembarkPathFinding", cur, new Hex(4, 5, -9));
        cur = path[cur];
        Tests.EqualHex("TestSimpleEmbarkDisembarkPathFinding", cur, new Hex(3, 5, -8));
        cur = path[cur];
        Tests.EqualHex("TestSimpleEmbarkDisembarkPathFinding", cur, new Hex(3, 4, -7));
        cur = path[cur];
        Tests.EqualHex("TestSimpleEmbarkDisembarkPathFinding", cur, new Hex(2, 4, -6));
    }

    static public void TestLineofSightFlat(bool printGameBoard)
    {
        String name = "TestLineofSightFlat";
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
                    gameHexDict.Add(new Hex(q, r, -q-r), new GameHex(new Hex(q, r, -q-r), TerrainType.Flat, TerrainTemperature.Grassland, new HashSet<FeatureType>(){}));
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
        
        float scoutSightRange = 3.0f;

        Hex start = new Hex(2, 4, -6);
        Hex[] visible = {new Hex(2, 4, -6), }
        //Hex end = new Hex(4, 6, -10);
        //Dictionary<Hex, Hex> path = mainBoard.PathFind(start, end, scoutMovementCosts, scoutMovementSpeed);
        //Hex cur = end;
       
        foreach (Hex visibleHex in visible)
        {
            if(!visibleHexDictionary.contains(visibleHex))
            {
                Tests.Complain(name);
            }
        }
        Tests.EqualHex("TestSimpleEmbarkDisembarkPathFinding", cur, new Hex(4, 6, -10));
        cur = path[cur];
        Tests.EqualHex("TestSimpleEmbarkDisembarkPathFinding", cur, new Hex(4, 5, -9));
        cur = path[cur];
        Tests.EqualHex("TestSimpleEmbarkDisembarkPathFinding", cur, new Hex(3, 5, -8));
        cur = path[cur];
        Tests.EqualHex("TestSimpleEmbarkDisembarkPathFinding", cur, new Hex(3, 4, -7));
        cur = path[cur];
        Tests.EqualHex("TestSimpleEmbarkDisembarkPathFinding", cur, new Hex(2, 4, -6));
    }
    static public void TestAll()
    {
        GameBoardTests.TestSimpleMountainPathFinding(false);
        GameBoardTests.TestSimpleRoughPathFinding(false);
        GameBoardTests.TestSimpleEmbarkDisembarkPathFinding(false);
        GameBoardTests.TestSimpleRoadPathFinding(true);
    }

}
