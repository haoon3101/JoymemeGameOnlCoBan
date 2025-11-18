using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.Collections;

public class CardPlace : MonoBehaviour, IDropHandler
{
    [Header("Tên scene sẽ load sau khi thả")]
    [SerializeField] public string sceneToLoad = "GameScene";

    [Header("Đợi bao nhiêu giây trước khi load scene")]
    [SerializeField] public float delayBeforeLoad = 2f;

    public void OnDrop(PointerEventData eventData)
    {
        GameObject droppedCard = eventData.pointerDrag;
        if (droppedCard != null)
        {
            UIDragCard drag = droppedCard.GetComponent<UIDragCard>();
            if (drag != null)
            {
                drag.MarkAsDropped(transform.position); // 👈 Bay vào mượt + âm thanh
                StartCoroutine(DelayedSceneLoad());
            }
        }
    }

    private IEnumerator DelayedSceneLoad()
    {
        yield return new WaitForSeconds(delayBeforeLoad);
        SceneManager.LoadScene(sceneToLoad);
    }
}