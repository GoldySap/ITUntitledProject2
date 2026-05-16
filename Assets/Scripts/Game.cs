using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// HackerTerminal — attach this to any empty GameObject in a blank scene.
/// NO TMP_InputField used — keyboard input is handled directly, which is
/// far more reliable when building UI entirely in code.
///
/// SETUP (3 steps only):
///   1. New Unity 2D project
///   2. Window > TextMeshPro > Import TMP Essential Resources > Import
///   3. Create empty GameObject, attach this script, hit Play
/// </summary>
public class Game : MonoBehaviour
{
    // UI
    private TMP_Text outputText;   // scrollable output area
    private TMP_Text inputLineText; // the live input line shown at bottom
    private Slider   traceSlider;
    private TMP_Text traceLabel;
    private ScrollRect scrollRect;

    // Input state (managed manually)
    private StringBuilder currentInput = new StringBuilder();
    private List<string>  commandHistory = new List<string>();
    private int           historyIndex   = -1;
    private bool          acceptInput    = false; // enabled after boot
    private float         caretTimer     = 0f;
    private bool          caretVisible   = true;

    // ── Game state ────────────────────────────────────────────────────
    private float traceLevel   = 0f;
    private int   evidenceCount = 0;
    private bool  gameOver     = false;

    // ── Colours ───────────────────────────────────────────────────────
    private static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out Color c); return c; }
    private static readonly Color COL_BG        = Hex("#0A0A0A");
    private static readonly Color COL_PANEL     = Hex("#111111");
    private static readonly Color COL_GREEN     = Hex("#7EC850");
    private static readonly Color COL_WARN      = Hex("#D4903A");
    private static readonly Color COL_ERR       = Hex("#C0392B");
    private static readonly Color COL_FILL_BG   = Hex("#1A1A1A");
    private static readonly Color COL_WHITE     = Hex("#CCCCCC");
    private static readonly Color COL_NONE      = new Color(0,0,0,0);

    // ── Rich text helpers ─────────────────────────────────────────────
    private string G(string s) => $"<color=#7EC850>{s}</color>";
    private string D(string s) => $"<color=#3A3A3A>{s}</color>";
    private string W(string s) => $"<color=#D4903A>{s}</color>";
    private string E(string s) => $"<color=#C0392B>{s}</color>";
    private string I(string s) => $"<color=#4A9ECA>{s}</color>";
    private string C(string s) => $"<color=#CCCCCC>{s}</color>";

    // =================================================================
    //  BOOTSTRAP
    // =================================================================
    void Start()
    {
        BuildUI();
        StartCoroutine(BootSequence());
    }

    // =================================================================
    //  UPDATE — manual keyboard handling, no InputField needed
    // =================================================================
    void Update()
    {
        // Blinking caret
        caretTimer += Time.deltaTime;
        if (caretTimer >= 0.5f) { caretTimer = 0f; caretVisible = !caretVisible; }
        RedrawInputLine();

        if (!acceptInput || gameOver) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        // Enter — submit command
        if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
        {
            string cmd = currentInput.ToString().Trim().ToLower();
            currentInput.Clear();
            historyIndex = -1;
            if (cmd.Length > 0)
            {
                commandHistory.Add(cmd);
                acceptInput = false;
                Append(D("SIGNAL@nexus:~$ ") + C(cmd));
                StartCoroutine(ProcessCommand(cmd));
            }
            return;
        }

        // Backspace
        if (kb.backspaceKey.wasPressedThisFrame && currentInput.Length > 0)
        {
            currentInput.Remove(currentInput.Length - 1, 1);
            return;
        }

        // History — Up
        if (kb.upArrowKey.wasPressedThisFrame && commandHistory.Count > 0)
        {
            historyIndex = Mathf.Clamp(historyIndex + 1, 0, commandHistory.Count - 1);
            currentInput.Clear();
            currentInput.Append(commandHistory[commandHistory.Count - 1 - historyIndex]);
            return;
        }

        // History — Down
        if (kb.downArrowKey.wasPressedThisFrame)
        {
            historyIndex = Mathf.Max(-1, historyIndex - 1);
            currentInput.Clear();
            if (historyIndex >= 0)
                currentInput.Append(commandHistory[commandHistory.Count - 1 - historyIndex]);
            return;
        }

        // Printable characters via onTextInput event (handles all layouts/IME)
        // We subscribe once in BuildUI; typed chars arrive via OnTextInput().
    }

    // Called by Keyboard.onTextInput — fires for every printable character typed
    void OnTextInput(char ch)
    {
        if (!acceptInput || gameOver) return;
        // Allow printable ASCII only (space through ~)
        if (ch >= 32 && ch <= 126 && currentInput.Length < 120)
            currentInput.Append(ch);
    }

    void RedrawInputLine()
    {
        if (inputLineText == null) return;
        string caret = caretVisible ? "<color=#7EC850>█</color>" : " ";
        inputLineText.text = G("SIGNAL@nexus:~$ ") + C(currentInput.ToString()) + caret;
    }

    // =================================================================
    //  UI BUILDER
    // =================================================================
    void BuildUI()
    {
        // Camera — black background
        Camera.main.backgroundColor = Color.black;
        Camera.main.clearFlags = CameraClearFlags.SolidColor;

        // Subscribe to text input events
        Keyboard.current.onTextInput += OnTextInput;

        // Canvas
        var canvasGO = new GameObject("Canvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1366, 768);
        scaler.screenMatchMode    = CanvasScaler.ScreenMatchMode.Expand;
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Terminal outer panel
        var term   = MakePanel(canvasGO, "Terminal", COL_BG);
        var termRT = term.GetComponent<RectTransform>();
        termRT.anchorMin = new Vector2(0.01f, 0.01f);
        termRT.anchorMax = new Vector2(0.99f, 0.99f);
        termRT.offsetMin = termRT.offsetMax = Vector2.zero;

        // ── Title bar (top) ───────────────────────────────────────────
        var titleBar = MakePanel(term, "TitleBar", COL_PANEL);
        PinTop(titleBar, 36);
        var titleTxt = MakeTMP(titleBar, "Title",
            D("// ") + G("NEXUS SHELL v4.1") + D(" — SIGNAL@unknown:~"), 12);
        Stretch(titleTxt.rectTransform, 14, 14, 0, 0);

        // ── Trace bar (bottom) ────────────────────────────────────────
        var traceRow = MakePanel(term, "TraceRow", COL_PANEL);
        PinBottom(traceRow, 0, 26);

        traceLabel = MakeTMP(traceRow, "TraceLabel", D("TRACE  ") + G("0%"), 11);
        var tlRT = traceLabel.rectTransform;
        tlRT.anchorMin = new Vector2(0,0); tlRT.anchorMax = new Vector2(0,1);
        tlRT.pivot = new Vector2(0, 0.5f);
        tlRT.sizeDelta = new Vector2(100, 0);
        tlRT.anchoredPosition = new Vector2(14, 0);

        traceSlider = BuildSlider(traceRow);

        // ── Input line (above trace bar) ──────────────────────────────
        var inputRow = MakePanel(term, "InputRow", COL_PANEL);
        PinBottom(inputRow, 26, 38);

        inputLineText = MakeTMP(inputRow, "InputLine", "", 13);
        Stretch(inputLineText.rectTransform, 14, 14, 0, 0);
        inputLineText.alignment = TextAlignmentOptions.MidlineLeft;

        // ── Scroll output (fills remaining space) ─────────────────────
        BuildScrollView(term, topOffset: 36, bottomOffset: 64);
    }

    Slider BuildSlider(GameObject parent)
    {
        var go = new GameObject("Slider");
        go.transform.SetParent(parent.transform, false);
        var slider = go.AddComponent<Slider>();
        slider.minValue = 0; slider.maxValue = 100; slider.value = 0;
        slider.interactable = false;
        slider.direction = Slider.Direction.LeftToRight;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0.5f); rt.anchorMax = new Vector2(1, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(-120, 5);
        rt.anchoredPosition = new Vector2(50, 0);

        // Fill area (covers full rect so fill works from left)
        var fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(go.transform, false);
        Stretch(fillArea.AddComponent<RectTransform>());

        var bg = new GameObject("BG"); bg.transform.SetParent(fillArea.transform, false);
        var bgImg = bg.AddComponent<Image>(); bgImg.color = COL_FILL_BG;
        Stretch(bg.GetComponent<RectTransform>());
        slider.targetGraphic = bgImg;

        var fill = new GameObject("Fill"); fill.transform.SetParent(fillArea.transform, false);
        var fillImg = fill.AddComponent<Image>(); fillImg.color = COL_GREEN;
        Stretch(fill.GetComponent<RectTransform>());
        slider.fillRect = fill.GetComponent<RectTransform>();

        return slider;
    }

    void BuildScrollView(GameObject parent, float topOffset, float bottomOffset)
    {
        var scrollGO = new GameObject("Scroll");
        scrollGO.transform.SetParent(parent.transform, false);
        scrollRect = scrollGO.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical   = true;
        scrollRect.scrollSensitivity = 30;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollGO.AddComponent<Image>().color = COL_NONE;
        var scRT = scrollGO.GetComponent<RectTransform>();
        scRT.anchorMin = Vector2.zero; scRT.anchorMax = Vector2.one;
        scRT.offsetMin = new Vector2(0, bottomOffset);
        scRT.offsetMax = new Vector2(0, -topOffset);

        var vp = new GameObject("Viewport"); vp.transform.SetParent(scrollGO.transform, false);
        vp.AddComponent<RectMask2D>();
        vp.AddComponent<Image>().color = COL_NONE;
        Stretch(vp.GetComponent<RectTransform>());
        scrollRect.viewport = vp.GetComponent<RectTransform>();

        var content = new GameObject("Content"); content.transform.SetParent(vp.transform, false);
        var contentRT = content.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0,1); contentRT.anchorMax = new Vector2(1,1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.sizeDelta = contentRT.anchoredPosition = Vector2.zero;
        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.padding = new RectOffset(16, 16, 12, 12);
        scrollRect.content = contentRT;

        var outGO = new GameObject("OutputText"); outGO.transform.SetParent(content.transform, false);
        outputText = outGO.AddComponent<TextMeshProUGUI>();
        outputText.fontSize = 13;
        outputText.color = COL_WHITE;
        outputText.alignment = TextAlignmentOptions.TopLeft;
        outputText.textWrappingMode = TextWrappingModes.Normal;
        outputText.richText = true;
        outputText.lineSpacing = 6;
        var outCSF = outGO.AddComponent<ContentSizeFitter>();
        outCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    // =================================================================
    //  BOOT SEQUENCE
    // =================================================================
    IEnumerator BootSequence()
    {
        acceptInput = false;
        // Wait two frames so Unity fully initialises all UI components before we write anything
        yield return null;
        yield return null;
        yield return TypeLine(D("// NEXUS SECURE SHELL v4.1"), 0.03f);
        yield return TypeLine(D("// encrypted tunnel established"), 0.03f);
        yield return TypeLine(D("// location spoofed: Oslo → Reykjavik → São Paulo"), 0.03f);
        yield return new WaitForSeconds(0.3f);
        Append("");
        yield return TypeLine(G("welcome back, SIGNAL."), 0.04f);
        yield return TypeLine(W("new intercept flagged in queue."), 0.04f);
        yield return TypeLine(D("type ") + C("[help]") + D(" to list available commands."), 0.03f);
        Append("");
        acceptInput = true;
    }

    // =================================================================
    //  COMMAND PROCESSING
    // =================================================================
    IEnumerator ProcessCommand(string cmd)
    {
        switch (cmd)
        {
            case "help":    yield return CmdHelp();    break;
            case "scan":    yield return CmdScan();    break;
            case "ls":      yield return CmdLs();      break;
            case "decrypt": yield return CmdDecrypt(); break;
            case "read 2":  yield return CmdRead2();   break;
            case "whoami":  yield return CmdWhoami();  break;
            case "status":  yield return CmdStatus();  break;
            case "spoof":   yield return CmdSpoof();   break;
            case "leak":    yield return CmdLeak();    break;
            case "clear":   outputText.text = "";      break;
            default:
                yield return TypeLine(E("command not found: ") + C(cmd));
                yield return TypeLine(D("type ") + C("[help]") + D(" for available commands."));
                break;
        }
        Append("");
        ScrollToBottom();
        CheckGameOver();
        if (!gameOver) acceptInput = true;
    }

    IEnumerator CmdHelp()
    {
        yield return TypeLine(I("available commands:"));
        string[][] rows = {
            new[]{"scan   ","scan for active government nodes"},
            new[]{"ls     ","list intercepted files"},
            new[]{"decrypt","decrypt the flagged payload"},
            new[]{"read 2 ","read intercepted plaintext file"},
            new[]{"whoami ","display operator profile"},
            new[]{"status ","display mission status"},
            new[]{"spoof  ","reroute packets, reduce trace"},
            new[]{"leak   ","publish evidence (win condition)"},
            new[]{"clear  ","clear terminal output"},
        };
        foreach (var r in rows)
            yield return TypeLine(C($"  {r[0]}") + D($" — {r[1]}"));
    }

    IEnumerator CmdScan()
    {
        yield return TypeLine(D("scanning government nodes..."));
        yield return new WaitForSeconds(0.6f);
        yield return TypeLine(G("GOV_NODE_03  [ACTIVE]   surveillance.hub"));
        yield return TypeLine(W("GOV_NODE_07  [ACTIVE]   data.archive — 3 encrypted files"));
        yield return TypeLine(D("GOV_NODE_11  [OFFLINE]  —"));
        AddTrace(8f);
        yield return TypeLine(E("▲ trace +8%"));
    }

    IEnumerator CmdLs()
    {
        yield return TypeLine(I("intercepted files:"));
        yield return TypeLine(D("  [0] payload_0041.enc    128-bit  ") + E("LOCKED"));
        yield return TypeLine(D("  [1] payload_0047.enc    128-bit  ") + E("LOCKED"));
        yield return TypeLine(D("  [2] ECHO_LIST_frag.txt  plaintext ") + G("readable"));
    }

    IEnumerator CmdDecrypt()
    {
        yield return TypeLine(D("target: payload_0047.enc"));
        yield return TypeLine(D("running cipher analysis..."));
        yield return new WaitForSeconds(0.5f);
        yield return TypeLine(W("████████░░░░░░░░  50%"));
        yield return new WaitForSeconds(0.3f);
        yield return TypeLine(G("████████████████  100% — decrypted."));
        yield return TypeLine(C("contents: OPERATION ECHO — citizen IDs flagged for pre-arrest."));
        evidenceCount++;
        AddTrace(15f);
        yield return TypeLine(G($"evidence collected: {evidenceCount}   ") + E("▲ trace +15%"));
    }

    IEnumerator CmdRead2()
    {
        yield return TypeLine(D("// ECHO_LIST_frag.txt ─────────────────"));
        yield return TypeLine(C("SUBJECT 4471: Mara Voss  — ") + E("flagged [HIGH]"));
        yield return TypeLine(C("SUBJECT 4472: Jin Park   — ") + W("flagged [MED]"));
        yield return TypeLine(D("... [file truncated — 2,847 more entries]"));
        yield return TypeLine(W("this is bigger than you thought."));
        evidenceCount++;
        AddTrace(5f);
        yield return TypeLine(G($"evidence collected: {evidenceCount}   ") + E("▲ trace +5%"));
    }

    IEnumerator CmdWhoami()
    {
        yield return TypeLine(G("operator : SIGNAL"));
        yield return TypeLine(C("status   : anonymous"));
        yield return TypeLine(C("location : unknown (spoofed)"));
        yield return TypeLine(W("mission  : expose OPERATION ECHO"));
    }

    IEnumerator CmdStatus()
    {
        string tCol = traceLevel < 40 ? "#7EC850" : traceLevel < 70 ? "#D4903A" : "#C0392B";
        yield return TypeLine(C("trace level     : ") + $"<color={tCol}>{traceLevel:0}%</color>");
        yield return TypeLine(C("evidence pieces : ") + G($"{evidenceCount}"));
        yield return TypeLine(evidenceCount >= 3
            ? G("enough evidence — type [leak] to go public.")
            : D($"need {3 - evidenceCount} more piece(s) before you can leak."));
    }

    IEnumerator CmdSpoof()
    {
        float r = Mathf.Min(traceLevel, 20f);
        yield return TypeLine(D("rerouting packets through proxy chain..."));
        yield return new WaitForSeconds(0.5f);
        traceLevel = Mathf.Max(0, traceLevel - r);
        UpdateTraceUI();
        yield return TypeLine(G($"trace reduced by {r:0}%  — current: {traceLevel:0}%"));
    }

    IEnumerator CmdLeak()
    {
        if (evidenceCount < 3)
        {
            yield return TypeLine(E("not enough evidence yet."));
            yield return TypeLine(D($"need {3 - evidenceCount} more piece(s) first."));
            yield break;
        }
        yield return TypeLine(W("uploading encrypted package to secure drop..."));
        yield return new WaitForSeconds(0.8f);
        yield return TypeLine(G("████████████████  transmitted."));
        Append("");
        yield return TypeLine(G("OPERATION ECHO is now public."));
        yield return TypeLine(C("2,849 citizens will not be silenced tonight."));
        yield return TypeLine(D("connection terminated. stay hidden, SIGNAL."));
        gameOver = true;
    }

    // =================================================================
    //  GAME STATE
    // =================================================================
    void AddTrace(float amount)
    {
        traceLevel = Mathf.Min(100f, traceLevel + amount);
        UpdateTraceUI();
    }

    void UpdateTraceUI()
    {
        if (traceSlider == null) return;
        traceSlider.value = traceLevel;
        Color col = traceLevel < 40f ? COL_GREEN : traceLevel < 70f ? COL_WARN : COL_ERR;
        traceSlider.fillRect.GetComponent<Image>().color = col;
        string hex = ColorUtility.ToHtmlStringRGB(col);
        traceLabel.text = D("TRACE  ") + $"<color=#{hex}>{traceLevel:0}%</color>";
    }

    void CheckGameOver()
    {
        if (traceLevel >= 100f && !gameOver)
        {
            gameOver = true;
            StartCoroutine(RunGameOver());
        }
    }

    IEnumerator RunGameOver()
    {
        yield return new WaitForSeconds(0.3f);
        Append("");
        yield return TypeLine(E("⚠ TRACE COMPLETE — LOCATION EXPOSED"));
        yield return TypeLine(E("incoming connection from GOV_NODE_03..."));
        yield return TypeLine(D("connection terminated."));
        yield return new WaitForSeconds(1.5f);
        gameOver = false;
        traceLevel = 0;
        UpdateTraceUI();
        Append("");
        yield return TypeLine(D("// rebooting secure shell..."));
        yield return TypeLine(G("reconnected. evidence preserved. keep going."));
        Append("");
        acceptInput = true;
    }

    // =================================================================
    //  OUTPUT HELPERS
    // =================================================================
    void Append(string line)
    {
        if (outputText == null) return;
        outputText.text += (outputText.text.Length > 0 ? "\n" : "") + line;
        ScrollToBottom();
    }

    IEnumerator TypeLine(string line, float delay = 0.018f)
    {
        string plain = Regex.Replace(line, "<.*?>", "");
        for (int i = 0; i < plain.Length; i++)
            yield return new WaitForSeconds(delay);
        Append(line);
    }

    void ScrollToBottom()
    {
        Canvas.ForceUpdateCanvases();
        if (scrollRect) scrollRect.normalizedPosition = new Vector2(0, 0);
    }

    // =================================================================
    //  UI FACTORY HELPERS
    // =================================================================
    GameObject MakePanel(GameObject parent, string name, Color col)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<Image>().color = col;
        return go;
    }

    TMP_Text MakeTMP(GameObject parent, string name, string text, float size)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.richText = true;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        return tmp;
    }

    // Pin a child panel to the top of its parent
    void PinTop(GameObject go, float height)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.sizeDelta = new Vector2(0, height);
        rt.anchoredPosition = Vector2.zero;
    }

    // Pin a child panel to the bottom, with a yOffset from bottom and given height
    void PinBottom(GameObject go, float yOffset, float height)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = new Vector2(1, 0);
        rt.pivot = new Vector2(0.5f, 0);
        rt.sizeDelta = new Vector2(0, height);
        rt.anchoredPosition = new Vector2(0, yOffset);
    }

    void Stretch(RectTransform rt, float l=0, float r=0, float b=0, float t=0)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
    }

    void OnDestroy()
    {
        // Clean up the text input subscription when the object is destroyed
        if (Keyboard.current != null)
            Keyboard.current.onTextInput -= OnTextInput;
    }
}