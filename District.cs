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
        this.ourCity = ourCity;
        buildings = new();
        AddBuilding(initialBuilding);
        initialBuilding.ourDistrict = this;
        
        this.ourGameHex = ourGameHex;
        ourGameHex.ClaimHex(ourCity.teamNum);
        if(ourGameHex.resourceType != ResourceType.None)
        {
            AddResource();
        }
        ourGameHex.district = this;
        foreach(Hex hex in ourGameHex.hex.WrappingNeighbors(ourGameHex.ourGameBoard.left, ourGameHex.ourGameBoard.right))
        {
            ourGameHex.ourGameBoard.gameHexDict[hex].TryClaimHex(ourCity.teamNum);
        }
        this.isCityCenter = isCityCenter;
        this.isUrban = isUrban;
        if(isUrban)
        {
            ourGameHex.AddTerrainFeature(FeatureType.Road);
        }
        ourCity.RecalculateYields();
        AddVision();
    }

    public List<Building> buildings;
    public GameHex ourGameHex;
    public bool isCityCenter;
    public bool isUrban;
    public City ourCity;
    public List<Hex> ourVisibleHexes = new();

    public void BeforeSwitchTeam()
    {
        RemoveVision();
        RemoveLostResource();        
    }
    
    public void AfterSwitchTeam()
    {      
        foreach(Building building in buildings)
        {
            SwitchTeams();
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
    }
    
    public void RecalculateYields()
    {
        foreach(Building building in buildings)
        {
            building.RecalculateYields();
        }
    }

    public void AddBuilding(Building building)
    {
        buildings.Add(building);
        ourCity.citySize += 1;
        ourCity.RecalculateYields();
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
