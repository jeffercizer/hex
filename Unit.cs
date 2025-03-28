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

    public Unit(UnitType unitType, GameHex currentGameHex, int teamNum)
    {
        this.name = UnitLoader.unitNames[unitType];
        this.currentGameHex = currentGameHex;
        this.teamNum = teamNum;
    
        if (UnitLoader.unitsDict.TryGetValue(unitType, out UnitInfo unitInfo))
        {
            this.unitClass = unitInfo.Class;
            this.movementCosts = unitInfo.MovementCosts;
            this.baseMovementCosts = unitInfo.MovementCosts;
            this.sightCosts = unitInfo.SightCosts;
            this.baseSightCosts = unitInfo.SightCosts;
    
            this.movementSpeed = unitInfo.MovementSpeed;
            this.sightRange = unitInfo.SightRange;
            this.healingFactor = unitInfo.HealingFactor;
            this.combatStrength = unitInfo.CombatPower;
            this.maintenanceCost = unitInfo.MaintenanceCost;

            foreach (String effectName in unitInfo.Effects)
            {
                AddEffect(new UnitEffect(effectName));
            }
    
            foreach (String abilityName in unitInfo.Abilities.Keys)
            {
                AddAbility(new UnitEffect(abilityName), unitInfo.Abilities[abilityName]);
            }
        }
        else
        {
            throw new ArgumentException($"Unit type '{name}' not found in unit data.");
        }
    
        currentGameHex.gameBoard.game.playerDictionary[teamNum].unitList.Add(this);
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
        currentGameHex.gameBoard.game.playerDictionary[teamNum].unitList.Add(this);
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
    public int healingFactor;
    public int teamNum;
    public UnitClass unitClass;
    public List<Hex>? currentPath = new();
    public List<Hex> visibleHexes = new();
    public List<UnitEffect> effects = new();
    public List<(int,UnitEffect)> abilities = new();
    public bool isTargetEnemy;

    public void OnTurnStarted(int turnNumber)
    {
        remainingMovement = movementSpeed;
        attacksLeft = maxAttackCount;
    }

    public void OnTurnEnded(int turnNumber)
    {
        currentGameHex.gameBoard.game.playerDictionary[teamNum].AddGold(maintenanceCost);
        if(remainingMovement > 0.0f & currentPath.Any())
        {
            MoveTowards(currentGameHex.gameBoard.gameHexDict[currentPath.Last()], currentGameHex.gameBoard.game.teamManager ,isTargetEnemy);
        }
        if(remainingMovement >= movementSpeed & attacksLeft == maxAttackCount)
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
        foreach(UnitEffect effect1 in effects)
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
        effects.Add(effect);
        RecalculateEffects();
    }

    public void RemoveEffect(UnitEffect effect)
    {
        effects.Remove(effect);
        RecalculateEffects();
    }

    public void AddAbility(UnitEffect ability, int usageCount)
    {
        abilities.Add((usageCount, ability));
    }

    public void UseAbilities()
    {
        for (int i = 0; i < abilities.Count; i++)
        {
            var ability = abilities[i];
            if (ability.Item1 >= 1)
            {
                ability.Item2.Apply(this);
                ability.Item1 -= 1; // Update the tuple directly
                abilities[i] = ability; // Write the modified tuple back to the list
            }
        }
    }

    private bool DistrictCombat(GameHex targetGameHex)
    {
        return !decreaseCurrentHealth(targetGameHex.district.GetCombatStrength()) & targetGameHex.district.decreaseCurrentHealth(combatStrength);
    }

    private bool UnitCombat(GameHex targetGameHex)
    {
        return !decreaseCurrentHealth(20.0f) & unit.decreaseCurrentHealth(25.0f);
    }

    public bool AttackTarget(GameHex targetGameHex, float moveCost, TeamManager teamManager)
    {
        remainingMovement -= moveCost;
        if (targetGameHex.district != null && teamManager.GetEnemies(teamNum).Contains(targetGameHex.district.city.teamNum) && targetGameHex.district.currentHealth > 0.0f)
        {
            attacksLeft -= 1;
            return DistrictCombat(targetGameHex);;
        }
        if (targetGameHex.unitsList.Any())
        {
            Unit unit = targetGameHex.unitsList[0];
            if (teamManager.GetEnemies(teamNum).Contains(unit.teamNum))
            {
                //combat math TODO
                //if we didn't die and the enemy has died we can move in otherwise atleast one of us should poof
                attacksLeft -= 1;
                return UnitCombat(targetGameHex);
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
        currentHealth = Math.Min(currentHealth, 100.0f);
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
        currentGameHex.gameBoard.game.playerDictionary[teamNum].unitList.Remove(this);
        RemoveVision();
    }

    public void UpdateVision()
    {
        RemoveVision();
        AddVision();
    }

    public void RemoveVision()
    {
        foreach (Hex hex in visibleHexes)
        {            
            int count;
            if(currentGameHex.gameBoard.game.playerDictionary[teamNum].visibleGameHexDict.TryGetValue(hex, out count))
            {
                if(count <= 1)
                {
                    currentGameHex.gameBoard.game.playerDictionary[teamNum].visibleGameHexDict.Remove(hex);
                }
                else
                {
                    currentGameHex.gameBoard.game.playerDictionary[teamNum].visibleGameHexDict[hex] = count - 1;
                }
            }
        }
        visibleHexes.Clear();
    }

    public void AddVision()
    {
        visibleHexes = CalculateVision().Keys.ToList();
        foreach (Hex hex in visibleHexes)
        {
            currentGameHex.gameBoard.game.playerDictionary[teamNum].seenGameHexDict.TryAdd(hex, true); //add to the seen dict no matter what since duplicates are thrown out
            int count;
            if(currentGameHex.gameBoard.game.playerDictionary[teamNum].visibleGameHexDict.TryGetValue(hex, out count))
            {
                currentGameHex.gameBoard.game.playerDictionary[teamNum].visibleGameHexDict[hex] = count + 1;
            }
            else
            {
                currentGameHex.gameBoard.game.playerDictionary[teamNum].visibleGameHexDict.TryAdd(hex, 1);
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

            foreach (Hex next in currentGameHex.hex.WrappingNeighbors(currentGameHex.gameBoard.left, currentGameHex.gameBoard.right))
            {
                float sightLeft = sightRange - reached[current];
                float visionCost = VisionCost(currentGameHex.gameBoard.gameHexDict[next], sightLeft); //vision cost is at most the cost of our remaining sight if we have atleast 1
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

    public Dictionary<Hex, float> MovementRange()
    {
        //breadth first using movement speed and move costs
        Queue<Hex> frontier = new();
        frontier.Enqueue(currentGameHex.hex);
        Dictionary<Hex, float> reached = new();
        reached.Add(currentGameHex.hex, 0.0f);

        while (frontier.Count > 0)
        {
            Hex current = frontier.Dequeue();

            foreach (Hex next in currentGameHex.hex.WrappingNeighbors(currentGameHex.gameBoard.left, currentGameHex.gameBoard.right))
            {
                float movementLeft = movementSpeed - reached[current];
                float moveCost = TravelCost(current, next, currentGameHex.gameBoard.game.teamManager, true, movementCosts, movementSpeed, reached[current]); 
                if (moveCost <= movementLeft)
                {
                    if (!reached.Keys.Contains(next))
                    {
                        if(reached[current]+moveCost < movementSpeed)
                        {
                            frontier.Enqueue(next);
                        }
                        reached.Add(next, reached[current]+moveCost);
                    }
                    else if(reached[next] > reached[current]+moveCost)
                    {
                        reached[next] = reached[current]+moveCost;
                    }
                }
            }
        }
        return reached;
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
                if(AttackTarget(targetGameHex, moveCost, teamManager))
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
        currentPath = PathFind(currentGameHex.hex, targetGameHex.hex, currentGameHex.gameBoard.game.teamManager, movementCosts, movementSpeed);
        currentPath.Remove(currentGameHex.hex);
        while (currentPath.Count > 0)
        {
            GameHex nextHex = currentGameHex.gameBoard.gameHexDict[currentPath[0]];
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
        if (!currentGameHex.gameBoard.gameHexDict.TryGetValue(first, out firstHex)) //if firstHex is somehow off the table return max
        {
            return 111111;
        }
        if (!currentGameHex.gameBoard.gameHexDict.TryGetValue(second, out secondHex)) //if secondHex is off the table return max
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
                if (movementCosts[TerrainMoveType.Disembark] <= 0) //we must use all remaining movement to disembark
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
                if (movementCosts[TerrainMoveType.Embark] <= 0)
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
        }
        return moveCost;
    }



    private int AstarHeuristic(Hex start, Hex end)
    {
        return start.WrapDistance(end, currentGameHex.gameBoard.right - currentGameHex.gameBoard.left);
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
    
            foreach (Hex next in current.WrappingNeighbors(currentGameHex.gameBoard.left, currentGameHex.gameBoard.right))
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
