using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;

[Serializable]
public class District
{
    public District(GameHex gameHex, Building initialBuilding, bool isCityCenter, bool isUrban, City city)
    {
        SetupDistrict(gameHex, isCityCenter, isUrban, city);
        AddBuilding(initialBuilding);
        initialBuilding.district = this;
    }

    public District(GameHex gameHex, bool isCityCenter, bool isUrban, City city)
    {
        SetupDistrict(gameHex, isCityCenter, isUrban, city);
    }

    private void SetupDistrict(GameHex gameHex, bool isCityCenter, bool isUrban, City city)
    {
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
        if(isCityCenter)
        {
            maxHealth = 50.0f;
            currentHealth = 50.0f;
        }
        this.isUrban = isUrban;
        if(isUrban)
        {
            gameHex.AddTerrainFeature(FeatureType.Road);
        }
        city.RecalculateYields();
        AddVision();
    }

    public List<Building> buildings;
    public List<Building> defenses;
    public GameHex gameHex;
    public bool isCityCenter;
    public bool isUrban;
    public bool hasWalls;
    public City city;
    public List<Hex> visibleHexes = new();
    public float currentHealth = 0.0f;
    public float maxHealth = 0.0f;
    public int maxBuildings = 2;
    public int maxDefenses = 1;

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

    public void Heal(float healAmount)
    {
        currentHealth += healAmount;
        currentHealth = Math.Min(currentHealth, maxHealth);
    }

    public void AddWalls(float wallStrength)
    {
        if(currentHealth >= maxHealth)
        {
            maxHealth += wallStrength;
            currentHealth += wallStrength;
            hasWalls = true;
        }
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
        if(buildings.Any())
        {
            foreach(Building building in buildings)
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
                }
                else
                {
                    gameHex.gameBoard.game.playerDictionary[city.teamNum].visibleGameHexDict[hex] = count - 1;
                }
            }
        }
        visibleHexes.Clear();
    }
    public void AddVision()
    {
        visibleHexes = gameHex.hex.WrappingNeighbors(gameHex.gameBoard.left, gameHex.gameBoard.right).ToList();
        foreach (Hex hex in visibleHexes)
        {
            gameHex.gameBoard.game.playerDictionary[city.teamNum].seenGameHexDict.TryAdd(hex, true); //add to the seen dict no matter what since duplicates are thrown out
            int count;
            if(gameHex.gameBoard.game.playerDictionary[city.teamNum].visibleGameHexDict.TryGetValue(hex, out count))
            {
                gameHex.gameBoard.game.playerDictionary[city.teamNum].visibleGameHexDict[hex] = count + 1;
            }
            else
            {
                gameHex.gameBoard.game.playerDictionary[city.teamNum].visibleGameHexDict.TryAdd(hex, 1);
            }
        }
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
