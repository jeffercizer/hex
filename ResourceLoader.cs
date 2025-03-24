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
    Cotton = 'j',
    Tobacco = 't',
}

public struct ResourceInfo
{
    public int Food { get; set; }
    public int Production { get; set; }
    public int Gold { get; set; }
}

public class ResourceLoader
{
    Dictionary<ResourceType, ResourceInfo> resources;
    public ResourceList()
    {
        string xmlPath = "Resources.xml";
        resources = LoadResourceData(xmlPath);
        //example usage
        //TileResource resource = TileResource.Wheat;
        //if (resources.TryGetValue(resource, out ResourceInfo info))
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
                    Gold = int.Parse(r.Attribute("Gold").Value)
                }
            );

        return resourceData;
    }
}
