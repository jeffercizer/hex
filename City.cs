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

    public ProductionQueueType(String name, BuildingType buildingType, UnitType unitType, GameHex targetGameHex, float productionCost, float productionLeft)
    {
        this.name = name;
        this.targetGameHex = targetGameHex;
        this.productionCost = productionCost;
        this.productionLeft = productionLeft;
        this.buildingType = buildingType;
        this.unitType = unitType;
    }
    public String name;
    public BuildingType buildingType;
    public UnitType unitType;
    public GameHex targetGameHex;
    public float productionLeft;
    public float productionCost;
}


[Serializable]
public class City
{
    public City(int id, int teamNum, String name, GameHex gameHex)
    {
        this.id = id;
        this.teamNum = teamNum;
        this.name = name;
        this.gameHex = gameHex;
        productionQueue = new();
        partialProductionDictionary = new();
        heldResources = new();
        heldHexes = new();
        gameHex.gameBoard.game.playerDictionary[teamNum].cityList.Add(this);
        districts = new();
        AddCityCenter();

        
        citySize = 0;
        readyToExpand = 0;
        foodToGrow = 10.0f;
        RecalculateYields();
        SetBaseHexYields();
    }
    public int id;
    public int teamNum;
    public String name;
    public List<District> districts;
    public GameHex gameHex;
    public Yields yields;
    public int citySize;
    public float foodToGrow;
    public float foodStockpile;
    public float productionOverflow;
    public Yields flatYields;
    public Yields roughYields;
    public Yields mountainYields;
    public Yields coastalYields;
    public Yields oceanYields;
    public Yields desertYields;
    public Yields plainsYields;
    public Yields grasslandYields;
    public Yields tundraYields;
    public Yields arcticYields;
    public List<ProductionQueueType> productionQueue;
    public Dictionary<string, ProductionQueueType> partialProductionDictionary;
    public Dictionary<Hex, ResourceType> heldResources;
    public HashSet<Hex> heldHexes;
    public int baseMaxResourcesHeld;
    public int maxResourcesHeld;
    public int readyToExpand;

    private District AddCityCenter()
    {
        Building building = new Building(BuildingType.CityCenter);
        District district = new District(gameHex, building, true, true, this);
        building.district = district;
        districts.Add(district);
        return district;
    }

    private void SetBaseHexYields()
    {
        flatYields = new();
        roughYields = new();
        mountainYields = new();
        coastalYields = new();
        oceanYields = new();
        desertYields = new();
        plainsYields = new();
        grasslandYields = new();
        tundraYields = new();
        arcticYields = new();
        
        flatYields.food += 1;
        roughYields.production += 1;
        //mountainYields.production += 0;
        coastalYields.food += 1;
        oceanYields.gold += 1;
        
        desertYields.gold += 1;
        plainsYields.production += 1;
        grasslandYields.food += 1;
        tundraYields.happiness += 1;
        //arcticYields
    }

    public (List<BuildingType>, List<UnitType>) GetProducables()
    {
        List<BuildingType> buildings = new();
        List<UnitType> units = new();
        foreach(BuildingType buildingType in gameHex.gameBoard.game.playerDictionary[teamNum].allowedBuildings)
        {
            int count = 0;
            if(buildingType != 0 & BuildingLoader.buildingsDict[buildingType].PerCity != 0 )
            {
                count = CountBuildingType(buildingType);
            }
            foreach(ProductionQueueType queueItem in productionQueue)
            {
                if (queueItem.buildingType == buildingType)
                {
                    count += 1;
                }
            }
            if(!gameHex.gameBoard.game.builtWonders.Contains(buildingType) & !BuildingLoader.buildingsDict[buildingType].Wonder)
            {
                if(count < BuildingLoader.buildingsDict[buildingType].PerCity)
                {
                    buildings.Add(buildingType);
                }
            }
        }

        foreach(UnitType unitType in gameHex.gameBoard.game.playerDictionary[teamNum].allowedUnits)
        {
            units.Add(unitType);
        }
        return (buildings, units);
    }

    public bool RemoveFromQueue(int index)
    {
        if(productionQueue.Count > index)
        {
            if(productionQueue[index].productionLeft < productionQueue[index].productionCost)
            {
                bool foundNewHome = false;
                for(int i = 0; i < productionQueue.Count; i++)
                {
                    if (productionQueue[i].name == productionQueue[index].name & productionQueue[i].productionLeft > productionQueue[index].productionLeft)
                    {
                        foundNewHome = true;
                        productionQueue[i] = productionQueue[index];
                        break;
                    }
                }
                if(!foundNewHome)
                {
                    partialProductionDictionary.Add(productionQueue[index].name, productionQueue[index]);
                }
            }
            productionQueue.RemoveAt(index);
            return true;
        }
        return false;
    }

    public int CountBuildingType(BuildingType buildingType)
    {
        int count = 0;
        foreach(District district in districts)
        {
            count += district.CountBuildingType(buildingType);
        }
        return count;
    }

    public bool AddToQueue(String name, BuildingType buildingType, UnitType unitType, GameHex targetGameHex, float productionCost)
    {
        int count = 0;
        if(buildingType != BuildingType.None && BuildingLoader.buildingsDict[buildingType].PerCity != 0 )
        {
            count = CountBuildingType(buildingType);
        }
        foreach(ProductionQueueType queueItem in productionQueue)
        {
            if (queueItem.buildingType == buildingType)
            {
                count += 1;
            }
        }
        if(buildingType != BuildingType.None && count >= BuildingLoader.buildingsDict[buildingType].PerCity)
        {
            return false;
        }
        if(gameHex.gameBoard.game.builtWonders.Contains(buildingType))
        {
            return false;
        }
        
        ProductionQueueType queueItem1;
        if(partialProductionDictionary.TryGetValue(name, out queueItem1))
        {
            partialProductionDictionary.Remove(name);
            productionQueue.Add(new ProductionQueueType(name, buildingType, unitType, targetGameHex, queueItem1.productionLeft, queueItem1.productionCost));
        }
        else
        {
            productionQueue.Add(new ProductionQueueType(name, buildingType, unitType, targetGameHex, productionCost, productionCost));
        }
        return true;
    }

    public bool ChangeTeam(int newTeamNum)
    {
        foreach (District district in districts)
        {
            district.BeforeSwitchTeam();
        }
        heldResources.Clear();
        teamNum = newTeamNum;
        foreach (District district in districts)
        {
            district.AfterSwitchTeam();
        }
        RecalculateYields();
        productionQueue = new();
        partialProductionDictionary = new();
        return true;
    }

    public void ChangeName(String name)
    {
        this.name = name;
    }

    public void OnTurnStarted(int turnNumber)
    {
        foreach(District district in districts)
        {
            district.HealForTurn(10.0f);
        }
        RecalculateYields();
        productionOverflow += yields.production;
        gameHex.gameBoard.game.playerDictionary[teamNum].AddGold(yields.gold);
        gameHex.gameBoard.game.playerDictionary[teamNum].AddScience(yields.science);
        gameHex.gameBoard.game.playerDictionary[teamNum].AddCulture(yields.culture);
        gameHex.gameBoard.game.playerDictionary[teamNum].AddHappiness(yields.happiness);
        
        foreach(ProductionQueueType queueItem in productionQueue)
        {
            if(gameHex.gameBoard.game.builtWonders.Contains(queueItem.buildingType))
            {
                productionQueue.Remove(queueItem);
                productionOverflow += queueItem.productionCost - queueItem.productionLeft;
            }
        }
        if(productionQueue.Any())
        {
            productionQueue[0].productionLeft -= productionOverflow;
            Math.Max(productionOverflow - productionQueue[0].productionLeft, 0);
            if(productionQueue[0].productionLeft <= 0)
            {
                if(productionQueue[0].buildingType > (BuildingType)0)
                {
                    BuildOnHex(productionQueue[0].targetGameHex.hex, new Building(productionQueue[0].buildingType));
                }
                else if(productionQueue[0].unitType > (UnitType)0)
                {
                    Unit tempUnit = new Unit(Enum.Parse<UnitType>(productionQueue[0].name), productionQueue[0].targetGameHex, teamNum);
                    if(tempUnit.baseCombatStrength > gameHex.gameBoard.game.playerDictionary[teamNum].strongestUnitBuilt)
                    {
                        gameHex.gameBoard.game.playerDictionary[teamNum].strongestUnitBuilt = tempUnit.baseCombatStrength;
                    }
                    if(!productionQueue[0].targetGameHex.SpawnUnit(tempUnit, false, true))
                    {
                        tempUnit.name = "Ghost Man";
                        tempUnit.decreaseCurrentHealth(99999.9f);
                    }
                }
                productionQueue.RemoveAt(0);
            }
        }

        foodStockpile += yields.food;
        if (foodToGrow <= foodStockpile)
        {
            readyToExpand += 1;
            foodStockpile = Math.Max(0.0f, foodStockpile - foodToGrow);
        }
    }

    public void OnTurnEnded(int turnNumber)
    {

    }

    public void DistrictFell()
    {
        bool allDistrictsFell = true;
        bool cityCenterOccupied = false;
        foreach(District district in districts)
        {
            if(district.currentHealth > 0.0f)
            {
                allDistrictsFell = false;
            }
            if(district.isCityCenter && district.currentHealth <= 0.0f)
            {
                if (district.gameHex.unitsList.Any())
                {
                    Unit unit = district.gameHex.unitsList[0];
                    if (gameHex.gameBoard.game.teamManager.GetEnemies(teamNum).Contains(unit.teamNum))
                    {
                        cityCenterOccupied = true;
                    }
                }
            }
        }
        if(allDistrictsFell && cityCenterOccupied)
        {
            ChangeTeam(gameHex.unitsList[0].teamNum);
        }
    }

    public void RecalculateYields()
    {
        yields = new();
        SetBaseHexYields();
        foreach(District district in districts)
        {
            district.PrepareYieldRecalculate();            
        }
        foreach(ResourceType resource in heldResources.Values)
        {
            ResourceInfo resourceInfo = ResourceLoader.resources[resource];
        }
        foreach(ResourceType resource in heldResources.Values)
        {
            ResourceLoader.ExecuteResourceEffect(resource);
        }
        foreach(District district in districts)
        {
            district.RecalculateYields();
            yields += district.gameHex.yields;
        }
    }

    public void BuildOnHex(Hex hex, Building building)
    {
        if(gameHex.gameBoard.gameHexDict[hex].district == null)
        {
            District district = new District(gameHex, building, false, true, this);
            building.district = district;
            districts.Add(district);
        }
        else
        {
            gameHex.gameBoard.gameHexDict[hex].district.AddBuilding(building);
            building.district = gameHex.gameBoard.gameHexDict[hex].district;
            gameHex.gameBoard.gameHexDict[hex].district.isUrban = true;
        }
    }

    public void BuildDefenseOnHex(Hex hex, Building building)
    {
        if(gameHex.gameBoard.gameHexDict[hex].district != null)
        {
            gameHex.gameBoard.gameHexDict[hex].district.AddDefense(building);
            building.district = gameHex.gameBoard.gameHexDict[hex].district;
        }
    }

    public void ExpandToHex(Hex hex)
    {
        if(readyToExpand > 0)
        {
            District district = new District(gameHex, false, false, this);
            districts.Add(district);
            readyToExpand -= 1;
        }
        else
        {
            Console.WriteLine("tried to expand without readyToExpand > 0");
        }
    }

    //valid hexes for a rural district
    public List<Hex> ValidExpandHexes(List<TerrainType> validTerrain)
    {
        List<Hex> validHexes = new();
        //gather valid targets
        foreach(Hex hex in gameHex.hex.WrappingRange(3, gameHex.gameBoard.left, gameHex.gameBoard.right, gameHex.gameBoard.top, gameHex.gameBoard.bottom))
        {
            if(validTerrain.Contains(gameHex.gameBoard.gameHexDict[hex].terrainType))
            {
                //hex is unowned or owned by us so continue
                if(gameHex.gameBoard.gameHexDict[hex].ownedBy == -1 | gameHex.gameBoard.gameHexDict[hex].ownedBy == teamNum)
                {
                    //hex does not have a district
                    if (gameHex.gameBoard.gameHexDict[hex].district == null)
                    {
                        //hex has only allies unit
                        bool valid = true;
                        foreach(Unit unit in gameHex.gameBoard.gameHexDict[hex].unitsList)
                        {
                            if(!gameHex.gameBoard.game.teamManager.GetAllies(teamNum).Contains(unit.teamNum))
                            {
                                valid = false;
                            }
                        }
                        bool adjacentDistrict = false;
                        foreach(Hex hex2 in gameHex.hex.WrappingNeighbors(gameHex.gameBoard.left, gameHex.gameBoard.right))
                        {
                            if(gameHex.gameBoard.gameHexDict[hex2].district != null)
                            {
                                adjacentDistrict = true;
                                break;
                            }
                        }
                        if(valid && adjacentDistrict)
                        {
                            validHexes.Add(hex);
                        }
                    }
                }
            }
        }
        return validHexes;
    }
    
    //valid hexes to build a district or build a new building on one
    public List<Hex> ValidUrbanBuildHexes(List<TerrainType> validTerrain)
    {
        List<Hex> validHexes = new();
        //gather valid targets
        foreach(Hex hex in gameHex.hex.WrappingRange(3, gameHex.gameBoard.left, gameHex.gameBoard.right, gameHex.gameBoard.top, gameHex.gameBoard.bottom))
        {
            if(validTerrain.Contains(gameHex.gameBoard.gameHexDict[hex].terrainType))
            {
                //hex is unowned or owned by us so continue
                if(gameHex.gameBoard.gameHexDict[hex].ownedBy == -1 | gameHex.gameBoard.gameHexDict[hex].ownedBy == teamNum)
                {
                    //hex does not have a district or it is not urban or has less than the max buildings TODO
                    if (gameHex.gameBoard.gameHexDict[hex].district == null | !gameHex.gameBoard.gameHexDict[hex].district.isUrban | gameHex.gameBoard.gameHexDict[hex].district.buildings.Count() < gameHex.gameBoard.gameHexDict[hex].district.maxBuildings)
                    {
                        //hex doesnt have a non-friendly unit
                        bool noEnemyUnit = true;
                        foreach(Unit unit in gameHex.gameBoard.gameHexDict[hex].unitsList)
                        {
                            if(!gameHex.gameBoard.game.teamManager.GetAllies(teamNum).Contains(unit.teamNum))
                            {
                                noEnemyUnit = false;
                                break;
                            }
                        }
                        bool adjacentUrbanDistrict = false;
                        foreach(Hex hex2 in gameHex.hex.WrappingNeighbors(gameHex.gameBoard.left, gameHex.gameBoard.right))
                        {
                            if(gameHex.gameBoard.gameHexDict[hex2].district != null && gameHex.gameBoard.gameHexDict[hex].district.isUrban)
                            {
                                adjacentUrbanDistrict = true;
                                break;
                            }
                        }
                                
                        if(noEnemyUnit && adjacentUrbanDistrict)
                        {
                            validHexes.Add(hex);
                        }
                    }
                }
            }
        }
        return validHexes;
    }

    public List<Hex> ValidDefensiveBuildHexes(List<TerrainType> validTerrain)
    {
        List<Hex> validHexes = new();
        //gather valid targets
        foreach(Hex hex in gameHex.hex.WrappingRange(3, gameHex.gameBoard.left, gameHex.gameBoard.right, gameHex.gameBoard.top, gameHex.gameBoard.bottom))
        {
            if(validTerrain.Contains(gameHex.gameBoard.gameHexDict[hex].terrainType))
            {
                //hex is owned by us so continue
                if(gameHex.gameBoard.gameHexDict[hex].ownedBy == teamNum)
                {
                    //hex has a district with less than the max defenses TODO
                    if (gameHex.gameBoard.gameHexDict[hex].district != null & gameHex.gameBoard.gameHexDict[hex].district.defenses.Count() < gameHex.gameBoard.gameHexDict[hex].district.maxDefenses)
                    {
                        //hex doesnt have a non-friendly unit
                        bool valid = true;
                        foreach(Unit unit in gameHex.gameBoard.gameHexDict[hex].unitsList)
                        {
                            if(!gameHex.gameBoard.game.teamManager.GetAllies(teamNum).Contains(unit.teamNum))
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
    // For terrain types
    public void AddFlatYields(GameHex gameHex)
    {
        gameHex.yields += flatYields;
    }
    
    public void AddRoughYields(GameHex gameHex)
    {
        gameHex.yields += roughYields;
    }
    
    public void AddMountainYields(GameHex gameHex)
    {
        gameHex.yields += mountainYields;
    }
    
    public void AddCoastYields(GameHex gameHex)
    {
        gameHex.yields += coastalYields;
    }
    
    public void AddOceanYields(GameHex gameHex)
    {
        gameHex.yields += oceanYields;
    }
    
    // For terrain temperatures
    public void AddDesertYields(GameHex gameHex)
    {
        gameHex.yields += desertYields;
    }
    
    public void AddPlainsYields(GameHex gameHex)
    {
        gameHex.yields += plainsYields;
    }
    
    public void AddGrasslandYields(GameHex gameHex)
    {
        gameHex.yields += grasslandYields;
    }
    
    public void AddTundraYields(GameHex gameHex)
    {
        gameHex.yields += tundraYields;
    }
    
    public void AddArcticYields(GameHex gameHex)
    {
        gameHex.yields += arcticYields;
    }

}
