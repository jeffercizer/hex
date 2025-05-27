using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;
using Godot;
using System.Reflection;
using NetworkMessages;
using System.IO;

public enum ProductionType
{
    Building,
    Unit,
}

[Serializable]
public class ProductionQueueType
{
    public String name { get; set; }
    public String buildingType { get; set; }
    public String unitType { get; set; }
    public GameHex targetGameHex { get; set; }
    public float productionLeft { get; set; }
    public float productionCost { get; set; }
    public String productionIconPath { get; set; }
    public ProductionQueueType(String name, String buildingType, String unitType, GameHex targetGameHex, float productionCost, float productionLeft)
    {
        this.name = name;
        this.targetGameHex = targetGameHex;
        this.productionCost = productionCost;
        this.productionLeft = productionLeft;
        this.buildingType = buildingType;
        this.unitType = unitType;
        if(unitType != "")
        {
            this.productionIconPath = UnitLoader.unitsDict[unitType].IconPath;
        }
        else if (buildingType != "")
        {
            this.productionIconPath = BuildingLoader.buildingsDict[buildingType].IconPath;
        }

    }

    public override bool Equals(object obj)
    {
        if (obj is ProductionQueueType)
        {
            if(((ProductionQueueType)obj).name == name && ((ProductionQueueType)obj).productionLeft == productionLeft && ((ProductionQueueType)obj).targetGameHex == targetGameHex && ((ProductionQueueType)obj).buildingType == buildingType && ((ProductionQueueType)obj).unitType == unitType)
            {
                return true;
            }
        }
        return false;
    }

    public ProductionQueueType()
    {

    }

    public void Serialize(BinaryWriter writer)
    {
        Serializer.Serialize(writer, this);
    }

    public static ProductionQueueType Deserialize(BinaryReader reader)
    {
        return Serializer.Deserialize<ProductionQueueType>(reader);
    }

}


[Serializable]
public class City
{
    public City(int id, int teamNum, String name, bool isCapital, GameHex gameHex)
    {
        Global.gameManager.game.cityDictionary.Add(id, this);
        this.id = id;
        originalCapitalTeamID = id;
        this.teamNum = teamNum;
        this.name = name;
        this.hex = gameHex.hex;
        productionQueue = new();
        partialProductionDictionary = new();
        heldResources = new();
        heldHexes = new();
        Global.gameManager.game.playerDictionary[teamNum].cityList.Add(this);
        districts = new();
        citySize = 0;
        naturalPopulation = 0;
        readyToExpand = 0;
        maxDistrictSize = 2;
        foodToGrow = GetFoodToGrowCost();

        if (Global.gameManager.game.TryGetGraphicManager(out GraphicManager manager))
        {
            manager.NewCity(this);
        }

        AddCityCenter(isCapital);
        this.isCapital = isCapital;
        this.wasCapital = isCapital;

        RecalculateYields();
        SetBaseHexYields();

    }
    public int id { get; set; }
    public int teamNum { get; set; }
    public String name { get; set; }
    public bool isCapital { get; set; }
    public bool wasCapital { get; set; }
    public int originalCapitalTeamID { get; set; }
    public int maxDistrictSize { get; set; }
    public List<District> districts { get; set; } = new();
    public Hex hex { get; set; }
    public Yields yields { get; set; }
    public int citySize { get; set; }
    public int naturalPopulation { get; set; }
    public float foodToGrow{ get; set; }
    public float foodStockpile{ get; set; }
    public float productionOverflow{ get; set; }
    public Yields flatYields{ get; set; }
    public Yields roughYields{ get; set; }
    public Yields mountainYields{ get; set; }
    public Yields coastalYields{ get; set; }
    public Yields oceanYields{ get; set; }
    public Yields desertYields{ get; set; }
    public Yields plainsYields{ get; set; }
    public Yields grasslandYields{ get; set; }
    public Yields tundraYields{ get; set; }
    public Yields arcticYields{ get; set; }
    public List<ProductionQueueType> productionQueue{ get; set; } = new();
    public Dictionary<string, ProductionQueueType> partialProductionDictionary{ get; set; } = new();
    public Dictionary<Hex, ResourceType> heldResources{ get; set; } = new();
    public HashSet<Hex> heldHexes{ get; set; } = new();
    public int baseMaxResourcesHeld{ get; set; }
    public int maxResourcesHeld{ get; set; }
    public int readyToExpand{ get; set; }

    private void AddCityCenter(bool isCapital)
    {
        District district;
        if (isCapital)
        {
            district = new District(Global.gameManager.game.mainGameBoard.gameHexDict[hex], "Palace", true, true, id);
        }
        else
        {
            district = new District(Global.gameManager.game.mainGameBoard.gameHexDict[hex], "CityCenter", true, true, id);
        }
        districts.Add(district);
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

    public (List<String>, List<String>) GetProducables()
    {
        List<String> buildings = new();
        List<String> units = new();
        foreach(String buildingType in Global.gameManager.game.playerDictionary[teamNum].allowedBuildings)
        {
            int count = 0;
            if(buildingType != "" & BuildingLoader.buildingsDict[buildingType].PerCity != 0 )
            {
                count = CountString(buildingType);
            }
            foreach(ProductionQueueType queueItem in productionQueue)
            {
                if (queueItem.buildingType == buildingType)
                {
                    count += 1;
                }
            }
            if(!Global.gameManager.game.builtWonders.Contains(buildingType) & !BuildingLoader.buildingsDict[buildingType].Wonder)
            {
                if(count < BuildingLoader.buildingsDict[buildingType].PerCity)
                {
                    buildings.Add(buildingType);
                }
            }
        }

        foreach(String unitType in Global.gameManager.game.playerDictionary[teamNum].allowedUnits)
        {
            units.Add(unitType);
        }
        return (buildings, units);
    }

    public bool RemoveFromQueue(ProductionQueueType productionQueueItem)
    {
        return RemoveFromQueue(productionQueue.IndexOf(productionQueueItem));
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

            if (Global.gameManager.game.TryGetGraphicManager(out GraphicManager manager))
            {
                manager.uiManager.cityInfoPanel.UpdateCityPanelInfo();
                manager.UpdateGraphic(id, GraphicUpdateType.Update);
            }
            return true;
        }
        return false;
    }

    public bool MoveToFrontOfProductionQueue(int indexToMove)
    {
        if (indexToMove < 0 || indexToMove >= productionQueue.Count)
        {
            GD.PushWarning("Index out of bounds");
            return false;
        }
        ProductionQueueType item = productionQueue[indexToMove];
        int frontalItemIndex = indexToMove;
        for (int i = 0; i < productionQueue.Count; i++)
        {
            if (productionQueue[i].name == productionQueue[indexToMove].name & productionQueue[i].productionLeft < productionQueue[indexToMove].productionLeft)
            {
                frontalItemIndex = i;
            }
        }

        //now we have frontalItemIndex, the index of the most finished item of our type and the index of the item we want to move, so we move our target item to the most finished item slot and move the most finished to the front
        //store bestItem, replace it with target, remove target, insert bestItem
        ProductionQueueType bestItem = productionQueue[frontalItemIndex];
        productionQueue[frontalItemIndex] = item;
        productionQueue.RemoveAt(indexToMove);
        productionQueue.Insert(0, bestItem);

        //RemoveFromQueue(indexToMove);
        //AddToFrontOfQueue(item.name, item.buildingType, item.unitType, item.targetGameHex, item.productionCost);
        if (Global.gameManager.game.TryGetGraphicManager(out GraphicManager manager))
        {
            manager.uiManager.cityInfoPanel.UpdateCityPanelInfo();
            manager.UpdateGraphic(id, GraphicUpdateType.Update);
        }
        return true;
    }

    public int CountString(String buildingType)
    {
        int count = 0;
        foreach(District district in districts)
        {
            count += district.CountString(buildingType);
        }
        return count;
    }

    public bool AddUnitToQueue(String unitType)
    {
        return AddToQueue(unitType, "", unitType, Global.gameManager.game.mainGameBoard.gameHexDict[hex], UnitLoader.unitsDict[unitType].ProductionCost);
    }

    public bool AddBuildingToQueue(String buildingType, GameHex targetGameHex)
    { 
        return AddToQueue(buildingType, buildingType, "", targetGameHex, BuildingLoader.buildingsDict[buildingType].ProductionCost);
    }

    public bool AddToQueue(String name, String buildingType, String unitType, GameHex targetGameHex, float productionCost)
    {
        int count = 0;
        if (buildingType != "" && BuildingLoader.buildingsDict[buildingType].PerCity != 0)
        {
            count = CountString(buildingType);
            foreach (ProductionQueueType queueItem in productionQueue)
            {
                if (queueItem.buildingType == buildingType)
                {
                    count += 1;
                }
            }
            if (buildingType != "" && (count >= BuildingLoader.buildingsDict[buildingType].PerCity))
            {
                return false;
            }
        }
        if(Global.gameManager.game.builtWonders.Contains(buildingType))
        {
            return false;
        }
        ProductionQueueType queueItem1;
        if(partialProductionDictionary.TryGetValue(name, out queueItem1))
        {
            partialProductionDictionary.Remove(name);
            productionQueue.Add(new ProductionQueueType(name, buildingType, unitType, targetGameHex, queueItem1.productionCost, queueItem1.productionLeft));
        }
        else
        {
            productionQueue.Add(new ProductionQueueType(name, buildingType, unitType, targetGameHex, productionCost, productionCost));
        }
        if (Global.gameManager.game.TryGetGraphicManager(out GraphicManager manager))
        {
            manager.uiManager.cityInfoPanel.UpdateCityPanelInfo();
            manager.UpdateGraphic(id, GraphicUpdateType.Update);
        }
        return true;
    }

    public bool AddToFrontOfQueue(String name, String buildingType, String unitType, GameHex targetGameHex, float productionCost)
    {
        int count = 0;
        if (buildingType != "" && BuildingLoader.buildingsDict[buildingType].PerCity != 0)
        {
            count = CountString(buildingType);
        }
        foreach (ProductionQueueType queueItem in productionQueue)
        {
            if (queueItem.buildingType == buildingType)
            {
                count += 1;
            }
        }
        if (buildingType != "" && count >= BuildingLoader.buildingsDict[buildingType].PerCity)
        {
            return false;
        }
        if (Global.gameManager.game.builtWonders.Contains(buildingType))
        {
            return false;
        }
        ProductionQueueType queueItem1;
        if (partialProductionDictionary.TryGetValue(name, out queueItem1))
        {
            partialProductionDictionary.Remove(name);
            productionQueue.Insert(0, new ProductionQueueType(name, buildingType, unitType, targetGameHex, queueItem1.productionCost, queueItem1.productionLeft));
        }
        else
        {
            productionQueue.Insert(0, new ProductionQueueType(name, buildingType, unitType, targetGameHex, productionCost, productionCost));
        }
        if (Global.gameManager.game.TryGetGraphicManager(out GraphicManager manager))
        {
            manager.uiManager.cityInfoPanel.UpdateCityPanelInfo();
            manager.UpdateGraphic(id, GraphicUpdateType.Update);
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
        if (Global.gameManager.game.TryGetGraphicManager(out GraphicManager manager))
        {
            if(manager.selectedObjectID == id)
            {
                manager.UnselectObject();
            }
            manager.UpdateGraphic(id, GraphicUpdateType.Update);
        }
        return true;
    }

    public void ChangeName(String name)
    {
        this.name = name;
        if (Global.gameManager.game.TryGetGraphicManager(out GraphicManager manager))
        {
            manager.uiManager.cityInfoPanel.UpdateCityPanelInfo();
            manager.UpdateGraphic(id, GraphicUpdateType.Update);
        }
    }

    public void OnTurnStarted(int turnNumber)
    {
        foreach(District district in districts)
        {
            district.HealForTurn(10.0f);
        }
        RecalculateYields();
        productionOverflow += yields.production;
        Global.gameManager.game.playerDictionary[teamNum].AddGold(yields.gold);
        Global.gameManager.game.playerDictionary[teamNum].AddScience(yields.science);
        Global.gameManager.game.playerDictionary[teamNum].AddCulture(yields.culture);
        Global.gameManager.game.playerDictionary[teamNum].AddHappiness(yields.happiness);
        Global.gameManager.game.playerDictionary[teamNum].AddInfluence(yields.influence);
        List<ProductionQueueType> toRemove = new List<ProductionQueueType>();
        for (int i = 0; i < productionQueue.Count; i++)
        {
            ProductionQueueType queueItem = productionQueue[i];

            if (Global.gameManager.game.builtWonders.Contains(queueItem.buildingType))
            {
                productionQueue.Remove(queueItem);
                productionOverflow += queueItem.productionCost - queueItem.productionLeft;
            }
            int count = 0;
            if (queueItem.buildingType != "" && BuildingLoader.buildingsDict[queueItem.buildingType].PerCity != 0)
            {
                count = CountString(queueItem.buildingType);
                foreach (ProductionQueueType queueItem2 in productionQueue)
                {
                    if (!toRemove.Contains(queueItem2))
                    {
                        if (queueItem2.buildingType == queueItem.buildingType)
                        {
                            count += 1;
                        }
                    }
                }
                if (count >= BuildingLoader.buildingsDict[queueItem.buildingType].PerCity)
                {
                    toRemove.Add(queueItem);
                    continue;
                }
            }
            if (queueItem.buildingType != "")
            {
                //hex doesnt have a enemy unit
                bool enemyUnitPresent = false;
                foreach (Unit unit in queueItem.targetGameHex.units)
                {
                    if (Global.gameManager.game.teamManager.GetEnemies(teamNum).Contains(unit.teamNum))
                    {
                        enemyUnitPresent = true;
                        break;
                    }
                }
                if (enemyUnitPresent)
                {
                    toRemove.Add(queueItem);
                    continue;
                }
                if (!ValidUrbanBuildHex(BuildingLoader.buildingsDict[queueItem.buildingType].TerrainTypes, queueItem.targetGameHex))
                {
                    toRemove.Add(queueItem);
                    continue;
                }
            }
        }
        foreach (ProductionQueueType item in toRemove)
        {
            RemoveFromQueue(item);
        }
        if (productionQueue.Any())
        {
            float productionLeftTemp = productionQueue[0].productionLeft;
            productionQueue[0].productionLeft -= productionOverflow;
            productionOverflow = Math.Max(productionOverflow - productionLeftTemp, 0);
            if (productionQueue[0].productionLeft <= 0)
            {
                if (productionQueue[0].buildingType != "")
                {
                    GD.Print("Build");
                    BuildOnHex(productionQueue[0].targetGameHex.hex, productionQueue[0].buildingType);
                }
                else if (productionQueue[0].unitType != "")
                {
                    GD.Print("spawn");
                    Unit tempUnit = new Unit(productionQueue[0].name, Global.gameManager.game.GetUniqueID(), teamNum);
                    if (!productionQueue[0].targetGameHex.SpawnUnit(tempUnit, false, true))
                    {
                        tempUnit.name = "Ghost Man";
                        tempUnit.decreaseHealth(99999.9f);
                        if (Global.gameManager.game.TryGetGraphicManager(out GraphicManager manager1)) manager1.NewUnit(tempUnit);
                    }
                    if (UnitLoader.unitsDict.TryGetValue(tempUnit.unitType, out UnitInfo unitInfo))
                    {
                        if (unitInfo.CombatPower > Global.gameManager.game.playerDictionary[teamNum].strongestUnitBuilt)
                        {
                            Global.gameManager.game.playerDictionary[teamNum].strongestUnitBuilt = unitInfo.CombatPower;
                        }
                    }

                }
                productionQueue.RemoveAt(0);
            }
        }

        foodStockpile += yields.food;
        if (foodToGrow <= foodStockpile)
        {
            naturalPopulation += 1; //we increase naturalPopulation only here, and city size is increased for every building we have, rural or urban
            readyToExpand += 1;
            if (Global.gameManager.game.TryGetGraphicManager(out GraphicManager manager2)) manager2.Update2DUI(UIElement.endTurnButton);
            foodStockpile = Math.Max(0.0f, foodStockpile - foodToGrow);
            foodToGrow = GetFoodToGrowCost(); //30 + (n-1) x 3 + (n-1) ^ 3.0 we use naturalPopulation so we dont punish the placement of urban buildings
        }
        if (Global.gameManager.game.TryGetGraphicManager(out GraphicManager manager)) manager.UpdateGraphic(id, GraphicUpdateType.Update);
    }

    public float GetFoodToGrowCost()
    {
        //15 + (n - 1) x 8 + (n - 1) ^ 1.5
        //30 + (n - 1) x 3 + (n - 1) ^ 3.0
        return (float)(15 + (naturalPopulation) * 8 + Math.Pow((naturalPopulation), 1.5));
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
            if(district.health > 0.0f)
            {
                allDistrictsFell = false;
            }
            if(district.isCityCenter && district.health <= 0.0f)
            {
                if (Global.gameManager.game.mainGameBoard.gameHexDict[district.hex].units.Any())
                {
                    Unit unit = Global.gameManager.game.mainGameBoard.gameHexDict[district.hex].units[0];
                    if (Global.gameManager.game.teamManager.GetEnemies(teamNum).Contains(unit.teamNum))
                    {
                        cityCenterOccupied = true;
                    }
                }
            }
        }
        if(allDistrictsFell && cityCenterOccupied)
        {
            ChangeTeam(Global.gameManager.game.mainGameBoard.gameHexDict[hex].units[0].teamNum);
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
            yields += Global.gameManager.game.mainGameBoard.gameHexDict[district.hex].yields;
        }
        yields.food -= naturalPopulation;
        if (Global.gameManager.game.TryGetGraphicManager(out GraphicManager manager))
        {
            manager.Update2DUI(UIElement.goldPerTurn);
            manager.Update2DUI(UIElement.sciencePerTurn);
            manager.Update2DUI(UIElement.culturePerTurn);
            manager.Update2DUI(UIElement.happinessPerTurn);
            manager.Update2DUI(UIElement.influencePerTurn);
            manager.UpdateGraphic(id, GraphicUpdateType.Update);
            manager.uiManager.cityInfoPanel.UpdateCityPanelInfo();
        }
    }

    public void BuildOnHex(Hex hex, String buildingType)
    {
        if(Global.gameManager.game.mainGameBoard.gameHexDict[hex].district == null)
        {
            District district = new District(Global.gameManager.game.mainGameBoard.gameHexDict[hex], buildingType, false, true, id);
            districts.Add(district);
        }
        else
        {
            Building building = new Building(buildingType, Global.gameManager.game.mainGameBoard.gameHexDict[hex].district.hex);
            Global.gameManager.game.mainGameBoard.gameHexDict[hex].district.AddBuilding(building);
            Global.gameManager.game.mainGameBoard.gameHexDict[hex].district.isUrban = true;
        }
    }

    public void BuildDefenseOnHex(Hex hex, Building building)
    {
        if(Global.gameManager.game.mainGameBoard.gameHexDict[hex].district != null)
        {
            Global.gameManager.game.mainGameBoard.gameHexDict[hex].district.AddDefense(building);
            building.districtHex = Global.gameManager.game.mainGameBoard.gameHexDict[hex].district.hex;
        }
    }

    public void ExpandToHex(Hex hex)
    {
        if(readyToExpand > 0)
        {
            District district = new District(Global.gameManager.game.mainGameBoard.gameHexDict[hex], false, false, id);
            districts.Add(district);
            readyToExpand -= 1;
            if (Global.gameManager.game.TryGetGraphicManager(out GraphicManager manager))
            {
                manager.UpdateGraphic(id, GraphicUpdateType.Update);
                manager.ClearWaitForTarget();
            }
        }
        else
        {
            GD.PushWarning("tried to expand without readyToExpand > 0");
        }
    }

    public void DevelopDistrict(Hex hex)
    {
        District targetDistrict = Global.gameManager.game.mainGameBoard.gameHexDict[hex].district;
        if (targetDistrict != null)
        {
            if(targetDistrict.cityID == this.id)
            {
                targetDistrict.DevelopDistrict();
                readyToExpand -= 1;
                if (Global.gameManager.game.TryGetGraphicManager(out GraphicManager manager))
                {
                    manager.ClearWaitForTarget();
                }
            }
        }
    }

    public bool ValidExpandHex(List<TerrainType> validTerrain, GameHex targetGameHex)
    {
        if (validTerrain.Count == 0 || validTerrain.Contains(targetGameHex.terrainType))
        {
            //hex is owned by us so continue
            if (targetGameHex.ownedBy == -1 | targetGameHex.ownedBy == teamNum)
            {
                //hex does not have a district
                if (targetGameHex.district == null)
                {
                    //hex doesnt have a enemy unit
                    bool noEnemyUnit = true;
                    foreach (Unit unit in targetGameHex.units)
                    {
                        if (Global.gameManager.game.teamManager.GetEnemies(teamNum).Contains(unit.teamNum))
                        {
                            noEnemyUnit = false;
                            break;
                        }
                    }
                    bool adjacentDistrict = false;
                    foreach (Hex hex in targetGameHex.hex.WrappingNeighbors(Global.gameManager.game.mainGameBoard.left, Global.gameManager.game.mainGameBoard.right, Global.gameManager.game.mainGameBoard.bottom))
                    {
                        if (Global.gameManager.game.mainGameBoard.gameHexDict[hex].district != null && Global.gameManager.game.mainGameBoard.gameHexDict[hex].district.cityID == id)
                        {
                            adjacentDistrict = true;
                            break;
                        }
                    }
                    if (noEnemyUnit && adjacentDistrict)
                    {
                        return true;

                    }
                }
            }
        }
        return false;
    }
    //valid hexes for a rural district
    public List<Hex> ValidExpandHexes(List<TerrainType> validTerrain, int range = 3)
    {
        List<Hex> validHexes = new();
        //gather valid targets
        foreach(Hex hex in hex.WrappingRange(range, Global.gameManager.game.mainGameBoard.left, Global.gameManager.game.mainGameBoard.right, Global.gameManager.game.mainGameBoard.top, Global.gameManager.game.mainGameBoard.bottom))
        {
            if(ValidExpandHex(validTerrain, Global.gameManager.game.mainGameBoard.gameHexDict[hex]))
            {
                validHexes.Add(hex);
            }
        }
        return validHexes;
    }
    

    public bool ValidUrbanBuildHex(List<TerrainType> validTerrain, GameHex targetGameHex)
    {
        if (validTerrain.Count == 0 || validTerrain.Contains(targetGameHex.terrainType))
        {
            //hex is owned by us so continue
            if (targetGameHex.ownedBy == teamNum)
            {
                //hex has less than the max buildings
                if (targetGameHex.district != null && targetGameHex.district.buildings.Count() < targetGameHex.district.maxBuildings && targetGameHex.district.cityID == id)
                {
                    //hex doesnt have a enemy unit
                    bool noEnemyUnit = true;
                    foreach (Unit unit in targetGameHex.units)
                    {
                        if (Global.gameManager.game.teamManager.GetEnemies(teamNum).Contains(unit.teamNum))
                        {
                            noEnemyUnit = false;
                            break;
                        }
                    } 
                    if (noEnemyUnit)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }
    //valid hexes to build a building
    public List<Hex> ValidUrbanBuildHexes(List<TerrainType> validTerrain, int range=3)
    {
        List<Hex> validHexes = new();
        //gather valid targets
        foreach(Hex hex in hex.WrappingRange(range, Global.gameManager.game.mainGameBoard.left, Global.gameManager.game.mainGameBoard.right, Global.gameManager.game.mainGameBoard.top, Global.gameManager.game.mainGameBoard.bottom))
        {
            if(ValidUrbanBuildHex(validTerrain, Global.gameManager.game.mainGameBoard.gameHexDict[hex]))
            {
                validHexes.Add(hex);
            }
        }
        return validHexes;
    }

    public bool ValidUrbanExpandHex(List<TerrainType> validTerrain, GameHex targetGameHex)
    {
        if (validTerrain.Count == 0 || validTerrain.Contains(targetGameHex.terrainType))
        {
            //hex is owned by us so continue
            if (targetGameHex.ownedBy == teamNum)
            {
                if((targetGameHex.district != null))
                {
                    GD.Print((targetGameHex.district != null) + " | " + (targetGameHex.district.maxBuildings < maxDistrictSize) + " | " + targetGameHex.district.maxBuildings.ToString());
                    foreach(Building building in targetGameHex.district.buildings)
                    {
                        GD.Print(building.name);
                    }
                }
                //hex does have a rural district
                if (targetGameHex.district != null && !targetGameHex.district.isUrban)
                {
                    //hex doesnt have a enemy unit
                    bool noEnemyUnit = true;
                    foreach (Unit unit in targetGameHex.units)
                    {
                        if (Global.gameManager.game.teamManager.GetEnemies(teamNum).Contains(unit.teamNum))
                        {
                            noEnemyUnit = false;
                            break;
                        }
                    }
                    //have an adjacent urban district
                    bool adjacentUrbanDistrict = false;
                    foreach (Hex hex in targetGameHex.hex.WrappingNeighbors(Global.gameManager.game.mainGameBoard.left, Global.gameManager.game.mainGameBoard.right, Global.gameManager.game.mainGameBoard.bottom))
                    {
                        if (Global.gameManager.game.mainGameBoard.gameHexDict[hex].district != null && Global.gameManager.game.mainGameBoard.gameHexDict[hex].district.isUrban && Global.gameManager.game.mainGameBoard.gameHexDict[hex].district.cityID == id)
                        {
                            adjacentUrbanDistrict = true;
                            break;
                        }
                    }
                    if (noEnemyUnit && adjacentUrbanDistrict)
                    {
                        return true;
                    }
                }
                //hex has a urban district with space to expand
                
                else if (targetGameHex.district != null && targetGameHex.district.isUrban && targetGameHex.district.maxBuildings < maxDistrictSize)
                {
                    GD.Print("has district with space");
                    //hex doesnt have a enemy unit
                    bool noEnemyUnit = true;
                    foreach (Unit unit in targetGameHex.units)
                    {
                        if (Global.gameManager.game.teamManager.GetEnemies(teamNum).Contains(unit.teamNum))
                        {
                            noEnemyUnit = false;
                            break;
                        }
                    }
                    //have an adjacent urban district
                    bool adjacentUrbanDistrict = false;
                    foreach (Hex hex in targetGameHex.hex.WrappingNeighbors(Global.gameManager.game.mainGameBoard.left, Global.gameManager.game.mainGameBoard.right, Global.gameManager.game.mainGameBoard.bottom))
                    {
                        if (Global.gameManager.game.mainGameBoard.gameHexDict[hex].district != null && Global.gameManager.game.mainGameBoard.gameHexDict[hex].district.isUrban && Global.gameManager.game.mainGameBoard.gameHexDict[hex].district.cityID == id)
                        {
                            adjacentUrbanDistrict = true;
                            break;
                        }
                    }
                    if (noEnemyUnit && adjacentUrbanDistrict)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    //valid hex to build an urban district or expand one
    public List<Hex> ValidUrbanExpandHexes(List<TerrainType> validTerrain, int range=3)
    {
        List<Hex> validHexes = new();
        //gather valid targets
        foreach (Hex hex in hex.WrappingRange(range, Global.gameManager.game.mainGameBoard.left, Global.gameManager.game.mainGameBoard.right, Global.gameManager.game.mainGameBoard.top, Global.gameManager.game.mainGameBoard.bottom))
        {
            if (ValidUrbanExpandHex(validTerrain, Global.gameManager.game.mainGameBoard.gameHexDict[hex]))
            {
                validHexes.Add(hex);
            }
        }
        return validHexes;
    }

    public List<Hex> ValidDefensiveBuildHexes(List<TerrainType> validTerrain)
    {
        List<Hex> validHexes = new();
        //gather valid targets
        foreach(Hex hex in hex.WrappingRange(3, Global.gameManager.game.mainGameBoard.left, Global.gameManager.game.mainGameBoard.right, Global.gameManager.game.mainGameBoard.top, Global.gameManager.game.mainGameBoard.bottom))
        {
            if(validTerrain.Contains(Global.gameManager.game.mainGameBoard.gameHexDict[hex].terrainType))
            {
                //hex is owned by us so continue
                if(Global.gameManager.game.mainGameBoard.gameHexDict[hex].ownedBy == teamNum)
                {
                    //hex has a district with less than the max defenses TODO
                    if (Global.gameManager.game.mainGameBoard.gameHexDict[hex].district != null & Global.gameManager.game.mainGameBoard.gameHexDict[hex].district.defenses.Count() < Global.gameManager.game.mainGameBoard.gameHexDict[hex].district.maxDefenses && Global.gameManager.game.mainGameBoard.gameHexDict[hex].district.cityID == id)
                    {
                        //hex doesnt have a non-friendly unit
                        bool valid = true;
                        foreach(Unit unit in Global.gameManager.game.mainGameBoard.gameHexDict[hex].units)
                        {
                            if(!Global.gameManager.game.teamManager.GetAllies(teamNum).Contains(unit.teamNum))
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

    public City()
    {
    }

    public void Serialize(BinaryWriter writer)
    {
        Serializer.Serialize(writer, this);
    }

    public static City Deserialize(BinaryReader reader)
    {
        return Serializer.Deserialize<City>(reader);
    }

}
