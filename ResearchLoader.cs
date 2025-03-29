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
    public static Dictionary<ResearchType, ResearchInfo> researchesDict;
    
    static ResearchLoader()
    {
        string xmlPath = "Researches.xml";
        researchesDict = LoadResearchData(xmlPath);
    }
        
    public static Dictionary<ResearchType, ResearchInfo> LoadResearchData(string xmlPath)
    {
        // Load the XML file
        XDocument xmlDoc = XDocument.Load(xmlPath);

        // Parse the research data into a dictionary, allowing for nulls
        var ResearchData = xmlDoc.Descendants("Research")
            .ToDictionary(
                r => Enum.Parse<ResearchType>(r.Attribute("Name")?.Value ?? throw new InvalidOperationException("Missing 'Name' attribute")),
                r => new ResearchInfo
                {
                    Tier = int.Parse(r.Attribute("Tier")?.Value ?? "0"),
                    Requirements = r.Element("Requirements")?.Elements("ResearchType")
                        .Select(e => Enum.TryParse<ResearchType>(e.Value, out var result) ? result : default)
                        .Where(e => e != default)
                        .ToList() ?? new List<ResearchType>(),
                    BuildingUnlocks = r.Element("BuildingUnlocks")?.Elements("BuildingType")
                        .Select(e => Enum.TryParse<BuildingType>(e.Value, out var result) ? result : default)
                        .Where(e => e != default)
                        .ToList() ?? new List<BuildingType>(),
                    UnitUnlocks = r.Element("UnitUnlocks")?.Elements("UnitType")
                        .Select(e => Enum.TryParse<UnitType>(e.Value, out var result) ? result : default)
                        .Where(e => e != default)
                        .ToList() ?? new List<UnitType>(),
                    Effects = r.Element("Effects")?.Elements("Effect")
                        .Select(e => e.Value)
                        .Where(e => !string.IsNullOrWhiteSpace(e))
                        .ToList() ?? new List<string>(),
                }
            );

        return ResearchData;
    }
    
    public static void ProcessFunctionString(String functionString, Player player)
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
    static void AgricultureEffect(Player player)
    {
    }
    static void SailingEffect(Player player)
    {
       player.unitResearchEffects.Add((new UnitEffect("EnableEmbarkDisembark"), UnitClass.Land));
    }
    static void PotteryEffect(Player player)
    {
    }
    static void AnimalHusbandryEffect(Player player)
    {
        player.unitResearchEffects.Add((new UnitEffect(UnitEffectType.MovementSpeed, EffectOperation.Add, 1.0f, 5), UnitClass.Recon));
    }
    static void IrrigationEffect(Player player)
    {
    }
    static void WritingEffect(Player player)
    {
    }
    static void MasonryEffect(Player player)
    {
    }
}
