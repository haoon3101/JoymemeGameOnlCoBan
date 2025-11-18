using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Photon.Pun.Demo.PunBasics;
using static SaveManagerCharacter;
public class SelecCharacter : MonoBehaviour
{
    public void SelectCharacter(string characterName)
    {
        PlayerData data = new PlayerData();
        data.selectedCharacter = characterName;
        SaveManagerCharacter.Save(data);

        // Đồng bộ với Photon
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
        props["Character"] = characterName;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        Debug.Log("Bạn đã chọn nhân vật: " + characterName);
    }
}
