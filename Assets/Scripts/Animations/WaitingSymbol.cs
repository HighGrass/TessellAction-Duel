using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WaitingSymbol : MonoBehaviour
{
    [SerializeField]
    float animationTime = 1f;

    [SerializeField]
    Image middleCircle;

    [SerializeField]
    Image redOutline;

    [SerializeField]
    Image blueOutline;

    [SerializeField]
    Image redCircle;

    [SerializeField]
    Image blueCircle;

    [SerializeField]
    bool Active = false;
    float animationMagnitude;
    float middleCircleSize;

    Coroutine animationCoroutine;

    void Awake()
    {
        animationMagnitude = redCircle.rectTransform.sizeDelta.x;
        middleCircleSize = middleCircle.rectTransform.sizeDelta.x;
        SetActivity(Active);
    }

    public void SetActive() => SetActivity(true);

    public void SetInactive() => SetActivity(false);

    void SetActivity(bool active)
    {
        Active = active;
        gameObject.SetActive(active);

        if (active)
            animationCoroutine = StartCoroutine(Animation());
        else
            StopCoroutine(animationCoroutine);
    }

    IEnumerator Animation()
    {
        float maxKeyframe = 180f;
        float animationKeyfame = maxKeyframe;
        while (Active)
        {
            float redSize = Mathf.Abs(
                Mathf.Sin(animationKeyfame * Mathf.Deg2Rad) * animationMagnitude
            );

            float blueSize = Mathf.Abs(
                Mathf.Cos(animationKeyfame * Mathf.Deg2Rad) * animationMagnitude
            );

            float redOutlineSize = Mathf.Abs(
                Mathf.Sin((animationKeyfame + maxKeyframe / 5) * Mathf.Deg2Rad) * animationMagnitude
            );
            float blueOutlineSize = Mathf.Abs(
                Mathf.Cos((animationKeyfame + maxKeyframe / 5) * Mathf.Deg2Rad) * animationMagnitude
            );

            middleCircle.rectTransform.sizeDelta = new Vector2(
                middleCircleSize + redSize * 0.1f,
                middleCircleSize + redSize * 0.1f
            );

            redCircle.rectTransform.sizeDelta = new Vector2(redSize, redSize);
            blueCircle.rectTransform.sizeDelta = new Vector2(blueSize, blueSize);

            redOutline.rectTransform.sizeDelta = new Vector2(
                redOutlineSize * 1.5f,
                redOutlineSize * 1.5f
            );
            blueOutline.rectTransform.sizeDelta = new Vector2(
                blueOutlineSize * 1.5f,
                blueOutlineSize * 1.5f
            );

            animationKeyfame -= Time.deltaTime / animationTime * 180f;
            if (animationKeyfame < 0)
                animationKeyfame = 180f;
            yield return null;
        }
    }
}
