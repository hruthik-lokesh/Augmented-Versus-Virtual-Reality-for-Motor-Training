using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

/// <summary>
/// Scene restart controller for the 2-mode experiment system.
/// Press R to advance to the next trial and reload the scene.
/// Displays current trial info in the Inspector.
/// </summary>
public class RestartScene7 : MonoBehaviour
{
    [Header("Current Trial Info (Read-Only)")]
    [SerializeField] private string currentTrialDisplay = "";
    [SerializeField] private int trialNumber = 0;
    [SerializeField] private string axisDisplay = "";
    [SerializeField] private string directionDisplay = "";
    [SerializeField] private string conditionDisplay = "";

    private void Start()
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (StaticValsReach8.IsSessionComplete)
        {
            currentTrialDisplay = "ALL 12 TRIALS COMPLETED!";
            Debug.Log("🏁 All 12 trials completed!");
            return;
        }

        TrialConfig trial = StaticValsReach8.GetCurrentTrial();
        trialNumber = StaticValsReach8.curindex + 1;
        axisDisplay = trial.AxisName;
        directionDisplay = trial.DirectionName;
        conditionDisplay = ExperimentConditionHelper.GetDisplayName(StaticValsReach8.condition);

        currentTrialDisplay = $"Trial {trialNumber}/12 - {conditionDisplay} | {trial}";
        Debug.Log($"📋 {currentTrialDisplay}");
    }

    private void Update()
    {
        // R key: Advance to next trial and reload
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            AdvanceAndRestart();
        }
    }

    private void AdvanceAndRestart()
    {
        if (StaticValsReach8.IsSessionComplete)
        {
            Debug.Log("🏁 Cannot advance - all 12 trials already completed!");
            return;
        }

        int previousTrial = StaticValsReach8.curindex + 1;
        StaticValsReach8.AdvanceTrial();

        if (StaticValsReach8.IsSessionComplete)
        {
            Debug.Log($"🏁 Trial {previousTrial} was the last trial. Session complete!");
            // Still reload to show completion state
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }

        int newTrial = StaticValsReach8.curindex + 1;
        TrialConfig newConfig = StaticValsReach8.GetCurrentTrial();

        Debug.Log($"🔄 Trial {previousTrial} → Trial {newTrial}: {newConfig}");
        Debug.Log($"🎮 Condition: {ExperimentConditionHelper.GetDisplayName(StaticValsReach8.condition)}");

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
