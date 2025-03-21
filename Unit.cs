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

[Serializable]
public class Unit
{
    public Unit(String name, Dictionary<TerrainMoveType, float> movementCosts, GameHex currentGameHex)
    {
        this.name = name;
        this.movementCosts = movementCosts;
        this.currentGameHex = currentGameHex;
    }

    public Unit(String name, Dictionary<TerrainMoveType, float> movementCosts, Dictionary<TerrainMoveType, float> sightCosts, GameHex currentGameHex, float sightRange, float movementSpeed, float combatStrength, int teamNum)
    {
        this.name = name;
        this.baseMovementCosts = movementCosts;
        this.movementCosts = movementCosts;
        this.baseSightCosts = sightCosts;
        this.sightCosts = sightCosts;
        this.currentGameHex = currentGameHex;
        this.baseSightRange = sightRange;
        this.sightRange = sightRange;
        this.baseMovementSpeed = movementSpeed;
        this.movementSpeed = movementSpeed;
        this.teamNum = teamNum;
        this.baseCombatStrength = combatStrength;
        this.combatStrength = combatStrength;
    }

    public String name;
    public Dictionary<TerrainMoveType, float> baseMovementCosts;
    public Dictionary<TerrainMoveType, float> movementCosts;
    public Dictionary<TerrainMoveType, float> baseSightCosts;
    public Dictionary<TerrainMoveType, float> sightCosts;
    public GameHex currentGameHex;
    public float baseMovementSpeed = 2.0f;
    public float movementSpeed = 2.0f;
    public float remainingMovement = 2.0f;
    public float baseSightRange = 3.0f;
    public float sightRange = 3.0f;
    public float currentHealth = 100.0f;
    public float baseCombatStrength = 10.0f;
    public float combatStrength = 10.0f;
    public int teamNum = 1;
    public List<Hex>? currentPath;
    public List<Hex> ourVisibleHexes;
    public List<Effect> ourEffects;
    public bool isTargetEnemy;

    public void OnTurnStarted(int turnNumber)
    {
        remainingMovement = movementSpeed;
        Console.WriteLine($"Unit ({name}): Started turn {turnNumber}.");
    }

    public void OnTurnEnded(int turnNumber)
    {
        if(remainingMovement > 0.0f & currentPath.Any())
        {
            MoveTowards(currentGameHex.ourGameBoard.gameHexDict[currentPath.Last()], currentGameHex.ourGameBoard.game.teamManager ,isTargetEnemy);
        }
        Console.WriteLine($"Unit ({name}): Ended turn {turnNumber}.");
    }
    
    public void RecalculateEffects()
    {
        //must reset all to base and recalculate
        movementCosts = baseMovementCosts;
        sightCosts = baseSightCosts;
        movementSpeed = baseMovementSpeed;
        sightRange = baseSightRange;
        combatStrength = baseCombatStrength;
        //also order all effects, multiply/divide after add/subtract priority
        //0 means it is applied first 100 means it is applied "last" (highest number last)
        //so multiply/divide effects should be 20 and add/subtract will be 10 to give wiggle room
        PriorityQueue<Effect, int> orderedEffects = new();
        foreach(Effect effect1 in ourEffects)
        {
            orderedEffects.Enqueue(effect1, effect1.priority);
        }
        Effect effect;
        int priority;
        while(orderedEffects.TryDequeue(out effect, out priority))
        {
            effect.ApplyEffect(this);
        }
    }

    public void AddEffect(Effect effect)
    {
        ourEffects.Add(effect);
        RecalculateEffects();
    }

    public void RemoveEffect(Effect effect)
    {
        ourEffects.Remove(effect);
        RecalculateEffects();
    }

    public bool AttackTarget(GameHex targetGameHex, TeamManager teamManager)
    {
        if (targetGameHex.unitsList.Any())
        {
            foreach (Unit unit in targetGameHex.unitsList)
            {
                if (teamManager.GetEnemies(teamNum).Contains(unit.teamNum))
                {
                    //combat math TODO
                    //if we didn't die and the enemy has died we can move in otherwise atleast one of us should poof
                    return !decreaseCurrentHealth(25.0f) & unit.decreaseCurrentHealth(25.0f);
                }
            }
            return false;
        }
        else
        {
            return true;
        }
    }
    
    public void increaseCurrentHealth(float amount)
    {
        currentHealth += amount;
    }

    public bool decreaseCurrentHealth(float amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0.0f)
        {
            onDeathEffects();
            return true;
        }
        else
        {
            return false;
        }
    }

    public void onDeathEffects()
    {
        currentGameHex.unitsList.Remove(this);
        RemoveVision();
    }

    public void UpdateVision()
    {
        RemoveVision();
        ourVisibleHexes = CalculateVision().Keys.ToList();
        foreach (Hex hex in ourVisibleHexes)
        {
            currentGameHex.ourGameBoard.game.playerDictionary[teamNum].seenGameHexDict.Add(hex, true); //add to the seen dict no matter what since duplicates are thrown out
            int count;
            if(currentGameHex.ourGameBoard.game.playerDictionary[teamNum].visibleGameHexDict.TryGetValue(hex, out count))
            {
                currentGameHex.ourGameBoard.game.playerDictionary[teamNum].visibleGameHexDict[hex] = count + 1;
            }
            else
            {
                currentGameHex.ourGameBoard.game.playerDictionary[teamNum].visibleGameHexDict.Add(hex, 1);
            }
        }
    }

    public void RemoveVision()
    {
        foreach (Hex hex in ourVisibleHexes)
        {            
            int count;
            if(currentGameHex.ourGameBoard.game.playerDictionary[teamNum].visibleGameHexDict.TryGetValue(hex, out count))
            {
                if(count <= 1)
                {
                    currentGameHex.ourGameBoard.game.playerDictionary[teamNum].visibleGameHexDict.Remove(hex);
                }
                else
                {
                    currentGameHex.ourGameBoard.game.playerDictionary[teamNum].visibleGameHexDict[hex] = count - 1;
                }
            }
        }
        ourVisibleHexes.Clear();
    }

    public Dictionary<Hex, float> CalculateVision()
    {
        Queue<Hex> frontier = new();
        frontier.Enqueue(currentGameHex.hex);
        Dictionary<Hex, float> reached = new();
        reached.Add(currentGameHex.hex, 0.0f);

        while (frontier.Count > 0)
        {
            Hex current = frontier.Dequeue();

            foreach (Hex next in currentGameHex.hex.WrappingNeighbors(currentGameHex.ourGameBoard.left, currentGameHex.ourGameBoard.right))
            {
                float sightLeft = sightRange - reached[current];
                float visionCost = VisionCost(currentGameHex.ourGameBoard.gameHexDict[next], sightLeft); //vision cost is at most the cost of our remaining sight if we have atleast 1
                if (visionCost <= sightLeft)
                {
                    if (!reached.Keys.Contains(next))
                    {
                        if(reached[current]+visionCost < sightRange)
                        {
                            frontier.Enqueue(next);
                        }
                        reached.Add(next, reached[current]+visionCost);
                    }
                    else if(reached[next] > reached[current]+visionCost)
                    {
                        reached[next] = reached[current]+visionCost;
                    }
                }
            }
        }
        return reached;
    }
    

    public float VisionCost(GameHex targetGameHex, float sightLeft)
    {
        float visionCost = 0.0f;
        float cost = 0.0f;
        foreach (FeatureType feature in targetGameHex.featureSet)
        {
            if (feature == FeatureType.Forest)
            {
                visionCost += sightCosts[TerrainMoveType.Forest];
            }
            if (feature == FeatureType.Coral)
            {
                visionCost += sightCosts[TerrainMoveType.Coral];
            }
        }
        if(sightCosts.TryGetValue((TerrainMoveType)targetGameHex.terrainType, out cost))
        {
            visionCost += cost;
        }
        if(sightLeft >= 1)
        {
            return Math.Min(visionCost, sightLeft);
        }
        else
        {
            return visionCost;
        }
    }

    public void MovementRange()
    {
        //breadth first using movement speed and move costs
    }
    

    public bool SetGameHex(GameHex newGameHex)
    {
        currentGameHex = newGameHex;
        return true;
    }

    public bool TryMoveToGameHex(GameHex targetGameHex, TeamManager teamManager)
    {
        if(targetGameHex.unitsList.Any())
        {
            return false;
        }
        float moveCost = TravelCost(currentGameHex.hex, targetGameHex.hex, teamManager, isTargetEnemy, movementCosts, movementSpeed, movementSpeed-remainingMovement);
        if(moveCost <= remainingMovement)
        {
            if(isTargetEnemy & targetGameHex.Equals(currentPath.Last()))
            {
                if(AttackTarget(targetGameHex, teamManager))
                {
                    remainingMovement -= moveCost;
                    UpdateVision();
                    currentGameHex.unitsList.Remove(this);
                    currentGameHex = targetGameHex;
                    currentGameHex.unitsList.Add(this);
                    return true;
                }
            }
            else
            {
                remainingMovement -= moveCost;
                UpdateVision();
                currentGameHex.unitsList.Remove(this);
                currentGameHex = targetGameHex;
                currentGameHex.unitsList.Add(this);
                return true;
            }
        }
        return false;
    }

    public bool MoveToGameHex(GameHex targetGameHex)
    {
        UpdateVision();
        currentGameHex.unitsList.Remove(this);
        currentGameHex = targetGameHex;
        currentGameHex.unitsList.Add(this);
        return true;
    }

    public bool MoveTowards(GameHex targetGameHex, TeamManager teamManager, bool isTargetEnemy)
    {
        this.isTargetEnemy = isTargetEnemy;
        currentPath = PathFind(currentGameHex.hex, targetGameHex.hex, currentGameHex.ourGameBoard.game.teamManager, movementCosts, movementSpeed);
        currentPath.Remove(currentGameHex.hex);
        while (currentPath.Count > 0)
        {
            GameHex nextHex = currentGameHex.ourGameBoard.gameHexDict[currentPath[0]];
            if (!TryMoveToGameHex(nextHex, teamManager))
            {
                return false;
            }
            currentPath.Remove(nextHex.hex);
        }
        return true;
    }
    
    public float TravelCost(Hex first, Hex second, TeamManager teamManager, bool isTargetEnemy, Dictionary<TerrainMoveType, float> movementCosts, float unitMovementSpeed, float costSoFar)
    {
        //cost for river, embark, disembark are custom (0 = end turn to enter, 1/2/3/4 = normal cost)\\
        GameHex firstHex;
        GameHex secondHex;
        if (!currentGameHex.ourGameBoard.gameHexDict.TryGetValue(first, out firstHex)) //if firstHex is somehow off the table return max
        {
            return 111111;
        }
        if (!currentGameHex.ourGameBoard.gameHexDict.TryGetValue(second, out secondHex)) //if secondHex is off the table return max
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
            if(isTargetEnemy & teamManager.GetEnemies(teamNum).Contains(unit.teamNum))
            {
                break;
            }
            else
            {
                moveCost += 555555;
            }
            // if (unit.teamNum != this.teamNum | stacking) //add back if we allow units of the same team to stack
            // {
            //     break;
            // }
        }
        return moveCost;
    }



    private int AstarHeuristic(Hex start, Hex end)
    {
        return start.WrapDistance(end, currentGameHex.ourGameBoard.right - currentGameHex.ourGameBoard.left);
    }

    public List<Hex> PathFind(Hex start, Hex end, TeamManager teamManager, Dictionary<TerrainMoveType, float> movementCosts, float unitMovementSpeed)
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
                if (cost_so_far[current] > 10000)
                {
                    return new List<Hex>();
                }
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
    
            foreach (Hex next in current.WrappingNeighbors(currentGameHex.ourGameBoard.left, currentGameHex.ourGameBoard.right))
            {
                float new_cost = cost_so_far[current] + TravelCost(current, next, teamManager, isTargetEnemy, movementCosts, unitMovementSpeed, cost_so_far[current]);
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


// struct UnitTests
// {   
//     static public void TestSimpleMountainPathFinding(bool printGameBoard)
//     {
//         int top = 0;
//         int bottom = 10;
//         int left = 0;
//         int right = 30;
//         Dictionary<Hex, GameHex> gameHexDict = new();
//         for (int r = top; r <= bottom; r++){
//             int r_offset = r>>1; //same as (int)Math.Floor(r/2.0f)
//             for (int q = left - r_offset; q <= right - r_offset; q++){
//                 if(r==0 || r == bottom || q == left - r_offset || q == right - r_offset || (r == bottom/2 && q > left - r_offset + 2 && q < right - r_offset - 2))
//                 {
//                     gameHexDict.Add(new Hex(q, r, -q-r), new GameHex(new Hex(q, r, -q-r), TerrainType.Mountain, TerrainTemperature.Grassland, new HashSet<FeatureType>()));
//                 }
//                 else
//                 {
//                     gameHexDict.Add(new Hex(q, r, -q-r), new GameHex(new Hex(q, r, -q-r), TerrainType.Flat, TerrainTemperature.Grassland, new HashSet<FeatureType>()));
//                 }
//             }
//         }
//         GameBoard mainBoard = new GameBoard(gameHexDict, top, bottom, left, right);
//         Dictionary<TerrainMoveType,float> scoutMovementCosts = new Dictionary<TerrainMoveType, float>{
//             { TerrainMoveType.Flat, 1 },
//             { TerrainMoveType.Rough, 2 },
//             { TerrainMoveType.Mountain, 9999 },
//             { TerrainMoveType.Coast, 1 },
//             { TerrainMoveType.Ocean, 1 },
//             { TerrainMoveType.Forest, 1 },
//             { TerrainMoveType.River, 0 },
//             { TerrainMoveType.Road, 0.5f },
//             { TerrainMoveType.Embark, 0 },
//             { TerrainMoveType.Disembark, 0 },
//         };
//         float scoutMovementSpeed = 2.0f;

//         Hex start = new Hex(1, 1, -2);
//         Hex end = new Hex(12, 6, -18);
//         List<Hex> path = mainBoard.PathFind(start, end, scoutMovementCosts, scoutMovementSpeed);
//         Hex cur = end;
//         if (printGameBoard)
//         {
//             mainBoard.PrintGameBoard();
//         }
//         Tests.EqualHex("TestMountainPathFinding", path[17], new Hex(12, 6, -18));
//         Tests.EqualHex("TestMountainPathFinding", path[16], new Hex(11, 6, -17));
//         Tests.EqualHex("TestMountainPathFinding", path[15], new Hex(10, 6, -16));
//         Tests.EqualHex("TestMountainPathFinding", path[14], new Hex(9, 6, -15));
//         Tests.EqualHex("TestMountainPathFinding", path[13], new Hex(8, 6, -14));
//         Tests.EqualHex("TestMountainPathFinding", path[12], new Hex(7, 6, -13));
//         Tests.EqualHex("TestMountainPathFinding", path[11], new Hex(6, 6, -12));
//         Tests.EqualHex("TestMountainPathFinding", path[10], new Hex(5, 6, -11));
//         Tests.EqualHex("TestMountainPathFinding", path[9], new Hex(4, 6, -10));
//         Tests.EqualHex("TestMountainPathFinding", path[8], new Hex(3, 6, -9));
//         Tests.EqualHex("TestMountainPathFinding", path[7], new Hex(2, 6, -8));
//         Tests.EqualHex("TestMountainPathFinding", path[6], new Hex(1, 6, -7));
//         Tests.EqualHex("TestMountainPathFinding", path[5], new Hex(0, 6, -6));
//         Tests.EqualHex("TestMountainPathFinding", path[4], new Hex(0, 5, -5));
//         Tests.EqualHex("TestMountainPathFinding", path[3], new Hex(1, 4, -5));
//         Tests.EqualHex("TestMountainPathFinding", path[2], new Hex(1, 3, -4));
//         Tests.EqualHex("TestMountainPathFinding", path[1], new Hex(1, 2, -3));
//         Tests.EqualHex("TestMountainPathFinding", path[0], new Hex(1, 1, -2));
//     }
    
//     static public void TestNeutralUnitObstaclePathFinding(bool printGameBoard)
//     {
//         int top = 0;
//         int bottom = 10;
//         int left = 0;
//         int right = 30;
//         String name = "TestNeutralUnitObstaclePathFinding";
//         Dictionary<Hex, GameHex> gameHexDict = new();
//         for (int r = top; r <= bottom; r++){
//             int r_offset = r>>1; //same as (int)Math.Floor(r/2.0f)
//             for (int q = left - r_offset; q <= right - r_offset; q++){
//                 if(r==0 || r == bottom || q == left - r_offset || q == right - r_offset || (r == bottom/2 && q > left - r_offset + 2 && q < right - r_offset - 2))
//                 {
//                     gameHexDict.Add(new Hex(q, r, -q-r), new GameHex(new Hex(q, r, -q-r), TerrainType.Flat, TerrainTemperature.Grassland, new HashSet<FeatureType>()));
//                 }
//                 else
//                 {
//                     gameHexDict.Add(new Hex(q, r, -q-r), new GameHex(new Hex(q, r, -q-r), TerrainType.Flat, TerrainTemperature.Grassland, new HashSet<FeatureType>()));
//                 }
//             }
//         }
//         GameBoard mainBoard = new GameBoard(gameHexDict, top, bottom, left, right);
//         Dictionary<TerrainMoveType,float> scoutMovementCosts = new Dictionary<TerrainMoveType, float>{
//             { TerrainMoveType.Flat, 1 },
//             { TerrainMoveType.Rough, 2 },
//             { TerrainMoveType.Mountain, 9999 },
//             { TerrainMoveType.Coast, 1 },
//             { TerrainMoveType.Ocean, 1 },
//             { TerrainMoveType.Forest, 1 },
//             { TerrainMoveType.River, 0 },
//             { TerrainMoveType.Road, 0.5f },
//             { TerrainMoveType.Embark, 0 },
//             { TerrainMoveType.Disembark, 0 },
//         };
//         float scoutMovementSpeed = 2.0f;
        
//         Unit testUnit = new Unit("testUnit", movementCosts, null);
//         Unit testUnit2 = new Unit("testUnit2", movementCosts, null);
//         //Unit testUnit3 = new Unit("testUnit2", movementCosts, null);
//         Unit testUnit4 = new Unit("testUnit2", movementCosts, null);
//         Unit testUnit5 = new Unit("testUnit2", movementCosts, null);
//         Unit testUnit6 = new Unit("testUnit2", movementCosts, null);
//         Unit testUnit7 = new Unit("testUnit2", movementCosts, null);
//         Hex target = new Hex(1, 1, -2);
//         Hex target2 = new Hex(2, 0, -2);
//         //Hex target3 = new Hex(1, 0, -1);
//         Hex target4 = new Hex(0, 1, -1);
//         Hex target5= new Hex(0, 2, -2);
//         Hex target6 = new Hex(1, 2, -3);
//         Hex target7= new Hex(2, 1, -3);
//         mainBoard.gameHexDict[target].SpawnUnit(testUnit, true, true)
//         mainBoard.gameHexDict[target2].SpawnUnit(testUnit2, true, true)
//         mainBoard.gameHexDict[target4].SpawnUnit(testUnit4, true, true)
//         mainBoard.gameHexDict[target5].SpawnUnit(testUnit5, true, true)
//         mainBoard.gameHexDict[target6].SpawnUnit(testUnit6, true, true)
//         mainBoard.gameHexDict[target7].SpawnUnit(testUnit7, true, true)

//         Hex start = new Hex(1, 1, -2);
//         Hex end = new Hex(3, 1, -4);
//         List<Hex> path = mainBoard.PathFind(start, end, scoutMovementCosts, scoutMovementSpeed);
//         Hex cur = end;
//         if (printGameBoard)
//         {
//             mainBoard.PrintGameBoard();
//         }
//         Tests.EqualHex(name, path[3], new Hex(3, 1, -4));
//         Tests.EqualHex(name, path[2], new Hex(2, 0, -2));
//         Tests.EqualHex(name, path[1], new Hex(1, 0, -1));
//         Tests.EqualHex(name, path[0], new Hex(1, 1, -2));
        
//     }

//     static public void TestWrappingAStar(bool printGameBoard)
//     {
//         int top = 0;
//         int bottom = 10;
//         int left = 0;
//         int right = 30;
//         String name = "TestWrappingAStart";
//         Dictionary<Hex, GameHex> gameHexDict = new();
//         for (int r = top; r <= bottom; r++){
//             for (int q = left; q <= right; q++){
//                 if(r==0 || r == bottom || q == left || q == right || (r == bottom/2 && q > left + 2 && q < right - 2))
//                 {
//                     gameHexDict.Add(new Hex(q, r, -q-r), new GameHex(new Hex(q, r, -q-r), TerrainType.Rough, TerrainTemperature.Grassland, new HashSet<FeatureType>()));
//                 }
//                 else
//                 {
//                     gameHexDict.Add(new Hex(q, r, -q-r), new GameHex(new Hex(q, r, -q-r), TerrainType.Flat, TerrainTemperature.Grassland, new HashSet<FeatureType>()));
//                 }
//             }
//         }
//         GameBoard mainBoard = new GameBoard(gameHexDict, top, bottom, left, right);
//         if (printGameBoard)
//         {
//             mainBoard.PrintGameBoard();
//         }
//         Dictionary<TerrainMoveType,float> scoutMovementCosts = new Dictionary<TerrainMoveType, float>{
//             { TerrainMoveType.Flat, 1 },
//             { TerrainMoveType.Rough, 2 },
//             { TerrainMoveType.Mountain, 9999 },
//             { TerrainMoveType.Coast, 1 },
//             { TerrainMoveType.Ocean, 1 },
//             { TerrainMoveType.Forest, 1 },
//             { TerrainMoveType.River, 0 },
//             { TerrainMoveType.Road, 0.5f },
//             { TerrainMoveType.Embark, 0 },
//             { TerrainMoveType.Disembark, 0 },
//         };
//         float scoutMovementSpeed = 2.0f;

//         Hex start = new Hex(2, 2, -4);
//         Hex end = new Hex(28, 2, -30);
//         List<Hex> path = mainBoard.PathFind(start, end, scoutMovementCosts, scoutMovementSpeed);
//         Tests.EqualHex(name, path[2], new Hex(28, 2, -30));
//         Tests.EqualHex(name, path[2], new Hex(29, 2, -31));
//         Tests.EqualHex(name, path[2], new Hex(30, 2, -32));
//         Tests.EqualHex(name, path[2], new Hex(0, 2, -2));
//         Tests.EqualHex(name, path[2], new Hex(1, 2, -3));
//         Tests.EqualHex(name, path[1], new Hex(2, 2, -4));
//     }

//     static public void TestImpossiblePathMountain(bool printGameBoard)
//     {
//         int top = 0;
//         int bottom = 10;
//         int left = 0;
//         int right = 30;
//         String name = "TestImpossiblePathMountain";
//         Dictionary<Hex, GameHex> gameHexDict = new();
//         for (int r = top; r <= bottom; r++){
//             for (int q = left; q <= right; q++){
//                 if(r==0 || r == bottom || q == left || q == right || (r == bottom/2 && q > left + 2 && q < right - 2))
//                 {
//                     gameHexDict.Add(new Hex(q, r, -q-r), new GameHex(new Hex(q, r, -q-r), TerrainType.Mountain, TerrainTemperature.Grassland, new HashSet<FeatureType>()));
//                 }
//                 else
//                 {
//                     gameHexDict.Add(new Hex(q, r, -q-r), new GameHex(new Hex(q, r, -q-r), TerrainType.Mountain, TerrainTemperature.Grassland, new HashSet<FeatureType>()));
//                 }
//             }
//         }
//         GameBoard mainBoard = new GameBoard(gameHexDict, top, bottom, left, right);
//         if (printGameBoard)
//         {
//             mainBoard.PrintGameBoard();
//         }
//         Dictionary<TerrainMoveType,float> scoutMovementCosts = new Dictionary<TerrainMoveType, float>{
//             { TerrainMoveType.Flat, 1 },
//             { TerrainMoveType.Rough, 2 },
//             { TerrainMoveType.Mountain, 9999 },
//             { TerrainMoveType.Coast, 1 },
//             { TerrainMoveType.Ocean, 1 },
//             { TerrainMoveType.Forest, 1 },
//             { TerrainMoveType.River, 0 },
//             { TerrainMoveType.Road, 0.5f },
//             { TerrainMoveType.Embark, 0 },
//             { TerrainMoveType.Disembark, 0 },
//         };
//         float scoutMovementSpeed = 2.0f;

//         Hex start = new Hex(2, 2, -4);
//         Hex end = new Hex(28, 2, -30);
//         List<Hex> path = mainBoard.PathFind(start, end, scoutMovementCosts, scoutMovementSpeed);
//         if(path.Any())
//         {
//             Tests.Complain(name);
//         }
//     }

//     static public void TestSimpleRoughPathFinding(bool printGameBoard)
//     {
//         int top = 0;
//         int bottom = 10;
//         int left = 0;
//         int right = 30;
//         Dictionary<Hex, GameHex> gameHexDict = new();
//         for (int r = top; r <= bottom; r++){
//             int r_offset = r>>1; //same as (int)Math.Floor(r/2.0f)
//             for (int q = left - r_offset; q <= right - r_offset; q++){
//                 if(r==0 || r == bottom || q == left - r_offset || q == right - r_offset || (r == bottom/2 && q > left - r_offset + 2 && q < right - r_offset - 2))
//                 {
//                     gameHexDict.Add(new Hex(q, r, -q-r), new GameHex(new Hex(q, r, -q-r), TerrainType.Rough, TerrainTemperature.Grassland, new HashSet<FeatureType>()));
//                 }
//                 else
//                 {
//                     gameHexDict.Add(new Hex(q, r, -q-r), new GameHex(new Hex(q, r, -q-r), TerrainType.Flat, TerrainTemperature.Grassland, new HashSet<FeatureType>()));
//                 }
//             }
//         }
//         GameBoard mainBoard = new GameBoard(gameHexDict, top, bottom, left, right);
//         if (printGameBoard)
//         {
//             mainBoard.PrintGameBoard();
//         }
//         Dictionary<TerrainMoveType,float> scoutMovementCosts = new Dictionary<TerrainMoveType, float>{
//             { TerrainMoveType.Flat, 1 },
//             { TerrainMoveType.Rough, 2 },
//             { TerrainMoveType.Mountain, 9999 },
//             { TerrainMoveType.Coast, 1 },
//             { TerrainMoveType.Ocean, 1 },
//             { TerrainMoveType.Forest, 1 },
//             { TerrainMoveType.River, 0 },
//             { TerrainMoveType.Road, 0.5f },
//             { TerrainMoveType.Embark, 0 },
//             { TerrainMoveType.Disembark, 0 },
//         };
//         float scoutMovementSpeed = 2.0f;

//         Hex start = new Hex(2, 4, -6);
//         Hex end = new Hex(1, 6, -7);
//         List<Hex> path = mainBoard.PathFind(start, end, scoutMovementCosts, scoutMovementSpeed);
//         Tests.EqualHex("TestRoughPathFinding", path[2], new Hex(1, 6, -7));
//         Tests.EqualHex("TestRoughPathFinding", path[1], new Hex(2, 5, -7));
//         Tests.EqualHex("TestRoughPathFinding", path[0], new Hex(2, 4, -6));
//     }
    
//     static public void TestUnitMovementRefreshOnNewTurn()
//     {
//         Unit testUnit = new Unit("testUnit", movementCosts, null, 2.0f, 1);
//         testUnit.remainingMovement = 0.0f;
//         testUnit.OnTurnEnded();
//         testUnit.OnTurnStarted();
//         if(testUnit.remainingMovement != 2.0f)
//         {
//             Tests.Complain("TestUnitMovementRefreshOnNewTurn");
//         }
//     }
    
//     static public void TestSimpleUnitMovement(bool printGameBoard)
//     {
//         int top = 0;
//         int bottom = 10;
//         int left = 0;
//         int right = 30;
//         String name = "TestSimpleUnitMovement";
//         Dictionary<Hex, GameHex> gameHexDict = new();
//         for (int r = top; r <= bottom; r++){
//             int r_offset = r>>1; //same as (int)Math.Floor(r/2.0f)
//             for (int q = left - r_offset; q <= right - r_offset; q++){
//                 if(r==0 || r == bottom || q == left - r_offset || q == right - r_offset || (r == bottom/2 && q > left - r_offset + 2 && q < right - r_offset - 2))
//                 {
//                     gameHexDict.Add(new Hex(q, r, -q-r), new GameHex(new Hex(q, r, -q-r), TerrainType.Rough, TerrainTemperature.Grassland, new HashSet<FeatureType>()));
//                 }
//                 else
//                 {
//                     gameHexDict.Add(new Hex(q, r, -q-r), new GameHex(new Hex(q, r, -q-r), TerrainType.Flat, TerrainTemperature.Grassland, new HashSet<FeatureType>()));
//                 }
//             }
//         }
//         GameBoard mainBoard = new GameBoard(gameHexDict, top, bottom, left, right);
//         if (printGameBoard)
//         {
//             mainBoard.PrintGameBoard();
//         }
//         Dictionary<TerrainMoveType,float> scoutMovementCosts = new Dictionary<TerrainMoveType, float>{
//             { TerrainMoveType.Flat, 1 },
//             { TerrainMoveType.Rough, 2 },
//             { TerrainMoveType.Mountain, 9999 },
//             { TerrainMoveType.Coast, 1 },
//             { TerrainMoveType.Ocean, 1 },
//             { TerrainMoveType.Forest, 1 },
//             { TerrainMoveType.River, 0 },
//             { TerrainMoveType.Road, 0.5f },
//             { TerrainMoveType.Embark, 0 },
//             { TerrainMoveType.Disembark, 0 },
//         };
//         float scoutMovementSpeed = 2.0f;

//         Hex start = new Hex(2, 4, -6);
//         Hex end = new Hex(1, 6, -7);
//         currentPath = mainBoard.PathFind(start, end, scoutMovementCosts, scoutMovementSpeed);
//         Tests.EqualHex(name, currentPath[2], new Hex(1, 6, -7));
//         Tests.EqualHex(name, currentPath[1], new Hex(2, 5, -7));
//         Tests.EqualHex(name, currentPath[0], new Hex(2, 4, -6));
        
//         Dictionary<TerrainMoveType, float> movementCosts = {
//             { TerrainMoveType.Flat, 1 },
//             { TerrainMoveType.Rough, 2 },
//             { TerrainMoveType.Mountain, 9999 },
//             { TerrainMoveType.Coast, 1 },
//             { TerrainMoveType.Ocean, 1 },
//             { TerrainMoveType.Forest, 1 },
//             { TerrainMoveType.River, 0 },
//             { TerrainMoveType.Road, 0.5f },
//             { TerrainMoveType.Embark, 0 },
//             { TerrainMoveType.Disembark, 0 },
//         };
//         Unit testUnit = new Unit("testUnit", movementCosts, null);
//         if (!mainBoard.gameHexDict[start].SpawnUnit(testUnit, true, true))
//         {
//             Tests.Complain(name);
//         }
//         Tests.EqualHex(name, testUnit.currentGameHex.hex, new Hex(2, 4, -6))
//         if(testUnit.MoveTowards(end))
//         {
//             Tests.Complain(name);
//         }
//         Tests.EqualHex(name, testUnit.currentGameHex.hex, new Hex(2, 5, -7))
//         testUnit.OnTurnEnded();
//         testUnit.OnTurnStarted();
//         if(!testUnit.MoveTowards(end))
//         {
//             Tests.Complain(name);
//         }
//         Tests.EqualHex(name, testUnit.currentGameHex.hex, new Hex(1, 6, -7))
//         if(testUnit.remainingMovement != 1.0f)
//         {
//             Tests.Complain(name);
//         }
//     }

//     static public void TestMoveIntoNeutralOrEnemy(bool printGameBoard)
//     {
//         int top = 0;
//         int bottom = 10;
//         int left = 0;
//         int right = 30;
//         String name = "TestMoveIntoNeutralOrEnemy";
//         Dictionary<Hex, GameHex> gameHexDict = new();
//         for (int r = top; r <= bottom; r++){
//             int r_offset = r>>1; //same as (int)Math.Floor(r/2.0f)
//             for (int q = left - r_offset; q <= right - r_offset; q++){
//                 if(r==0 || r == bottom || q == left - r_offset || q == right - r_offset || (r == bottom/2 && q > left - r_offset + 2 && q < right - r_offset - 2))
//                 {
//                     gameHexDict.Add(new Hex(q, r, -q-r), new GameHex(new Hex(q, r, -q-r), TerrainType.Flat, TerrainTemperature.Grassland, new HashSet<FeatureType>()));
//                 }
//                 else
//                 {
//                     gameHexDict.Add(new Hex(q, r, -q-r), new GameHex(new Hex(q, r, -q-r), TerrainType.Flat, TerrainTemperature.Grassland, new HashSet<FeatureType>()));
//                 }
//             }
//         }
//         GameBoard mainBoard = new GameBoard(gameHexDict, top, bottom, left, right);
//         if (printGameBoard)
//         {
//             mainBoard.PrintGameBoard();
//         }
        
//         Dictionary<TerrainMoveType, float> movementCosts = {
//             { TerrainMoveType.Flat, 1 },
//             { TerrainMoveType.Rough, 2 },
//             { TerrainMoveType.Mountain, 9999 },
//             { TerrainMoveType.Coast, 1 },
//             { TerrainMoveType.Ocean, 1 },
//             { TerrainMoveType.Forest, 1 },
//             { TerrainMoveType.River, 0 },
//             { TerrainMoveType.Road, 0.5f },
//             { TerrainMoveType.Embark, 0 },
//             { TerrainMoveType.Disembark, 0 },
//         };
//         Unit testUnit = new Unit("testUnit", movementCosts, null, 2.0f, 1);
//         Unit testEnemyUnit = new Unit("testEnemyUnit", movementCosts, null, 2.0f, 2);
//         Unit testNeutralUnit = new Unit("testNeutralUnit", movementCosts, null, 2.0f, 0);
//         Hex start = new Hex(3, 3, -6);
//         Hex enemyStart = new Hex(4, 3, -7);
//         Hex neutralStart = new Hex(2, 3, -5);
//         if (!mainBoard.gameHexDict[start].SpawnUnit(testUnit, false, true) | !mainBoard.gameHexDict[enemyStart].SpawnUnit(testEnemyUnit, false, true) | !mainBoard.gameHexDict[neutralStart].SpawnUnit(testNeutralUnit, false, true))
//         {
//             Tests.Complain(name);
//         }
//         Tests.EqualHex(name, testUnit.currentGameHex.hex, new Hex(3, 3, -6))
//         if(testUnit.MoveTowards(neutralStart))
//         {
//             Tests.Complain(name);
//         }
//         Tests.EqualHex(name, testUnit.currentGameHex.hex, new Hex(3, 3, -6))
//         testUnit.OnTurnEnded();
//         testUnit.OnTurnStarted();
//         Tests.EqualHex(name, testUnit.currentGameHex.hex, new Hex(3, 3, -6))
//         if(!testUnit.MoveTowards(enemyStart))
//         {
//             Tests.Complain(name);
//         }
//         Tests.EqualHex(name, testUnit.currentGameHex.hex, new Hex(3, 3, -6))
//         if(testUnit.currentHealth != 50.0f | testEnemyUnit.currentHealth != 50.0f)
//         {
//             Tests.Complain(name);
//         }
        
//         testUnit.OnTurnEnded();
//         testUnit.OnTurnStarted();
//         Tests.EqualHex(name, testUnit.currentGameHex.hex, new Hex(3, 3, -6))
//         if(!testUnit.MoveTowards(enemyStart))
//         {
//             Tests.Complain(name+"part2");
//         }
//         Tests.EqualHex(name, testUnit.currentGameHex.hex, null)
//         Tests.EqualHex(name, testEnemyUnit.currentGameHex.hex, null)
//         if(testUnit.currentHealth != 0.0f | testEnemyUnit.currentHealth != 0.0f)
//         {
//             Tests.Complain(name+"part2");
//         }
//     }

//     static public void TestMoveIntoHurtEnemy(bool printGameBoard)
//     {
//         int top = 0;
//         int bottom = 10;
//         int left = 0;
//         int right = 30;
//         String name = "TestMoveIntoHurtEnemy";
//         Dictionary<Hex, GameHex> gameHexDict = new();
//         for (int r = top; r <= bottom; r++){
//             int r_offset = r>>1; //same as (int)Math.Floor(r/2.0f)
//             for (int q = left - r_offset; q <= right - r_offset; q++){
//                 if(r==0 || r == bottom || q == left - r_offset || q == right - r_offset || (r == bottom/2 && q > left - r_offset + 2 && q < right - r_offset - 2))
//                 {
//                     gameHexDict.Add(new Hex(q, r, -q-r), new GameHex(new Hex(q, r, -q-r), TerrainType.Flat, TerrainTemperature.Grassland, new HashSet<FeatureType>()));
//                 }
//                 else
//                 {
//                     gameHexDict.Add(new Hex(q, r, -q-r), new GameHex(new Hex(q, r, -q-r), TerrainType.Flat, TerrainTemperature.Grassland, new HashSet<FeatureType>()));
//                 }
//             }
//         }
//         GameBoard mainBoard = new GameBoard(gameHexDict, top, bottom, left, right);
//         if (printGameBoard)
//         {
//             mainBoard.PrintGameBoard();
//         }
        
//         Dictionary<TerrainMoveType, float> movementCosts = {
//             { TerrainMoveType.Flat, 1 },
//             { TerrainMoveType.Rough, 2 },
//             { TerrainMoveType.Mountain, 9999 },
//             { TerrainMoveType.Coast, 1 },
//             { TerrainMoveType.Ocean, 1 },
//             { TerrainMoveType.Forest, 1 },
//             { TerrainMoveType.River, 0 },
//             { TerrainMoveType.Road, 0.5f },
//             { TerrainMoveType.Embark, 0 },
//             { TerrainMoveType.Disembark, 0 },
//         };
//         Unit testUnit = new Unit("testUnit", movementCosts, null, 2.0f, 1);
//         Unit testEnemyUnit = new Unit("testEnemyUnit", movementCosts, null, 2.0f, 2);
//         testEnemyUnit.currentHealth = 1.0f;
//         Hex start = new Hex(3, 3, -6);
//         Hex enemyStart = new Hex(4, 3, -7);
//         Hex neutralStart = new Hex(2, 3, -5);
//         if (!mainBoard.gameHexDict[start].SpawnUnit(testUnit, false, true) | !mainBoard.gameHexDict[enemyStart].SpawnUnit(testEnemyUnit, false, true))
//         {
//             Tests.Complain(name);
//         }
//         if(!testUnit.MoveTowards(enemyStart))
//         {
//             Tests.Complain(name);
//         }
//         Tests.EqualHex(name, testUnit.currentGameHex.hex, enemyStart)
//         Tests.EqualHex(name, testEnemyUnit.currentGameHex.hex, null)
//         if(testUnit.currentHealth != 50.0f | testEnemyUnit.currentHealth <= 0.0f)
//         {
//             Tests.Complain(name);
//         }
//     }

//     static public void TestSimpleEmbarkDisembarkPathFinding(bool printGameBoard)
//     {
//         int top = 0;
//         int bottom = 10;
//         int left = 0;
//         int right = 30;
//         Dictionary<Hex, GameHex> gameHexDict = new();
//         for (int r = top; r <= bottom; r++){
//             int r_offset = r>>1; //same as (int)Math.Floor(r/2.0f)
//             for (int q = left - r_offset; q <= right - r_offset; q++){
//                 if(r==0 || r == bottom || q == left - r_offset || q == right - r_offset || (r == bottom/2 && q > left - r_offset + 2 && q < right - r_offset - 2))
//                 {
//                     gameHexDict.Add(new Hex(q, r, -q-r), new GameHex(new Hex(q, r, -q-r), TerrainType.Coast, TerrainTemperature.Grassland, new HashSet<FeatureType>()));
//                 }
//                 else
//                 {
//                     gameHexDict.Add(new Hex(q, r, -q-r), new GameHex(new Hex(q, r, -q-r), TerrainType.Flat, TerrainTemperature.Grassland, new HashSet<FeatureType>()));
//                 }
//             }
//         }
//         GameBoard mainBoard = new GameBoard(gameHexDict, top, bottom, left, right);
//         if (printGameBoard)
//         {
//             mainBoard.PrintGameBoard();
//         }
//         Dictionary<TerrainMoveType,float> scoutMovementCosts = new Dictionary<TerrainMoveType, float>{
//             { TerrainMoveType.Flat, 1 },
//             { TerrainMoveType.Rough, 2 },
//             { TerrainMoveType.Mountain, 9999 },
//             { TerrainMoveType.Coast, 1 },
//             { TerrainMoveType.Ocean, 1 },
//             { TerrainMoveType.Forest, 1 },
//             { TerrainMoveType.River, 0 },
//             { TerrainMoveType.Road, 0.5f },
//             { TerrainMoveType.Embark, 0 },
//             { TerrainMoveType.Disembark, 0 },
//         };
//         float scoutMovementSpeed = 2.0f;

//         Hex start = new Hex(2, 4, -6);
//         Hex end = new Hex(4, 6, -10);
//         List<Hex> path = mainBoard.PathFind(start, end, scoutMovementCosts, scoutMovementSpeed);
//         Hex cur = end;

//         Tests.EqualHex("TestSimpleEmbarkDisembarkPathFinding", path[4], new Hex(4, 6, -10));
//         Tests.EqualHex("TestSimpleEmbarkDisembarkPathFinding", path[3], new Hex(4, 5, -9));
//         Tests.EqualHex("TestSimpleEmbarkDisembarkPathFinding", path[2], new Hex(3, 5, -8));
//         Tests.EqualHex("TestSimpleEmbarkDisembarkPathFinding", path[1], new Hex(3, 4, -7));
//         Tests.EqualHex("TestSimpleEmbarkDisembarkPathFinding", path[0], new Hex(2, 4, -6));
//     }

//     static public void TestSimpleRoadPathFinding(bool printGameBoard)
//     {
//         int top = 0;
//         int bottom = 10;
//         int left = 0;
//         int right = 30;
//         Dictionary<Hex, GameHex> gameHexDict = new();
//         for (int r = top; r <= bottom; r++){
//             int r_offset = r>>1; //same as (int)Math.Floor(r/2.0f)
//             for (int q = left - r_offset; q <= right - r_offset; q++){
//                 if(r==0 || r == bottom || q == left - r_offset || q == right - r_offset || (r == bottom/2 && q > left - r_offset + 2 && q < right - r_offset - 2))
//                 {
//                     gameHexDict.Add(new Hex(q, r, -q-r), new GameHex(new Hex(q, r, -q-r), TerrainType.Flat, TerrainTemperature.Grassland, new HashSet<FeatureType>(){FeatureType.Road}));
//                 }
//                 else
//                 {
//                     gameHexDict.Add(new Hex(q, r, -q-r), new GameHex(new Hex(q, r, -q-r), TerrainType.Flat, TerrainTemperature.Grassland, new HashSet<FeatureType>()));
//                 }
//             }
//         }
//         GameBoard mainBoard = new GameBoard(gameHexDict, top, bottom, left, right);
//         if (printGameBoard)
//         {
//             mainBoard.PrintGameBoard();
//         }
//         Dictionary<TerrainMoveType,float> scoutMovementCosts = new Dictionary<TerrainMoveType, float>{
//             { TerrainMoveType.Flat, 1 },
//             { TerrainMoveType.Rough, 2 },
//             { TerrainMoveType.Mountain, 9999 },
//             { TerrainMoveType.Coast, 1 },
//             { TerrainMoveType.Ocean, 1 },
//             { TerrainMoveType.Forest, 1 },
//             { TerrainMoveType.River, 0 },
//             { TerrainMoveType.Road, 0.5f },
//             { TerrainMoveType.Embark, 0 },
//             { TerrainMoveType.Disembark, 0 },
//         };
//         float scoutMovementSpeed = 2.0f;

//         Hex start = new Hex(2, 4, -6);
//         Hex end = new Hex(4, 6, -10);
//         List<Hex> path = mainBoard.PathFind(start, end, scoutMovementCosts, scoutMovementSpeed);
//         Hex cur = end;
       
//         //print start node for testing
//         while (!path[cur].Equals(new Hex(-1, -1, 2)))
//         {
//             Console.WriteLine(cur.q + ", " + cur.r);
//             cur = path[cur];
//         }
//         Console.WriteLine(cur.q + ", " + cur.r);
//         cur = end;

//         Tests.EqualHex("TestSimpleEmbarkDisembarkPathFinding", path[4], new Hex(4, 6, -10));
//         Tests.EqualHex("TestSimpleEmbarkDisembarkPathFinding", path[3], new Hex(4, 5, -9));
//         Tests.EqualHex("TestSimpleEmbarkDisembarkPathFinding", path[2], new Hex(3, 5, -8));
//         Tests.EqualHex("TestSimpleEmbarkDisembarkPathFinding", path[1], new Hex(3, 4, -7));
//         Tests.EqualHex("TestSimpleEmbarkDisembarkPathFinding", path[0], new Hex(2, 4, -6));
//     }



//     // static public void TestLineofSightFlat(bool printGameBoard)
//     // {
//     //     String name = "TestLineofSightFlat";
//     //     int top = 0;
//     //     int bottom = 10;
//     //     int left = 0;
//     //     int right = 30;
//     //     Dictionary<Hex, GameHex> gameHexDict = new();
//     //     for (int r = top; r <= bottom; r++){
//     //         int r_offset = r>>1; //same as (int)Math.Floor(r/2.0f)
//     //         for (int q = left - r_offset; q <= right - r_offset; q++){
//     //             if(r==0 || r == bottom || q == left - r_offset || q == right - r_offset || (r == bottom/2 && q > left - r_offset + 2 && q < right - r_offset - 2))
//     //             {
//     //                 gameHexDict.Add(new Hex(q, r, -q-r), new GameHex(new Hex(q, r, -q-r), TerrainType.Flat, TerrainTemperature.Grassland, new HashSet<FeatureType>(){}));
//     //             }
//     //             else
//     //             {
//     //                 gameHexDict.Add(new Hex(q, r, -q-r), new GameHex(new Hex(q, r, -q-r), TerrainType.Flat, TerrainTemperature.Grassland, new HashSet<FeatureType>()));
//     //             }
//     //         }
//     //     }
//     //     GameBoard mainBoard = new GameBoard(gameHexDict, top, bottom, left, right);
//     //     if (printGameBoard)
//     //     {
//     //         mainBoard.PrintGameBoard();
//     //     }
        
//     //     float scoutSightRange = 3.0f;

//     //     Hex start = new Hex(2, 4, -6);
//     //     Hex[] visible = {new Hex(2, 4, -6), }
//     //     //Hex end = new Hex(4, 6, -10);
//     //     //Dictionary<Hex, Hex> path = mainBoard.PathFind(start, end, scoutMovementCosts, scoutMovementSpeed);
//     //     //Hex cur = end;
       
//     //     foreach (Hex visibleHex in visible)
//     //     {
//     //         if(!visibleHexDictionary.contains(visibleHex))
//     //         {
//     //             Tests.Complain(name);
//     //         }
//     //     }
//     //     Tests.EqualHex("TestSimpleEmbarkDisembarkPathFinding", cur, new Hex(4, 6, -10));
//     //     cur = path[cur];
//     //     Tests.EqualHex("TestSimpleEmbarkDisembarkPathFinding", cur, new Hex(4, 5, -9));
//     //     cur = path[cur];
//     //     Tests.EqualHex("TestSimpleEmbarkDisembarkPathFinding", cur, new Hex(3, 5, -8));
//     //     cur = path[cur];
//     //     Tests.EqualHex("TestSimpleEmbarkDisembarkPathFinding", cur, new Hex(3, 4, -7));
//     //     cur = path[cur];
//     //     Tests.EqualHex("TestSimpleEmbarkDisembarkPathFinding", cur, new Hex(2, 4, -6));
//     // }
    
//     static public void TestAll()
//     {
//         GameBoardTests.TestSimpleMountainPathFinding(false);
//         GameBoardTests.TestSimpleRoughPathFinding(false);
//         GameBoardTests.TestSimpleEmbarkDisembarkPathFinding(false);
//         GameBoardTests.TestSimpleRoadPathFinding(true);
//     }

}
