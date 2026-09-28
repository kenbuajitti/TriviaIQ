using UnityEngine;
using UnityEngine.UI;

// Shared menu-background template for the IQ games. Attach to the menu Canvas.
// Each project supplies its own IQMenu/Landscape and IQMenu/Portrait resources.
[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
public sealed class IQResponsiveMenuBackground : MonoBehaviour
{
    Canvas menuCanvas;
    RectTransform canvasRect;
    RawImage cover;
    Texture2D landscape, portrait;
    Vector2 lastSize;
    Rect lastViewport;
    bool attemptedLoad;

    void LateUpdate()
    {
        if (menuCanvas == null || canvasRect == null ||
            canvasRect.rect.size != lastSize || menuCanvas.pixelRect != lastViewport)
            Refresh();
    }

    public void Refresh()
    {
        if (menuCanvas == null) menuCanvas = GetComponent<Canvas>();
        if (canvasRect == null) canvasRect = GetComponent<RectTransform>();
        Vector2 size = canvasRect.rect.size;
        Rect viewport = menuCanvas.pixelRect;
        if (size.x <= 0 || size.y <= 0 || viewport.width <= 0 || viewport.height <= 0)
            return;

        if (!attemptedLoad)
        {
            attemptedLoad = true;
            landscape = Resources.Load<Texture2D>("IQMenu/Landscape");
            portrait = Resources.Load<Texture2D>("IQMenu/Portrait");
            if (landscape == null || portrait == null)
            {
                Debug.LogError("IQ menu covers missing. Import Assets/Resources/IQMenu from the update.", this);
                return; // Keep the original background if either cover is missing.
            }

            RectTransform backdrop = CreateLayer("IQMenuBackdrop", transform);
            Stretch(backdrop);
            backdrop.SetAsFirstSibling();
            Image fill = backdrop.gameObject.AddComponent<Image>();
            fill.color = new Color(.025f, .045f, .12f, 1f);
            fill.raycastTarget = false;

            RectTransform art = CreateLayer("Cover", backdrop);
            cover = art.gameObject.AddComponent<RawImage>();
            cover.raycastTarget = false;
            art.anchorMin = art.anchorMax = art.pivot = new Vector2(.5f, .5f);
            art.anchoredPosition = Vector2.zero;

            RectTransform shade = CreateLayer("ReadabilityShade", backdrop);
            Stretch(shade);
            Image tint = shade.gameObject.AddComponent<Image>();
            tint.color = new Color(.025f, .045f, .12f, .48f);
            tint.raycastTarget = false;

            Transform original = transform.Find("MenuBackground");
            if (original != null) original.gameObject.SetActive(false);
        }

        lastSize = size;
        lastViewport = viewport;
        if (cover == null) return;
        // Use the rendered viewport, including embedded WebGL and camera views.
        // Square viewports use the landscape cover. Never stretch or crop art.
        Texture2D texture = viewport.width >= viewport.height ? landscape : portrait;
        cover.texture = texture;
        float fit = Mathf.Min(size.x / texture.width, size.y / texture.height);
        cover.rectTransform.sizeDelta = new Vector2(texture.width * fit, texture.height * fit);
    }

    RectTransform CreateLayer(string layerName, Transform parent)
    {
        GameObject layer = new GameObject(layerName, typeof(RectTransform));
        layer.layer = gameObject.layer;
        layer.transform.SetParent(parent, false);
        return (RectTransform)layer.transform;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
