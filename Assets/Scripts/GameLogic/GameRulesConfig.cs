using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameRule", menuName = "Game/Logic/GameRule Config")]
public class GameRulesConfig : ScriptableObject
{
    [Header("Turn Settings")]
    public float turnDuration = 60f;
    public int maxTurns = 30;

    [Header("Victory Conditions")]
    public bool enableUnitCountCondition = true;
    public bool enableTurnLimitCondition = true;
    public bool enableInfiniteMoves = true;
    public int infiniteMovesTurn = 15;
}
