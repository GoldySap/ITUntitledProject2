using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class HackerTerminal : MonoBehaviour
{
    private TextMeshProUGUI screenText;

    private readonly List<string> outputLines = new List<string>();
    private const int MAX_LINES = 28;

    private readonly StringBuilder inputBuffer = new StringBuilder();
    private readonly List<string> history = new List<string>();
    private int historyIndex = -1;

    private float caretTimer = 0f;
    private bool caretVisible = true;

    private bool acceptInput = false;
    private bool gameOver = false;
    private float traceLevel = 0f;
    private int evidenceCount = 0;

    private string G(string s) => $"<color=#7EC850>{s}</color>";   // green
    private string D(string s) => $"<color=#3A3A3A>{s}</color>";   // dim
    private string W(string s) => $"<color=#D4903A>{s}</color>";   // warn/yellow
    private string E(string s) => $"<color=#C0392B>{s}</color>";   // error/red
    private string I(string s) => $"<color=#4A9ECA>{s}</color>";   // info/blue
    private string C(string s) => $"<color=#CCCCCC>{s}</color>";   // normal white

    void Awake()
    {
        BuildUI();
    }

    void Start()
    {
        Keyboard.current.onTextInput += OnCharTyped;
        StartCoroutine(Boot());
    }

    void OnDestroy()
    {
        if (Keyboard.current != null)
            Keyboard.current.onTextInput -= OnCharTyped;
    }

    void Update()
    {
        HandleKeys();
        BlinkCaret();
    }

    // UI BUILD
    void BuildUI()
    {
        if (Camera.main != null) {
            var cameraGO = new GameObject("Main Camera");
            var camera = cameraGO.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
        } else {
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = Color.black;
        }

        var canvasGO = new GameObject("Canvas");
        DontDestroyOnLoad(canvasGO);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode         = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1366, 768);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        var textGO = new GameObject("ScreenText");
        textGO.transform.SetParent(canvasGO.transform, false);
        screenText = textGO.AddComponent<TextMeshProUGUI>();
        screenText.fontSize = 16;
        screenText.color = new Color(0.8f, 0.8f, 0.8f);
        screenText.richText = true;
        screenText.textWrappingMode = TextWrappingModes.Normal;
        screenText.alignment = TextAlignmentOptions.TopLeft;
        screenText.overflowMode = TextOverflowModes.Truncate;

        var rt = textGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(20, 20);
        rt.offsetMax = new Vector2(-20, -20);

        Debug.Log($"[HackerTerminal] screenText created: {screenText != null}");
    }

    //  REDRAW
    void Redraw()
    {
        if (screenText == null)
        {
            Debug.LogError("[HackerTerminal] screenText is null in Redraw!");
            return;
        }

        var sb = new StringBuilder();

        int start = Mathf.Max(0, outputLines.Count - MAX_LINES);
        for (int i = start; i < outputLines.Count; i++)
        {
            sb.Append(outputLines[i]);
            sb.Append('\n');
        }

        if (!gameOver)
        {
            string caret = caretVisible
                ? "<color=#7EC850>\u2588</color>"   // symbol: █ color: light green
                : " ";
            sb.Append(G("SIGNAL@nexus:~$ "));
            sb.Append(C(inputBuffer.ToString()));
            sb.Append(caret);
        }

        screenText.text = sb.ToString();
    }

    //  OUTPUT HELPERS
    void Print(string line)
    {
        outputLines.Add(line);
        Redraw();
    }

    void PrintBlank() => Print("");

    IEnumerator TypeLine(string richLine, float charDelay = 0.02f)
    {
        string plain = Regex.Replace(richLine, "<.*?>", "");
        for (int i = 0; i < plain.Length; i++)
            yield return new WaitForSeconds(charDelay);
        Print(richLine);
    }

    //  CARET BLINK
    void BlinkCaret()
    {
        caretTimer += Time.deltaTime;
        if (caretTimer < 0.5f) return;
        caretTimer   = 0f;
        caretVisible = !caretVisible;
        Redraw();
    }

    //  KEYBOARD INPUT
    void OnCharTyped(char ch)
    {
        if (!acceptInput || gameOver) return;
        if (ch >= 32 && ch <= 126 && inputBuffer.Length < 100)
        {
            inputBuffer.Append(ch);
            caretVisible = true;
            caretTimer   = 0f;
            Redraw();
        }
    }

    void HandleKeys()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
        {
            if (!acceptInput || gameOver) return;
            string cmd = inputBuffer.ToString().Trim().ToLower();
            inputBuffer.Clear();
            historyIndex = -1;
            Redraw();
            if (cmd.Length == 0) return;
            history.Add(cmd);
            acceptInput = false;
            Print(D("SIGNAL@nexus:~$ ") + C(cmd));
            StartCoroutine(ProcessCommand(cmd));
            return;
        }

        if (kb.backspaceKey.wasPressedThisFrame && acceptInput && !gameOver)
        {
            if (inputBuffer.Length > 0)
            {
                inputBuffer.Remove(inputBuffer.Length - 1, 1);
                Redraw();
            }
            return;
        }

        if (kb.upArrowKey.wasPressedThisFrame && acceptInput && history.Count > 0)
        {
            historyIndex = Mathf.Clamp(historyIndex + 1, 0, history.Count - 1);
            inputBuffer.Clear();
            inputBuffer.Append(history[history.Count - 1 - historyIndex]);
            Redraw();
            return;
        }

        if (kb.downArrowKey.wasPressedThisFrame && acceptInput)
        {
            historyIndex = Mathf.Max(-1, historyIndex - 1);
            inputBuffer.Clear();
            if (historyIndex >= 0)
                inputBuffer.Append(history[history.Count - 1 - historyIndex]);
            Redraw();
        }
    }

    //  BOOT
    IEnumerator Boot()
    {
        yield return TypeLine(D("// NEXUS SECURE SHELL v4.1"));
        yield return TypeLine(D("// encrypted tunnel established"));
        yield return TypeLine(D("// location spoofed: Oslo \u2192 Reykjavik \u2192 \u00e3o Paulo"));
        yield return new WaitForSeconds(0.3f);
        PrintBlank();
        yield return TypeLine(G("welcome back, SIGNAL."), 0.04f);
        yield return TypeLine(W("new intercept flagged in queue."), 0.04f);
        yield return TypeLine(D("type ") + C("[help]") + D(" to list available commands."));
        PrintBlank();
        acceptInput = true;
    }

    //  COMMANDS
    IEnumerator ProcessCommand(string cmd)
    {
        switch (cmd)
        {
            case "help": yield return CmdHelp(); break;
            case "scan": yield return CmdScan(); break;
            case "ls": yield return CmdLs(); break;
            case "decrypt": yield return CmdDecrypt(); break;
            case "read 2": yield return CmdRead2(); break;
            case "whoami": yield return CmdWhoami(); break;
            case "status": yield return CmdStatus(); break;
            case "spoof": yield return CmdSpoof(); break;
            case "leak": yield return CmdLeak(); break;
            case "clear": outputLines.Clear(); Redraw(); break;
            default:
                yield return TypeLine(E("command not found: ") + C(cmd));
                yield return TypeLine(D("type ") + C("[help]") + D(" for available commands."));
                break;
        }

        PrintBlank();
        CheckGameOver();
        if (!gameOver) acceptInput = true;
    }

    IEnumerator CmdHelp()
    {
        yield return TypeLine(I("available commands:"));
        string[][] rows = {
            new[]{"scan ", "scan for active government nodes"},
            new[]{"ls ", "list intercepted files"},
            new[]{"decrypt", "decrypt the flagged payload"},
            new[]{"read 2 ", "read intercepted plaintext file"},
            new[]{"whoami ", "display operator profile"},
            new[]{"status ", "display mission status"},
            new[]{"spoof ", "reroute packets, reduce trace"},
            new[]{"leak ", "publish evidence (win condition)"},
            new[]{"clear ", "clear the screen"},
        };
        foreach (var r in rows)
            yield return TypeLine(C($"  {r[0]}") + D($" \u2014 {r[1]}"));
    }

    IEnumerator CmdScan()
    {
        yield return TypeLine(D("scanning government nodes..."));
        yield return new WaitForSeconds(0.6f);
        yield return TypeLine(G("GOV_NODE_03  [ACTIVE]   surveillance.hub"));
        yield return TypeLine(W("GOV_NODE_07  [ACTIVE]   data.archive \u2014 3 encrypted files"));
        yield return TypeLine(D("GOV_NODE_11  [OFFLINE]  \u2014"));
        AddTrace(8f);
        yield return TypeLine(E("\u25b2 trace +8%"));
    }

    IEnumerator CmdLs()
    {
        yield return TypeLine(I("intercepted files:"));
        yield return TypeLine(D("  [0] payload_0041.enc    128-bit   ") + E("LOCKED"));
        yield return TypeLine(D("  [1] payload_0047.enc    128-bit   ") + E("LOCKED"));
        yield return TypeLine(D("  [2] ECHO_LIST_frag.txt  plaintext ") + G("readable"));
    }

    IEnumerator CmdDecrypt()
    {
        yield return TypeLine(D("target: payload_0047.enc"));
        yield return TypeLine(D("running cipher analysis..."));
        yield return new WaitForSeconds(0.5f);
        yield return TypeLine(W("\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2591\u2591\u2591\u2591\u2591\u2591\u2591\u2591  50%"));
        yield return new WaitForSeconds(0.3f);
        yield return TypeLine(G("\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588  100% \u2014 decrypted."));
        yield return TypeLine(C("contents: OPERATION ECHO \u2014 citizen IDs flagged for pre-arrest."));
        evidenceCount++;
        AddTrace(15f);
        yield return TypeLine(G($"evidence collected: {evidenceCount}   ") + E("\u25b2 trace +15%"));
    }

    IEnumerator CmdRead2()
    {
        yield return TypeLine(D("// ECHO_LIST_frag.txt \u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500"));
        yield return TypeLine(C("SUBJECT 4471: Mara Voss  \u2014 ") + E("flagged [HIGH]"));
        yield return TypeLine(C("SUBJECT 4472: Jin Park   \u2014 ") + W("flagged [MED]"));
        yield return TypeLine(D("... [file truncated \u2014 2,847 more entries]"));
        yield return TypeLine(W("this is bigger than you thought."));
        evidenceCount++;
        AddTrace(5f);
        yield return TypeLine(G($"evidence collected: {evidenceCount}   ") + E("\u25b2 trace +5%"));
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
            ? G("enough evidence \u2014 type [leak] to go public.")
            : D($"need {3 - evidenceCount} more piece(s) before you can leak."));
    }

    IEnumerator CmdSpoof()
    {
        float r = Mathf.Min(traceLevel, 20f);
        yield return TypeLine(D("rerouting packets through proxy chain..."));
        yield return new WaitForSeconds(0.5f);
        traceLevel = Mathf.Max(0, traceLevel - r);
        yield return TypeLine(G($"trace reduced by {r:0}%  \u2014 current: {traceLevel:0}%"));
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
        yield return TypeLine(G("\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588  transmitted."));
        PrintBlank();
        yield return TypeLine(G("OPERATION ECHO is now public."));
        yield return TypeLine(C("2,849 citizens will not be silenced tonight."));
        yield return TypeLine(D("connection terminated. stay hidden, SIGNAL."));
        gameOver = true;
    }

    //  GAME STATE
    void AddTrace(float amount)
    {
        traceLevel = Mathf.Min(100f, traceLevel + amount);
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
        PrintBlank();
        yield return TypeLine(E("!! TRACE COMPLETE -- LOCATION EXPOSED"));
        yield return TypeLine(E("incoming connection from GOV_NODE_03..."));
        yield return TypeLine(D("connection terminated."));
        yield return new WaitForSeconds(1.5f);
        gameOver   = false;
        traceLevel = 0f;
        PrintBlank();
        yield return TypeLine(D("// rebooting secure shell..."));
        yield return TypeLine(G("reconnected. evidence preserved. keep going."));
        PrintBlank();
        acceptInput = true;
    }
}