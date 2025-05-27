using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;
using System.Formats.Asn1;
using Godot;
using System.IO;
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
    public String name { get; set; }
    public int id { get; set; }
    public String unitType { get; set; }
    public Dictionary<TerrainMoveType, float> movementCosts { get; set; } = new();
    public Dictionary<TerrainMoveType, float> sightCosts { get; set; } = new();
    public Hex hex { get; set; }
    public float movementSpeed { get; set; } = 2.0f;
    public float remainingMovement { get; set; } = 2.0f;
    public float sightRange { get; set; } = 3.0f;
    public float health { get; set; } = 100.0f;
    public float combatStrength { get; set; } = 10.0f;
    public float maintenanceCost { get; set; } = 1.0f;
    public int maxAttackCount { get; set; } = 1;
    public int attacksLeft { get; set; } = 1;
    public int healingFactor { get; set; }
    public int teamNum { get; set; }
    public UnitClass unitClass { get; set; }
    public List<Hex>? currentPath { get; set; } = new();
    public List<Hex> visibleHexes { get; set; } = new();
    public List<UnitEffect> effects { get; set; } = new();
    public List<UnitAbility> abilities { get; set; } = new();
    public bool isTargetEnemy { get; set; }
    public Unit(String unitType, int id, int teamNum)
    {
        this.id = id;
        this.name = unitType;
        this.teamNum = teamNum;
        this.unitType = unitType;
    
        if (UnitLoader.unitsDict.TryGetValue(unitType, out UnitInfo unitInfo))
        {
            Global.gameManager.game.unitDictionary.Add(id, this);
            this.unitClass = unitInfo.Class;
            this.movementCosts = unitInfo.MovementCosts;
            this.sightCosts = unitInfo.SightCosts;
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

    }


    public void SpawnSetup(GameHex targetGameHex)
    {
        hex = targetGameHex.hex;
        foreach ((UnitEffect, UnitClass) effect in Global.gameManager.game.playerDictionary[teamNum].unitResearchEffects)
        {
            if (unitClass.HasFlag(effect.Item2))
            {
                AddEffect(effect.Item1);
            }
        }
        //Global.gameManager.game.mainGameBoard.gameHexDict[hex].units.Add(this);
        Global.gameManager.game.playerDictionary[teamNum].unitList.Add(this);
        AddVision(true);
        if (Global.gameManager.game.TryGetGraphicManager(out GraphicManager manager)) manager.NewUnit(this);
    }



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
        Global.gameManager.game.playerDictionary[teamNum].AddGold(-maintenanceCost);
        if(remainingMovement > 0.0f && currentPath.Any() && !isTargetEnemy)
        {
            MoveTowards(Global.gameManager.game.mainGameBoard.gameHexDict[currentPath.Last()], Global.gameManager.game.teamManager, isTargetEnemy);
        }
        if(remainingMovement >= movementSpeed && attacksLeft == maxAttackCount)
        {
            increaseHealth(healingFactor);
        }
    }

    public void SetAttacksLeft(int attacksLeft)
    {
        this.attacksLeft = attacksLeft;
        foreach (UnitAbility ability in abilities)
        {
            if (ability.name.EndsWith("Attack"))
            {
                ability.currentCharges = attacksLeft;
            }
        }
        if (Global.gameManager.game.TryGetGraphicManager(out GraphicManager manager))
        {
            manager.Update2DUI(UIElement.unitDisplay);
            manager.UpdateGraphic(id, GraphicUpdateType.Update);
        }
    }

    public void SetRemainingMovement(float remainingMovement)
    {
        this.remainingMovement = remainingMovement;
        if (Global.gameManager.game.TryGetGraphicManager(out GraphicManager manager)) manager.UpdateGraphic(id, GraphicUpdateType.Update);
    }

    public void RecalculateEffects()
    {
        //must reset all to base and recalculate
        if (UnitLoader.unitsDict.TryGetValue(unitType, out UnitInfo unitInfo))
        {
            movementCosts = unitInfo.MovementCosts;
            sightCosts = unitInfo.SightCosts;
            movementSpeed = unitInfo.MovementSpeed;
            sightRange = unitInfo.SightRange;
            combatStrength = unitInfo.CombatPower;
            maintenanceCost = unitInfo.MaintenanceCost;
        }
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
            effect.Apply(id);
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
        abilities.Add(new UnitAbility(id, new UnitEffect(abilityName), unitInfo.Abilities[abilityName].Item1, unitInfo.Abilities[abilityName].Item2, unitInfo.Abilities[abilityName].Item3, unitInfo.Abilities[abilityName].Item4, unitInfo.Abilities[abilityName].Item5));
    }

    public void UseAbilities()
    {
        for (int i = 0; i < abilities.Count; i++)
        {
            var ability = abilities[i];
            if (ability.currentCharges >= 1)
            {
                ability.effect.Apply(id);
                ability.currentCharges -= 1; // Update the tuple directly
                abilities[i] = ability; // Write the modified tuple back to the list
            }
        }
    }

    //civ 6 formula
    public static float CalculateDamage(float friendlyCombatStrength, float enemyCombatStrength)
    {
        float strengthDifference = (friendlyCombatStrength - enemyCombatStrength) / 25;
        float randomFactor = (float)new Random().NextDouble() * 0.4f + 0.8f;
        float x = strengthDifference * randomFactor;

        return 30 * (float)Math.Exp(x); // Exponential scaling
    }

    private bool DistrictCombat(GameHex targetGameHex)
    {

        return !decreaseHealth(CalculateDamage(combatStrength, targetGameHex.district.GetCombatStrength())) & targetGameHex.district.decreaseHealth(CalculateDamage(targetGameHex.district.GetCombatStrength(), combatStrength));
    }

    private bool UnitCombat(GameHex targetGameHex, Unit unit)
    {
        return !decreaseHealth(CalculateDamage(combatStrength, unit.combatStrength)) & unit.decreaseHealth(CalculateDamage(unit.combatStrength, combatStrength));
    }

    public bool AttackTarget(GameHex targetGameHex, float moveCost, TeamManager teamManager)
    {
        SetRemainingMovement(remainingMovement - moveCost); 
        if (targetGameHex.district != null && teamManager.GetEnemies(teamNum).Contains(Global.gameManager.game.cityDictionary[targetGameHex.district.cityID].teamNum) && targetGameHex.district.health > 0.0f)
        {
            SetAttacksLeft(attacksLeft - 1);
            return DistrictCombat(targetGameHex);;
        }
        if (targetGameHex.units.Any())
        {
            Unit unit = targetGameHex.units[0];
            if (teamManager.GetEnemies(teamNum).Contains(unit.teamNum))
            {
                //combat math TODO
                //if we didn't die and the enemy has died we can move in otherwise atleast one of us should poof
                SetAttacksLeft(attacksLeft - 1);
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
        return targetGameHex.district.decreaseHealth(CalculateDamage(rangedPower, targetGameHex.district.GetCombatStrength()));
    }

    private bool RangedUnitCombat(GameHex targetGameHex, Unit unit, float rangedPower)
    {
        return unit.decreaseHealth(CalculateDamage(rangedPower, unit.combatStrength));
    }

    public bool RangedAttackTarget(GameHex targetGameHex, float rangedPower, TeamManager teamManager)
    {
        //remainingMovement -= moveCost;
        if (targetGameHex.district != null && teamManager.GetEnemies(teamNum).Contains(Global.gameManager.game.cityDictionary[targetGameHex.district.cityID].teamNum) && targetGameHex.district.health > 0.0f)
        {
            SetAttacksLeft(attacksLeft - 1);
            return RangedDistrictCombat(targetGameHex, rangedPower);
        }
        if (targetGameHex.units.Any())
        {
            
            Unit unit = targetGameHex.units[0];
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
        if (Global.gameManager.game.TryGetGraphicManager(out GraphicManager manager))
        {
            manager.Update2DUI(UIElement.unitDisplay);
            manager.UpdateGraphic(id, GraphicUpdateType.Update);
        }
    }

    public bool decreaseHealth(float amount)
    {
        health -= amount;
        if (Global.gameManager.game.TryGetGraphicManager(out GraphicManager manager)) manager.Update2DUI(UIElement.unitDisplay);
        if (health <= 0.0f)
        {
            onDeathEffects();
            return true;
        }
        else
        {
            if (Global.gameManager.game.TryGetGraphicManager(out GraphicManager manager2)) manager.UpdateGraphic(id, GraphicUpdateType.Update);
            return false;
        }
    }

    public void onDeathEffects()
    {
        Global.gameManager.game.mainGameBoard.gameHexDict[hex].units.Remove(this);
        Global.gameManager.game.playerDictionary[teamNum].unitList.Remove(this);
        RemoveVision(true);
        if (Global.gameManager.game.TryGetGraphicManager(out GraphicManager manager)) manager.UpdateGraphic(id, GraphicUpdateType.Remove);
    }

    public void UpdateVision()
    {
        RemoveVision(false);
        AddVision(false);
        if (Global.gameManager.game.TryGetGraphicManager(out GraphicManager manager)) manager.UpdateGraphic(Global.gameManager.game.mainGameBoard.id, GraphicUpdateType.Update);
    }

    public void RemoveVision(bool updateGraphic)
    {
        foreach (Hex hex in visibleHexes)
        {            
            int count;
            if(Global.gameManager.game.playerDictionary[teamNum].visibleGameHexDict.TryGetValue(hex, out count))
            {
                if(count <= 1)
                {
                   Global.gameManager.game.playerDictionary[teamNum].visibleGameHexDict.Remove(hex);
                   Global.gameManager.game.playerDictionary[teamNum].visibilityChangedList.Add(hex);
                }
                else
                {
                   Global.gameManager.game.playerDictionary[teamNum].visibleGameHexDict[hex] = count - 1;
                }
            }
        }
        visibleHexes.Clear();
        if (updateGraphic &&Global.gameManager.game.TryGetGraphicManager(out GraphicManager manager)) manager.UpdateGraphic(Global.gameManager.game.mainGameBoard.id, GraphicUpdateType.Update);
    }

    public void AddVision(bool updateGraphic)
    {
        visibleHexes = CalculateVision().Keys.ToList();
        foreach (Hex hex in visibleHexes)
        {
           Global.gameManager.game.playerDictionary[teamNum].seenGameHexDict.TryAdd(hex, true); //add to the seen dict no matter what since duplicates are thrown out
            int count;
            if(Global.gameManager.game.playerDictionary[teamNum].visibleGameHexDict.TryGetValue(hex, out count))
            {
               Global.gameManager.game.playerDictionary[teamNum].visibleGameHexDict[hex] = count + 1;
            }
            else
            {
               Global.gameManager.game.playerDictionary[teamNum].visibleGameHexDict.TryAdd(hex, 1);
               Global.gameManager.game.playerDictionary[teamNum].visibilityChangedList.Add(hex);
            }
        }
        if (updateGraphic &&Global.gameManager.game.TryGetGraphicManager(out GraphicManager manager)) manager.UpdateGraphic(Global.gameManager.game.mainGameBoard.id, GraphicUpdateType.Update);

    }

    public Dictionary<Hex, float> CalculateVision()
    {
        Queue<Hex> frontier = new();
        frontier.Enqueue(Global.gameManager.game.mainGameBoard.gameHexDict[hex].hex);
        Dictionary<Hex, float> reached = new();
        reached.Add(Global.gameManager.game.mainGameBoard.gameHexDict[hex].hex, 0.0f);

        while (frontier.Count > 0)
        {
            Hex current = frontier.Dequeue();

            foreach (Hex next in current.WrappingNeighbors(Global.gameManager.game.mainGameBoard.left, Global.gameManager.game.mainGameBoard.right, Global.gameManager.game.mainGameBoard.bottom))
            {
                float sightLeft = sightRange - reached[current];
                float visionCost = VisionCost(Global.gameManager.game.mainGameBoard.gameHexDict[next], sightLeft); //vision cost is at most the cost of our remaining sight if we have atleast 1
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
        frontier.Enqueue(Global.gameManager.game.mainGameBoard.gameHexDict[hex].hex);
        Dictionary<Hex, float> reached = new();
        reached.Add(Global.gameManager.game.mainGameBoard.gameHexDict[hex].hex, 0.0f);

        Dictionary<Hex, float> tempreached = new();

        while (frontier.Count > 0)
        {
            Hex current = frontier.Dequeue();

            foreach (Hex next in current.WrappingNeighbors(Global.gameManager.game.mainGameBoard.left, Global.gameManager.game.mainGameBoard.right, Global.gameManager.game.mainGameBoard.bottom))
            {
                float movementLeft = remainingMovement - reached[current];
                float moveCost = TravelCost(current, next,Global.gameManager.game.teamManager, true, movementCosts, remainingMovement, reached[current], false); 
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
                    moveCost = TravelCost(current, next,Global.gameManager.game.teamManager, true, movementCosts, remainingMovement, reached[current], true);
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
        hex = newGameHex.hex;
        if (Global.gameManager.game.TryGetGraphicManager(out GraphicManager manager)) manager.UpdateGraphic(id, GraphicUpdateType.Move);
        return true;
    }

    public bool TryMoveToGameHex(GameHex targetGameHex, TeamManager teamManager)
    {
        if(targetGameHex.units.Any() & !isTargetEnemy)
        {
            return false;
        }
        float moveCost = TravelCost(Global.gameManager.game.mainGameBoard.gameHexDict[hex].hex, targetGameHex.hex, teamManager, isTargetEnemy, movementCosts, movementSpeed, movementSpeed-remainingMovement, false);
        if(moveCost <= remainingMovement)
        {
            if(isTargetEnemy & targetGameHex.hex.Equals(currentPath.Last()))
            {
                if(attacksLeft > 0)
                {
                    if (AttackTarget(targetGameHex, moveCost, teamManager))
                    {
                        Global.gameManager.game.mainGameBoard.gameHexDict[hex].units.Remove(this);
                        hex = targetGameHex.hex;
                        Global.gameManager.game.mainGameBoard.gameHexDict[hex].units.Add(this);
                        UpdateVision();
                        if (Global.gameManager.game.TryGetGraphicManager(out GraphicManager manager)) manager.UpdateGraphic(id, GraphicUpdateType.Move);
                        return true;
                    }
                }
            }
            else if(!targetGameHex.units.Any())
            {
                SetRemainingMovement(remainingMovement - moveCost);
                Global.gameManager.game.mainGameBoard.gameHexDict[hex].units.Remove(this);
                hex = targetGameHex.hex;
                Global.gameManager.game.mainGameBoard.gameHexDict[hex].units.Add(this);
                UpdateVision();
                if (Global.gameManager.game.TryGetGraphicManager(out GraphicManager manager)) manager.UpdateGraphic(id, GraphicUpdateType.Move);
                return true;
            }
        }
        return false;
    }

    public bool MoveToGameHex(GameHex targetGameHex)
    {
        Global.gameManager.game.mainGameBoard.gameHexDict[hex].units.Remove(this);
        hex = targetGameHex.hex;
        Global.gameManager.game.mainGameBoard.gameHexDict[hex].units.Add(this);
        UpdateVision();
        if (Global.gameManager.game.TryGetGraphicManager(out GraphicManager manager)) manager.UpdateGraphic(id, GraphicUpdateType.Move);
        return true;
    }

    public bool MoveTowards(GameHex targetGameHex, TeamManager teamManager, bool isTargetEnemy)
    {
        this.isTargetEnemy = isTargetEnemy;
        
        currentPath = PathFind(Global.gameManager.game.mainGameBoard.gameHexDict[hex].hex, targetGameHex.hex,Global.gameManager.game.teamManager, movementCosts, movementSpeed);
        currentPath.Remove(Global.gameManager.game.mainGameBoard.gameHexDict[hex].hex);
        while (currentPath.Count > 0)
        {
            GameHex nextHex = Global.gameManager.game.mainGameBoard.gameHexDict[currentPath[0]];
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
        if (!Global.gameManager.game.mainGameBoard.gameHexDict.TryGetValue(first, out firstHex)) //if firstHex is somehow off the table return max
        {
            return 111111;
        }
        if (!Global.gameManager.game.mainGameBoard.gameHexDict.TryGetValue(second, out secondHex)) //if secondHex is off the table return max
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
            foreach (Unit unit in secondHex.units)
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
        if(secondHex.district != null && Global.gameManager.game.cityDictionary[secondHex.district.cityID].teamNum != teamNum)
        {
            if(!(isTargetEnemy && teamManager.GetEnemies(teamNum).Contains(Global.gameManager.game.cityDictionary[secondHex.district.cityID].teamNum) && attacksLeft > 0))
            {
                moveCost += 12121212;
            }
        }
        return moveCost;
    }



    private int AstarHeuristic(Hex start, Hex end)
    {
        return start.WrapDistance(end, Global.gameManager.game.mainGameBoard.right - Global.gameManager.game.mainGameBoard.left);
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
    
            foreach (Hex next in current.WrappingNeighbors(Global.gameManager.game.mainGameBoard.left, Global.gameManager.game.mainGameBoard.right, Global.gameManager.game.mainGameBoard.bottom))
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

    public Unit()
    {
        //used for loading
    }

    public void Serialize(BinaryWriter writer)
    {
        Serializer.Serialize(writer, this);
    }

    public static Unit Deserialize(BinaryReader reader)
    {
        return Serializer.Deserialize<Unit>(reader);
    }

}
