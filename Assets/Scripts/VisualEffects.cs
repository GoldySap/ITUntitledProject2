using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class VisualEffects : MonoBehaviour
{
    [HideInInspector] public GameState State;
    [HideInInspector] public SettingsSystem Settings;

    private CanvasGroup canvasGroup;
    private Image scanlineOverlay;
    private Canvas mainCanvas;

    private Color colSafe = new Color(0.85f, 1f, 0.85f); // cool green tint
    private Color colWarn = new Color(1f, 0.92f, 0.75f); // warm amber
    private Color colDanger = new Color(1f, 0.80f, 0.80f); // red
    private Color currentTint;
    
    public void Init(Canvas canvas)
    {
        mainCanvas = canvas;
        canvasGroup = canvas.gameObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = canvas.gameObject.AddComponent<CanvasGroup>();

        BuildScanlines(canvas);
        currentTint = colSafe;
        ApplySettings(Settings.Scanlines, Settings.Flicker);
    }

    void BuildScanlines(Canvas canvas)
    {
        var go = new GameObject("Scanlines");
        go.transform.SetParent(canvas.transform, false);
        scanlineOverlay = go.AddComponent<Image>();

        var tex = new Texture2D(1, 4, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.SetPixel(0, 0, new Color(0, 0, 0, 0.10f));
        tex.SetPixel(0, 1, new Color(0, 0, 0, 0.10f));
        tex.SetPixel(0, 2, new Color(0, 0, 0, 0));
        tex.SetPixel(0, 3, new Color(0, 0, 0, 0));
        tex.Apply();

        var sprite = Sprite.Create(tex,
            new Rect(0, 0, 1, 4),
            new Vector2(0.5f, 0.5f),
            1f,
            0,
            SpriteMeshType.FullRect,
            Vector4.zero,
            false);

        scanlineOverlay.sprite = sprite;
        scanlineOverlay.type = Image.Type.Tiled;
        scanlineOverlay.raycastTarget = false;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        go.transform.SetAsLastSibling();
    }

    public void ApplySettings(bool scanlines, bool flicker)
    {
        if (scanlineOverlay != null)
            scanlineOverlay.enabled = scanlines;
    }
    
    void Update()
    {
        if (State == null || mainCanvas == null) return;
        UpdateColourTemperature();
    }

    void UpdateColourTemperature()
    {
        Color target;
        if      (State.Trace < 50f) target = colSafe;
        else if (State.Trace < 75f) target = Color.Lerp(colSafe,   colWarn,   (State.Trace - 50f) / 25f);
        else                         target = Color.Lerp(colWarn,   colDanger, (State.Trace - 75f) / 25f);

        currentTint = Color.Lerp(currentTint, target, Time.deltaTime * 1.5f);
        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
    }
    
    public void Flicker(int pulses = 3)
    {
        if (Settings != null && !Settings.Flicker) return;
        StartCoroutine(DoFlicker(pulses));
    }

    IEnumerator DoFlicker(int pulses)
    {
        for (int i = 0; i < pulses; i++)
        {
            canvasGroup.alpha = 0.3f;
            yield return new WaitForSeconds(0.05f);
            canvasGroup.alpha = 1f;
            yield return new WaitForSeconds(0.05f);
        }
        canvasGroup.alpha = 1f;
    }
    
    public string SweepBar(float secondsRemaining, float totalSeconds)
    {
        int total  = 10;
        int filled = Mathf.RoundToInt((secondsRemaining / totalSeconds) * total);
        filled = Mathf.Clamp(filled, 0, total);
        var sb = new System.Text.StringBuilder();
        sb.Append("<color=#C0392B>!! SWEEP [");
        for (int i = 0; i < total; i++)
            sb.Append(i < filled ? "#" : ".");
        sb.Append($"] {secondsRemaining:0}s</color>");
        return sb.ToString();
    }
}
