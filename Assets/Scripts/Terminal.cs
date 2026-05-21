using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class Terminal : MonoBehaviour
{
    [HideInInspector] public GameState State;
    [HideInInspector] public CommandRouter Router;
    [HideInInspector] public SettingsSystem Settings;

    private TextMeshProUGUI screenText;
    private TextMeshProUGUI statusBar;
    public  Canvas MainCanvas { get; private set; }

    private readonly List<string> outputLines = new List<string>();
    private const int MAX_LINES = 32;
    private int scrollOffset = 0;
    private const int SCROLL_STEP = 1;

    private readonly StringBuilder inputBuffer = new StringBuilder();
    private readonly List<string>  inputHistory = new List<string>();
    private int historyIndex = -1;
    private float caretTimer = 0f;
    private bool  caretVisible = true;

    public string G(string s) => $"<color=#7EC850>{s}</color>";
    public string D(string s) => $"<color=#3A3A3A>{s}</color>";
    public string W(string s) => $"<color=#D4903A>{s}</color>";
    public string E(string s) => $"<color=#C0392B>{s}</color>";
    public string I(string s) => $"<color=#4A9ECA>{s}</color>";
    public string C(string s) => $"<color=#CCCCCC>{s}</color>";
    
    public void BuildUI()
    {
        if (Camera.main != null)
        {
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = Color.black;
        } 
        else
        {
            var cameraGO = new GameObject("Main Camera");
            Camera cam = cameraGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
        }

        var canvasGO = new GameObject("Canvas");
        DontDestroyOnLoad(canvasGO);
        MainCanvas = canvasGO.AddComponent<Canvas>();
        MainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1366, 768);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        var sGO = new GameObject("StatusBar");
        sGO.transform.SetParent(canvasGO.transform, false);
        statusBar = sGO.AddComponent<TextMeshProUGUI>();
        statusBar.fontSize         = 12;
        statusBar.richText         = true;
        statusBar.alignment        = TextAlignmentOptions.TopLeft;
        statusBar.textWrappingMode = TextWrappingModes.NoWrap;
        var sRT = sGO.GetComponent<RectTransform>();
        sRT.anchorMin = new Vector2(0,1); sRT.anchorMax = new Vector2(1,1);
        sRT.pivot = new Vector2(0.5f,1);
        sRT.sizeDelta = new Vector2(0,26);
        sRT.offsetMin = new Vector2(20,0); sRT.offsetMax = new Vector2(-20,0);
        sRT.anchoredPosition = new Vector2(0,-4);

        var lGO = new GameObject("Sep");
        lGO.transform.SetParent(canvasGO.transform, false);
        lGO.AddComponent<UnityEngine.UI.Image>().color = new Color(0.15f,0.15f,0.15f,1f);
        var lRT = lGO.GetComponent<RectTransform>();
        lRT.anchorMin = new Vector2(0,1); lRT.anchorMax = new Vector2(1,1);
        lRT.pivot = new Vector2(0.5f,1); lRT.sizeDelta = new Vector2(0,1);
        lRT.offsetMin = Vector2.zero; lRT.offsetMax = Vector2.zero;
        lRT.anchoredPosition = new Vector2(0,-30);

        var tGO = new GameObject("ScreenText");
        tGO.transform.SetParent(canvasGO.transform, false);
        screenText = tGO.AddComponent<TextMeshProUGUI>();
        screenText.fontSize         = 14;
        screenText.richText         = true;
        screenText.textWrappingMode = TextWrappingModes.Normal;
        screenText.alignment        = TextAlignmentOptions.TopLeft;
        screenText.overflowMode     = TextOverflowModes.Truncate;
        var tRT = tGO.GetComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
        tRT.offsetMin = new Vector2(20,16); tRT.offsetMax = new Vector2(-20,-34);
    }
    
    public void HandleInput()
    {
        BlinkCaret();
        HandleScrollKeys();

        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
        {
            if (!State.AcceptInput || State.GameOver) return;
            string cmd = inputBuffer.ToString().Trim().ToLower();
            inputBuffer.Clear(); historyIndex = -1;
            if (cmd.Length == 0) { Redraw(); return; }
            inputHistory.Add(cmd);
            State.AcceptInput = false;
            scrollOffset = 0;
            string prompt = State.ActiveNode.Length > 0
                ? $"SIGNAL@{State.ActiveNode}:~$ "
                : "SIGNAL@nexus:~$ ";
            Print(D(prompt) + C(cmd));
            StartCoroutine(Router.Route(cmd));
            return;
        }

        if (kb.backspaceKey.wasPressedThisFrame && State.AcceptInput && !State.GameOver)
        {
            if (inputBuffer.Length > 0) { inputBuffer.Remove(inputBuffer.Length - 1, 1); Redraw(); }
            return;
        }

        bool shift = kb.shiftKey.isPressed || kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
        if (!shift)
        {
            if (kb.upArrowKey.wasPressedThisFrame && State.AcceptInput && inputHistory.Count > 0)
            {
                historyIndex = Mathf.Clamp(historyIndex + 1, 0, inputHistory.Count - 1);
                inputBuffer.Clear();
                inputBuffer.Append(inputHistory[inputHistory.Count - 1 - historyIndex]);
                Redraw(); return;
            }
            if (kb.downArrowKey.wasPressedThisFrame && State.AcceptInput)
            {
                historyIndex = Mathf.Max(-1, historyIndex - 1);
                inputBuffer.Clear();
                if (historyIndex >= 0)
                    inputBuffer.Append(inputHistory[inputHistory.Count - 1 - historyIndex]);
                Redraw();
            }
        }
    }

    private float nextScrollTime = 0f;
    private float holdDelay = 0.5f;
    private float continuousScrollRate = 0.1f;

    void HandleScrollKeys()
    {
        var kb = Keyboard.current;
        if (kb == null) return;


        bool shift = kb.shiftKey.isPressed || kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
        float scroll = Mouse.current.scroll.ReadValue().y;

        bool isInputPressedUp = kb.pageUpKey.wasPressedThisFrame || (shift && kb.upArrowKey.wasPressedThisFrame); 
        bool isInputHeldUp = kb.pageUpKey.isPressed || (shift && kb.upArrowKey.isPressed);

        bool isInputPressedDown = kb.pageDownKey.wasPressedThisFrame || (shift && kb.downArrowKey.wasPressedThisFrame);
        bool isInputHeldDown = kb.pageDownKey.isPressed || (shift && kb.downArrowKey.isPressed);

        if (isInputPressedUp)
        {
            int maxScroll = Mathf.Max(0, outputLines.Count - MAX_LINES);
            scrollOffset = Mathf.Min(scrollOffset + SCROLL_STEP, maxScroll);
            Redraw();
            nextScrollTime = Time.time + holdDelay;
        }
        
        if (isInputHeldUp && Time.time >= nextScrollTime || scroll > 0f && Time.time >= nextScrollTime)
        {
            int maxScroll = Mathf.Max(0, outputLines.Count - MAX_LINES);
            scrollOffset = Mathf.Min(scrollOffset + SCROLL_STEP, maxScroll);
            Redraw();
            nextScrollTime = Time.time + continuousScrollRate;
        }

        if (isInputPressedDown)
        {
            scrollOffset = Mathf.Max(0, scrollOffset - SCROLL_STEP);
            Redraw();
            nextScrollTime = Time.time + holdDelay;
        }
        
        if (isInputHeldDown && Time.time >= nextScrollTime || scroll < 0f && Time.time >= nextScrollTime)
        {
            scrollOffset = Mathf.Max(0, scrollOffset - SCROLL_STEP);
            Redraw();
            nextScrollTime = Time.time + continuousScrollRate;
        }
        
        else if (kb.homeKey.wasPressedThisFrame)
        {
            scrollOffset = Mathf.Max(0, outputLines.Count - MAX_LINES);
            Redraw();
        }

        else if (kb.endKey.wasPressedThisFrame)
        {
            scrollOffset = 0;
            Redraw();
        }
    }

    public void OnCharTyped(char ch)
    {
        if (!State.AcceptInput || State.GameOver) return;
        if (ch >= 32 && ch <= 126 && inputBuffer.Length < 120)
        {
            inputBuffer.Append(ch);
            caretVisible = true; caretTimer = 0f;
            Redraw();
        }
    }
    
    public void Print(string line)
    {
        outputLines.Add(line);
        if (scrollOffset == 0) Redraw();
        else
        {
            UpdateStatusBar();
        }
    }

    public void Blank() => Print("");

    public void ClearScreen()
    {
        outputLines.Clear();
        scrollOffset = 0;
        Redraw();
    }

    public IEnumerator TypeLine(string rich, float delay = -1f)
    {
        float d = delay < 0 ? (Settings != null ? Settings.TypeDelay : 0.018f) : delay;
        if (d <= 0f) { Print(rich); yield break; }
        string plain = Regex.Replace(rich, "<.*?>", "");
        for (int i = 0; i < plain.Length; i++)
            yield return new WaitForSeconds(d);
        Print(rich);
    }

    public IEnumerator Slow(string rich)
    {
        float d = Settings != null ? Mathf.Max(Settings.TypeDelay, 0.03f) : 0.038f;
        yield return TypeLine(rich, d);
    }
    
    public void Redraw()
    {
        if (screenText == null) return;
        UpdateStatusBar();

        var sb = new StringBuilder();

        if (scrollOffset > 0)
            sb.Append(D($"  -- scrolled up {scrollOffset} lines -- [PageDn / Shift+Down to return] --\n"));

        int totalLines = outputLines.Count;
        int end = totalLines - scrollOffset;
        int start = Mathf.Max(0, end - MAX_LINES);
        end = Mathf.Max(start, end);

        for (int i = start; i < end; i++)
        {
            sb.Append(outputLines[i]);
            sb.Append('\n');
        }

        if (!State.GameOver && scrollOffset == 0)
        {
            string prompt = State.ActiveNode.Length > 0
                ? G($"SIGNAL@{State.ActiveNode}:~$ ")
                : G("SIGNAL@nexus:~$ ");
            string caret = caretVisible ? "<color=#7EC850>\u2588</color>" : " ";
            sb.Append(prompt);
            sb.Append(C(inputBuffer.ToString()));
            sb.Append(caret);
        }
        screenText.text = sb.ToString();
    }

    public void UpdateStatusBar()
    {
        if (statusBar == null) return;
        string tc = State.Trace < 40 ? "#7EC850" : State.Trace < 70 ? "#D4903A" : "#C0392B";
        string hc = State.Heat  < 40 ? "#7EC850" : State.Heat  < 70 ? "#D4903A" : "#C0392B";
        string node  = State.ActiveNode.Length > 0 ? D($"  [{State.ActiveNode.ToUpper()}]") : "";
        string dark  = State.InDarkMode  ? W("  [DARK]")     : "";
        string proxy = State.ProxyActive ? I("  [PROXY]")    : "";
        string sweep = State.SweepActive ? E("  [!!SWEEP]")  : "";
        string scroll= scrollOffset > 0  ? W($"  [^{scrollOffset}]") : "";
        string miss  = State.CurrentMission < MissionSystem.Missions.Length
            ? D($"  M{State.CurrentMission + 1}")
            : D("  END");
        statusBar.text =
            $"<color={tc}>TRACE {State.Trace:0}%</color>   " +
            $"<color={hc}>HEAT {State.Heat:0}%</color>   " +
            I($"FAV {State.Favours}") + "   " +
            G($"EV {State.Evidence}/5") +
            node + dark + proxy + sweep + scroll + miss;
    }
    
    public string NodeMap()
    {
        string h  = G("[HUB_03]");
        string a  = G("[ARCHIVE_07]");
        string d  = State.Deep12Locked ? E("[DEEP_12]")  : W("[DEEP_12]");
        string c2 = State.Core19Locked ? E("[CORE_19]")  : I("[CORE_19]");
        string dl = State.Deep12Locked ? E("locked")     : G("open");
        string cl = State.Core19Locked ? E("locked")     : G("open");
        return
            D("   ") + h + D("---") + a + D("---") + d + D("---") + c2 + "\n" +
            D("   open        open           ") + dl + D("         ") + cl;
    }

    public string TraceBar()
    {
        int filled = Mathf.RoundToInt(State.Trace / 5f);
        var sb = new StringBuilder();
        sb.Append(D("TRACE ["));
        for (int i = 0; i < 20; i++)
        {
            if (i < filled) { string col = i < 8 ? "#7EC850" : i < 14 ? "#D4903A" : "#C0392B"; sb.Append($"<color={col}>#</color>"); }
            else sb.Append(D("."));
        }
        sb.Append(D("]  ") + $"<color={(State.Trace<40?"#7EC850":State.Trace<70?"#D4903A":"#C0392B")}>{State.Trace:0}%</color>");
        return sb.ToString();
    }
    
    void BlinkCaret()
    {
        caretTimer += Time.deltaTime;
        if (caretTimer < 0.5f) return;
        caretTimer = 0f; caretVisible = !caretVisible;
        Redraw();
    }
}
