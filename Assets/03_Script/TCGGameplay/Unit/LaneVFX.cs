using UnityEngine;

public class LaneVFX : MonoBehaviour
{
    public GameObject visualObject; // Assign a plane or highlight mesh here
    public Renderer laneRenderer;

    [Header("Colors")]
    public Color placementColor = Color.blue;
    public Color combatColor = Color.white;

    public void SetHighlight(bool active, LaneVisualizerManager.LaneVisualMode mode)
    {
        if (visualObject != null)
            visualObject.SetActive(active);

        if (!active || laneRenderer == null) return;

        Color targetColor = Color.white;

        switch (mode)
        {
            case LaneVisualizerManager.LaneVisualMode.Placement:
                targetColor = placementColor;
                break;
            case LaneVisualizerManager.LaneVisualMode.Combat:
                targetColor = combatColor;
                break;
        }

        laneRenderer.material.color = targetColor;
    }
}
