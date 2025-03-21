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
        building.district = district;
        districts.Add(district);
    }

    public bool ChangeTeam(int newTeamNum)
    {
        teamNum = newTeamNum;
        return true;
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

    public void BuildOnHex(Hex hex, Building building)
    {
        if(ourGameHex.ourGameBoard.gameHexDict[hex].district == null)
        {
            District district = new District(ourGameHex, building, false, this);
            building.district = district;
            districts.Add(district);
        }
        else
        {
            district.AddBuilding(building);
            building.district = district;
        }

    }

    public void ExpandToHex(Hex hex)
    {
        Building ruralBuilding = GetRuralBuilding(hex);
        District district = new District(ourGameHex, ruralBuilding, false, this);
        ruralBuilding.district = district;
        districts.Add(district);
    }

    public Building GetRuralBuilding(Hex hex)
    {
        Building ruralBuilding;
        switch (ourGameHex.ourGameBoard.gameHexDict[hex].terrainType)
        {
            case TerrainType.Flat:
                ruralBuilding = new Building("Farm");
                break;
            case TerrainType.Rough:
                ruralBuilding = new Building("Mine");
                break;
            case TerrainType.Mountain:
                ruralBuilding = new Building("Hunting Camp");
                break;
            case TerrainType.Coast:
                ruralBuilding = new Building("Fishing Boat");
                break;
            case TerrainType.Ocean:
                ruralBuilding = new Building("Whaling Ship");
                break;
        }
        return ruralBuilding;
    }

    public List<Hex> ValidExpandHexes(List<TerrainType> validTerrain)
    {
        List<Hex> validHexes = new();
        //gather valid targets
        foreach(Hex hex in hex.Range(3))
        {
            if(validTerrainTypes.Contains(ourGameHex.ourGameBoard.gameHexDict[hex]))
            {
                //hex is unowned or owned by us so continue
                if(ourGameHex.ourGameBoard.gameHexDict[hex].ownedBy == -1 | ourGameHex.ourGameBoard.gameHexDict[hex].ownedBy == teamNum)
                {
                    //hex does not have a district
                    if (ourGameHex.ourGameBoard.gameHexDict[hex].district == null)
                    {
                        //hex has only allies unit
                        bool valid = true;
                        foreach(Unit unit in ourGameHex.ourGameBoard.gameHexDict[hex].unitsList)
                        {
                            if(!teamManager.GetAllies(teamNum).Contains(unit.teamNum))
                            {
                                valid = false;
                            }
                        }
                        if(valid)
                        {
                            validHexes.Add(hex);
                        }
                    }
                }
            }
        }
        return validHexes;
    }

    public List<Hex> ValidUrbanBuildHexes(List<TerrainType> validTerrain)
    {
        List<Hex> validHexes = new();
        //gather valid targets
        foreach(Hex hex in hex.Range(3))
        {
            if(validTerrainTypes.Contains(ourGameHex.ourGameBoard.gameHexDict[hex]))
            {
                //hex is unowned or owned by us so continue
                if(ourGameHex.ourGameBoard.gameHexDict[hex].ownedBy == -1 | ourGameHex.ourGameBoard.gameHexDict[hex].ownedBy == teamNum)
                {
                    //hex does not have a district or it is not urban or has less than the max buildings TODO
                    if (ourGameHex.ourGameBoard.gameHexDict[hex].district == null | !ourGameHex.ourGameBoard.gameHexDict[hex].district.isUrban)
                    {
                        //hex doesnt have a non-friendly unit
                        bool valid = true;
                        foreach(Unit unit in ourGameHex.ourGameBoard.gameHexDict[hex].unitsList)
                        {
                            if(!teamManager.GetAllies(teamNum).Contains(unit.teamNum))
                            {
                                valid = false;
                            }
                        }
                        if(valid)
                        {
                            validHexes.Add(hex);
                        }
                    }
                }
            }
        }
        return validHexes;
    }
}
