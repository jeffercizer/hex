using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;
using Godot;
using System.IO;

[Serializable]
public class Player
{
    public Player(float goldTotal, int teamNum)
    {
        this.teamNum = teamNum;
        this.goldTotal = goldTotal;
        this.visibleGameHexDict = new();
        this.seenGameHexDict = new();
        this.visibilityChangedList = new();
        this.unitList = new();
        this.cityList = new();
        this.unassignedResources = new();
        this.allowedBuildings = new();
        this.allowedUnits = new();
        this.unitResearchEffects = new();
        this.buildingResearchEffects = new();
        this.queuedResearch = new();
        this.partialResearchDictionary = new();
        //SelectResearch(String.Agriculture);
        Global.gameManager.game.teamManager.AddTeam(teamNum, 50);
        OnResearchComplete("Agriculture");
    }
    public Player(int teamNum, Dictionary<Hex, int> visibleGameHexDict, Dictionary<Hex, bool> seenGameHexDict, List<Unit> unitList, List<City> cityList, float scienceTotal, float cultureTotal, float goldTotal, float happinessTotal)
    {
        this.teamNum = teamNum;
        this.visibleGameHexDict = visibleGameHexDict;
        this.seenGameHexDict = seenGameHexDict;
        this.unitList = unitList;
        this.cityList = cityList;
        this.scienceTotal = scienceTotal;
        this.cultureTotal = cultureTotal;
        this.goldTotal = goldTotal;
        this.happinessTotal = happinessTotal;
        this.unassignedResources = new();
        this.allowedBuildings = new();
        this.allowedUnits = new();
        Global.gameManager.game.teamManager.AddTeam(teamNum, 50);
    }

    public Player()
    {
        //used for loading
    }
    public int teamNum { get; set; }
    public bool turnFinished { get; set; }
    public Dictionary<Hex, int> visibleGameHexDict { get; set; } = new();
    public Dictionary<Hex, bool> seenGameHexDict { get; set; } = new();
    public List<Hex> visibilityChangedList { get; set; } = new();
    public List<ResearchQueueType> queuedResearch { get; set; } = new();
    public Dictionary<String, ResearchQueueType> partialResearchDictionary { get; set; } = new();
    public List<Unit> unitList { get; set; } = new();
    public List<City> cityList { get; set; } = new();
    public List<(UnitEffect, UnitClass)> unitResearchEffects { get; set; } = new();
    public List<(BuildingEffect, String)> buildingResearchEffects { get; set; } = new();
    public HashSet<String> allowedBuildings { get; set; } = new();
    public HashSet<String> allowedUnits { get; set; } = new();
    public Dictionary<Hex, ResourceType> unassignedResources { get; set; } = new();
    public float strongestUnitBuilt { get; set; } = 0.0f;

    public float goldTotal { get; set; }
    public float scienceTotal { get; set; }
    public float cultureTotal { get; set; }
    public float happinessTotal { get; set; }
    public float influenceTotal { get; set; }


    public void SetGoldTotal(float goldTotal)
    {
        this.goldTotal = goldTotal;
        if(teamNum == Global.gameManager.game.localPlayerTeamNum)
        {
            if (Global.gameManager.game.TryGetGraphicManager(out GraphicManager manager)) manager.Update2DUI(UIElement.gold);
        }
    }

    public float GetGoldTotal()
    {
        return goldTotal;
    }

    public void SetScienceTotal(float scienceTotal)
    {
        this.scienceTotal = scienceTotal;
        if (teamNum == Global.gameManager.game.localPlayerTeamNum)
        {
            if (Global.gameManager.game.TryGetGraphicManager(out GraphicManager manager)) manager.Update2DUI(UIElement.science);
        }
    }

    public float GetScienceTotal()
    {
        return scienceTotal;
    }

    public void SetCultureTotal(float cultureTotal)
    {
        this.cultureTotal = cultureTotal;
        if (teamNum == Global.gameManager.game.localPlayerTeamNum)
        {
            if (Global.gameManager.game.TryGetGraphicManager(out GraphicManager manager)) manager.Update2DUI(UIElement.culture);
        }
    }

    public float GetCultureTotal()
    {
        return cultureTotal;
    }

    public void SetHappinessTotal(float happinessTotal)
    {
        this.happinessTotal = happinessTotal;
        if (teamNum == Global.gameManager.game.localPlayerTeamNum)
        {
            if (Global.gameManager.game.TryGetGraphicManager(out GraphicManager manager)) manager.Update2DUI(UIElement.happiness);
        }
    }

    public float GetHappinessTotal()
    {
        return happinessTotal;
    }

    public void SetInfluenceTotal(float influenceTotal)
    {
        this.influenceTotal = influenceTotal;
        if (teamNum == Global.gameManager.game.localPlayerTeamNum)
        {
            if (Global.gameManager.game.TryGetGraphicManager(out GraphicManager manager)) manager.Update2DUI(UIElement.influence);
        }
    }

    public float GetInfluenceTotal()
    {
        return influenceTotal;
    }

    public float GetGoldPerTurn()
    {
        float goldPerTurn = 0.0f;
        foreach (City city in cityList)
        {
            goldPerTurn += city.yields.gold;
        }
        return goldPerTurn;
    }

    public float GetSciencePerTurn()
    {
        float sciencePerTurn = 0.0f;
        foreach(City city in cityList)
        {
            sciencePerTurn += city.yields.science;
        }
        return sciencePerTurn;
    }

    public float GetCulturePerTurn()
    {
        float culturePerTurn = 0.0f;
        foreach (City city in cityList)
        {
            culturePerTurn += city.yields.culture;
        }
        return culturePerTurn;
    }

    public float GetHappinessPerTurn()
    {
        float happinessPerTurn = 0.0f;
        foreach (City city in cityList)
        {
            happinessPerTurn += city.yields.happiness;
        }
        return happinessPerTurn;
    }

    public float GetInfluencePerTurn()
    {
        float influencePerTurn = 0.0f;
        foreach (City city in cityList)
        {
            influencePerTurn += city.yields.influence;
        }
        return influencePerTurn;
    }

    public void OnTurnStarted(int turnNumber)
    {
        turnFinished = false;
        foreach (Unit unit in unitList)
        {
            unit.OnTurnStarted(turnNumber);
        }
        foreach (City city in cityList)
        {
            city.OnTurnStarted(turnNumber);
        }
        if(queuedResearch.Any())
        {
            float cost = queuedResearch[0].researchLeft;
            queuedResearch[0].researchLeft -= (int)Math.Round(scienceTotal);
            scienceTotal -= cost;
            scienceTotal = Math.Max(0.0f, scienceTotal);
            if(queuedResearch[0].researchLeft <= 0)
            {
                OnResearchComplete(queuedResearch[0].researchType);
                queuedResearch.RemoveAt(0);
            }
        }
        if (Global.gameManager.game.TryGetGraphicManager(out GraphicManager manager))
        {
            manager.Update2DUI(UIElement.gold);
            manager.Update2DUI(UIElement.happiness);
            manager.Update2DUI(UIElement.influence);
        }
    }

    public void OnTurnEnded(int turnNumber)
    {
        foreach (Unit unit in unitList)
        {
            unit.OnTurnEnded(turnNumber);
        }
        foreach (City city in cityList)
        {
            city.OnTurnEnded(turnNumber);
        }
        turnFinished = true;
    }

    public List<ResearchQueueType> SelectResearch(String researchType)
    {
        HashSet<String> visited = new();
        List<ResearchQueueType> queue = new();
        if(queuedResearch.Any())
        {
            partialResearchDictionary[researchType] = queuedResearch[0];
        }
        void TopologicalSort(String researchType)
        {
            if (visited.Contains(researchType))
                return; 

            visited.Add(researchType);

            if (ResearchLoader.researchesDict.ContainsKey(researchType))
            {
                foreach (String requirement in ResearchLoader.researchesDict[researchType].Requirements)
                {
                    TopologicalSort(requirement);
                }
            }
            if(partialResearchDictionary.ContainsKey(researchType))
            {
                queuedResearch.Add(partialResearchDictionary[researchType]);
            }
            else
            {
                queuedResearch.Add(new ResearchQueueType(researchType, ResearchLoader.researchesDict[researchType].Tier, ResearchLoader.researchesDict[researchType].Tier)); //apply cost mod TODO
            }
        }

        TopologicalSort(researchType);
        return queue;
    }

    public void OnResearchComplete(String researchType)
    {
        foreach (String unitType in ResearchLoader.researchesDict[researchType].UnitUnlocks)
        {
            allowedUnits.Add(unitType);
        }
        foreach(String buildingType in ResearchLoader.researchesDict[researchType].BuildingUnlocks)
        {
            allowedBuildings.Add(buildingType);
        }
        foreach(String effect in ResearchLoader.researchesDict[researchType].Effects)
        {
            ResearchLoader.ProcessFunctionString(effect, this);
        }
    }

    public bool AddResource(Hex hex, ResourceType resourceType, City targetCity)
    {
        if(targetCity.heldResources.Count < targetCity.maxResourcesHeld)
        {
            targetCity.heldResources.Add(hex, resourceType);
            unassignedResources.Remove(hex);
            return true;
        }
        return false;
    }



    public bool RemoveResource(Hex hex)
    {
        foreach(City city in cityList)
        {
            if(city.heldResources.Keys.Contains(hex))
            {
                ResourceType temp = city.heldResources[hex];
                city.heldResources.Remove(hex);
                unassignedResources.Add(hex, temp);
                return true;
            }
        }
        return false;
    }
    
    
    public bool RemoveLostResource(Hex hex)
    {
        foreach(City city in cityList)
        {
            if(city.heldResources.Remove(hex))
            {
                return true;
            }
        }
        return false;
    }

    public void AddGold(float gold)
    {
        SetGoldTotal(GetGoldTotal() + gold);
    }
    public void AddScience(float science)
    {
        SetScienceTotal(GetScienceTotal() + science);
    }
    public void AddCulture(float culture)
    {
        SetCultureTotal(GetCultureTotal() + culture);
    }
    public void AddHappiness(float happiness)
    {
        SetHappinessTotal(GetHappinessTotal() + happiness);
    }
    public void AddInfluence(float influence)
    {
        SetInfluenceTotal(GetInfluenceTotal() + influence);
    }

    public void Serialize(BinaryWriter writer)
    {
        Serializer.Serialize(writer, this);
    }

    public static Player Deserialize(BinaryReader reader)
    {
        return Serializer.Deserialize<Player>(reader);
    }

}

public class ResearchQueueType
{

    public ResearchQueueType(String researchType, int researchCost, int researchLeft)
    {
        this.researchType = researchType;
        this.researchCost = researchCost;
        this.researchLeft = researchLeft;
    }
    private ResearchQueueType()
    {
        //for loading
    }
    public String researchType { get; set; }
    public int researchCost { get; set; }
    public int researchLeft { get; set; }
    public void Serialize(BinaryWriter writer)
    {
        writer.Write(researchType);
        writer.Write(researchCost);
        writer.Write(researchLeft);
    }

    public static ResearchQueueType Deserialize(BinaryReader reader)
    {
        return new ResearchQueueType(
            reader.ReadString(),
            reader.ReadInt32(),
            reader.ReadInt32()
        );
    }

}
