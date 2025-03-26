using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;
public enum TerrainMoveType
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

    public Unit(String name, GameHex currentGameHex, int teamNum)
    {
        this.name = name;
        this.currentGameHex = currentGameHex;
        this.teamNum = teamNum;
        //LOAD FROM XML BASED ON NAME
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
        this.movementCosts = scoutMovementCosts;
        this.baseMovementCosts = movementCosts;
        
        Action<Unit> scoutAbility = (unit) =>
        {
            Console.WriteLine("We Used Scout Ability");
        };
        AddAbility(new UnitEffect(scoutAbility, 100));
        
        Dictionary<TerrainMoveType,float> scoutSightCosts = new Dictionary<TerrainMoveType, float>{
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
        this.sightCosts = scoutSightCosts;
        this.baseSightCosts = sightCosts;

        this.movementSpeed = 2.0f;
        this.sightRange = 3.0f;
        this.healingFactor = 15;
        this.combatStrength = 15.0f;
        this.maintenanceCost = 1.0f;

        currentGameHex.ourGameBoard.game.playerDictionary[teamNum].unitList.Add(this);
        AddVision();
    }

    public Unit(String name, Dictionary<TerrainMoveType, float> movementCosts, Dictionary<TerrainMoveType, float> sightCosts, GameHex currentGameHex, float sightRange, float movementSpeed, float combatStrength, float maintenanceCost, int teamNum)
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
        this.baseMaintenanceCost = maintenanceCost;
        this.maintenanceCost = maintenanceCost;
        currentGameHex.ourGameBoard.game.playerDictionary[teamNum].unitList.Add(this);
        AddVision();
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
    public float baseMaintenanceCost = 1.0f;
    public float maintenanceCost = 1.0f;
    public int maxAttackCount = 1;
    public int attacksLeft = 1;
    public int healingFactor = 15;
    public int teamNum;
    public List<Hex>? currentPath = new();
    public List<Hex> ourVisibleHexes = new();
    public List<UnitEffect> ourEffects = new();
    public List<(int,UnitEffect)> ourAbilities = new();
    public bool isTargetEnemy;

    public void OnTurnStarted(int turnNumber)
    {
        remainingMovement = movementSpeed;
        attacksLeft = maxAttackCount;
    }

    public void OnTurnEnded(int turnNumber)
    {
        currentGameHex.ourGameBoard.game.playerDictionary[teamNum].AddGold(maintenanceCost);
        if(remainingMovement > 0.0f & currentPath.Any())
        {
            MoveTowards(currentGameHex.ourGameBoard.gameHexDict[currentPath.Last()], currentGameHex.ourGameBoard.game.teamManager ,isTargetEnemy);
        }
        if(remaningMovement >= movementSpeed & attacksLeft == maxAttackCount)
        {
            increaseCurrentHealth(healingFactor);
        }
    }
    
    public void RecalculateEffects()
    {
        //must reset all to base and recalculate
        movementCosts = baseMovementCosts;
        sightCosts = baseSightCosts;
        movementSpeed = baseMovementSpeed;
        sightRange = baseSightRange;
        combatStrength = baseCombatStrength;
        maintenanceCost = baseMaintenanceCost;
        //also order all effects, multiply/divide after add/subtract priority
        //0 means it is applied first 100 means it is applied "last" (highest number last)
        //so multiply/divide effects should be 20 and add/subtract will be 10 to give wiggle room
        PriorityQueue<UnitEffect, int> orderedEffects = new();
        foreach(UnitEffect effect1 in ourEffects)
        {
            orderedEffects.Enqueue(effect1, effect1.priority);
        }
        UnitEffect effect;
        int priority;
        while(orderedEffects.TryDequeue(out effect, out priority))
        {
            effect.Apply(this);
        }
        UpdateVision();
    }

    public void AddEffect(UnitEffect effect)
    {
        ourEffects.Add(effect);
        RecalculateEffects();
    }

    public void RemoveEffect(UnitEffect effect)
    {
        ourEffects.Remove(effect);
        RecalculateEffects();
    }

    public void AddAbility(UnitEffect ability, int usageCount)
    {
        ourAbilities.Add((ability, usageCount));
    }

    public void UseAbilities()
    {
        foreach((UnitEffect, int) ability in ourAbilities)
        {
            if(ability.Item2 >= 1)
            {
                ability.Item1.Apply(this);
                ability.Item2 -= 1;
            }
        }
    }

    public bool AttackTarget(GameHex targetGameHex, TeamManager teamManager)
    {
        remainingMovement -= moveCost;
        if (targetGameHex.unitsList.Any())
        {
            Unit unit = targetGameHex.unitsList[0];
            if (teamManager.GetEnemies(teamNum).Contains(unit.teamNum))
            {
                //combat math TODO
                //if we didn't die and the enemy has died we can move in otherwise atleast one of us should poof
                attacksLeft -= 1;
                return !decreaseCurrentHealth(20.0f) & unit.decreaseCurrentHealth(25.0f);
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
        currentGameHex.ourGameBoard.game.playerDictionary[teamNum].unitList.Remove(this);
        RemoveVision();
    }

    public void UpdateVision()
    {
        RemoveVision();
        AddVision();
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

    public void AddVision()
    {
        ourVisibleHexes = CalculateVision().Keys.ToList();
        foreach (Hex hex in ourVisibleHexes)
        {
            currentGameHex.ourGameBoard.game.playerDictionary[teamNum].seenGameHexDict.TryAdd(hex, true); //add to the seen dict no matter what since duplicates are thrown out
            int count;
            if(currentGameHex.ourGameBoard.game.playerDictionary[teamNum].visibleGameHexDict.TryGetValue(hex, out count))
            {
                currentGameHex.ourGameBoard.game.playerDictionary[teamNum].visibleGameHexDict[hex] = count + 1;
            }
            else
            {
                currentGameHex.ourGameBoard.game.playerDictionary[teamNum].visibleGameHexDict.TryAdd(hex, 1);
            }
        }
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
        if(targetGameHex.unitsList.Any() & !isTargetEnemy)
        {
            return false;
        }
        float moveCost = TravelCost(currentGameHex.hex, targetGameHex.hex, teamManager, isTargetEnemy, movementCosts, movementSpeed, movementSpeed-remainingMovement);
        if(moveCost <= remainingMovement)
        {
            if(isTargetEnemy & targetGameHex.hex.Equals(currentPath.Last()) & attacksLeft > 0)
            {
                if(AttackTarget(targetGameHex, teamManager))
                {
                    UpdateVision();
                    currentGameHex.unitsList.Remove(this);
                    currentGameHex = targetGameHex;
                    currentGameHex.unitsList.Add(this);
                    return true;
                }
            }
            else if(!targetGameHex.unitsList.Any())
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
}
