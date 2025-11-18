using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class VolumeInitializer : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private GameObject settingPanel;


    public void SettingPanelFalse()
    {
        settingPanel.SetActive(false);
    }

    public void SettingPanelTrue()
    {
        settingPanel.SetActive(true);
    }

    private const string MasterVolumeKey = "MasterVolume";
    private const string MusicVolumeKey = "MusicVolume";

    private const string MasterMixerParam = "MasterVolume";
    private const string MusicMixerParam = "MusicVolume";

    private void Awake()
    {
        if (audioMixer == null)
        {
            Debug.LogError("AudioMixer is not assigned in VolumeInitializer!");
            return;
        }

        // Áp dụng âm lượng tổng
        float masterVol = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
        float masterDb = Mathf.Log10(Mathf.Clamp(masterVol, 0.0001f, 1f)) * 20f;
        audioMixer.SetFloat(MasterMixerParam, masterDb);
        Debug.Log($"[Volume Init] Master Volume: {masterVol} → {masterDb} dB");

        // Áp dụng âm lượng nhạc nền
        float musicVol = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
        float musicDb = Mathf.Log10(Mathf.Clamp(musicVol, 0.0001f, 1f)) * 20f;
        audioMixer.SetFloat(MusicMixerParam, musicDb);
        Debug.Log($"[Volume Init] Music Volume: {musicVol} → {musicDb} dB");
    }
}
