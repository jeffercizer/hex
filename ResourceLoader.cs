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
    
    public ResourceList()
    {
        resourceEffects = new Dictionary<ResourceType, ResourceEffect>
        {
            { ResourceType.Silk, ApplySilkEffect },
            { ResourceType.Jade, ApplyJadeEffect },
            { ResourceType.Iron, ApplyIronEffect },
            { ResourceType.Horses, ApplyHorsesEffect },
            { ResourceType.Oil, ApplyOilEffect },
        };
        string xmlPath = "Resources.xml";
        resources = LoadResourceData(xmlPath);
        //if (resources.TryGetValue(resource, out ResourceInfo info))
        //ExecuteResourceEffect(resource);
    }
    public static Dictionary<TileResource, ResourceInfo> LoadResourceData(string xmlPath)
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
}
