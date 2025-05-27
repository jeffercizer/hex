using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;
using Godot;
using System.IO;

[Serializable]
public class Building
{
    public String name { get; set; }
    public int id { get; set; }
    public String buildingType { get; set; }
    public Hex districtHex { get; set; }
    public List<BuildingEffect> buildingEffects { get; set; } 
    public float baseProductionCost { get; set; }
    public float productionCost { get; set; }
    public float baseGoldCost { get; set; } 
    public float goldCost { get; set; } 
    public float baseMaintenanceCost { get; set; }
    public float maintenanceCost { get; set; }
    public Yields baseYields { get; set; }
    public Yields yields { get; set; }

    public Building(String buildingType, Hex districtHex)
    {
        this.districtHex = districtHex;
        this.buildingType = buildingType;
        this.name = buildingType;
        // 'City Center' 'Farm' 'Mine' 'Hunting Camp' 'Fishing Boat' 'Whaling Ship'
        if (BuildingLoader.buildingsDict.TryGetValue(buildingType, out BuildingInfo buildingInfo))
        {
            this.productionCost = buildingInfo.ProductionCost;
            this.baseProductionCost = buildingInfo.ProductionCost;
            
            this.goldCost = buildingInfo.GoldCost;
            this.baseGoldCost = buildingInfo.GoldCost;
            
            this.maintenanceCost = buildingInfo.MaintenanceCost;
            this.baseMaintenanceCost = buildingInfo.MaintenanceCost;
            
            this.yields = buildingInfo.yields;
            this.baseYields = buildingInfo.yields;
            
            this.buildingEffects = new();
            foreach (String effectName in buildingInfo.Effects)
            {
                buildingEffects.Add(new BuildingEffect(effectName));
            }
        }
        if(BuildingLoader.buildingsDict[buildingType].Wonder)
        {
            Global.gameManager.game.builtWonders.Add(buildingType);
        }
        id = Global.gameManager.game.GetUniqueID();
        if (Global.gameManager.game.TryGetGraphicManager(out GraphicManager manager)) manager.NewBuilding(this);
    }
    public Building()
    {
    }

    public void Serialize(BinaryWriter writer)
    {
        Serializer.Serialize(writer, this);
    }

    public static Building Deserialize(BinaryReader reader)
    {
        return Serializer.Deserialize<Building>(reader);
    }


    public void SwitchTeams()
    {
        RecalculateYields();
    }

    public void DestroyBuilding()
    {
        districtHex = new Hex(0,0,0);
        if(BuildingLoader.buildingsDict[buildingType].Wonder)
        {
            Global.gameManager.game.builtWonders.Remove(buildingType);
        }
    }

    public void AddEffect(BuildingEffect effect)
    {
        buildingEffects.Add(effect);
        Global.gameManager.game.cityDictionary[Global.gameManager.game.mainGameBoard.gameHexDict[districtHex].district.cityID].RecalculateYields();
    }

    public void RemoveEffect(BuildingEffect effect)
    {
        buildingEffects.Remove(effect);
        Global.gameManager.game.cityDictionary[Global.gameManager.game.mainGameBoard.gameHexDict[districtHex].district.cityID].RecalculateYields();
    }

    public void PrepareYieldRecalculate()
    {
        yields = baseYields;
        PriorityQueue<BuildingEffect, int> orderedEffects = new();
        foreach(BuildingEffect effect1 in buildingEffects)
        {
            orderedEffects.Enqueue(effect1, effect1.priority);
        }
        BuildingEffect effect;
        int priority;
        while(orderedEffects.TryDequeue(out effect, out priority))
        {
            effect.ApplyEffect(this);
        }
    }

    public void RecalculateYields()
    {
        Global.gameManager.game.mainGameBoard.gameHexDict[districtHex].yields += yields;
    }
}
