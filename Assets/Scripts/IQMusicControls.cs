using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(100)]
public sealed class IQMusicControls : MonoBehaviour
{
    IQMusic music;
    RectTransform footer;
    IQSpeakerGraphic speaker;
    RectTransform cover;
    readonly Vector3[] corners = new Vector3[4];
    public void Initialize()
    {
        music = IQMusic.GetPlayer();
        footer = Rect("Music controls", transform);
        var sound = ButtonAt("Toggle sound", footer, new Vector2(-12, 0), new Vector2(52, 48));
        sound.GetComponent<Image>().color = new Color(.12f, .19f, .065f, 1f);
        var icon = Rect("Speaker", sound.transform);
        icon.anchorMin = icon.anchorMax = icon.pivot = new Vector2(.5f, .5f);
        icon.sizeDelta = new Vector2(30, 25);
        icon.anchoredPosition = Vector2.zero;
        icon.gameObject.AddComponent<CanvasRenderer>();
        speaker = icon.gameObject.AddComponent<IQSpeakerGraphic>();
        speaker.color = Color.white; speaker.raycastTarget = false;
        sound.onClick.AddListener(() => { music.ToggleSound(); Refresh(); });
        Refresh();
    }
    void Refresh()
    {
        speaker.IsOn = music.SoundEnabled;
        speaker.SetVerticesDirty();
        speaker.transform.parent.name = music.SoundEnabled ? "Sound on - click to mute" : "Sound off - click to unmute";
    }
    public void Place(float left, float top, float width, float height)
    {
        footer.anchorMin = footer.anchorMax = footer.pivot = new Vector2(0, 1);
        footer.anchoredPosition = new Vector2(left, -top - height + 56);
        footer.sizeDelta = new Vector2(width, 56);
    }
    void LateUpdate()
    {
        if (footer == null) return;
        if (cover == null)
            cover = transform.Find("IQMenuBackdrop/Cover") as RectTransform;
        if (cover == null) return; // Gameplay uses the reserved footer.
        cover.GetWorldCorners(corners);
        Vector3 bottomLeft = transform.InverseTransformPoint(corners[0]);
        Vector3 bottomRight = transform.InverseTransformPoint(corners[3]);
        // Keep controls above the cover/shade but inside its bottom-right edge.
        footer.anchorMin = footer.anchorMax = new Vector2(.5f, .5f);
        footer.pivot = new Vector2(0, 1);
        footer.localPosition = new Vector3(bottomLeft.x, bottomLeft.y + 60, 0);
        footer.sizeDelta = new Vector2(bottomRight.x - bottomLeft.x, 48);
    }
    static RectTransform Rect(string title, Transform parent)
    {
        var go = new GameObject(title, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }
    static Button ButtonAt(string title, Transform parent, Vector2 position, Vector2 size)
    {
        var rect = Rect(title, parent);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, 1);
        rect.anchoredPosition = position; rect.sizeDelta = size;
        var image = rect.gameObject.AddComponent<Image>(); image.color = Color.white;
        var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        return button;
    }
}

