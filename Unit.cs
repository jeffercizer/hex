using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;
using System.Formats.Asn1;
using Godot;
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

    public Unit(UnitType unitType, int id, GameHex gameHex, int teamNum)
    {
        this.id = id;
        this.name = UnitLoader.unitNames[unitType];
        this.gameHex = gameHex;
        this.teamNum = teamNum;
        this.unitType = unitType;
    
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
                AddAbility(abilityName, unitInfo);
            }
        }
        else
        {
            throw new ArgumentException($"Unit type '{name}' not found in unit data.");
        }

        foreach((UnitEffect, UnitClass) effect in gameHex.gameBoard.game.playerDictionary[teamNum].unitResearchEffects)
        {
            if(unitClass.HasFlag(effect.Item2))
            {
                AddEffect(effect.Item1);
            }
        }
        
        gameHex.gameBoard.game.playerDictionary[teamNum].unitList.Add(this);
        AddVision(true);
        if (gameHex.gameBoard.game.TryGetGraphicManager(out GraphicManager manager)) manager.NewUnit(this);
    }

    public String name;
    public int id;
    public UnitType unitType;
    public Dictionary<TerrainMoveType, float> baseMovementCosts;
    public Dictionary<TerrainMoveType, float> movementCosts;
    public Dictionary<TerrainMoveType, float> baseSightCosts;
    public Dictionary<TerrainMoveType, float> sightCosts;
    public GameHex gameHex;
    public float baseMovementSpeed = 2.0f;
    public float movementSpeed = 2.0f;
    public float remainingMovement = 2.0f;
    public float baseSightRange = 3.0f;
    public float sightRange = 3.0f;
    public float health = 100.0f;
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
    public List<UnitAbility> abilities = new();
    public bool isTargetEnemy;

    public void OnTurnStarted(int turnNumber)
    {
        foreach (UnitAbility ability in abilities)
        {
            ability.ResetAbilityUses();
        }
        SetRemainingMovement(movementSpeed);
        SetAttacksLeft(maxAttackCount);
    }

    public void OnTurnEnded(int turnNumber)
    {
        gameHex.gameBoard.game.playerDictionary[teamNum].AddGold(maintenanceCost);
        if(remainingMovement > 0.0f && currentPath.Any() && !isTargetEnemy)
        {
            MoveTowards(gameHex.gameBoard.gameHexDict[currentPath.Last()], gameHex.gameBoard.game.teamManager, isTargetEnemy);
        }
        if(remainingMovement >= movementSpeed && attacksLeft == maxAttackCount)
        {
            increaseHealth(healingFactor);
        }
    }

    public void SetAttacksLeft(int attacksLeft)
    {
        this.attacksLeft = attacksLeft;
        if (gameHex.gameBoard.game.TryGetGraphicManager(out GraphicManager manager)) manager.UpdateGraphic(id, GraphicUpdateType.Update);
    }

    public void SetRemainingMovement(float remainingMovement)
    {
        this.remainingMovement = remainingMovement;
        if (gameHex.gameBoard.game.TryGetGraphicManager(out GraphicManager manager)) manager.UpdateGraphic(id, GraphicUpdateType.Update);
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

    public void AddAbility(string abilityName, UnitInfo unitInfo)
    {
        abilities.Add(new UnitAbility(new UnitEffect(abilityName), unitInfo.Abilities[abilityName].Item1, unitInfo.Abilities[abilityName].Item2, unitInfo.Abilities[abilityName].Item3, unitInfo.Abilities[abilityName].Item4));
    }

    public void UseAbilities()
    {
        for (int i = 0; i < abilities.Count; i++)
        {
            var ability = abilities[i];
            if (ability.currentCharges >= 1)
            {
                ability.effect.Apply(this);
                ability.currentCharges -= 1; // Update the tuple directly
                abilities[i] = ability; // Write the modified tuple back to the list
            }
        }
    }

    private bool DistrictCombat(GameHex targetGameHex)
    {
        return !decreaseHealth(targetGameHex.district.GetCombatStrength()) & targetGameHex.district.decreaseHealth(combatStrength);
    }

    private bool UnitCombat(GameHex targetGameHex, Unit unit)
    {
        return !decreaseHealth(20.0f) & unit.decreaseHealth(25.0f);
    }

    public bool AttackTarget(GameHex targetGameHex, float moveCost, TeamManager teamManager)
    {
        SetRemainingMovement(remainingMovement - moveCost);
        if (targetGameHex.district != null && teamManager.GetEnemies(teamNum).Contains(targetGameHex.district.city.teamNum) && targetGameHex.district.health > 0.0f)
        {
            SetAttacksLeft(attacksLeft - 1);
            GD.Print("ATTACK DISTRICT");
            return DistrictCombat(targetGameHex);;
        }
        if (targetGameHex.unitsList.Any())
        {
            Unit unit = targetGameHex.unitsList[0];
            if (teamManager.GetEnemies(teamNum).Contains(unit.teamNum))
            {
                //combat math TODO
                //if we didn't die and the enemy has died we can move in otherwise atleast one of us should poof
                SetAttacksLeft(attacksLeft - 1);
                GD.Print("ATTACK UNIT");
                return UnitCombat(targetGameHex, unit);
            }
            return false;
        }
        else
        {
            return true;
        }
    }

    private bool RangedDistrictCombat(GameHex targetGameHex, float rangedPower)
    {
        return targetGameHex.district.decreaseHealth(rangedPower);
    }

    private bool RangedUnitCombat(GameHex targetGameHex, Unit unit, float rangedPower)
    {
        return unit.decreaseHealth(rangedPower);
    }

    public bool RangedAttackTarget(GameHex targetGameHex, float rangedPower, TeamManager teamManager)
    {
        //remainingMovement -= moveCost;
        if (targetGameHex.district != null && teamManager.GetEnemies(teamNum).Contains(targetGameHex.district.city.teamNum) && targetGameHex.district.health > 0.0f)
        {
            SetAttacksLeft(attacksLeft - 1);
            return RangedDistrictCombat(targetGameHex, rangedPower);
        }
        if (targetGameHex.unitsList.Any())
        {
            
            Unit unit = targetGameHex.unitsList[0];
            if (teamManager.GetEnemies(teamNum).Contains(unit.teamNum))
            {
                //combat math TODO
                //if we didn't die and the enemy has died we can move in otherwise atleast one of us should poof
                SetAttacksLeft(attacksLeft - 1);
                return RangedUnitCombat(targetGameHex, unit, rangedPower);
            }
            return false;
        }
        else
        {
            return false;
        }
    }
    
    public void increaseHealth(float amount)
    {
        health += amount;
        health = Math.Min(health, 100.0f);
        if (gameHex.gameBoard.game.TryGetGraphicManager(out GraphicManager manager)) manager.UpdateGraphic(id, GraphicUpdateType.Update);
    }

    public bool decreaseHealth(float amount)
    {
        health -= amount;
        if (health <= 0.0f)
        {
            onDeathEffects();
            return true;
        }
        else
        {
            if (gameHex.gameBoard.game.TryGetGraphicManager(out GraphicManager manager)) manager.UpdateGraphic(id, GraphicUpdateType.Update);
            return false;
        }
    }

    public void onDeathEffects()
    {
        gameHex.unitsList.Remove(this);
        gameHex.gameBoard.game.playerDictionary[teamNum].unitList.Remove(this);
        RemoveVision(true);
        if (gameHex.gameBoard.game.TryGetGraphicManager(out GraphicManager manager)) manager.UpdateGraphic(id, GraphicUpdateType.Remove);
    }

    public void UpdateVision()
    {
        RemoveVision(false);
        AddVision(false);
        if (gameHex.gameBoard.game.TryGetGraphicManager(out GraphicManager manager)) manager.UpdateGraphic(gameHex.gameBoard.id, GraphicUpdateType.Update);
    }

    public void RemoveVision(bool updateGraphic)
    {
        foreach (Hex hex in visibleHexes)
        {            
            int count;
            if(gameHex.gameBoard.game.playerDictionary[teamNum].visibleGameHexDict.TryGetValue(hex, out count))
            {
                if(count <= 1)
                {
                    gameHex.gameBoard.game.playerDictionary[teamNum].visibleGameHexDict.Remove(hex);
                }
                else
                {
                    gameHex.gameBoard.game.playerDictionary[teamNum].visibleGameHexDict[hex] = count - 1;
                }
            }
        }
        visibleHexes.Clear();
        if (updateGraphic && gameHex.gameBoard.game.TryGetGraphicManager(out GraphicManager manager)) manager.UpdateGraphic(gameHex.gameBoard.id, GraphicUpdateType.Update);
    }

    public void AddVision(bool updateGraphic)
    {
        visibleHexes = CalculateVision().Keys.ToList();
        foreach (Hex hex in visibleHexes)
        {
            gameHex.gameBoard.game.playerDictionary[teamNum].seenGameHexDict.TryAdd(hex, true); //add to the seen dict no matter what since duplicates are thrown out
            int count;
            if(gameHex.gameBoard.game.playerDictionary[teamNum].visibleGameHexDict.TryGetValue(hex, out count))
            {
                gameHex.gameBoard.game.playerDictionary[teamNum].visibleGameHexDict[hex] = count + 1;
            }
            else
            {
                gameHex.gameBoard.game.playerDictionary[teamNum].visibleGameHexDict.TryAdd(hex, 1);
            }
        }
        if (updateGraphic && gameHex.gameBoard.game.TryGetGraphicManager(out GraphicManager manager)) manager.UpdateGraphic(gameHex.gameBoard.id, GraphicUpdateType.Update);

    }

    public Dictionary<Hex, float> CalculateVision()
    {
        Queue<Hex> frontier = new();
        frontier.Enqueue(gameHex.hex);
        Dictionary<Hex, float> reached = new();
        reached.Add(gameHex.hex, 0.0f);

        while (frontier.Count > 0)
        {
            Hex current = frontier.Dequeue();

            foreach (Hex next in gameHex.hex.WrappingNeighbors(gameHex.gameBoard.left, gameHex.gameBoard.right))
            {
                float sightLeft = sightRange - reached[current];
                float visionCost = VisionCost(gameHex.gameBoard.gameHexDict[next], sightLeft); //vision cost is at most the cost of our remaining sight if we have atleast 1
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
        frontier.Enqueue(gameHex.hex);
        Dictionary<Hex, float> reached = new();
        reached.Add(gameHex.hex, 0.0f);

        Dictionary<Hex, float> tempreached = new();

        while (frontier.Count > 0)
        {
            Hex current = frontier.Dequeue();

            foreach (Hex next in current.WrappingNeighbors(gameHex.gameBoard.left, gameHex.gameBoard.right))
            {
                float movementLeft = remainingMovement - reached[current];
                float moveCost = TravelCost(current, next, gameHex.gameBoard.game.teamManager, true, movementCosts, remainingMovement, reached[current], false); 
                if (moveCost <= movementLeft)
                {
                    if (!reached.Keys.Contains(next))
                    {
                        if(reached[current]+moveCost < remainingMovement)
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
                //DISABLE HOPPING FOR THE TIME BEING BECAUSE PATHFIND DOESNT SUPPORT IT AND IDK HOW TO STORE IT
/*                if(moveCost == 555555)
                {
                    moveCost = TravelCost(current, next, gameHex.gameBoard.game.teamManager, true, movementCosts, remainingMovement, reached[current], true);
                    if (!reached.Keys.Contains(next))
                    {
                        if (reached[current] + moveCost < remainingMovement)
                        {
                            frontier.Enqueue(next);
                        }
                        reached.Add(next, reached[current] + moveCost);
                        tempreached.Add(next, reached[current] + moveCost);
                    }
                    else if (reached[next] > reached[current] + moveCost)
                    {
                        reached[next] = reached[current] + moveCost;
                    }
                }*/
            }
        }
        foreach(Hex tempHex in  tempreached.Keys)
        {
            reached.Remove(tempHex);
        }
        
        return reached;
    }
    

    public bool SetGameHex(GameHex newGameHex)
    {
        gameHex = newGameHex;
        if (gameHex.gameBoard.game.TryGetGraphicManager(out GraphicManager manager)) manager.UpdateGraphic(id, GraphicUpdateType.Move);
        return true;
    }

    public bool TryMoveToGameHex(GameHex targetGameHex, TeamManager teamManager)
    {
        if(targetGameHex.unitsList.Any() & !isTargetEnemy)
        {
            return false;
        }
        float moveCost = TravelCost(gameHex.hex, targetGameHex.hex, teamManager, isTargetEnemy, movementCosts, movementSpeed, movementSpeed-remainingMovement, false);
        if(moveCost <= remainingMovement)
        {
            if(isTargetEnemy & targetGameHex.hex.Equals(currentPath.Last()))
            {
                if(attacksLeft > 0)
                {
                    if (AttackTarget(targetGameHex, moveCost, teamManager))
                    {
                        UpdateVision();
                        gameHex.unitsList.Remove(this);
                        gameHex = targetGameHex;
                        gameHex.unitsList.Add(this);
                        if (gameHex.gameBoard.game.TryGetGraphicManager(out GraphicManager manager)) manager.UpdateGraphic(id, GraphicUpdateType.Move);
                        return true;
                    }
                }
            }
            else if(!targetGameHex.unitsList.Any())
            {
                SetRemainingMovement(remainingMovement - moveCost);
                UpdateVision();
                gameHex.unitsList.Remove(this);
                gameHex = targetGameHex;
                gameHex.unitsList.Add(this);
                if (gameHex.gameBoard.game.TryGetGraphicManager(out GraphicManager manager)) manager.UpdateGraphic(id, GraphicUpdateType.Move);
                return true;
            }
        }
        return false;
    }

    public bool MoveToGameHex(GameHex targetGameHex)
    {
        UpdateVision();
        gameHex.unitsList.Remove(this);
        gameHex = targetGameHex;
        gameHex.unitsList.Add(this);
        if (gameHex.gameBoard.game.TryGetGraphicManager(out GraphicManager manager)) manager.UpdateGraphic(id, GraphicUpdateType.Move);
        return true;
    }

    public bool MoveTowards(GameHex targetGameHex, TeamManager teamManager, bool isTargetEnemy)
    {
        this.isTargetEnemy = isTargetEnemy;
        
        currentPath = PathFind(gameHex.hex, targetGameHex.hex, gameHex.gameBoard.game.teamManager, movementCosts, movementSpeed);
        currentPath.Remove(gameHex.hex);
        while (currentPath.Count > 0)
        {
            GameHex nextHex = gameHex.gameBoard.gameHexDict[currentPath[0]];
            if (!TryMoveToGameHex(nextHex, teamManager))
            {
                return false;
            }
            currentPath.Remove(nextHex.hex);
        }
        return true;
    }
    
    public float TravelCost(Hex first, Hex second, TeamManager teamManager, bool isTargetEnemy, Dictionary<TerrainMoveType, float> movementCosts, float unitMovementSpeed, float costSoFar, bool ignoreUnits)
    {
        //cost for river, embark, disembark are custom (0 = end turn to enter, 1/2/3/4 = normal cost)\\
        GameHex firstHex;
        GameHex secondHex;
        if (!gameHex.gameBoard.gameHexDict.TryGetValue(first, out firstHex)) //if firstHex is somehow off the table return max
        {
            return 111111;
        }
        if (!gameHex.gameBoard.gameHexDict.TryGetValue(second, out secondHex)) //if secondHex is off the table return max
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
                if(movementCosts[TerrainMoveType.Disembark] < 0) //we CANT disembark
                {
                    moveCost = 777777;
                }
                else if(movementCosts[TerrainMoveType.Disembark] == 0) //we must use all remaining movement to disembark
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
                if (movementCosts[TerrainMoveType.Embark] < 0) //we CANT embark
                {
                    moveCost = 666666;
                }
                else if (movementCosts[TerrainMoveType.Embark] == 0)
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
        //check for units
        if(!ignoreUnits)
        {
            foreach (Unit unit in secondHex.unitsList)
            {
                if (isTargetEnemy && teamManager.GetEnemies(teamNum).Contains(unit.teamNum) && attacksLeft > 0)
                {
                    break;
                }
                else
                {
                    moveCost = 555555;
                }
            }
        }
        //check for districts, your districts OK, all others are a no no, unless attacking enemy
        if(secondHex.district != null && secondHex.district.city.teamNum != teamNum)
        {
            if(!(isTargetEnemy && teamManager.GetEnemies(teamNum).Contains(secondHex.district.city.teamNum) && attacksLeft > 0))
            {
                moveCost += 12121212;
            }
        }
        return moveCost;
    }



    private int AstarHeuristic(Hex start, Hex end)
    {
        return start.WrapDistance(end, gameHex.gameBoard.right - gameHex.gameBoard.left);
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
    
            foreach (Hex next in current.WrappingNeighbors(gameHex.gameBoard.left, gameHex.gameBoard.right))
            {
                float new_cost = cost_so_far[current] + TravelCost(current, next, teamManager, isTargetEnemy, movementCosts, unitMovementSpeed, cost_so_far[current], false);
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
