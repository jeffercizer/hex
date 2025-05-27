using System;
using System.Collections.Generic;
using System.IO;

[Serializable]
public class TeamManager
{
    private Dictionary<int, Dictionary<int, int>> relationships { get; set; } = new Dictionary<int, Dictionary<int, int>>();

    public void Serialize(BinaryWriter writer)
    {
        Serializer.Serialize(writer, this);
    }

    public static TeamManager Deserialize(BinaryReader reader)
    {
        return Serializer.Deserialize<TeamManager>(reader);
    }


    public void AddTeam(int newTeamNum, int defaultRelationship)
    {
        Dictionary<int, int> newTeam = new();
        foreach(int oldTeamNum in relationships.Keys)
        {
            relationships[oldTeamNum].Add(newTeamNum, defaultRelationship);
            newTeam.Add(oldTeamNum, defaultRelationship);
        }
        relationships.Add(newTeamNum, newTeam);
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

    public void IncreaseRelationship(int team1, int team2, int relationshipChange)
    {
        if (!relationships.ContainsKey(team1) || !relationships.ContainsKey(team2))
        {
            throw new Exception("One or both teams do not exist.");
        }

        relationships[team1][team2] += relationshipChange;
        relationships[team1][team2] = Math.Min(relationships[team1][team2], 100);
        //relationships[team2][team1] = relationship; //for symmetric relationships
    }

    public void DecreaseRelationship(int team1, int team2, int relationshipChange)
    {
        if (!relationships.ContainsKey(team1) || !relationships.ContainsKey(team2))
        {
            throw new Exception("One or both teams do not exist.");
        }

        relationships[team1][team2] -= relationshipChange;
        relationships[team1][team2] = Math.Max(relationships[team1][team2], 0);
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
                if (relationshipDict[relationshipID] >= 80)
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
                if (relationshipDict[relationshipID] <= 0)
                {
                    enemies.Add(relationshipID);
                }
            }
        }

        return enemies;
    }
}
