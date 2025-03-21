using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;

[Serializable]
public class City
{
    public City(int id, int teamNum, String name, GameHex ourGameHex)
    {
        this.id = id;
        this.teamNum = teamNum;
        this.name = name;
        this.ourGameHex = ourGameHex;
        ourGameHex.ourGameBoard.game.playerDictionary[teamNum].AddCity(this);
        districts = new();
        AddCityCenter();
        ourGameHex.districts.Add(district);
    }
    public int id;
    public int teamNum;
    public String name;
    public List<District> districts;
    public GameHex ourGameHex;
    public int foodYield;
    public int productionYield;
    public int goldYield;
    public int scienceYield;
    public int cultureYield;
    public int happinessYield;

    public void AddCityCenter()
    {
        Building building = new Building("City Center");
        Action<Building> adjacentDistrictsGoldFunction = (building) =>
        {
            GameHex temp = building.ourDistrict.ourGameHex;
            int counter = 0;
            foreach(Hex hex in temp.hex.WrappingNeighbors(temp.ourGameBoard.left, temp.ourGameBoard.right))
            {
                if(temp.ourGameBoard.gameHexDict[hex].district)
                {
                    counter++;
                }
            }
            building.goldYield += counter;
        };
        BuildingEffect effect = new BuildingEffect(effectFunction);
        building.AddEffect(effect);
        District district = new District(ourGameHex, building, true, this);
        districts.Add(district);
    }

    public bool ChangeTeam(int newTeamNum)
    {
        teamNum = newTeamNum;
        return true;
    }

    public bool AddDistrict(District newDistrict)
    {
        districts.Add(newDistrict);
    }

    public String ChangeName(String name)
    {
        this.name = name;
    }
    public void RecalculateYields()
    {
        foreach(District district in districts)
        {
            district.RecalculateYields();
            foreach(Building building in district.buildings)
            {
                foodYield += building.foodYield;
                productionYield += building.productionYield;
                goldYield += building.goldYield;
                scienceYield += building.scienceYield;
                cultureYield += building.cultureYield;
                happinessYield += building.happinessYield;
            }
        }
    }

    public bool ExpandToHex(Hex hex)
    {
        foreach(Hex hex in hex.Range(3))
        {
            
        }
    }
}
