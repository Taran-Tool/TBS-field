using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IVictoryCondition
{
    GameResult? CheckCondition();
}

public class UnitCountVictoryCondition : IVictoryCondition
{
    public GameResult? CheckCondition()
    {
        var p1Units = NetworkUnitsManager.instance.GetPlayerUnits(Player.Player1).Count;
        var p2Units = NetworkUnitsManager.instance.GetPlayerUnits(Player.Player2).Count;

        if (p1Units == 0)
        {
            return GameResult.Player2Win;
        }
        if (p2Units == 0)
        {
            return GameResult.Player1Win;
        } 
        return null;
    }
}

public class Turn15InfiniteMoves:IVictoryCondition
{
    public GameResult? CheckCondition()
    {
        if (!NetworkTurnManager.instance.infiniteMode.Value)
        {
            return null;
        }

        var p1Units = NetworkUnitsManager.instance.GetPlayerUnits(Player.Player1).Count;
        var p2Units = NetworkUnitsManager.instance.GetPlayerUnits(Player.Player2).Count;

        if (p1Units != p2Units)
        {
            return p1Units > p2Units ? GameResult.Player1Win : GameResult.Player2Win;
        }
        
        return GameResult.InfiniteMoves;
    }
}

public enum GameResult
{
    None,
    Player1Win,
    Player2Win,
    InfiniteMoves
}
