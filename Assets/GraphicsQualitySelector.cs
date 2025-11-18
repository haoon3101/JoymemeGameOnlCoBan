using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GraphicsQualitySelector : MonoBehaviour
{
    [SerializeField] private TMP_Text qualityText; // Text để hiển thị chất lượng
    private string[] qualityLevels;
    private int currentIndex;

    private void Start()
    {
        // Lấy danh sách các mức từ Project Settings > Quality
        qualityLevels = QualitySettings.names;

        // Tải mức đã lưu hoặc dùng mức hiện tại
        currentIndex = PlayerPrefs.GetInt("QualityLevel", QualitySettings.GetQualityLevel());
        UpdateQuality();
    }

    public void IncreaseQuality()
    {
        currentIndex = (currentIndex + 1) % qualityLevels.Length;
        UpdateQuality();
    }

    public void DecreaseQuality()
    {
        currentIndex = (currentIndex - 1 + qualityLevels.Length) % qualityLevels.Length;
        UpdateQuality();
    }

    private void UpdateQuality()
    {
        QualitySettings.SetQualityLevel(currentIndex, true);
        PlayerPrefs.SetInt("QualityLevel", currentIndex);
        qualityText.text = qualityLevels[currentIndex];
        Debug.Log("Graphics set to: " + qualityLevels[currentIndex]);
    }
}
