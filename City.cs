using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;

class City
{
    public City(int id, int teamNum, String name, GameHex ourGameHex)
    {
        this.id = id;
        this.teamNum = teamNum;
        this.name = name;
        this.ourGameHex = ourGameHex;
        ourGameHex.ourGameBoard.game.playerDictionary[teamNum].AddCity(this);
        districts = new();
        districts.Add(new District(ourGameHex, new Building("City Center"), true, this));
    }
    public int id;
    public int teamNum;
    public String name;
    public List<District> districts;
    public GameHex ourGameHex;

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
