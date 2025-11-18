using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GraphicsSettingController : MonoBehaviour
{
    [SerializeField] private GameObject[] qualityTexts; // 0 = Low, 1 = Medium, 2 = High
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;

    private int currentQuality = 0;

    private void Start()
    {
        // Gán sự kiện click
        leftButton.onClick.AddListener(OnLeftClick);
        rightButton.onClick.AddListener(OnRightClick);

        // Khởi tạo theo chất lượng hiện tại
        currentQuality = QualitySettings.GetQualityLevel();
        currentQuality = Mathf.Clamp(currentQuality, 0, qualityTexts.Length - 1);
        UpdateUI();
    }

    private void OnLeftClick()
    {
        currentQuality--;
        if (currentQuality < 0) currentQuality = qualityTexts.Length - 1;
        ApplySetting();
    }

    private void OnRightClick()
    {
        currentQuality++;
        if (currentQuality >= qualityTexts.Length) currentQuality = 0;
        ApplySetting();
    }

    private void ApplySetting()
    {
        QualitySettings.SetQualityLevel(currentQuality, true);
        UpdateUI();
    }

    private void UpdateUI()
    {
        for (int i = 0; i < qualityTexts.Length; i++)
        {
            qualityTexts[i].SetActive(i == currentQuality);
        }
    }
}
