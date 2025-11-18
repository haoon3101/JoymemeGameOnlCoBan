using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class MasterVolumeController : MonoBehaviour
{
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TextMeshProUGUI volumeText;
    [SerializeField] private AudioMixer audioMixer;
    private const string VolumePrefKey = "MasterVolume";

    public void OnEnable()
    {
        if (volumeSlider != null)
        {
            float volume = PlayerPrefs.GetFloat(VolumePrefKey, 1f);
            volumeSlider.value = volume;
        }
    }

    private void Start()
    {
        // Đọc volume từ PlayerPrefs (nếu có)
        float savedVolume = PlayerPrefs.GetFloat(VolumePrefKey, 1f);

        SetVolume(savedVolume);

        // Gắn sự kiện khi thay đổi slider
        volumeSlider.onValueChanged.AddListener(SetVolume);
    }

    public void SetVolume(float value)
    {
        float dB = Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20f; // ✅ Sửa dòng này
        audioMixer.SetFloat("MasterVolume", dB);

        volumeText.text = Mathf.RoundToInt(value * 100) + "";
        PlayerPrefs.SetFloat(VolumePrefKey, value);
    }

    //private void ApplyVolume(float value)
    //{
    //    float dB = Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20f; // ✅ Sửa tương tự
    //    audioMixer.SetFloat("MasterVolume", dB);

    //    if (volumeText != null)
    //        volumeText.text = Mathf.RoundToInt(value * 100) + "";

    //    PlayerPrefs.SetFloat("MasterVolume", value);
    //}
}
