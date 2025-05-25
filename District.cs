using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;
using Godot;
using System.Text;

[Serializable]
public class District
{
    public District(GameHex gameHex, String initialString, bool isCityCenter, bool isUrban, City city)
    {
        SetupDistrict(gameHex, isCityCenter, isUrban, city);
        AddBuilding(new Building(initialString, this));
    }

    public District(GameHex gameHex, bool isCityCenter, bool isUrban, City city)
    {
        SetupDistrict(gameHex, isCityCenter, isUrban, city);
    }

    private void SetupDistrict(GameHex gameHex, bool isCityCenter, bool isUrban, City city)
    {
        id = gameHex.gameBoard.game.GetUniqueID();
        this.city = city;
        buildings = new();
        defenses = new();
        this.gameHex = gameHex;

        
        gameHex.ClaimHex(city);
        gameHex.district = this;
        foreach(Hex hex in gameHex.hex.WrappingNeighbors(gameHex.gameBoard.left, gameHex.gameBoard.right))
        {
            gameHex.gameBoard.gameHexDict[hex].TryClaimHex(city);
        }
        
        if(gameHex.resourceType != ResourceType.None)
        {
            AddResource();
        }

        this.isCityCenter = isCityCenter;
        if (city.gameHex.gameBoard.game.TryGetGraphicManager(out GraphicManager manager))
        {
            manager.NewDistrict(this);
        }
        if (isCityCenter)
        {
            maxHealth = 50.0f;
            health = 50.0f;
        }
        else
        {
            if (gameHex.featureSet.Contains(FeatureType.Forest))
            {
                AddBuilding(new Building("Lumbermill", this));
            }
            else
            {
                if (gameHex.terrainType == TerrainType.Flat)
                {
                    AddBuilding(new Building("Farm", this));
                }
                else if (gameHex.terrainType == TerrainType.Rough)
                {
                    AddBuilding(new Building("Lumbermill", this));
                }
                else if (gameHex.terrainType == TerrainType.Mountain)
                {
                    AddBuilding(new Building("Mine", this));
                }
                else if (gameHex.terrainType == TerrainType.Coast)
                {
                    AddBuilding(new Building("FishingBoat", this));
                }
                else if (gameHex.terrainType == TerrainType.Ocean)
                {
                    AddBuilding(new Building("FishingBoat", this));
                }
            }

        }
        this.isUrban = isUrban;
        if(isUrban)
        {
            maxBuildings += 1;
            gameHex.AddTerrainFeature(FeatureType.Road);
        }

        city.RecalculateYields();
        AddVision();

    }

    public int id;
    public List<Building> buildings;
    public List<Building> defenses;
    public GameHex gameHex;
    public bool isCityCenter;
    public bool isUrban;
    public bool hasWalls;
    public City city;
    public List<Hex> visibleHexes = new();
    public float health = 0.0f;
    public float maxHealth = 0.0f;
    public int maxBuildings = 1;
    public int maxDefenses = 1;
    public int turnsUntilHealing = 0;

    public void BeforeSwitchTeam()
    {
        RemoveVision();
        RemoveLostResource();        
    }
    
    public void AfterSwitchTeam()
    {      
        foreach(Building building in buildings)
        {
            building.SwitchTeams();
        }
        foreach(Building building in defenses)
        {
            building.SwitchTeams();
        }
        AddVision();
        AddResource();
    }

    public bool decreaseHealth(float amount)
    {
        health -= amount;
        health = Math.Max(0.0f, health);
        GD.Print(city.name + " | " + city.id + " | " + health);
        if (gameHex.gameBoard.game.TryGetGraphicManager(out GraphicManager manager))
        {
            manager.UpdateGraphic(city.id, GraphicUpdateType.Update);
        }
        if (health <= 0.0f)
        {
            city.DistrictFell();
            turnsUntilHealing = 5;
            return true;
        }
        return false;
    }

    public float GetCombatStrength()
    {
        float strength = gameHex.gameBoard.game.playerDictionary[city.teamNum].strongestUnitBuilt;
        if (hasWalls)
        {
            strength += 15.0f;
        }
        return strength;
    }

    public void DestroyDistrict()
    {
        RemoveVision();
        RemoveLostResource();
        city.districts.Remove(this);
        gameHex.district = null;
        foreach(Building building in buildings)
        {
            building.DestroyBuilding();
        }
        foreach(Building building in defenses)
        {
            building.DestroyBuilding();
        }
    }

    public void HealForTurn(float healAmount)
    {
        if(turnsUntilHealing <= 0)
        {
            health += healAmount;
            health = Math.Min(health, maxHealth);
        }
        else if(turnsUntilHealing > 0)
        {
            if (gameHex.units.Any())
            {
                Unit unit = gameHex.units[0];
                if (gameHex.gameBoard.game.teamManager.GetEnemies(city.teamNum).Contains(unit.teamNum))
                {
                    turnsUntilHealing += 1;
                }
            }
            turnsUntilHealing -= 1;
        }
    }

    public void AddWalls(float wallStrength)
    {
        if(health >= maxHealth)
        {
            maxHealth += wallStrength;
            health += wallStrength;
            hasWalls = true;
        }
    }

    public void DevelopDistrict()
    {
        maxBuildings += 1;
        isUrban = true;
    }

    public int CountString(String buildingType)
    {
        int count = 0;
        foreach(Building building in buildings)
        {
            if(building.buildingType == buildingType)
            {
                count += 1;
            }
        }
        return count;
    }
    
    public void RecalculateYields()
    {
        gameHex.RecalculateYields();
        foreach(Building building in buildings)
        {
            building.RecalculateYields();
        }
    }

    public void PrepareYieldRecalculate()
    {
        if (buildings.Any())
        {
            foreach (Building building in buildings)
            {
                building.PrepareYieldRecalculate();
            }
        }
    }

    public void AddBuilding(Building building)
    {
        if(buildings.Count() < maxBuildings)
        {
            buildings.Add(building);
            city.citySize += 1;
            city.RecalculateYields();
        }
    }

    public void AddDefense(Building building)
    {
        if(defenses.Count() < maxDefenses)
        {
            defenses.Add(building);
            city.RecalculateYields();
        }
    }

    public void UpdateVision()
    {
        RemoveVision();
        AddVision();
        if (gameHex.gameBoard.game.TryGetGraphicManager(out GraphicManager manager)) manager.UpdateGraphic(gameHex.gameBoard.id, GraphicUpdateType.Update);
    }

    public void RemoveVision()
    {
        foreach (Hex hex in visibleHexes)
        {            
            int count;
            if(gameHex.gameBoard.game.playerDictionary[city.teamNum].visibleGameHexDict.TryGetValue(hex, out count))
            {
                if(count <= 1)
                {
                    gameHex.gameBoard.game.playerDictionary[city.teamNum].visibleGameHexDict.Remove(hex);
                    gameHex.gameBoard.game.playerDictionary[city.teamNum].visibilityChangedList.Add(hex);
                }
                else
                {
                    gameHex.gameBoard.game.playerDictionary[city.teamNum].visibleGameHexDict[hex] = count - 1;
                }
            }
        }
        visibleHexes.Clear();
        if (gameHex.gameBoard.game.TryGetGraphicManager(out GraphicManager manager)) manager.UpdateGraphic(gameHex.gameBoard.id, GraphicUpdateType.Update);
    }
    public void AddVision()
    {
        if (isCityCenter)
        {
            foreach (Hex hex in gameHex.hex.WrappingRange(3, gameHex.gameBoard.left, gameHex.gameBoard.right, gameHex.gameBoard.top, gameHex.gameBoard.bottom))
            {
                gameHex.gameBoard.game.playerDictionary[city.teamNum].seenGameHexDict.TryAdd(hex, true);
                int count;
                if (gameHex.gameBoard.game.playerDictionary[city.teamNum].visibleGameHexDict.TryGetValue(hex, out count))
                {
                    gameHex.gameBoard.game.playerDictionary[city.teamNum].visibleGameHexDict[hex] = count + 1;
                }
                else
                {
                    gameHex.gameBoard.game.playerDictionary[city.teamNum].visibleGameHexDict.TryAdd(hex, 1);
                    gameHex.gameBoard.game.playerDictionary[city.teamNum].visibilityChangedList.Add(hex);
                }
            }
        }
        else
        {
            visibleHexes = gameHex.hex.WrappingNeighbors(gameHex.gameBoard.left, gameHex.gameBoard.right).ToList();
            foreach (Hex hex in visibleHexes)
            {
                gameHex.gameBoard.game.playerDictionary[city.teamNum].seenGameHexDict.TryAdd(hex, true); //add to the seen dict no matter what since duplicates are thrown out
                int count;
                if (gameHex.gameBoard.game.playerDictionary[city.teamNum].visibleGameHexDict.TryGetValue(hex, out count))
                {
                    gameHex.gameBoard.game.playerDictionary[city.teamNum].visibleGameHexDict[hex] = count + 1;
                }
                else
                {
                    gameHex.gameBoard.game.playerDictionary[city.teamNum].visibleGameHexDict.TryAdd(hex, 1);
                    gameHex.gameBoard.game.playerDictionary[city.teamNum].visibilityChangedList.Add(hex);
                }
            }
        }
        if (gameHex.gameBoard.game.TryGetGraphicManager(out GraphicManager manager)) manager.UpdateGraphic(gameHex.gameBoard.id, GraphicUpdateType.Update);
    }

    public void AddResource()
    {
        if(gameHex.resourceType != ResourceType.None)
        {
            gameHex.gameBoard.game.playerDictionary[city.teamNum].unassignedResources.Add(gameHex.hex, gameHex.resourceType);
        }
    }

    public void RemoveLostResource()
    {
        gameHex.gameBoard.game.playerDictionary[city.teamNum].RemoveLostResource(gameHex.hex);
    }
}
