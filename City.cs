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

    public void AddCityCenter()
    {
        Building building = new Building("City Center");
        Action<Building> adjacentDistrictsFoodFunction = (building) =>
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
            building.foodYield += counter;
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
}
