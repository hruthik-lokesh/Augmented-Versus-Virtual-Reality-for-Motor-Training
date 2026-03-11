using System;
using System.Linq;
using UnityEngine;

public static class StaticValsReach7
{
    /// <summary>
    /// The currently requested experiment mode condition (1-6)
    /// 1=Proactive_VR, 2=Reactive_VR, 3=Baseline_VR, 4=Proactive_AR, 5=Reactive_AR, 6=Baseline_AR
    /// </summary>
    public static ExperimentModeCondition requestedExperimentMode = ExperimentModeCondition._3_Baseline_VR;

    /// <summary>
    /// Legacy property for backward compatibility - returns behavior mode (0, 1, or 2)
    /// 0=Baseline, 1=Proactive, 2=Reactive
    /// </summary>
    public static int requestedMode
    {
        get => ExperimentModeConditionHelper.GetBehaviorMode(requestedExperimentMode);
        set
        {
            // Convert legacy mode to new enum (assumes VR for backward compatibility)
            switch (value)
            {
                case 1:
                    requestedExperimentMode = ExperimentModeCondition._1_Proactive_VR;
                    break;
                case 2:
                    requestedExperimentMode = ExperimentModeCondition._2_Reactive_VR;
                    break;
                default:
                    requestedExperimentMode = ExperimentModeCondition._3_Baseline_VR;
                    break;
            }
        }
    }

    // Possible coin locations in cm (will be converted to Unity units by dividing by 100)
    public static int[] possiblelocations = { 52, 63, 74, 115, 125, 136, 178, 188, 199 };

    // Training trial locations and directions
    public static int[] breaklocations_training = { 188, 115, 136, 52, 74, 199, 52, 199, 136, 63, 115, 74, 125, 52, 125, 63, 178, 74, 63, 74, 188, 188, 52, 115, 136, 178, 125, 199, 136, 63, 115, 199, 125, 178, 188, 178 };
    public static int[] dir_training = { 1, 1, 2, 2, 2, 1, 1, 2, 1, 2, 2, 1, 1, 1, 1, 2, 2, 1, 1, 2, 1, 2, 2, 2, 1, 2, 2, 1, 2, 1, 1, 2, 2, 1, 2, 1 };

    // Post-training locations and directions
    public static int[] breaklocations_post = { 74, 63, 125, 188, 115, 178, 52, 199, 125, 52, 63, 136, 178, 136, 188, 115, 74, 199 };
    public static int[] dir_post = { 1, 1, 1, 1, 2, 2, 2, 2, 2, 1, 2, 1, 1, 2, 2, 1, 2, 1 };

    // Current state
    public static int curindex = 0;
    public static bool restart = false;
    public static int breakloc;
    public static int direc;
    public static int block;

    // Random coin values
    public static int ran1;
    public static int ran2;
    public static int ran3;

    /// <summary>
    /// Select 3 locations from predefined sets with minimum 50 units apart (?10cm physical spacing)
    /// </summary>
    public static int[] Shuffle()
    {
        System.Random rand = new System.Random();

        // Predefined sets with guaranteed 50+ unit spacing between all pairs
        // Format: {left, middle, right} - ensures good physical separation (~10cm+)
        int[][] validSets = new int[][]
        {
            // Sets with 50+ spacing (?10cm physical distance)
            new int[] {52, 115, 178},   // spacing: 63, 126, 63
            new int[] {52, 115, 188},   // spacing: 63, 136, 73
            new int[] {52, 125, 178},   // spacing: 73, 126, 53
            new int[] {52, 125, 188},   // spacing: 73, 136, 63
            new int[] {52, 125, 199},   // spacing: 73, 147, 74
            new int[] {52, 136, 188},   // spacing: 84, 136, 52
            new int[] {52, 136, 199},   // spacing: 84, 147, 63
            new int[] {63, 115, 178},   // spacing: 52, 115, 63
            new int[] {63, 115, 188},   // spacing: 52, 125, 73
            new int[] {63, 125, 178},   // spacing: 62, 115, 53
            new int[] {63, 125, 188},   // spacing: 62, 125, 63
            new int[] {63, 125, 199},   // spacing: 62, 136, 74
            new int[] {63, 136, 188},   // spacing: 73, 125, 52
            new int[] {63, 136, 199},   // spacing: 73, 136, 63
            new int[] {74, 125, 178},   // spacing: 51, 104, 53
            new int[] {74, 125, 188},   // spacing: 51, 114, 63
            new int[] {74, 125, 199},   // spacing: 51, 125, 74
            new int[] {74, 136, 188},   // spacing: 62, 114, 52
            new int[] {74, 136, 199},   // spacing: 62, 125, 63
        };

        // Randomly select one of the valid sets
        int idx = rand.Next(validSets.Length);
        int[] selectedNumbers = validSets[idx];

        // Validate spacing (should always pass, but log for verification)
        int d01 = Math.Abs(selectedNumbers[0] - selectedNumbers[1]);
        int d02 = Math.Abs(selectedNumbers[0] - selectedNumbers[2]);
        int d12 = Math.Abs(selectedNumbers[1] - selectedNumbers[2]);

        if (d01 < 50 || d02 < 50 || d12 < 50)
        {
            Debug.LogError($"StaticValsReach7: SPACING ERROR! [{selectedNumbers[0]}, {selectedNumbers[1]}, {selectedNumbers[2]}] distances: {d01}, {d02}, {d12}");
        }
        else
        {
            Debug.Log($"StaticValsReach7: Using set #{idx} [{selectedNumbers[0]}, {selectedNumbers[1]}, {selectedNumbers[2]}] distances: {d01}, {d02}, {d12} (all ?50 units ?10cm)");
        }

        return selectedNumbers;
    }

    /// <summary>
    /// Set trial parameters (simplified for XR - removed networking code)
    /// </summary>
    public static (int, bool, int, int, int, int, int) Set(int next)
    {
        Debug.Log($"StaticValsReach7.Set() called - Mode: {ExperimentModeConditionHelper.GetDisplayName(requestedExperimentMode)} ({ExperimentModeConditionHelper.GetModeDescription(requestedExperimentMode)})");

        // Increment trial index
        curindex = curindex + next;
        restart = true;

        // Get trial parameters from training arrays
        if (curindex < breaklocations_training.Length)
        {
            breakloc = breaklocations_training[curindex];
            direc = dir_training[curindex];
        }
        else
        {
            Debug.LogWarning($"curindex {curindex} exceeds training array length. Using post-training values.");
            int postIndex = curindex - breaklocations_training.Length;
            if (postIndex < breaklocations_post.Length)
            {
                breakloc = breaklocations_post[postIndex];
                direc = dir_post[postIndex];
            }
            else
            {
                Debug.LogWarning($"curindex exceeds all arrays. Resetting to 0.");
                curindex = 0;
                breakloc = breaklocations_training[0];
                direc = dir_training[0];
            }
        }

        // Get random coin locations
        var rrvv = Shuffle();
        /*
         * 
 
        int[] ranval = new int[3];
        ranval[0] = rrvv[0];
         




        // Additional spacing check (from your original code)
        int j = 1;
        for (int k = 1; k < 3; k++)
        {
            if (k == 1)
            {
                if (Math.Abs(rrvv[j] - ranval[k - 1]) < 12)
                {
                    ranval[k] = rrvv[j + 1];
                    j = j + 2;
                }
                else
                {
                    ranval[k] = rrvv[j];
                    j++;
                }
            }
            else if (k == 2)
            {
                if (Math.Abs(rrvv[k] - ranval[k - 1]) < 12 || Math.Abs(rrvv[k] - ranval[k - 2]) < 12)
                {
                    ranval[k] = rrvv[j + 1];
                    j = j + 2;
                }
                else
                {
                    ranval[k] = rrvv[j];
                    j++;
                }
            }
        }
        */



        ran1 = rrvv[0];
        ran2 = rrvv[1];
        ran3 = rrvv[2];

        Debug.Log($"StaticValsReach7.Set() - curindex: {curindex}, breakloc: {breakloc}, direc: {direc}");
        Debug.Log($"StaticValsReach7.Set() - Coin locations: [{ran1}, {ran2}, {ran3}]");

        return (curindex, restart, breakloc, direc, ran1, ran2, ran3);
    }

    /// <summary>
    /// Reset the trial counter
    /// </summary>
    public static void Reset()
    {
        curindex = 0;
        restart = false;
        Debug.Log("StaticValsReach7 reset to initial state");
    }

    /// <summary>
    /// Set the experiment mode condition directly
    /// </summary>
    public static void SetExperimentMode(ExperimentModeCondition mode)
    {
        requestedExperimentMode = mode;
        Debug.Log($"StaticValsReach7: Mode set to {ExperimentModeConditionHelper.GetDisplayName(mode)} ({(int)mode}) - {ExperimentModeConditionHelper.GetModeDescription(mode)}");
    }
}


//using System;
//using System.Linq;
//using UnityEngine;

//public static class StaticValsReach7
//{
//    public static int requestedMode = 0; // 0 = normal, 1 = proactive, 2 = reactive

//    // Possible coin locations in cm (will be converted to Unity units by dividing by 100)
//    public static int[] possiblelocations = { 52, 63, 74, 115, 125, 136, 178, 188, 199 };

//    // Training trial locations and directions
//    public static int[] breaklocations_training = { 188, 115, 136, 52, 74, 199, 52, 199, 136, 63, 115, 74, 125, 52, 125, 63, 178, 74, 63, 74, 188, 188, 52, 115, 136, 178, 125, 199, 136, 63, 115, 199, 125, 178, 188, 178 };
//    public static int[] dir_training = { 1, 1, 2, 2, 2, 1, 1, 2, 1, 2, 2, 1, 1, 1, 1, 2, 2, 1, 1, 2, 1, 2, 2, 2, 1, 2, 2, 1, 2, 1, 1, 2, 2, 1, 2, 1 };

//    // Post-training locations and directions
//    public static int[] breaklocations_post = { 74, 63, 125, 188, 115, 178, 52, 199, 125, 52, 63, 136, 178, 136, 188, 115, 74, 199 };
//    public static int[] dir_post = { 1, 1, 1, 1, 2, 2, 2, 2, 2, 1, 2, 1, 1, 2, 2, 1, 2, 1 };

//    // Current state
//    public static int curindex = 0;
//    public static bool restart = false;
//    public static int breakloc;
//    public static int direc;
//    public static int block;

//    // Random coin values
//    public static int ran1;
//    public static int ran2;
//    public static int ran3;

//    /// <summary>
//    /// Select 3 random locations from possiblelocations with minimum 15 units apart (all pairs)
//    /// </summary>
//    public static int[] Shuffle()
//    {
//        System.Random rand = new System.Random();
//        var shuffled = possiblelocations.OrderBy(x => rand.Next()).ToArray();

//        // Try all combinations to find 3 numbers with all pairs >= 15 units apart
//        for (int i = 0; i < shuffled.Length; i++)
//        {
//            for (int j = i + 1; j < shuffled.Length; j++)
//            {
//                if (Math.Abs(shuffled[i] - shuffled[j]) < 15) continue;
//                for (int k = j + 1; k < shuffled.Length; k++)
//                {
//                    if (Math.Abs(shuffled[i] - shuffled[k]) < 15) continue;
//                    if (Math.Abs(shuffled[j] - shuffled[k]) < 15) continue;
//                    // Found a valid set
//                    Debug.Log($"StaticValsReach7.Shuffle() returned: [{shuffled[i]}, {shuffled[j]}, {shuffled[k]}]");
//                    return new int[] { shuffled[i], shuffled[j], shuffled[k] };
//                }
//            }
//        }

//        // Fallback: just pick the first three if no valid set found
//        Debug.LogWarning("StaticValsReach7: Could not find 3 locations with 15+ units spacing. Using closest available.");
//        return shuffled.Take(3).ToArray();
//    }

//    /// <summary>
//    /// Set trial parameters (simplified for XR - removed networking code)
//    /// </summary>
//    public static (int, bool, int, int, int, int, int) Set(int next)
//    {
//        Debug.Log("StaticValsReach7.Set() called");

//        // Increment trial index
//        curindex = curindex + next;
//        restart = true;

//        // Get trial parameters from training arrays
//        if (curindex < breaklocations_training.Length)
//        {
//            breakloc = breaklocations_training[curindex];
//            direc = dir_training[curindex];
//        }
//        else
//        {
//            Debug.LogWarning($"curindex {curindex} exceeds training array length. Using post-training values.");
//            int postIndex = curindex - breaklocations_training.Length;
//            if (postIndex < breaklocations_post.Length)
//            {
//                breakloc = breaklocations_post[postIndex];
//                direc = dir_post[postIndex];
//            }
//            else
//            {
//                Debug.LogWarning($"curindex exceeds all arrays. Resetting to 0.");
//                curindex = 0;
//                breakloc = breaklocations_training[0];
//                direc = dir_training[0];
//            }
//        }

//        // Get random coin locations
//        var rrvv = Shuffle();

//        int[] ranval = new int[3];
//        ranval[0] = rrvv[0];

//        // Additional spacing check (from your original code)
//        int j = 1;
//        for (int k = 1; k < 3; k++)
//        {
//            if (k == 1)
//            {
//                if (Math.Abs(rrvv[j] - ranval[k - 1]) < 12)
//                {
//                    ranval[k] = rrvv[j + 1];
//                    j = j + 2;
//                }
//                else
//                {
//                    ranval[k] = rrvv[j];
//                    j++;
//                }
//            }
//            else if (k == 2)
//            {
//                if (Math.Abs(rrvv[k] - ranval[k - 1]) < 12 || Math.Abs(rrvv[k] - ranval[k - 2]) < 12)
//                {
//                    ranval[k] = rrvv[j + 1];
//                    j = j + 2;
//                }
//                else
//                {
//                    ranval[k] = rrvv[j];
//                    j++;
//                }
//            }
//        }

//        ran1 = ranval[0];
//        ran2 = ranval[1];
//        ran3 = ranval[2];

//        Debug.Log($"StaticValsReach7.Set() - curindex: {curindex}, breakloc: {breakloc}, direc: {direc}");
//        Debug.Log($"StaticValsReach7.Set() - Coin locations: [{ran1}, {ran2}, {ran3}]");

//        return (curindex, restart, breakloc, direc, ran1, ran2, ran3);
//    }

//    /// <summary>
//    /// Reset the trial counter
//    /// </summary>
//    public static void Reset()
//    {
//        curindex = 0;
//        restart = false;
//        Debug.Log("StaticValsReach7 reset to initial state");
//    }
//}