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
        buildings = new();
        AddBuilding(initialBuilding);
        initialBuilding.ourDistrict = this;
        
        this.ourGameHex = ourGameHex;
        ourGameHex.ClaimHex(ourCity.teamNum);
        foreach(Hex hex in ourGameHex.hex.WrappingNeighbors(ourGameHex.ourGameBoard.left, ourGameHex.ourGameBoard.right))
        {
            ourGameHex.ourGameBoard.gameHexDict[hex].TryClaimHex(ourCity.teamNum);
        }
        this.isCityCenter = isCityCenter;
        this.isUrban = isUrban;
        this.ourCity = ourCity;
        ourCity.RecalculateYields();
    }

    public District(GameHex ourGameHex, bool isCityCenter, bool isUrban, City ourCity)
    {
        buildings = new();        
        this.ourGameHex = ourGameHex;
        ourGameHex.ClaimHex(ourCity.teamNum);
        foreach(Hex hex in ourGameHex.hex.WrappingNeighbors(ourGameHex.ourGameBoard.left, ourGameHex.ourGameBoard.right))
        {
            ourGameHex.ourGameBoard.gameHexDict[hex].TryClaimHex(ourCity.teamNum);
        }
        this.isCityCenter = isCityCenter;
        this.isUrban = isUrban;
        this.ourCity = ourCity;
        ourCity.RecalculateYields();
    }
    public List<Building> buildings;
    public GameHex ourGameHex;
    public bool isCityCenter;
    public bool isUrban;
    public City ourCity;
    
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
}
