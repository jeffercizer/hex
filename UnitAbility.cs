using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;
using System.Formats.Asn1;
using Godot;
using System.IO;

[Serializable]
public class UnitAbility
{
    public String name { get; set; }
    public UnitEffect effect { get; set; }
    public int usingUnitID { get; set; }
    public float combatPower { get; set; }
    public int currentCharges { get; set; }
    public int maxChargesPerTurn { get; set; } //-1 means no reset... we use the charge then its gone I think is the idea
    public int range { get; set; }
    public String iconPath { get; set; }
    public TargetSpecification validTargetTypes { get; set; }

    public UnitAbility(int usingUnitID, UnitEffect effect, float combatPower = 0.0f, int maxChargesPerTurn = 1, int range = 0, TargetSpecification validTargetTypes = null, String iconPath = "")
    {
        this.usingUnitID = usingUnitID;
        name = effect.functionName;
        this.effect = effect;
        this.iconPath = iconPath;
        this.combatPower = combatPower;
        this.maxChargesPerTurn = maxChargesPerTurn;
        this.currentCharges = maxChargesPerTurn;
        this.range = range;
        if(validTargetTypes == null)
        {
            validTargetTypes = new TargetSpecification();
        }
        this.validTargetTypes = validTargetTypes;
    }
    public UnitAbility()
    {
    }

    public void Serialize(BinaryWriter writer)
    {
        Serializer.Serialize(writer, this);
    }

    public static UnitAbility Deserialize(BinaryReader reader)
    {
        return Serializer.Deserialize<UnitAbility>(reader);
    }


    public void ResetAbilityUses()
    {
        if(maxChargesPerTurn > -1)
        {
            currentCharges = maxChargesPerTurn;
        }
    }

    public bool ActivateAbility(GameHex abilityTarget = null)
    {
        if(currentCharges > 0)
        {
            currentCharges -= 1;
            if(Global.gameManager.game.TryGetGraphicManager(out GraphicManager manager))
            {
                manager.Update2DUI(UIElement.unitDisplay);
            }
            if (abilityTarget != null)
            {
                return effect.Apply(usingUnitID, combatPower, abilityTarget);
            }
            else
            {
                return effect.Apply(usingUnitID, combatPower);
            }
        }
        return false;
    }

    // public List<Hex> ValidAbilityTargets(Unit unit)
    // {
    //     foreach(Hex hex in unit.hex.WrappingRange(range, Global.gameManager.game.mainGameBoard.left, Global.gameManager.game.mainGameBoard.right, Global.gameManager.game.mainGameBoard.top, Global.gameManager.game.mainGameBoard.bottom))
    //     {
    //         IsValidTarget(UnitType? unitType, UnitClass? unitClass, String? buildingType, TerrainType? terrainType, bool isEnemy = false, bool isAlly = false)
    //         //TODO
    //     }
    // }
}
