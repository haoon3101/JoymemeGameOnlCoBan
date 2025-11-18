using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class MusicVolumeController : MonoBehaviour
{
    [SerializeField] private Slider musicSlider;
    [SerializeField] private TextMeshProUGUI musicText;
    [SerializeField] private AudioMixer audioMixer;

    private const string VolumePrefKey = "MusicVolume";
    private const string MixerParameter = "MusicVolume"; // exposed parameter in AudioMixer

    public void OnEnable()
    {
        // Khi UI bật lên, đồng bộ slider và text từ PlayerPrefs
        float savedVolume = PlayerPrefs.GetFloat(VolumePrefKey, 1f);

        if (musicSlider != null)
        {
            musicSlider.value = savedVolume;
        }
        UpdateVolumeText(savedVolume);
    }

    private void Start()
    {
        float savedVolume = PlayerPrefs.GetFloat(VolumePrefKey, 1f);
        SetVolume(savedVolume); // 👈 thêm dòng này nếu cần (nếu chưa có)

        if (musicSlider != null)
        {
            musicSlider.onValueChanged.AddListener(SetVolume);
        }
    }

    public void SetVolume(float value)
    {
        // Chuyển volume sang dB và áp dụng
        float dB = Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20f;
        audioMixer.SetFloat(MixerParameter, dB);

        // Cập nhật text và lưu PlayerPrefs
        UpdateVolumeText(value);
        PlayerPrefs.SetFloat(VolumePrefKey, value);
    }

    private void UpdateVolumeText(float value)
    {
        if (musicText != null)
        {
            int percent = Mathf.RoundToInt(value * 100);
            musicText.text = percent + "";
        }
    }
}
