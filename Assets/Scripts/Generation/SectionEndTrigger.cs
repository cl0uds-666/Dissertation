using UnityEngine;

/// <summary>
/// Invisible trigger placed near the end of each generated section.
/// 
/// The trigger only spawns the next section if the current section has been cleared.
/// This prevents the player from simply running through sections without fighting.
/// </summary>
public class SectionEndTrigger : MonoBehaviour
{
    private SectionGenerator sectionGenerator;

    private SectionInstance owningSection;

    private bool hasBeenTriggered = false;

    /// <summary>
    /// Called by SectionGenerator immediately after creating the trigger.
    /// </summary>
    public void Setup(SectionGenerator generator, SectionInstance section)
    {
        sectionGenerator = generator;
        owningSection = section;
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

        if (!owningSection.CanProgress())
        {
            Debug.Log("Clear all enemies before progressing.");
            return;
        }

        hasBeenTriggered = true;

        Debug.Log("Progressing from Section " + owningSection.sectionIndex);

        if (sectionGenerator != null)
        {
            sectionGenerator.ProgressToNextSection(owningSection);
        }
    }
}
