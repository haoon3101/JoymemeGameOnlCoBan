//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;
//using System.Collections;
//using DG.Tweening;

//public class GameOverTransition : MonoBehaviour
//{
//    [Header("Core Components")]
//    [SerializeField] private CanvasGroup panelCanvasGroup;
//    // --- CHANGED: Now uses Image components ---
//    [SerializeField] private Image resultImage; // Image for "YOU WON" / "YOU LOST"
//    [SerializeField] private Image symbolImage; // Image for the arrow symbol
//    [SerializeField] private Image characterImage;
//    [SerializeField] private TextMeshProUGUI digitalRainText;

//    [Header("Animation Settings")]
//    [SerializeField] private float fadeInDuration = 1.0f;
//    [SerializeField] private float holdDuration = 5.0f;

//    [Header("Digital Rain Effect")]
//    // --- CHANGED: Character is now '^' ---
//    [SerializeField] private string characters = "^";
//    [SerializeField] private int rainDensity = 20;

//    private void Awake()
//    {
//        if (panelCanvasGroup != null)
//        {
//            panelCanvasGroup.alpha = 0;
//            panelCanvasGroup.gameObject.SetActive(false);
//        }
//    }

//    // --- CHANGED: Method now accepts Sprites instead of strings and colors ---
//    public void Play(Sprite resultSprite, Sprite symbolSprite, Color themeColor, Sprite characterSprite)
//    {
//        if (panelCanvasGroup == null) return;

//        // 1. Configure the panel's content
//        if (resultImage != null)
//        {
//            resultImage.sprite = resultSprite;
//            resultImage.color = themeColor;
//        }
//        if (symbolImage != null)
//        {
//            symbolImage.sprite = symbolSprite;
//            symbolImage.color = themeColor;
//        }
//        if (characterImage != null)
//        {
//            characterImage.sprite = characterSprite;
//            characterImage.gameObject.SetActive(characterSprite != null);
//        }
//        if (digitalRainText != null)
//        {
//            digitalRainText.color = new Color(themeColor.r, themeColor.g, themeColor.b, 0.3f);
//        }

//        // 2. Start the animation sequence
//        StartCoroutine(TransitionSequence());
//    }

//    private IEnumerator TransitionSequence()
//    {
//        panelCanvasGroup.gameObject.SetActive(true);
//        panelCanvasGroup.alpha = 0;
//        panelCanvasGroup.DOFade(1f, fadeInDuration);

//        float timer = 0f;
//        while (timer < holdDuration)
//        {
//            UpdateDigitalRain();
//            timer += Time.deltaTime;
//            yield return null;
//        }
//    }

//    private void UpdateDigitalRain()
//    {
//        if (digitalRainText == null) return;
//        System.Text.StringBuilder sb = new System.Text.StringBuilder();
//        for (int i = 0; i < rainDensity; i++)
//        {
//            for (int j = 0; j < 50; j++)
//            {
//                sb.Append(characters[Random.Range(0, characters.Length)]);
//            }
//            sb.AppendLine();
//        }
//        digitalRainText.text = sb.ToString();
//    }
//}
