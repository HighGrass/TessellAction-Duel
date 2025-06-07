using System.Collections;
using TMPro;
using UnityEngine;

public class TimeCounter : MonoBehaviour
{
    [SerializeField]
    TMP_Text counterText;

    Coroutine countingCoroutine;

    IEnumerator StartCounting()
    {
        int seconds = 0;
        WaitForSeconds wait = new WaitForSeconds(1f);
        while (true)
        {
            counterText.text = FormatTime(seconds);
            yield return wait;
            seconds++;
        }
    }

    public void StartCounter()
    {
        gameObject.SetActive(true);
        countingCoroutine = StartCoroutine(StartCounting());
    }

    public void StopCounter()
    {
        gameObject.SetActive(false);
        StopCoroutine(countingCoroutine);
    }

    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);
        return $"{minutes:D2}:{seconds:D2}";
    }
}
