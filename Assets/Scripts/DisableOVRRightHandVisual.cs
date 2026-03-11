using UnityEngine;

public class DisableOVRRightHandVisual : MonoBehaviour
{
    private GameObject rightHandVisual;
    private GameObject leftHandVisual;
    private bool foundBoth = false;

    void LateUpdate()
    {
        // Only search if we haven't found both yet
        if (!foundBoth)
        {
            if (rightHandVisual == null)
            {
                rightHandVisual = GameObject.Find("OVRRightHandVisual");
            }

            if (leftHandVisual == null)
            {
                leftHandVisual = GameObject.Find("OVRLeftHandVisual");
            }

            if (rightHandVisual != null && leftHandVisual != null)
            {
                foundBoth = true;
            }
        }

        // Disable if active (cheap check)
        if (rightHandVisual != null && rightHandVisual.activeSelf)
        {
            rightHandVisual.SetActive(false);
        }

        if (leftHandVisual != null && leftHandVisual.activeSelf)
        {
            leftHandVisual.SetActive(false);
        }
    }
}