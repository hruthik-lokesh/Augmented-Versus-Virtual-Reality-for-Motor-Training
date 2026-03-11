using UnityEngine;

/// <summary>
/// Defines the 2 experiment conditions for the new XR training system.
/// </summary>
public enum ExperimentCondition
{
    [InspectorName("1. Reactive")]
    Reactive = 1,

    [InspectorName("2. Baseline")]
    Baseline = 2
}

/// <summary>
/// Represents a single trial's configuration.
/// </summary>
[System.Serializable]
public struct TrialConfig
{
    /// <summary>1 = X (horizontal), 2 = Y (vertical)</summary>
    public int Axis;

    /// <summary>1 = Positive (go0→go251), 2 = Negative (go251→go0)</summary>
    public int MoveDir;

    public TrialConfig(int axis, int moveDir)
    {
        Axis = axis;
        MoveDir = moveDir;
    }

    /// <summary>Is this a horizontal sine wave?</summary>
    public bool IsHorizontal => Axis == 1;

    /// <summary>Is this a vertical sine wave?</summary>
    public bool IsVertical => Axis == 2;

    /// <summary>Is direction positive (go0 is start)?</summary>
    public bool IsPositiveDirection => MoveDir == 1;

    /// <summary>Name of the starting sphere</summary>
    public string StartPointName => IsPositiveDirection ? "go0" : "go251";

    /// <summary>Name of the ending sphere</summary>
    public string EndPointName => IsPositiveDirection ? "go251" : "go0";

    /// <summary>Axis name for display</summary>
    public string AxisName => IsHorizontal ? "X (Horizontal)" : "Y (Vertical)";

    /// <summary>Direction name for display</summary>
    public string DirectionName => IsPositiveDirection ? "Positive" : "Negative";

    public override string ToString()
    {
        return $"Axis={AxisName}, Dir={DirectionName}, Start={StartPointName}";
    }
}

/// <summary>
/// Helper class for ExperimentCondition operations.
/// </summary>
public static class ExperimentConditionHelper
{
    public static string GetDisplayName(ExperimentCondition condition)
    {
        return condition == ExperimentCondition.Reactive ? "Reactive" : "Baseline";
    }

    public static string GetDescription(ExperimentCondition condition)
    {
        return condition == ExperimentCondition.Reactive
            ? "Reactive - Coins revealed on approach"
            : "Baseline - No coins, sine wave tracing only";
    }

    /// <summary>
    /// Returns true if coins should be placed in this condition.
    /// </summary>
    public static bool HasCoins(ExperimentCondition condition)
    {
        return condition == ExperimentCondition.Reactive;
    }
}
