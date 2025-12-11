using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using DG.Tweening;
using Button = UnityEngine.UI.Button;

public class ButtonAnimation : MonoBehaviour,IPointerDownHandler,IPointerUpHandler
{
    public float startsize=1;
    public float Size_Down = 0.85f, _Duration = .15f;
    public Ease EaseType_Down = Ease.Linear,EaseType_Up = Ease.Linear;
    public void OnPointerDown(PointerEventData eventData)
    {
        if (GetComponent<Button>().interactable == true)
        {
            transform.DOScale(Vector3.one * Size_Down, _Duration).SetEase(EaseType_Down); //.85f,.15f
        }

    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (GetComponent<Button>().interactable == true)
        {
            if (startsize == 0)
            { startsize = 1;}
            transform.DOScale(Vector3.one*startsize, _Duration).SetEase(EaseType_Up);
        }
    }
}
