using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;

struct City
{
    public City(int id, int teamNum, String name)
    {
        this.id = id;
        this.teamNum = teamNum;
        this.name = name;
    }
    public int id;
    public int teamNum;
    public String name;
    public List<District> districts;

    public bool ChangeTeam(int newTeamNum)
    {
        teamNum = newTeamNum;
        return true;
    }

    public bool AddDistrict(District newDistrict)
    {
        districts.Add(newDistrict);
    }
}
