using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;
using Godot;
using System.Text;
using System.IO;

[Serializable]
public class District
{

    public int id { get; set; }
    public List<Building> buildings { get; set; }
    public List<Building> defenses { get; set; }
    public Hex hex { get; set; }
    public bool isCityCenter { get; set; }
    public bool isUrban { get; set; }
    public bool hasWalls { get; set; }
    public int cityID { get; set; }
    public List<Hex> visibleHexes { get; set; } = new();
    public float health { get; set; } = 0.0f;
    public float maxHealth{ get; set; } = 0.0f;
    public int maxBuildings { get; set; }  = 1;
    public int maxDefenses { get; set; } = 1;
    public int turnsUntilHealing { get; set; } = 0;

    public District(GameHex gameHex, String initialString, bool isCityCenter, bool isUrban, int cityID)
    {
        SetupDistrict(gameHex, isCityCenter, isUrban, cityID);
        AddBuilding(new Building(initialString, hex));
    }

    public District(GameHex gameHex, bool isCityCenter, bool isUrban, int cityID)
    {
        SetupDistrict(gameHex, isCityCenter, isUrban, cityID);
    }

    public District()
    {
    }

    public void Serialize(BinaryWriter writer)
    {
        Serializer.Serialize(writer, this);
    }

    public static District Deserialize(BinaryReader reader)
    {
        return Serializer.Deserialize<District>(reader);
    }

    private void SetupDistrict(GameHex gameHex, bool isCityCenter, bool isUrban, int cityID)
    {
        id = Global.gameManager.game.GetUniqueID();
        this.cityID = cityID;
        buildings = new();
        defenses = new();
        this.hex = gameHex.hex;


        Global.gameManager.game.mainGameBoard.gameHexDict[hex].ClaimHex(Global.gameManager.game.cityDictionary[cityID]);
        Global.gameManager.game.mainGameBoard.gameHexDict[hex].district = this;
        foreach (Hex hex in gameHex.hex.WrappingNeighbors(Global.gameManager.game.mainGameBoard.left, Global.gameManager.game.mainGameBoard.right, Global.gameManager.game.mainGameBoard.bottom))
        {
            Global.gameManager.game.mainGameBoard.gameHexDict[hex].TryClaimHex(Global.gameManager.game.cityDictionary[cityID]);
        }
        
        if(Global.gameManager.game.mainGameBoard.gameHexDict[hex].resourceType != ResourceType.None)
        {
            AddResource();
        }

        this.isCityCenter = isCityCenter;
        if (Global.gameManager.game.TryGetGraphicManager(out GraphicManager manager))
        {
            manager.NewDistrict(this);
        }
        if (isCityCenter)
        {
            maxHealth = 50.0f;
            health = 50.0f;
        }
        else
        {
            if (Global.gameManager.game.mainGameBoard.gameHexDict[hex].featureSet.Contains(FeatureType.Forest))
            {
                AddBuilding(new Building("Lumbermill", hex));
            }
            else
            {
                if (Global.gameManager.game.mainGameBoard.gameHexDict[hex].terrainType == TerrainType.Flat)
                {
                    AddBuilding(new Building("Farm", hex));
                }
                else if (Global.gameManager.game.mainGameBoard.gameHexDict[hex].terrainType == TerrainType.Rough)
                {
                    AddBuilding(new Building("Lumbermill", hex));
                }
                else if (Global.gameManager.game.mainGameBoard.gameHexDict[hex].terrainType == TerrainType.Mountain)
                {
                    AddBuilding(new Building("Mine", hex));
                }
                else if (Global.gameManager.game.mainGameBoard.gameHexDict[hex].terrainType == TerrainType.Coast)
                {
                    AddBuilding(new Building("FishingBoat", hex));
                }
                else if (Global.gameManager.game.mainGameBoard.gameHexDict[hex].terrainType == TerrainType.Ocean)
                {
                    AddBuilding(new Building("FishingBoat", hex));
                }
            }

        }
        this.isUrban = isUrban;
        if(isUrban)
        {
            maxBuildings += 1;
            Global.gameManager.game.mainGameBoard.gameHexDict[hex].AddTerrainFeature(FeatureType.Road);
        }

        Global.gameManager.game.cityDictionary[cityID].RecalculateYields();
        AddVision();

    }

    public void BeforeSwitchTeam()
    {
        RemoveVision();
        RemoveLostResource();        
    }
    
    public void AfterSwitchTeam()
    {      
        foreach(Building building in buildings)
        {
            building.SwitchTeams();
        }
        foreach(Building building in defenses)
        {
            building.SwitchTeams();
        }
        AddVision();
        AddResource();
    }

    public bool decreaseHealth(float amount)
    {
        health -= amount;
        health = Math.Max(0.0f, health);
        GD.Print(Global.gameManager.game.cityDictionary[cityID].name + " | " + Global.gameManager.game.cityDictionary[cityID].id + " | " + health);
        if (Global.gameManager.game.TryGetGraphicManager(out GraphicManager manager))
        {
            manager.UpdateGraphic(Global.gameManager.game.cityDictionary[cityID].id, GraphicUpdateType.Update);
        }
        if (health <= 0.0f)
        {
            Global.gameManager.game.cityDictionary[cityID].DistrictFell();
            turnsUntilHealing = 5;
            return true;
        }
        return false;
    }

    public float GetCombatStrength()
    {
        float strength = Global.gameManager.game.playerDictionary[Global.gameManager.game.cityDictionary[cityID].teamNum].strongestUnitBuilt;
        if (hasWalls)
        {
            strength += 15.0f;
        }
        return strength;
    }

    public void DestroyDistrict()
    {
        RemoveVision();
        RemoveLostResource();
        Global.gameManager.game.cityDictionary[cityID].districts.Remove(this);
        Global.gameManager.game.mainGameBoard.gameHexDict[hex].district = null;
        foreach(Building building in buildings)
        {
            building.DestroyBuilding();
        }
        foreach(Building building in defenses)
        {
            building.DestroyBuilding();
        }
    }

    public void HealForTurn(float healAmount)
    {
        if(turnsUntilHealing <= 0)
        {
            health += healAmount;
            health = Math.Min(health, maxHealth);
        }
        else if(turnsUntilHealing > 0)
        {
            if (Global.gameManager.game.mainGameBoard.gameHexDict[hex].units.Any())
            {
                Unit unit = Global.gameManager.game.mainGameBoard.gameHexDict[hex].units[0];
                if (Global.gameManager.game.teamManager.GetEnemies(Global.gameManager.game.cityDictionary[cityID].teamNum).Contains(unit.teamNum))
                {
                    turnsUntilHealing += 1;
                }
            }
            turnsUntilHealing -= 1;
        }
    }

    public void AddWalls(float wallStrength)
    {
        if(health >= maxHealth)
        {
            maxHealth += wallStrength;
            health += wallStrength;
            hasWalls = true;
        }
    }

    public void DevelopDistrict()
    {
        maxBuildings += 1;
        isUrban = true;
    }

    public int CountString(String buildingType)
    {
        int count = 0;
        foreach(Building building in buildings)
        {
            if(building.buildingType == buildingType)
            {
                count += 1;
            }
        }
        return count;
    }
    
    public void RecalculateYields()
    {
        Global.gameManager.game.mainGameBoard.gameHexDict[hex].RecalculateYields();
        foreach(Building building in buildings)
        {
            building.RecalculateYields();
        }
    }

    public void PrepareYieldRecalculate()
    {
        if (buildings.Any())
        {
            foreach (Building building in buildings)
            {
                building.PrepareYieldRecalculate();
            }
        }
    }

    public void AddBuilding(Building building)
    {
        if(buildings.Count() < maxBuildings)
        {
            buildings.Add(building);
            Global.gameManager.game.cityDictionary[cityID].citySize += 1;
            Global.gameManager.game.cityDictionary[cityID].RecalculateYields();
        }
    }

    public void AddDefense(Building building)
    {
        if(defenses.Count() < maxDefenses)
        {
            defenses.Add(building);
            Global.gameManager.game.cityDictionary[cityID].RecalculateYields();
        }
    }

    public void UpdateVision()
    {
        RemoveVision();
        AddVision();
        if (Global.gameManager.game.TryGetGraphicManager(out GraphicManager manager)) manager.UpdateGraphic(Global.gameManager.game.mainGameBoard.id, GraphicUpdateType.Update);
    }

    public void RemoveVision()
    {
        foreach (Hex hex in visibleHexes)
        {            
            int count;
            if(Global.gameManager.game.playerDictionary[Global.gameManager.game.cityDictionary[cityID].teamNum].visibleGameHexDict.TryGetValue(hex, out count))
            {
                if(count <= 1)
                {
                    Global.gameManager.game.playerDictionary[Global.gameManager.game.cityDictionary[cityID].teamNum].visibleGameHexDict.Remove(hex);
                    Global.gameManager.game.playerDictionary[Global.gameManager.game.cityDictionary[cityID].teamNum].visibilityChangedList.Add(hex);
                }
                else
                {
                    Global.gameManager.game.playerDictionary[Global.gameManager.game.cityDictionary[cityID].teamNum].visibleGameHexDict[hex] = count - 1;
                }
            }
        }
        visibleHexes.Clear();
        if (Global.gameManager.game.TryGetGraphicManager(out GraphicManager manager)) manager.UpdateGraphic(Global.gameManager.game.mainGameBoard.id, GraphicUpdateType.Update);
    }
    public void AddVision()
    {
        if (isCityCenter)
        {
            foreach (Hex hex in hex.WrappingRange(3, Global.gameManager.game.mainGameBoard.left, Global.gameManager.game.mainGameBoard.right, Global.gameManager.game.mainGameBoard.top, Global.gameManager.game.mainGameBoard.bottom))
            {
                Global.gameManager.game.playerDictionary[Global.gameManager.game.cityDictionary[cityID].teamNum].seenGameHexDict.TryAdd(hex, true);
                int count;
                if (Global.gameManager.game.playerDictionary[Global.gameManager.game.cityDictionary[cityID].teamNum].visibleGameHexDict.TryGetValue(hex, out count))
                {
                    Global.gameManager.game.playerDictionary[Global.gameManager.game.cityDictionary[cityID].teamNum].visibleGameHexDict[hex] = count + 1;
                }
                else
                {
                    Global.gameManager.game.playerDictionary[Global.gameManager.game.cityDictionary[cityID].teamNum].visibleGameHexDict.TryAdd(hex, 1);
                    Global.gameManager.game.playerDictionary[Global.gameManager.game.cityDictionary[cityID].teamNum].visibilityChangedList.Add(hex);
                }
            }
        }
        else
        {
            visibleHexes = hex.WrappingNeighbors(Global.gameManager.game.mainGameBoard.left, Global.gameManager.game.mainGameBoard.right, Global.gameManager.game.mainGameBoard.bottom).ToList();
            foreach (Hex hex in visibleHexes)
            {
                Global.gameManager.game.playerDictionary[Global.gameManager.game.cityDictionary[cityID].teamNum].seenGameHexDict.TryAdd(hex, true); //add to the seen dict no matter what since duplicates are thrown out
                int count;
                if (Global.gameManager.game.playerDictionary[Global.gameManager.game.cityDictionary[cityID].teamNum].visibleGameHexDict.TryGetValue(hex, out count))
                {
                    Global.gameManager.game.playerDictionary[Global.gameManager.game.cityDictionary[cityID].teamNum].visibleGameHexDict[hex] = count + 1;
                }
                else
                {
                    Global.gameManager.game.playerDictionary[Global.gameManager.game.cityDictionary[cityID].teamNum].visibleGameHexDict.TryAdd(hex, 1);
                    Global.gameManager.game.playerDictionary[Global.gameManager.game.cityDictionary[cityID].teamNum].visibilityChangedList.Add(hex);
                }
            }
        }
        if (Global.gameManager.game.TryGetGraphicManager(out GraphicManager manager)) manager.UpdateGraphic(Global.gameManager.game.mainGameBoard.id, GraphicUpdateType.Update);
    }

    public void AddResource()
    {
        if (Global.gameManager.game.mainGameBoard.gameHexDict[hex].resourceType != ResourceType.None)
        {
            Global.gameManager.game.playerDictionary[Global.gameManager.game.cityDictionary[cityID].teamNum].unassignedResources.Add(hex, Global.gameManager.game.mainGameBoard.gameHexDict[hex].resourceType);
        }
    }

    public void RemoveLostResource()
    {
        Global.gameManager.game.playerDictionary[Global.gameManager.game.cityDictionary[cityID].teamNum].RemoveLostResource(hex);
    }
}
