using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;

public enum ProductionType
{
    Building,
    Unit,
}

public class ProductionQueueType
{

    public ProductionQueueType(String name, ProductionType prodType, GameHex targetGameHex, float productionCost, float productionLeft, bool isUnique)
    {
        this.name = name;
        this.prodType = prodType;
        this.targetGameHex = targetGameHex;
        this.productionCost = productionCost;
        this.productionLeft = productionLeft;
        this.isUnique = isUnique;
    }
    public String name;
    public ProductionType prodType;
    public GameHex targetGameHex;
    public float productionLeft;
    public float productionCost;
    public bool isUnique;
}


[Serializable]
public class City
{
    public City(int id, int teamNum, String name, GameHex ourGameHex)
    {
        this.id = id;
        this.teamNum = teamNum;
        this.name = name;
        this.ourGameHex = ourGameHex;
        ourGameHex.ourGameBoard.game.playerDictionary[teamNum].cityList.Add(this);
        districts = new();
        AddCityCenter();
        productionList = new();
        partialProductionDictionary = new();
        
        citySize = 0;
        foodToGrow = 10.0f;
        RecalculateYields();
    }
    public int id;
    public int teamNum;
    public String name;
    public List<District> districts;
    public GameHex ourGameHex;
    public int citySize;
    public float foodToGrow;
    public float foodStockpile;
    public float foodYield;
    public float productionYield;
    public float goldYield;
    public float scienceYield;
    public float cultureYield;
    public float happinessYield;
    public List<ProductionQueueType> productionList;
    public Dictionary<string, ProductionQueueType> partialProductionDictionary;

    private District AddCityCenter()
    {
        District district = new District(ourGameHex, true, true, this);
        Building building = new Building("City Center");
        Action<Building> adjacentDistrictsGoldFunction = (building) =>
        {
            GameHex temp = building.ourDistrict.ourGameHex;
            int counter = 0;
            foreach(Hex hex in temp.hex.WrappingNeighbors(temp.ourGameBoard.left, temp.ourGameBoard.right))
            {
                if(temp.ourGameBoard.gameHexDict[hex].terrainType == TerrainType.Flat)
                {
                    counter++;
                }
            }
            building.goldYield += counter;
        };
        BuildingEffect effect = new BuildingEffect(adjacentDistrictsGoldFunction, 5);
        building.ourDistrict = district;
        building.AddEffect(effect);

        district.AddBuilding(building);
        districts.Add(district);
        return district;
    }


        //TODO a fun edge case if we have two of the same unit and remove one we should keep the most recent one as the partial built
        //ie this situation showcases it well, the new top item will be a scout with 10 prod left but we have a 5 prod left scout in the partialDictionary'
        //so when we remove check the whole queue for a item that matches our name and replace it if our prodLeft is < than theirs
    public bool RemoveFromQueue(int index)
    {
        if(productionList.Count > index)
        {
            if(productionList[index].productionLeft < productionList[index].productionCost)
            {
                bool foundNewHome = false;
                for(int i = 0; i < productionList.Count; i++)
                {
                    if (productionList[i].name == productionList[index].name & productionList[i].productionLeft > productionList[index].productionLeft)
                    {
                        foundNewHome = true;
                        productionList[i] = productionList[index];
                        break;
                    }
                }
                if(!foundNewHome)
                {
                    partialProductionDictionary.Add(productionList[index].name, productionList[index]);
                }
            }
            productionList.RemoveAt(index);
            return true;
        }
        return false;
    }

    public bool AddToQueue(String name, ProductionType prodType, GameHex targetGameHex, float productionCost, bool isUnique)
    {
        foreach(ProductionQueueType queueItem in productionList)
        {
            if(queueItem.name == name & (isUnique | queueItem.isUnique))
            {
                return false;
            }
        }
        ProductionQueueType queueItem1;
        if(partialProductionDictionary.TryGetValue(name, out queueItem1))
        {
            partialProductionDictionary.Remove(name);
            productionList.Add(new ProductionQueueType(name, prodType, targetGameHex, queueItem1.productionLeft, queueItem1.productionCost, isUnique));
            return true;
        }

        productionList.Add(new ProductionQueueType(name, prodType, targetGameHex, productionCost, productionCost, isUnique));
        return true;
    }

    public bool ChangeTeam(int newTeamNum)
    {
        teamNum = newTeamNum;
        return true;
    }

    public void ChangeName(String name)
    {
        this.name = name;
    }

    public void OnTurnStarted(int turnNumber)
    {
        RecalculateYields();
        ourGameHex.ourGameBoard.game.playerDictionary[teamNum].AddGold(goldYield);
        ourGameHex.ourGameBoard.game.playerDictionary[teamNum].AddScience(scienceYield);
        ourGameHex.ourGameBoard.game.playerDictionary[teamNum].AddCulture(cultureYield);
        ourGameHex.ourGameBoard.game.playerDictionary[teamNum].AddHappiness(cultureYield);
        if(productionList.Any())
        {
            productionList[0].productionLeft -= productionYield;
            if(productionList[0].productionLeft <= 0)
            {
                if(productionList[0].prodType == ProductionType.Building)
                {
                    BuildOnHex(productionList[0].targetGameHex.hex, new Building(productionList[0].name));
                }
                else if(productionList[0].prodType == ProductionType.Unit)
                {
                    productionList[0].targetGameHex.SpawnUnit(new Unit(productionList[0].name, productionList[0].targetGameHex, teamNum), false, true);
                }
                productionList.RemoveAt(0);
            }
        }

        foodStockpile += foodYield;
        if (foodToGrow <= foodStockpile)
        {
            citySize += 1;
            foodStockpile = 0.0f;
        }
    }

    public void OnTurnEnded(int turnNumber)
    {

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
                goldYield -= building.maintenanceCost;
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
            District district = new District(ourGameHex, building, false, true, this);
            building.ourDistrict = district;
            districts.Add(district);
        }
        else
        {
            ourGameHex.ourGameBoard.gameHexDict[hex].district.AddBuilding(building);
            building.ourDistrict = ourGameHex.ourGameBoard.gameHexDict[hex].district;
        }

    }

    public void ExpandToHex(Hex hex)
    {
        Building ruralBuilding = GetRuralBuilding(hex);
        District district = new District(ourGameHex, ruralBuilding, false, false, this);
        ruralBuilding.ourDistrict = district;
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
            default:
                ruralBuilding = new Building("Sad Person");
                break;
        }
        return ruralBuilding;
    }

    public List<Hex> ValidExpandHexes(List<TerrainType> validTerrain)
    {
        List<Hex> validHexes = new();
        //gather valid targets
        foreach(Hex hex in ourGameHex.hex.Range(3))
        {
            if(validTerrain.Contains(ourGameHex.ourGameBoard.gameHexDict[hex].terrainType))
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
                            if(!ourGameHex.ourGameBoard.game.teamManager.GetAllies(teamNum).Contains(unit.teamNum))
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
        foreach(Hex hex in ourGameHex.hex.Range(3))
        {
            if(validTerrain.Contains(ourGameHex.ourGameBoard.gameHexDict[hex].terrainType))
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
                            if(!ourGameHex.ourGameBoard.game.teamManager.GetAllies(teamNum).Contains(unit.teamNum))
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
