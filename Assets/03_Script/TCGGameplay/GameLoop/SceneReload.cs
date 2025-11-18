using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneReload : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private string sceneName;

    public void LoadSceneByName()
    { 
        SceneManager.LoadScene(sceneName);
    }
}
