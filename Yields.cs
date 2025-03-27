using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;

public struct Yields
{
    public float foodYield;
    public float productionYield;
    public float productionOverflow;
    public float goldYield;
    public float scienceYield;
    public float cultureYield;
    public float happinessYield;
    
    // Overload the + operator
    public static Yields operator +(Yields a, Yields b)
    {
        return new Yields
        {
            foodYield = a.foodYield + b.foodYield,
            productionYield = a.productionYield + b.productionYield,
            productionOverflow = a.productionOverflow + b.productionOverflow,
            goldYield = a.goldYield + b.goldYield,
            scienceYield = a.scienceYield + b.scienceYield,
            cultureYield = a.cultureYield + b.cultureYield,
            happinessYield = a.happinessYield + b.happinessYield
        };
    }
}
