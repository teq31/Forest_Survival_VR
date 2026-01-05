using UnityEngine;
using TMPro;
using System.Collections;

public class MessageUI : MonoBehaviour
{
    public TMP_Text messageText;
    public CanvasGroup canvasGroup;

    [Header("Timing")]
    public float fadeIn = 0.15f;
    public float stay = 1.5f;
    public float fadeOut = 0.25f;

    Coroutine _routine;

    void Awake()
    {
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        if (messageText != null) messageText.text = "";
    }

    public void Show(string msg)
    {
        if (messageText == null || canvasGroup == null) return;

        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(ShowRoutine(msg));
    }

    IEnumerator ShowRoutine(string msg)
    {
        messageText.text = msg;

        float t = 0f;
        while (t < fadeIn)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t / fadeIn);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        yield return new WaitForSeconds(stay);

        t = 0f;
        while (t < fadeOut)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / fadeOut);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        messageText.text = "";
    }
  
}
