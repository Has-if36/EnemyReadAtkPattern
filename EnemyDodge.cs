using sys = System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;

public class EnemyDodge : MonoBehaviour
{
    /**
     * This Component should be Add on Enemy
     */

    public GameObject player;
    public bool smartDodge = true, customLearningRate = false, ableDodgeCounter = true;
    public float DODGE_RATE = 0.5f;
    public int intel = 10;

    /**Dodging Example Usage
     * // AttackID is to prevent duplicate attacks in a single weapon swing or attack
     * if (GetComponent<BoxCollider2D>().IsTouching(enemyHitBox) && enemy.attackID != attackID)
        {
            enemy.attackID = attackID;

            bool dodging = enemy.DodgeDecision();
            if (!dodging)
            {
                // Code for Enemy Miss
            }
            else
            {
                // Code for Enemy Hit
            }
            enemy.ClearDecision();
        }
     */

    [System.NonSerialized]
    public bool dodging = false;        // Triggers dodge when got triggered by hitbox // isDodging = false;

    /**
     * constVarDodgeAct is the probability for enemy to parry Player's Attack.
     * Higher consecutive dodge will increase parry probability.
     * If the number of consecutive dodge == constVarDodgeAct, parry probability should equal 1.0f
     * 
     * Formula to determine parry threshold and example usage
     *      parryThresh = Mathf.Clamp01(Mathf.Exp(constVarDodgeAct / consecutDodge));
     *      if (Random.value < parryThresh) {
     *          // Code for Parry
     *      }
     */

    public float constVarDodgeAct = 15.0f;
    [System.NonSerialized]
    public int consecutDodge = 0;

    private const float MIN_Q = 1.0f, MAX_Q = 10.0f;
    private float dodgeRand = 0.0f;
    private AttackPattern ap;

    /*
     * isDodging = Dodge status
     */

    /**
     * AI Stuff
     * Epsilon = Rate of Exloring (Higher = Lower Chance of Exploring)
     * Decay Rate (Not Used)
     * Discount Rate = Rate for how important is the reward (Used for Short Term, Long Term)
     * Learning Rate = Rate for how fast AI can Learn / Adapt
     * 
     * Action   : N , where N = (Attack Type) ^ (Attack Chain or Combo)
     *              - Predict Pattern based on N List Combo
     * 
     * Observation: 1 
     *              - Player's Attack
     */

    [SerializeField] private float learningRate;
    private float totalScore;
    private const float HIT_PENALTY = -100.0f, DODGE_REWARD = 10.0f;
    private const float MIN_LEARNING_RATE = 0.35f, MAX_LEARNING_RATE = 0.55f;
    private static float epsilon = 0.05f, discountRate = 0.95f;
    private float[] qTable;     // Since there is only 1 Env, no need 2D array
    private int predAct, currKnownChain;
    private float maxFutureQ;
    private static float chainMultiplier = 1.0f, actualMult = 0.5f;

    /*
     * chainMultiplier = Multiplier for Combo
     * actualMult = Multiplier for Actual Result from prediction
     */

    // Start is called before the first frame update
    void Start()
    {
        dodging = false; // isDodging = false;

        if (player == null) player = GameObject.Find("Player");

        ap = player.GetComponent<AttackPattern>();

        if (GetComponent<EnemyStats>() != null) intel = GetComponent<EnemyStats>().intel;

        try {
            // Generate Q Table
            // Debug.Log("LengthArr: " + player.GetComponent<PlayerMovement>().patternList.GetLength(0));
            qTable = new float[AttackPattern.patternList.GetLength(0)];
            // Action (Enemy's Reading Prediction)
            for (int i = 0; i < qTable.GetLength(0); i++)
            {
                qTable[i] = Random.Range(10.0f, 20.0f); // Random.value;
            }
            currKnownChain = ap.currGeneratedChain;
            totalScore = 0.0f;
        } catch (UnityException e)
        {
            Debug.LogWarning("EnemyAI: " + e);
        } catch (sys::NullReferenceException e)
        {
            Debug.LogWarning(e);
        }

        if (smartDodge)
        {
            if (!customLearningRate)
            {
                // Calculate based on Enemy's Intelligence
                float sigmoid = 1.0f / (1.0f + 100.0f * Mathf.Exp(-0.09f * intel));

                // Calculate Learning Rate
                learningRate = sigmoid * (MAX_LEARNING_RATE - MIN_LEARNING_RATE) + MIN_LEARNING_RATE;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Triggered when player hit enemy's hitbox
    public bool DodgeDecision()
    {
        ap = player.GetComponent<AttackPattern>();

        if (smartDodge)
        {
            SmartDodge();
        }
        else
        {
            dodgeRand = Random.value;
            if (dodgeRand <= DODGE_RATE)
            {
                dodging = true;
                consecutDodge++;
            }
            else
            {
                dodging = false;
                consecutDodge = 0;
            }
        }

        return dodging;
    }

    public void ClearDecision()
    {
        dodging = false;
        // CalculateScore();
        StartCoroutine(CalculateScore());
    }

    void SmartDodge()
    {
        if (ap.attackChainLim != ap.currGeneratedChain)
        {
            ap.GenerateAtkPattern();
        }

        if (currKnownChain != ap.currGeneratedChain)
        {
            // Reconstruct Q Table
            qTable = new float[AttackPattern.patternList.GetLength(0)];

            // Action (Enemy's Reading Prediction)
            for (int i = 0; i < qTable.GetLength(0); i++)
            {
                qTable[i] = Random.Range(10.0f, 20.0f); // Random.value;
            }
            currKnownChain = ap.currGeneratedChain;
        }

        // Smart Dodge
        float rand = Random.value;
        if (rand > epsilon)
        {
            string qTableStr = "";
            for (int i = 0; i < qTable.GetLength(0); i++)
            {
                qTableStr += "Index " + i + ": " + qTable[i] + ", ";
            }
            Debug.Log(qTableStr);

            // Not Explore
            maxFutureQ = qTable.Max();  
            predAct = qTable.ToList().IndexOf(maxFutureQ);    // Requires Namespace System.Linq
        }
        else
        {
            // Explore
            predAct = Random.Range(0, AttackPattern.patternList.GetLength(0));
        }

        bool similarCombo = true;
        string atk1 = "Atk 1: ", atk2 = "Atk 2: ";
        for (int i = 0; i < ap.currAttackChain; i++)
        {
            try
            {
                atk1 += AttackPattern.patternList[predAct, i] + ", ";
                atk2 += ap.attackChainStr[i] + ", ";
                if (!AttackPattern.patternList[predAct, i].Equals(ap.attackChainStr[i]))
                {
                    similarCombo = false;
                    break;
                }
            } catch (sys.Exception e)
            {
                Debug.LogError(e);
                break;
            }
            
        }

        atk1 += "\b\b";
        atk2 += "\b\b";

        if (similarCombo)
        {
            dodging = true;
            consecutDodge++;
        }
        else
        {
            dodging = false;
            consecutDodge = 0;
        }
    }

    IEnumerator CalculateScore()
    {
        if (smartDodge)
        {
            if (dodging)
            {
                totalScore = DODGE_REWARD;
            }
            else
            {
                totalScore = HIT_PENALTY;
            }

            /*
             * Loop:
             * To read and validate array that has similar sequence.
             * If sequence is similar, its qTable will be updated
             */

            float currQ, newQ;
            for (int i = 0; i < qTable.Length; i++)
            {
                bool similarInSeq = true;
                if (dodging)
                {
                    for (int j = 0; j < ap.currAttackChain; j++)
                    {
                        if (!AttackPattern.patternList[i, j].Equals(AttackPattern.patternList[predAct, j]))
                        {
                            similarInSeq = false;
                            break;
                        }
                    }

                    // Current Action
                    if (similarInSeq)
                    {

                        totalScore *= Mathf.Pow(ap.currAttackChain, chainMultiplier); // Score Multiplier
                        currQ = qTable[i];
                        newQ = (1 - learningRate) * currQ + learningRate * (totalScore + discountRate * maxFutureQ);
                        qTable[i] = Mathf.Clamp(newQ, MIN_Q, MAX_Q);
                    }
                }
                else
                {
                    // if (i == 0) Debug.Log("Wrong Guess");
                    for (int j = 0; j < ap.currAttackChain; j++)
                    {
                        try
                        {
                            if (!AttackPattern.patternList[i, j].Equals(ap.attackChainStr[j]))
                            {
                                similarInSeq = false;
                                break;
                            }
                        }
                        catch (sys.Exception e)
                        {
                            Debug.LogError("J: " + j + "\n" + e);
                        }

                    }

                    // Current Action
                    if (similarInSeq)
                    {
                        totalScore = DODGE_REWARD * Mathf.Pow(ap.currAttackChain, chainMultiplier) * actualMult;
                        currQ = qTable[i];
                        newQ = (1 - learningRate) * currQ + learningRate * (totalScore + discountRate * maxFutureQ);
                        qTable[i] = Mathf.Clamp(newQ, MIN_Q, MAX_Q);
                    }
                    else
                    {
                        totalScore *= Mathf.Pow(ap.currAttackChain, chainMultiplier); // Score Multiplier
                        currQ = qTable[i];
                        newQ = (1 - learningRate) * currQ + learningRate * (totalScore + discountRate * maxFutureQ);
                        qTable[i] = Mathf.Clamp(newQ, MIN_Q, MAX_Q);
                    }
                }
            }
        }

        yield break;
    }

    //public void CalculateScore()
    //{
    //    if (smartDodge)
    //    {
    //        if (dodging)
    //        {
    //            totalScore = DODGE_REWARD;
    //        } else
    //        {
    //            totalScore = HIT_PENALTY;
    //        }

    //        // Loop Here
    //        EnemyThreadJob enemyJob = new EnemyThreadJob();
    //        enemyJob.qTable = new NativeArray<float>(qTable.Length, Allocator.TempJob);
    //        enemyJob.isDodging = dodging;
    //        enemyJob.learningRate = learningRate;
    //        enemyJob.totalScore = totalScore;
    //        // enemyJob.currAttackChain = player.GetComponent<PlayerMovement>().ap.currAttackChain;
    //        enemyJob.ap = player.GetComponent<AttackPattern>();
    //        enemyJob.predAct = predAct;
    //        enemyJob.maxFutureQ = maxFutureQ;

    //        enemyJob.qTable.CopyFrom(enemyJob.qTable);

    //        // Schedule the job with one Execute per index in the results array and only 1 item per processing batch
    //        JobHandle jobHandle = enemyJob.Schedule(qTable.Length, 1);
    //        jobHandle.Complete();
    //        enemyJob.qTable.CopyTo(qTable);
    //        enemyJob.qTable.Dispose();
    //    }
    //}

    //struct EnemyThreadJob : IJobParallelFor
    //{
    //    public NativeArray<float> qTable;
    //    public bool isDodging;
    //    public float learningRate, totalScore, maxFutureQ;
    //    public int predAct; // currAttackChain,
    //    public AttackPattern ap;

    //    private float currQ, newQ;

    //    public void Execute(int i)
    //    {
    //        /*
    //         * Loop:
    //         * To read and validate array that has similar sequence.
    //         * If sequence is similar, its qTable will be updated
    //         */

    //        bool similarInSeq = true;
    //        if (isDodging)
    //        {
    //            for (int j = 0; j < ap.currAttackChain; j++)
    //            {
    //                if (!AttackPattern.patternList[i, j].Equals(AttackPattern.patternList[predAct, j]))
    //                {
    //                    similarInSeq = false;
    //                    break;
    //                }
    //            }

    //            // Current Action
    //            if (similarInSeq)
    //            {

    //                totalScore *= Mathf.Pow(ap.currAttackChain, chainMultiplier); // Score Multiplier
    //                currQ = qTable[i];
    //                newQ = (1 - learningRate) * currQ + learningRate * (totalScore + discountRate * maxFutureQ);
    //                qTable[i] = Mathf.Clamp(newQ, MIN_Q, MAX_Q);
    //            }
    //        }
    //        else
    //        {
    //            // if (i == 0) Debug.Log("Wrong Guess");
    //            for (int j = 0; j < ap.currAttackChain; j++)
    //            {
    //                try
    //                {
    //                    if (!AttackPattern.patternList[i, j].Equals(ap.attackChainStr[j]))
    //                    {
    //                        similarInSeq = false;
    //                        break;
    //                    }
    //                } catch (sys.Exception e)
    //                {
    //                    Debug.LogError("J: " + j + "\n" + e);
    //                }

    //            }

    //            // Current Action
    //            if (similarInSeq)
    //            {
    //                totalScore = DODGE_REWARD * Mathf.Pow(ap.currAttackChain, chainMultiplier) * actualMult;
    //                currQ = qTable[i];
    //                newQ = (1 - learningRate) * currQ + learningRate * (totalScore + discountRate * maxFutureQ);
    //                qTable[i] = Mathf.Clamp(newQ, MIN_Q, MAX_Q);
    //            }
    //            else
    //            {
    //                totalScore *= Mathf.Pow(ap.currAttackChain, chainMultiplier); // Score Multiplier
    //                currQ = qTable[i];
    //                newQ = (1 - learningRate) * currQ + learningRate * (totalScore + discountRate * maxFutureQ);
    //                qTable[i] = Mathf.Clamp(newQ, MIN_Q, MAX_Q);
    //            }
    //        }
    //    }
    //}
}
