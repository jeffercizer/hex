using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;

public struct Yields
{
    public float food;
    public float production;
    public float gold;
    public float science;
    public float culture;
    public float happiness;
    
    // Overload the + operator
    public static Yields operator +(Yields a, Yields b)
    {
        return new Yields
        {
            food = a.food + b.food,
            production = a.production + b.production,
            gold = a.gold + b.gold,
            science = a.science + b.science,
            culture = a.culture + b.culture,
            happiness = a.happiness + b.happiness
        };
    }
}
