using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Static trial state for the 2-mode experiment system.
/// Manages 12 randomized trials per session (3 per Axis/MoveDir combination).
/// </summary>
public static class StaticValsReach8
{
    // ===== Experiment Condition =====
    public static ExperimentCondition condition = ExperimentCondition.Baseline;

    // ===== Current Trial State =====
    public static int curindex = 0;
    public static int currentAxis = 1;      // 1=X, 2=Y
    public static int currentMoveDir = 1;   // 1=Positive, 2=Negative
    public static bool restart = false;

    // ===== Coin Locations =====
    public static int ran1;
    public static int ran2;
    public static int ran3;

    // ===== Trial Order =====
    private static TrialConfig[] trialOrder;
    public static int TotalTrials => 12;

    // ===== Predefined Coin Location Sets =====
    // Each set has 3 locations with 50+ unit spacing (~10cm physical distance)
    public static int[][] validSets = new int[][]
    {
        new int[] {52, 115, 178},   // spacing: 63, 126, 63
        new int[] {63, 115, 178},   // spacing: 52, 115, 63
        new int[] {74, 125, 178},   // spacing: 51, 104, 53
        new int[] {52, 115, 188},   // spacing: 63, 136, 73
        new int[] {63, 115, 188},   // spacing: 52, 125, 73
        new int[] {74, 125, 188},   // spacing: 51, 114, 63
        new int[] {52, 125, 178},   // spacing: 73, 126, 53
        new int[] {63, 125, 178},   // spacing: 62, 115, 53
        new int[] {74, 125, 199},   // spacing: 51, 125, 74
        new int[] {52, 125, 188},   // spacing: 73, 136, 63
        new int[] {63, 125, 188},   // spacing: 62, 125, 63
        new int[] {74, 136, 188},   // spacing: 62, 114, 52
        new int[] {52, 125, 199},   // spacing: 73, 147, 74
        new int[] {63, 125, 199},   // spacing: 62, 136, 74
        new int[] {74, 136, 199},   // spacing: 62, 125, 63
        new int[] {52, 136, 188},   // spacing: 84, 136, 52
        new int[] {63, 136, 188},   // spacing: 73, 125, 52
        new int[] {52, 136, 199},   // spacing: 84, 147, 63
        new int[] {63, 136, 199},   // spacing: 73, 136, 63
    };

    /// <summary>
    /// Generate the randomized trial order for a session.
    /// Creates 12 trials: 3 each for (X,+), (X,-), (Y,+), (Y,-), then shuffles.
    /// Call this once at the start of a session.
    /// </summary>
    public static void GenerateTrialOrder()
    {
        List<TrialConfig> trials = new List<TrialConfig>();

        // 3 trials for each of 4 combinations
        for (int i = 0; i < 3; i++)
        {
            trials.Add(new TrialConfig(1, 1)); // X, Positive
            trials.Add(new TrialConfig(1, 2)); // X, Negative
            trials.Add(new TrialConfig(2, 1)); // Y, Positive
            trials.Add(new TrialConfig(2, 2)); // Y, Negative
        }

        // Fisher-Yates shuffle
        System.Random rand = new System.Random();
        for (int i = trials.Count - 1; i > 0; i--)
        {
            int j = rand.Next(0, i + 1);
            TrialConfig temp = trials[i];
            trials[i] = trials[j];
            trials[j] = temp;
        }

        trialOrder = trials.ToArray();

        // Log the generated order
        Debug.Log("=== StaticValsReach8: Generated Trial Order ===");
        for (int i = 0; i < trialOrder.Length; i++)
        {
            Debug.Log($"  Trial {i + 1}: {trialOrder[i]}");
        }
    }

    /// <summary>
    /// Get the trial config for the current trial index.
    /// </summary>
    public static TrialConfig GetCurrentTrial()
    {
        if (trialOrder == null || trialOrder.Length == 0)
        {
            Debug.LogWarning("StaticValsReach8: Trial order not generated! Calling GenerateTrialOrder()...");
            GenerateTrialOrder();
        }

        if (curindex < 0 || curindex >= trialOrder.Length)
        {
            Debug.LogWarning($"StaticValsReach8: curindex {curindex} out of range (0-{trialOrder.Length - 1}). Clamping.");
            curindex = Mathf.Clamp(curindex, 0, trialOrder.Length - 1);
        }

        return trialOrder[curindex];
    }

    /// <summary>
    /// Initialize the first trial (call at session start).
    /// </summary>
    public static void InitializeSession()
    {
        curindex = 0;
        restart = false;
        GenerateTrialOrder();
        ApplyCurrentTrial();

        Debug.Log($"StaticValsReach8: Session started. Condition={ExperimentConditionHelper.GetDisplayName(condition)}, Total trials={TotalTrials}");
    }

    /// <summary>
    /// Advance to the next trial and apply its config.
    /// </summary>
    public static void AdvanceTrial()
    {
        curindex++;
        restart = true;

        if (curindex >= TotalTrials)
        {
            Debug.Log("StaticValsReach8: All 12 trials completed!");
            curindex = TotalTrials; // Keep at max to indicate completion
            return;
        }

        ApplyCurrentTrial();

        Debug.Log($"StaticValsReach8: Advanced to Trial {curindex + 1}/{TotalTrials}");
    }

    /// <summary>
    /// Apply the current trial's Axis/MoveDir and pick coin locations.
    /// </summary>
    private static void ApplyCurrentTrial()
    {
        TrialConfig trial = GetCurrentTrial();
        currentAxis = trial.Axis;
        currentMoveDir = trial.MoveDir;

        // Pick coin locations from predefined sets
        int[] coinSet = PickCoinSet();
        ran1 = coinSet[0];
        ran2 = coinSet[1];
        ran3 = coinSet[2];

        Debug.Log($"StaticValsReach8: Trial {curindex + 1} - {trial}");
        Debug.Log($"StaticValsReach8: Coin locations: [{ran1}, {ran2}, {ran3}]");
    }

    /// <summary>
    /// Pick a random coin location set from the predefined validSets.
    /// </summary>
    public static int[] PickCoinSet()
    {
        System.Random rand = new System.Random();
        int idx = rand.Next(validSets.Length);
        int[] selected = validSets[idx];

        Debug.Log($"StaticValsReach8: Using coin set #{idx}: [{selected[0]}, {selected[1]}, {selected[2]}]");
        return selected;
    }

    /// <summary>
    /// Check if all trials are completed.
    /// </summary>
    public static bool IsSessionComplete => curindex >= TotalTrials;

    /// <summary>
    /// Reset the session completely.
    /// </summary>
    public static void Reset()
    {
        curindex = 0;
        restart = false;
        currentAxis = 1;
        currentMoveDir = 1;
        ran1 = ran2 = ran3 = 0;
        trialOrder = null;
        Debug.Log("StaticValsReach8: Session reset.");
    }

    /// <summary>
    /// Set the experiment condition.
    /// </summary>
    public static void SetCondition(ExperimentCondition cond)
    {
        condition = cond;
        Debug.Log($"StaticValsReach8: Condition set to {ExperimentConditionHelper.GetDisplayName(cond)}");
    }
}