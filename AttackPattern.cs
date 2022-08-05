using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackPattern: MonoBehaviour
{
    /**
     * This Component should be Add on Player
     * 
        currAttackChain will be used as index forattackChain & attackChainStr.
        attackChainLim is the Limit Attack Combo, depending on player's proficiency. (5 (or maybe 4?) is the maximum combo / proficiency)
     */
    public string[] attackChainStr = { "", "", "", "", "" };
    public short attackChainLim = 2, currAttackChain = 0, currGeneratedChain = 0;
    public static readonly string[] attackData = { "Slash", "Pierce", "Slash Up", "Pierce Down" };
    public static string[,] patternList;

    public void GenerateAtkPattern()
    {
        int numPatern = (int)Mathf.Pow(attackData.Length, attackChainLim);
        patternList = new string[numPatern, attackChainLim];
        int tempSize = 0;
        for (int i = 0; i < attackChainLim; i++)
        {
            tempSize = numPatern / (int)Mathf.Pow(attackData.Length, (i + 1));
            for (int j = 0; j < patternList.GetLength(0); j++)
            {
                patternList[j, i] = attackData[(int)Mathf.Floor(j / tempSize) % attackData.Length];
            }
        }
        currGeneratedChain = attackChainLim;
    }
}
