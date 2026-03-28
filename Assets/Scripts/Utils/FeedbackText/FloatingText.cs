using DG.Tweening;
using TMPro;
using UnityEngine;

public class FloatingText : MonoBehaviour
{
    public TextMeshProUGUI text;
    public CanvasGroup canvasGroup;

    public float moveY = 50f;
    public float duration = 1f;

    public void Show(string message, Color color)
    {
        text.text = message;
        text.color = color;

        canvasGroup.alpha = 1f;

        // animação
        transform.localPosition = Vector3.zero;

        Sequence s = DOTween.Sequence();

        s.Append(transform.DOLocalMoveY(moveY, duration).SetEase(Ease.OutQuad))
         .Join(canvasGroup.DOFade(0f, duration))
         .OnComplete(() => Destroy(gameObject));
    }
}
