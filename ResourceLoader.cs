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

public class ResourceLoader
{
    Dictionary<ResourceType, ResourceInfo> resources;
    Dictionary<ResourceType, ResourceEffect> resourceEffects;
    public delegate void ResourceEffect(ResourceInfo resourceInfo);
    
    public ResourceLoader()
    { //Iron, Horses, Niter, Coal, Oil, Uranium, Lithium, Jade, Silk, Tobacco, Silver, Gold, Camels
        resourceEffects = new Dictionary<ResourceType, ResourceEffect>
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
        string xmlPath = "Resources.xml";
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
    public void ExecuteResourceEffect(ResourceType resourceType)
    {
        if (resources.TryGetValue(resourceType, out ResourceInfo info) &&
            resourceEffects.TryGetValue(resourceType, out ResourceEffect effect))
        {
            effect.Invoke(info);
        }
    }
    
    //all infantry (non-horse, non-siege land) +1 cs
    void ApplyIronEffect()
    {
    }
    
    //all mounted units (horse, siege) +1 cs
    void ApplyHorsesEffect()    
    {
    }
    
    //idk
    void ApplyNiterEffect()
    {
    }
    
    //idk
    void ApplyCoalEffect()
    {
    }
    
    //idk
    void ApplyOilEffect()
    {
    }
    
    //idk
    void ApplyUraniumEffect()
    {
    }
    
    //idk
    void ApplyLithiumEffect()
    {
    }
    
    //15% more gold in city
    void ApplyJadeEffect()
    {
    }
    
    //10% more culture in city
    void ApplySilkEffect()
    {
    }

    //10% more science in city
    void ApplyCoffeeEffect()
    {
    }

    //15% off units purchased with gold
    void ApplySilverEffect()
    {
    }

    //15% off buildings purchased with gold
    void ApplyGoldEffect()
    {
    }

    //allow 3 more resources to be assigned to this city
    void ApplyCamelsEffect()
    {
    }
}
