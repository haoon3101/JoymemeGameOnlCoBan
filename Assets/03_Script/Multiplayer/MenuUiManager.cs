using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuUiManager : MonoBehaviour
{
    [Header("Play Mutiplayer")]
    [SerializeField] private Button play;
    [SerializeField] private GameObject panelCreateAndJoin;

    [Header("More Options")]
    [SerializeField] private Button back;
    [SerializeField] private GameObject panelCreAndJoin;

    [SerializeField] private Button howToPlay;
    [SerializeField] private Button backHowToPlay;
    [SerializeField] private GameObject panelHowToPlay;

    void Start()
    {
        play.onClick.AddListener(PlayButton);
        back.onClick.AddListener(BackBtn);
        howToPlay.onClick.AddListener(HowToPlayPannel);
        backHowToPlay.onClick.AddListener (BackHtpBtn);
    }
    private void HowToPlayPannel()
    {
        panelHowToPlay.SetActive(true);
    }

    private void BackHtpBtn()
    {
        panelHowToPlay.SetActive(false);
    }

    private void BackBtn()
    {
        panelCreAndJoin.SetActive(false);
    }
    private void PlayButton()
    {
        panelCreateAndJoin.SetActive(true);
    }
}
