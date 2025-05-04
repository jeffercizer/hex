using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

public enum ResourceType
{
    None,
    Iron = 'I',
    Horses = 'H',
    Niter = 'N',
    Coal = 'C',
    Oil = 'O',
    Uranium = 'U',
    Lithium = 'L',
    SciFi = 'S',
    Jade = 'j',
    Wheat = 'w',
    Sheep = 's',
    Marble = 'm',
    Dates = 'd',
    Silk = 'u',
    Salt = 'n',
    Rubber = 'R',
    Ivory = 'i',
    Gold = 'g',
    Silver = 'k',
    Camels = 'c',
    Coffee = 'e',
    Cotton = 'q',
    Tobacco = 't',
    Stone = 'z',
}

public struct ResourceInfo
{
    public int Food { get; set; }
    public int Production { get; set; }
    public int Gold { get; set; }
    public int Science { get; set; }
    public int Culture { get; set; }
    public int Happiness { get; set; }
}

public static class ResourceLoader
{
    public static Dictionary<ResourceType, ResourceInfo> resources;
    public static Dictionary<ResourceType, Action> resourceEffects;
    public static Dictionary<String, ResourceType> resourceNames = new Dictionary<String, ResourceType>
    {
        { "0", ResourceType.None},
        { "I", ResourceType.Iron },
        { "H", ResourceType.Horses },
        { "N", ResourceType.Niter },
        { "C", ResourceType.Coal },
        { "O", ResourceType.Oil },
        { "U", ResourceType.Uranium },
        { "L", ResourceType.Lithium },
        { "S", ResourceType.SciFi },
        { "j", ResourceType.Jade },
        { "w", ResourceType.Wheat },
        { "s", ResourceType.Sheep },
        { "m", ResourceType.Marble },
        { "d", ResourceType.Dates },
        { "u", ResourceType.Silk },
        { "n", ResourceType.Salt },
        { "R", ResourceType.Rubber },
        { "i", ResourceType.Ivory },
        { "g", ResourceType.Gold },
        { "k", ResourceType.Silver },
        { "c", ResourceType.Camels },
        { "e", ResourceType.Coffee },
        { "q", ResourceType.Cotton },
        { "t", ResourceType.Tobacco },
        { "z", ResourceType.Stone }
    };
    
    static ResourceLoader()
    { //Iron, Horses, Niter, Coal, Oil, Uranium, Lithium, Jade, Silk, Tobacco, Silver, Gold, Camels
        resourceEffects = new Dictionary<ResourceType, Action>
        {
            { ResourceType.Iron, ApplyIronEffect },
            { ResourceType.Horses, ApplyHorsesEffect },
            { ResourceType.Niter, ApplyNiterEffect },
            { ResourceType.Coal, ApplyCoalEffect },
            { ResourceType.Oil, ApplyOilEffect },
            { ResourceType.Uranium, ApplyUraniumEffect },
            { ResourceType.Lithium, ApplyLithiumEffect },
            { ResourceType.Jade, ApplyJadeEffect },
            { ResourceType.Silk, ApplySilkEffect },
            { ResourceType.Coffee, ApplyCoffeeEffect },
            { ResourceType.Silver, ApplySilverEffect },
            { ResourceType.Gold, ApplyGoldEffect },
            { ResourceType.Camels, ApplyCamelsEffect }
        };
        string xmlPath = "hex/Resources.xml";
        resources = LoadResourceData(xmlPath);
        //if (resources.TryGetValue(resource, out ResourceInfo info))
        //ExecuteResourceEffect(resource);
    }
    public static Dictionary<ResourceType, ResourceInfo> LoadResourceData(string xmlPath)
    {
        // Load the XML file
        XDocument xmlDoc = XDocument.Load(xmlPath);

        // Parse the resource information into a dictionary
        var resourceData = xmlDoc.Descendants("Resource")
            .ToDictionary(
                r => (ResourceType)Enum.Parse(typeof(ResourceType), r.Attribute("Name").Value),
                r => new ResourceInfo
                {
                    Food = int.Parse(r.Attribute("Food").Value),
                    Production = int.Parse(r.Attribute("Production").Value),
                    Gold = int.Parse(r.Attribute("Gold").Value),
                    Science = int.Parse(r.Attribute("Science").Value),
                    Culture = int.Parse(r.Attribute("Culture").Value),
                    Happiness = int.Parse(r.Attribute("Happiness").Value)
                }
            );

        return resourceData;
    }
    public static void ExecuteResourceEffect(ResourceType resourceType)
    {
        if (resources.TryGetValue(resourceType, out ResourceInfo info) &&
            resourceEffects.TryGetValue(resourceType, out Action effect))
        {
            effect.Invoke();
        }
    }
    
    //all infantry (non-horse, non-siege land) +1 cs
    static void ApplyIronEffect()
    {
    }
    
    //all mounted units (horse, siege) +1 cs
    static void ApplyHorsesEffect()    
    {
    }
    
    //idk
    static void ApplyNiterEffect()
    {
    }
    
    //idk
    static void ApplyCoalEffect()
    {
    }
    
    //idk
    static void ApplyOilEffect()
    {
    }
    
    //idk
    static void ApplyUraniumEffect()
    {
    }
    
    //idk
    static void ApplyLithiumEffect()
    {
    }
    
    //15% more gold in city
    static void ApplyJadeEffect()
    {
    }
    
    //10% more culture in city
    static void ApplySilkEffect()
    {
    }

    //10% more science in city
    static void ApplyCoffeeEffect()
    {
    }

    //15% off units purchased with gold
    static void ApplySilverEffect()
    {
    }

    //15% off buildings purchased with gold
    static void ApplyGoldEffect()
    {
    }

    //allow 3 more resources to be assigned to this city
    static void ApplyCamelsEffect()
    {
    }
}
