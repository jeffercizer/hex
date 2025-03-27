using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;

[Serializable]
public class District
{
    public District(GameHex ourGameHex, Building initialBuilding, bool isCityCenter, bool isUrban, City ourCity)
    {
        SetupDistrict(ourGameHex, isCityCenter, isUrban, ourCity);
        AddBuilding(initialBuilding);
        initialBuilding.ourDistrict = this;
    }

    public District(GameHex ourGameHex, bool isCityCenter, bool isUrban, City ourCity)
    {
        SetupDistrict(ourGameHex, isCityCenter, isUrban, ourCity);
    }

    private void SetupDistrict(GameHex ourGameHex, bool isCityCenter, bool isUrban, City ourCity)
    {
        this.ourCity = ourCity;
        buildings = new();
        defenses = new();
        this.ourGameHex = ourGameHex;
        
        ourGameHex.ClaimHex(ourCity);
        ourGameHex.district = this;
        foreach(Hex hex in ourGameHex.hex.WrappingNeighbors(ourGameHex.ourGameBoard.left, ourGameHex.ourGameBoard.right))
        {
            ourGameHex.ourGameBoard.gameHexDict[hex].TryClaimHex(ourCity);
        }
        
        if(ourGameHex.resourceType != ResourceType.None)
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
            ourGameHex.AddTerrainFeature(FeatureType.Road);
        }
        ourCity.RecalculateYields();
        AddVision();
    }

    public List<Building> buildings;
    public List<Building> defenses;
    public GameHex ourGameHex;
    public bool isCityCenter;
    public bool isUrban;
    public bool hasWalls;
    public City ourCity;
    public List<Hex> ourVisibleHexes = new();
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
        ourCity.districts.Remove(this);
        ourGameHex.district = null;
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
        ourGameHex.RecalculateYields();
        foreach(Building building in buildings)
        {
            building.RecalculateYields();
        }
    }

    public void PrepareYieldRecalculate()
    {
        if(builings.Any())
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
            ourCity.citySize += 1;
            ourCity.RecalculateYields();
        }
    }

    public void AddDefense(Building building)
    {
        if(defenses.Count() < maxDefenses)
        {
            defenses.Add(building);
            ourCity.RecalculateYields();
        }
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
            if(ourGameHex.ourGameBoard.game.playerDictionary[ourCity.teamNum].visibleGameHexDict.TryGetValue(hex, out count))
            {
                if(count <= 1)
                {
                    ourGameHex.ourGameBoard.game.playerDictionary[ourCity.teamNum].visibleGameHexDict.Remove(hex);
                }
                else
                {
                    ourGameHex.ourGameBoard.game.playerDictionary[ourCity.teamNum].visibleGameHexDict[hex] = count - 1;
                }
            }
        }
        ourVisibleHexes.Clear();
    }
    public void AddVision()
    {
        ourVisibleHexes = ourGameHex.hex.WrappingNeighbors(ourGameHex.ourGameBoard.left, ourGameHex.ourGameBoard.right).ToList();
        foreach (Hex hex in ourVisibleHexes)
        {
            ourGameHex.ourGameBoard.game.playerDictionary[ourCity.teamNum].seenGameHexDict.TryAdd(hex, true); //add to the seen dict no matter what since duplicates are thrown out
            int count;
            if(ourGameHex.ourGameBoard.game.playerDictionary[ourCity.teamNum].visibleGameHexDict.TryGetValue(hex, out count))
            {
                ourGameHex.ourGameBoard.game.playerDictionary[ourCity.teamNum].visibleGameHexDict[hex] = count + 1;
            }
            else
            {
                ourGameHex.ourGameBoard.game.playerDictionary[ourCity.teamNum].visibleGameHexDict.TryAdd(hex, 1);
            }
        }
    }

    public void AddResource()
    {
        if(ourGameHex.resourceType != ResourceType.None)
        {
            ourGameHex.ourGameBoard.game.playerDictionary[ourCity.teamNum].unassignedResources.Add(ourGameHex.hex, ourGameHex.resourceType)
        }
    }

    public void RemoveLostResource()
    {
        ourGameHex.ourGameBoard.game.playerDictionary[ourCity.teamNum].RemoveLostResource(ourGameHex.hex);
    }
}
