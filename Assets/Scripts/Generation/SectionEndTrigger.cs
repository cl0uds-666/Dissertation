using UnityEngine;

/// <summary>
/// Invisible trigger placed near the end of each generated section.
/// 
/// The trigger only spawns the next section if the current section has been cleared.
/// This prevents the player from simply running through sections without fighting.
/// </summary>
public class SectionEndTrigger : MonoBehaviour
{
    private bool allowGhostClear = true;
    private float requiredUndetectedRatio = 0.8f;
    private int requiredStealthKills = 2;
    private bool requireZeroDetections;

    private SectionGenerator sectionGenerator;

    private SectionInstance owningSection;

    private bool hasBeenTriggered = false;

    /// <summary>
    /// Called by SectionGenerator immediately after creating the trigger.
    /// </summary>
    public void Setup(SectionGenerator generator, SectionInstance section, bool allowGhost, float undetectedRatioRequirement, int stealthKillRequirement, bool requireNoDetections)
    {
        sectionGenerator = generator;
        owningSection = section;
        allowGhostClear = allowGhost;
        requiredUndetectedRatio = Mathf.Clamp01(undetectedRatioRequirement);
        requiredStealthKills = Mathf.Max(0, stealthKillRequirement);
        requireZeroDetections = requireNoDetections;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasBeenTriggered)
        {
            return;
        }

        if (!other.CompareTag("Player"))
        {
            return;
        }

        if (owningSection == null)
        {
            Debug.LogWarning("SectionEndTrigger has no owning section.");
            return;
        }

        bool canProgressViaCombatClear = owningSection.CanProgress();
        bool canProgressViaStealth = allowGhostClear && owningSection.CanProgressViaStealth(requiredUndetectedRatio, requiredStealthKills, requireZeroDetections);

        if (!canProgressViaCombatClear && !canProgressViaStealth)
        {
            float totalTrackedTime = owningSection.metrics.timeDetected + owningSection.metrics.timeUndetected;
            float undetectedRatio = totalTrackedTime > 0f ? owningSection.metrics.timeUndetected / totalTrackedTime : 0f;

            string stealthRequirementFeedback = "Stealth requirements missing:" +
                " undetected ratio " + undetectedRatio.ToString("P0") + "/" + requiredUndetectedRatio.ToString("P0") +
                ", stealth kills " + owningSection.metrics.stealthKills + "/" + requiredStealthKills +
                (requireZeroDetections ? ", detections " + owningSection.metrics.timesDetected + "/0" + " (detected time " + owningSection.metrics.timeDetected.ToString("F2") + "s)" : "");

            Debug.Log("Clear all enemies before progressing. " + stealthRequirementFeedback);
            return;
        }

        hasBeenTriggered = true;

        if (canProgressViaStealth && !canProgressViaCombatClear)
        {
            Debug.Log("Ghost Clear achieved in Section " + owningSection.sectionIndex);
        }

        Debug.Log("Progressing from Section " + owningSection.sectionIndex);

        if (sectionGenerator != null)
        {
            sectionGenerator.ProgressToNextSection(owningSection);
        }
    }
}
