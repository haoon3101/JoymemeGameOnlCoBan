using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening; // Make sure you have DOTween in your project

public class MatrixTransition : MonoBehaviour
{
    [Header("Effect Settings")]
    [SerializeField] private float fallSpeed = 50f;
    [SerializeField] private float characterSwapRate = 0.05f; // How often characters change
    [SerializeField] private int fontSize = 24;
    [SerializeField] private Color textColor = Color.green;
    [SerializeField] private Color highlightColor = Color.white;
    [SerializeField] private string characters = "......................................................";
    [SerializeField] private Image bgImage; // Optional background image, can be set in the inspector
    [Header("Transition Timing")]
    [SerializeField] private float fadeInDuration = 1.0f;
    [SerializeField] private float holdDuration = 2.0f;
    [SerializeField] private float fadeOutDuration = 1.0f;

    private CanvasGroup canvasGroup;
    private List<TextMeshProUGUI> columns = new List<TextMeshProUGUI>();
    private List<float> yPositions = new List<float>();

    void Start()
    {
        // 1. Create the UI structure from code
        SetupUI();

        // 2. Start the main transition sequence
        StartCoroutine(TransitionSequence());
    }

    private void SetupUI()
    {
        // Create a Canvas that covers the whole screen
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // Ensure it's on top of everything

        gameObject.AddComponent<CanvasScaler>();
        gameObject.AddComponent<GraphicRaycaster>();

        // Add a CanvasGroup for fading
        canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;

        // Add a black background image
        GameObject background = new GameObject("Background");
        background.transform.SetParent(transform, false);
        Image bgImage = GetComponent<Image>();
        bgImage.rectTransform.anchorMin = Vector2.zero;
        bgImage.rectTransform.anchorMax = Vector2.one;
        bgImage.rectTransform.sizeDelta = Vector2.zero;

        // Create the columns of text
        int columnCount = Mathf.CeilToInt(Screen.width / (float)fontSize);
        for (int i = 0; i < columnCount; i++)
        {
            GameObject columnObj = new GameObject($"Column_{i}");
            columnObj.transform.SetParent(transform, false);
            TextMeshProUGUI text = columnObj.AddComponent<TextMeshProUGUI>();

            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = fontSize;
            text.color = textColor;
            text.alignment = TextAlignmentOptions.Center;
            text.overflowMode = TextOverflowModes.Overflow; // Allow text to go off-screen

            // Position the column
            RectTransform rt = text.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(i * fontSize + (fontSize / 2f), 0);

            columns.Add(text);
            yPositions.Add(Random.Range(0, Screen.height * 1.5f)); // Start some off-screen
        }

        Transform foregroundImage = transform.Find("Image");
        if (foregroundImage != null)
        {
            // If it exists, make it the last child in the hierarchy so it renders on top.
            foregroundImage.SetAsLastSibling();
            Debug.Log("Foreground 'Image' object found and brought to front.");
        }
    }

    private IEnumerator TransitionSequence()
    {
        // Fade in
        canvasGroup.DOFade(1f, fadeInDuration);
        yield return new WaitForSeconds(fadeInDuration);

        // Hold and play the effect
        float timer = 0f;
        while (timer < holdDuration)
        {
            UpdateMatrixEffect();
            timer += Time.deltaTime;
            yield return null;
        }

        // Fade out
        canvasGroup.DOFade(0f, fadeOutDuration);
        yield return new WaitForSeconds(fadeOutDuration);

        // Clean up
        Destroy(gameObject);
    }

    private void UpdateMatrixEffect()
    {
        for (int i = 0; i < columns.Count; i++)
        {
            TextMeshProUGUI text = columns[i];

            // Build the string for the column
            string columnText = "";
            int charCount = Mathf.CeilToInt(Screen.height / (float)fontSize);
            for (int j = 0; j < charCount; j++)
            {
                char randomChar = characters[Random.Range(0, characters.Length)];

                // Highlight the "leading" character
                if (j == charCount - 2) // Second to last character
                {
                    columnText += $"<color=#{ColorUtility.ToHtmlStringRGB(highlightColor)}>{randomChar}</color>\n";
                }
                else
                {
                    columnText += $"{randomChar}\n";
                }
            }
            text.text = columnText;

            // Move the column down
            yPositions[i] -= fallSpeed * Time.deltaTime;
            text.rectTransform.anchoredPosition = new Vector2(text.rectTransform.anchoredPosition.x, -yPositions[i]);

            // Reset if it goes off screen
            if (yPositions[i] > Screen.height)
            {
                yPositions[i] = Random.Range(-100f, -500f);
            }
        }
    }
}
