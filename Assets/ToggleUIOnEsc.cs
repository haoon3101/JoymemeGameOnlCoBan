using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleUIOnEsc : MonoBehaviour
{
    [SerializeField] private GameObject uiToToggle; // Kéo UI vào đây

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (uiToToggle != null)
            {
                bool isActive = uiToToggle.activeSelf;
                uiToToggle.SetActive(!isActive);
            }
            else
            {
                Debug.LogWarning("Chưa gán UI cần bật/tắt!");
            }
        }
    }
    public void QuitGame()
    {
        Debug.Log("Quit Game");

#if UNITY_EDITOR
        // Dừng Play Mode nếu đang chạy trong Editor
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // Thoát game nếu đang chạy file build
        Application.Quit();
#endif
    }
}
