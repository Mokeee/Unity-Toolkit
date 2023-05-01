using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SpriteCarousel : MonoBehaviour
{
    public RectTransform contentHolder;
    public GameObject imagePrefab;

    public float rotationTime = 0.1f;

    private int index = 0;
    private List<Image> spriteObjects = new List<Image>();

    private float contentWidth;

    public UnityAction<int> IndexChanged;

    // Start is called before the first frame update
    public void Setup(List<Sprite> sprites)
    {
        contentWidth = 1000;// contentHolder.rect.width;

        int i = 0;
        foreach (var sprite in sprites)
        {
            Image spriteObject = null;
            if (i == 0)
            {
                spriteObject = imagePrefab.GetComponent<Image>();
            }
            else
            {
                spriteObject = GameObject.Instantiate(imagePrefab, contentHolder).GetComponent<Image>();
                spriteObject.rectTransform.anchoredPosition = new Vector2(contentWidth, 0);
            }
            spriteObject.sprite = sprite;
            spriteObjects.Add(spriteObject);
            i++;
        }
    }

    public void GetNext()
    {
        contentWidth = contentHolder.rect.width / 2 + contentHolder.rect.height; 
        int currIndex = index;
        index = (index + 1) % spriteObjects.Count;

        foreach(var sprite in spriteObjects)
        {
            sprite.rectTransform.anchoredPosition = new Vector2(contentWidth, 0);
        }
        spriteObjects[currIndex].rectTransform.anchoredPosition = new Vector2(0, 0);

        StopAllCoroutines();
        StartCoroutine(Rotate(spriteObjects[currIndex].rectTransform, spriteObjects[index].rectTransform));
        IndexChanged.Invoke(index);
    }

    public void GetPrevious()
    {
        contentWidth = contentHolder.rect.width / 2 + contentHolder.rect.height; 
        int currIndex = index;
        index = (index + spriteObjects.Count - 1) % spriteObjects.Count;

        foreach (var sprite in spriteObjects)
        {
            sprite.rectTransform.anchoredPosition = new Vector2(-contentWidth, 0);
        }
        spriteObjects[currIndex].rectTransform.anchoredPosition = new Vector2(0, 0);

        StopAllCoroutines();
        StartCoroutine(Rotate(spriteObjects[currIndex].rectTransform, spriteObjects[index].rectTransform, false));
        IndexChanged.Invoke(index);
    }

    private IEnumerator Rotate(RectTransform oldTarget, RectTransform newTarget, bool clockwise = true)
    {
        float timeDelta = 0;
        float directionFactor = (clockwise) ? -1f : 1f;

        while (timeDelta < rotationTime)
        {
            float frameDelta = Time.deltaTime;
            timeDelta += frameDelta;


            oldTarget.anchoredPosition += new Vector2(contentWidth * frameDelta / rotationTime * directionFactor, 0);
            newTarget.anchoredPosition += new Vector2(contentWidth * frameDelta / rotationTime * directionFactor, 0);

            yield return null;
        }

        oldTarget.anchoredPosition = new Vector2(contentWidth * directionFactor, 0);
        newTarget.anchoredPosition = new Vector2(0, 0);
    }
}
