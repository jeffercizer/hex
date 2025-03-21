using System;
using System.Collections.Generic;

public class Team
{
    public int Id { get; set; }
    public string Name { get; set; }
}

[Serializable]
public class TeamManager
{
    private Dictionary<int, Team> teams = new Dictionary<int, Team>();
    private Dictionary<int, Dictionary<int, int>> relationships = new Dictionary<int, Dictionary<int, int>>();

    public void AddTeam(int id, string name)
    {
        teams[id] = new Team { Id = id, Name = name };
        relationships[id] = new Dictionary<int, int>();
        foreach (int teamId in relationships.Keys)
        {
            if (teamId != id)
            {
                relationships[id][teamId] = 50;
                relationships[teamId][id] = 50; // Symmetric relationship to start
            }
        }
    }

    public void SetRelationship(int team1, int team2, int relationship)
    {
        if (!relationships.ContainsKey(team1) || !relationships.ContainsKey(team2))
        {
            throw new Exception("One or both teams do not exist.");
        }

        relationships[team1][team2] = relationship;
        //relationships[team2][team1] = relationship; //for symmetric relationships
    }

    public void IncreaseRelationship(int team1, int team2, int relationship)
    {
        if (!relationships.ContainsKey(team1) || !relationships.ContainsKey(team2))
        {
            throw new Exception("One or both teams do not exist.");
        }

        relationships[team1][team2] += relationship;
        //relationships[team2][team1] = relationship; //for symmetric relationships
    }

    public void DecreaseRelationship(int team1, int team2, int relationship)
    {
        if (!relationships.ContainsKey(team1) || !relationships.ContainsKey(team2))
        {
            throw new Exception("One or both teams do not exist.");
        }

        relationships[team1][team2] -= relationship;
        //relationships[team2][team1] = relationship; //for symmetric relationships
    }

    public int GetRelationship(int team1, int team2)
    {
        if (relationships.ContainsKey(team1) && relationships[team1].ContainsKey(team2))
        {
            return relationships[team1][team2];
        }

        return -99; // Default relationship
    }

    public List<int> GetAllies(int teamId)
    {
        var allies = new List<int>();

        if (relationships.ContainsKey(teamId))
        {
            Dictionary<int, int> relationshipDict = relationships[teamId];
            foreach (int relationshipID in relationshipDict.Keys)
            {
                if (relationshipDict[relationshipID] > 80)
                {
                    allies.Add(relationshipID);
                }
            }
        }

        return allies;
    }

    public List<int> GetEnemies(int teamId)
    {
        var enemies = new List<int>();

        if (relationships.ContainsKey(teamId))
        {
            Dictionary<int, int> relationshipDict = relationships[teamId];
            foreach (int relationshipID in relationshipDict.Keys)
            {
                if (relationshipDict[relationshipID] == 0)
                {
                    enemies.Add(relationshipID);
                }
            }
        }

        return enemies;
    }
}
