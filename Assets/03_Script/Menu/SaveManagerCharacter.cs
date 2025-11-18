using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class SaveManagerCharacter : MonoBehaviour
{

    private static string savePath => Path.Combine(Application.persistentDataPath, "characterplayerdata.json");

    public static void Save(PlayerData data)
    {
        string json = JsonUtility.ToJson(data, true); // true để format đẹp
        File.WriteAllText(savePath, json);
        Debug.Log("Đã lưu vào: " + savePath);
    }

    // Tải dữ liệu
    public static PlayerData Load()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            return JsonUtility.FromJson<PlayerData>(json);
        }
        else
        {
            Debug.Log("Chưa có file lưu. Tạo mới.");
            return new PlayerData(); // Trả về mặc định
        }
    }
}
