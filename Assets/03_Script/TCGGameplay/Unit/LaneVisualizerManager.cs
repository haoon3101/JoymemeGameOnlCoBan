using Photon.Pun;
using UnityEngine;

public class LaneVisualizerManager : MonoBehaviourPun
{
    public static LaneVisualizerManager Instance;

    [SerializeField] private GameObject[] laneVFXObjects;

    public enum LaneVisualMode { None, Combat, Placement }

    private void Awake()
    {
        Instance = this;
        foreach (var vfx in laneVFXObjects)
        {
            if (vfx != null)
            {
                vfx.GetComponent<LaneVFX>();
            }
        }
    }

    public void HighlightLane(int laneIndex, LaneVisualMode mode)
    {
        if (laneIndex < 0 || laneIndex >= laneVFXObjects.Length) return;

        GameObject laneVFX = laneVFXObjects[laneIndex];
        laneVFX.SetActive(true);

        var renderer = laneVFX.GetComponent<Renderer>();
        if (renderer != null)
        {
            Color color = Color.white;
            if (mode == LaneVisualMode.Placement)
                color = Color.blue;

            renderer.material.color = color;
        }

        Debug.Log($"[LaneVisualizer] Lane {laneIndex} highlighted with mode {mode}");
    }
    public void ClearAllLaneHighlights()
    {
        foreach (var vfx in laneVFXObjects)
        {
            vfx.SetActive(false);
        }
    }

}
