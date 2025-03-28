using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

public enum ResearchType
{
    None,
    Agriculture,
    Sailing,
    Pottery,
    AnimalHusbandry,
    Irrigation,
    Writing,
    Masonry,
    Wheel,
    BronzeWorking,
    Archery,
    Currency,
}

public struct ResearchInfo
{
    public int Tier;
    public List<ResearchType> Requirements;
    public List<BuildingType> BuildingUnlocks;
    public List<UnitType> UnitUnlocks;
    public List<String> Effects;
}

public static class ResearchLoader
{

    public static Dictionary<ResearchType, string> researchNames = new Dictionary<ResearchType, string>
    {
        { ResearchType.Agriculture, "Agriculture" },
        { ResearchType.Sailing, "Sailing" },
        { ResearchType.Pottery, "Pottery"},
        { ResearchType.AnimalHusbandry, "Animal Husbandry" },
        { ResearchType.Irrigation, "Irrigation" },
        { ResearchType.Writing, "Writing" },
        { ResearchType.Masonry, "Masonry" },
        { ResearchType.Wheel, "Wheel" },
        { ResearchType.BronzeWorking, "Bronze Working"},
        { ResearchType.Archery, "Archery" },
        { ResearchType.Currency, "Currency" },
    };
    public static Dictionary<ResearchType, ResearchInfo> researchsDict;
    
    static ResearchLoader()
    {
        string xmlPath = "Researches.xml";
        researchsDict = LoadResearchData(xmlPath);
    }
    
    public static Dictionary<ResearchType, ResearchInfo> LoadResearchData(string xmlPath)
    {
        XDocument xmlDoc = XDocument.Load(xmlPath);
        var ResearchData = xmlDoc.Descendants("Research")
            .ToDictionary(
                r => Enum.Parse<ResearchType>(r.Attribute("Name").Value),
                r => new ResearchInfo
                {
                    Tier = int.Parse(r.Attribute("Tier").Value),
                    Requirements = r.Element("Requirements").Elements("ResearchType").Select(e => Enum.Parse<ResearchType>(e.Value)).ToList(),
                    BuildingUnlocks = r.Element("BuildingUnlocks").Elements("BuildingType").Select(e => Enum.Parse<BuildingType>(e.Value)).ToList(),
                    UnitUnlocks = r.Element("UnitUnlocks").Elements("UnitType").Select(e => Enum.Parse<UnitType>(e.Value)).ToList(),
                    Effects = r.Element("Effects").Elements("Effect").Select(e => e.Value).ToList(),
                }
            );
        return ResearchData;
    }
    
    void ProcessResearchFunctionString(String functionString, Player player)
    {
        Dictionary<String, Action<Player>> effectFunctions = new Dictionary<string, Action<Player>>
        {
            { "AgricultureEffect", AgricultureEffect },
            { "SailingEffect", SailingEffect },
            { "PotteryEffect", PotteryEffect },
            { "AnimalHusbandryEffect", AnimalHusbandryEffect },
            { "IrrigationEffect", IrrigationEffect },
            { "WritingEffect", WritingEffect },
            { "MasonryEffect", MasonryEffect },
        };
        
        if (effectFunctions.TryGetValue(functionString, out Action<Player> effectFunction))
        {
            effectFunction(player);
        }
        else
        {
            throw new ArgumentException($"Function '{functionString}' not recognized in BuildingEffect from Buildings file.");
        }
    }
    void ProcessFunctionString(String functionString, Player player)
    {
        Dictionary<String, Action<Player>> effectFunctions = new Dictionary<string, Action<Player>>
        {
            { "AgricultureEffect", AgricultureEffect },
            { "SailingEffect", SailingEffect },
            { "PotteryEffect", PotteryEffect },
            { "AnimalHusbandryEffect", AnimalHusbandryEffect },
            { "IrrigationEffect", IrrigationEffect },
            { "WritingEffect", WritingEffect },
            { "MasonryEffect", MasonryEffect },
        };
        
        if (effectFunctions.TryGetValue(functionString, out Action<Player> effectFunction))
        {
            effectFunction(player);
        }
        else
        {
            throw new ArgumentException($"Function '{functionString}' not recognized in ResearchEffects from Researches file.");
        }
    }
    void AgricultureEffect(Player player)
    {
    }
    void SailingEffect(Player player)
    {
       player.unitResearchEffects.Add((new UnitEffect("EnableEmbarkDisembark"), UnitClass.Recon));
    }
    void PotteryEffect(Player player)
    {
    }
    void AnimalHusbandryEffect(Player player)
    {
        player.unitResearchEffects.Add(new UnitEffect(UnitEffectType.MovementSpeed, EffectOperation.Add, 1.0f, 5));
    }
    void IrrigationEffect(Player player)
    {
    }
    void WritingEffect(Player player)
    {
    }
    void MasonryEffect(Player player)
    {
    }
}
