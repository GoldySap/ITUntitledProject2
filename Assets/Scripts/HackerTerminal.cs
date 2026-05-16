using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// Attach to any empty GameObject in a blank Unity 2D scene.
/// Requires: TextMeshPro (Window > TextMeshPro > Import TMP Essential Resources)
public class HackerTerminal : MonoBehaviour
{
    private TextMeshProUGUI screenText;
    private TextMeshProUGUI statusBar;

    private readonly List<string> outputLines = new List<string>();
    private const int MAX_LINES = 24;

    private readonly StringBuilder inputBuffer = new StringBuilder();
    private readonly List<string>  history     = new List<string>();
    private int   historyIndex = -1;
    private float caretTimer   = 0f;
    private bool  caretVisible = true;

    private bool acceptInput = false;
    private bool gameOver    = false;
    private bool inDarkMode  = false;

    private float trace   = 0f;
    private float heat    = 0f;
    private int   favours = 3;
    private int   evidence = 0;

    private float heatTimer      = 0f;
    private const float HEAT_INTERVAL = 15f;
    private const float HEAT_PER_TICK = 2f;

    private int  act          = 1;   // 1, 2, or 3
    private bool proxyActive  = false;
    private int  atlasUseCount = 0;

    private bool hasDecryptKey = false;
    private bool echoPieceScan    = false;
    private bool echoPieceDecrypt = false;
    private bool echoPieceRead    = false;
    private bool echoPieceCrack   = false;
    private bool echoPieceBonus   = false;

    private string G(string s) => $"<color=#7EC850>{s}</color>"; // green
    private string D(string s) => $"<color=#3A3A3A>{s}</color>"; // gray
    private string W(string s) => $"<color=#D4903A>{s}</color>"; // yellow
    private string E(string s) => $"<color=#C0392B>{s}</color>"; // red
    private string I(string s) => $"<color=#4A9ECA>{s}</color>"; // blue
    private string C(string s) => $"<color=#CCCCCC>{s}</color>"; // light gray
    private string P(string s) => $"<color=#9B59B6>{s}</color>"; // purple
    void Awake()  => BuildUI();
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
        TickHeat();
    }
    
    //  UI BUILD
    void BuildUI()
    {
        if (Camera.main == null)
        {
            var cameraGO = new GameObject("Main Camera");
            var camera = cameraGO.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
        }
        else
        {
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

        var statusGO = new GameObject("StatusBar");
        statusGO.transform.SetParent(canvasGO.transform, false);
        statusBar = statusGO.AddComponent<TextMeshProUGUI>();
        statusBar.fontSize         = 12;
        statusBar.richText         = true;
        statusBar.alignment        = TextAlignmentOptions.TopLeft;
        statusBar.textWrappingMode = TextWrappingModes.NoWrap;
        var srt = statusGO.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(0, 1);
        srt.anchorMax = new Vector2(1, 1);
        srt.pivot     = new Vector2(0.5f, 1);
        srt.sizeDelta = new Vector2(0, 28);
        srt.offsetMin = new Vector2(20, 0);
        srt.offsetMax = new Vector2(-20, 0);
        srt.anchoredPosition = new Vector2(0, -4);

        var textGO = new GameObject("ScreenText");
        textGO.transform.SetParent(canvasGO.transform, false);
        screenText = textGO.AddComponent<TextMeshProUGUI>();
        screenText.fontSize         = 15;
        screenText.richText         = true;
        screenText.textWrappingMode = TextWrappingModes.Normal;
        screenText.alignment        = TextAlignmentOptions.TopLeft;
        screenText.overflowMode     = TextOverflowModes.Truncate;
        var rt = textGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(20, 20);
        rt.offsetMax = new Vector2(-20, -36);
    }

    //  REDRAW
    void Redraw()
    {
        if (screenText == null) return;

        string traceCol  = trace  < 40 ? "#7EC850" : trace  < 70 ? "#D4903A" : "#C0392B";
        string heatCol   = heat   < 40 ? "#7EC850" : heat   < 70 ? "#D4903A" : "#C0392B";
        string darkTag   = inDarkMode ? D(" [DARK]") : "";
        string proxyTag  = proxyActive ? I(" [PROXY]") : "";
        statusBar.text =
            $"<color={traceCol}>TRACE {trace:0}%</color>   " +
            $"<color={heatCol}>HEAT {heat:0}%</color>   " +
            I($"FAVOURS {favours}") + "   " +
            G($"EVIDENCE {evidence}/5") + "   " +
            D($"ACT {act}") +
            darkTag + proxyTag;

        var sb = new StringBuilder();
        int start = Mathf.Max(0, outputLines.Count - MAX_LINES);
        for (int i = start; i < outputLines.Count; i++)
        {
            sb.Append(outputLines[i]);
            sb.Append('\n');
        }

        if (!gameOver)
        {
            string caret = caretVisible ? "<color=#7EC850>\u2588</color>" : " ";
            sb.Append(G("SIGNAL@nexus:~$ "));
            sb.Append(C(inputBuffer.ToString()));
            sb.Append(caret);
        }

        screenText.text = sb.ToString();
    }

    void RedrawStatusOnly()
    {
        if (statusBar == null) return;
        string traceCol = trace < 40 ? "#7EC850" : trace < 70 ? "#D4903A" : "#C0392B";
        string heatCol  = heat  < 40 ? "#7EC850" : heat  < 70 ? "#D4903A" : "#C0392B";
        string darkTag  = inDarkMode ? D(" [DARK]") : "";
        string proxyTag = proxyActive ? I(" [PROXY]") : "";
        statusBar.text =
            $"<color={traceCol}>TRACE {trace:0}%</color>   " +
            $"<color={heatCol}>HEAT {heat:0}%</color>   " +
            I($"FAVOURS {favours}") + "   " +
            G($"EVIDENCE {evidence}/5") + "   " +
            D($"ACT {act}") +
            darkTag + proxyTag;
    }

    //  PASSIVE HEAT TICK
    void TickHeat()
    {
        if (gameOver || !acceptInput) return;
        heatTimer += Time.deltaTime;
        if (heatTimer < HEAT_INTERVAL) return;
        heatTimer = 0f;

        float gain = inDarkMode ? 0f : HEAT_PER_TICK * (act == 3 ? 1.5f : 1f);
        heat = Mathf.Min(100f, heat + gain);
        RedrawStatusOnly();

        if (heat >= 75f && !inDarkMode)
            PrintSilent(W(">> agents are closing in. heat is critical."));

        CheckLoss();
    }

    //  TRACE HELPER
    void AddTrace(float amount)
    {
        float actual = proxyActive ? amount * 0.5f : amount;
        proxyActive  = false;
        trace        = Mathf.Min(100f, trace + actual);
        RedrawStatusOnly();
        CheckLoss();
    }

    void ReduceTrace(float amount)
    {
        trace = Mathf.Max(0f, trace - amount);
        RedrawStatusOnly();
    }

    void ReduceHeat(float amount)
    {
        heat = Mathf.Max(0f, heat - amount);
        RedrawStatusOnly();
    }

    //  LOSS CHECK
    void CheckLoss()
    {
        if (gameOver) return;
        if (trace >= 100f) { gameOver = true; StartCoroutine(LossTraced()); }
        else if (heat >= 100f) { gameOver = true; StartCoroutine(LossBurned()); }
    }

    //  OUTPUT HELPERS
    void Print(string line)
    {
        outputLines.Add(line);
        Redraw();
    }

    void PrintSilent(string line)
    {
        outputLines.Add(line);
        Redraw();
    }

    void Blank() => Print("");

    IEnumerator TypeLine(string rich, float delay = 0.018f)
    {
        string plain = Regex.Replace(rich, "<.*?>", "");
        for (int i = 0; i < plain.Length; i++)
            yield return new WaitForSeconds(delay);
        Print(rich);
    }

    IEnumerator TypeSlow(string rich) => TypeLine(rich, 0.04f);

    //  CARET
    void BlinkCaret()
    {
        caretTimer += Time.deltaTime;
        if (caretTimer < 0.5f) return;
        caretTimer   = 0f;
        caretVisible = !caretVisible;
        Redraw();
    }

    //  KEYBOARD
    void OnCharTyped(char ch)
    {
        if (!acceptInput || gameOver) return;
        if (ch >= 32 && ch <= 126 && inputBuffer.Length < 120)
        {
            inputBuffer.Append(ch);
            caretVisible = true; caretTimer = 0f;
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
            inputBuffer.Clear(); historyIndex = -1; Redraw();
            if (cmd.Length == 0) return;
            history.Add(cmd);
            acceptInput = false;
            Print(D("SIGNAL@nexus:~$ ") + C(cmd));
            StartCoroutine(ProcessCommand(cmd));
            return;
        }
        if (kb.backspaceKey.wasPressedThisFrame && acceptInput && !gameOver)
        {
            if (inputBuffer.Length > 0) { inputBuffer.Remove(inputBuffer.Length - 1, 1); Redraw(); }
            return;
        }
        if (kb.upArrowKey.wasPressedThisFrame && acceptInput && history.Count > 0)
        {
            historyIndex = Mathf.Clamp(historyIndex + 1, 0, history.Count - 1);
            inputBuffer.Clear();
            inputBuffer.Append(history[history.Count - 1 - historyIndex]);
            Redraw(); return;
        }
        if (kb.downArrowKey.wasPressedThisFrame && acceptInput)
        {
            historyIndex = Mathf.Max(-1, historyIndex - 1);
            inputBuffer.Clear();
            if (historyIndex >= 0) inputBuffer.Append(history[history.Count - 1 - historyIndex]);
            Redraw();
        }
    }

    //  BOOT
    IEnumerator Boot()
    {
        yield return TypeLine(D("// NEXUS SECURE SHELL v4.1"), 0.03f);
        yield return TypeLine(D("// encrypted tunnel established"), 0.03f);
        yield return TypeLine(D("// location spoofed — identity masked"), 0.03f);
        yield return new WaitForSeconds(0.4f);
        Blank();
        yield return TypeSlow(G("welcome back, SIGNAL."));
        yield return TypeSlow(W("OPERATION ECHO is real. we need to expose it."));
        Blank();
        yield return TypeLine(D("contacts online: ") + G("GHOST") + D(", ") + W("MAVEN") + D(", ") + E("ATLAS"));
        yield return TypeLine(D("starting favours: ") + I("3") + D("  |  type ") + C("help") + D(" to begin."));
        Blank();
        yield return StartActBriefing(1);
        acceptInput = true;
    }

    IEnumerator StartActBriefing(int actNum)
    {
        act = actNum;
        switch (actNum)
        {
            case 1:
                yield return TypeLine(I("-- ACT 1: THE INTERCEPT --"));
                yield return TypeLine(C("objectives: scan GOV_NODE_07, read the ECHO fragment, decrypt one payload."));
                yield return TypeLine(D("risk level: low. learn the tools. stay quiet."));
                break;
            case 2:
                yield return TypeLine(I("-- ACT 2: INSIDE THE ARCHIVE --"));
                yield return TypeLine(C("objectives: access the deep archive, find 2 more evidence pieces, stay under 60% trace."));
                yield return TypeLine(W("risk level: elevated. agents are watching node clusters."));
                break;
            case 3:
                yield return TypeLine(I("-- ACT 3: THE UPLOAD --"));
                yield return TypeLine(C("objectives: gather final evidence and leak everything. this is the endgame."));
                yield return TypeLine(E("risk level: critical. active countermeasures. heat escalates faster."));
                break;
        }
        Blank();
    }

    //  COMMAND ROUTER
    IEnumerator ProcessCommand(string cmd)
    {
        // parse "call [contact] [action]"
        if (cmd.StartsWith("call "))
        {
            string[] parts = cmd.Split(' ');
            if (parts.Length >= 3)
                yield return CallContact(parts[1], string.Join(" ", parts, 2, parts.Length - 2));
            else
                yield return TypeLine(E("usage: call [ghost|maven|atlas] [action]"));
        }
        else switch (cmd)
        {
            case "help":         yield return CmdHelp();        break;
            case "scan":         yield return CmdScan();        break;
            case "ls":           yield return CmdLs();          break;
            case "decrypt":      yield return CmdDecrypt();     break;
            case "read 2":       yield return CmdRead();        break;
            case "crack":        yield return CmdCrack();       break;
            case "ping":         yield return CmdPing();        break;
            case "wait":         yield return CmdWait();        break;
            case "status":       yield return CmdStatus();      break;
            case "contacts":     yield return CmdContacts();    break;
            case "leak":         yield return CmdLeak();        break;
            case "clear":        outputLines.Clear(); Redraw(); break;
            default:
                yield return TypeLine(E("command not found: ") + C(cmd));
                yield return TypeLine(D("type ") + C("help") + D(" for available commands."));
                break;
        }

        Blank();
        if (!gameOver) acceptInput = true;
    }

    //  COMMANDS
    IEnumerator CmdHelp()
    {
        yield return TypeLine(I("-- available commands --"));
        yield return TypeLine(C("  scan          ") + D("scan government node cluster"));
        yield return TypeLine(C("  ping          ") + D("low-risk recon (+2% trace)"));
        yield return TypeLine(C("  ls            ") + D("list intercepted files"));
        yield return TypeLine(C("  decrypt       ") + D("decrypt a payload (+15% trace)"));
        yield return TypeLine(C("  crack         ") + D("force-decrypt locked file (+25% trace)"));
        yield return TypeLine(C("  read 2        ") + D("read plaintext intercept"));
        yield return TypeLine(C("  wait          ") + D("pause — trace decays, heat ticks slowly"));
        yield return TypeLine(C("  status        ") + D("detailed stat readout"));
        yield return TypeLine(C("  contacts      ") + D("show available contacts and actions"));
        yield return TypeLine(C("  call [c] [a]  ") + D("call a contact (costs 1-2 favours)"));
        yield return TypeLine(C("  leak          ") + D("publish evidence (win condition)"));
        yield return TypeLine(C("  clear         ") + D("clear the screen"));
    }

    IEnumerator CmdScan()
    {
        yield return TypeLine(D("scanning node cluster..."));
        yield return new WaitForSeconds(0.5f);
        AddTrace(8f);
        yield return TypeLine(G("GOV_NODE_03  [ACTIVE]   surveillance.hub"));
        yield return TypeLine(W("GOV_NODE_07  [ACTIVE]   data.archive  [3 files]"));
        yield return TypeLine(act >= 2 ? W("GOV_NODE_12  [ACTIVE]   deep.archive  [LOCKED]") : D("GOV_NODE_12  [OFFLINE]  --"));
        yield return TypeLine(E(">> trace +8%"));

        if (!echoPieceScan && act >= 1)
        {
            echoPieceScan = true;
            evidence++;
            favours++;
            yield return TypeLine(G(">> new evidence: node map confirmed. +1 evidence, +1 favour (clean recon)"));
        }
        CheckActProgress();
    }

    IEnumerator CmdLs()
    {
        yield return TypeLine(I("intercepted files on GOV_NODE_07:"));
        yield return TypeLine(D("  [0] payload_0041.enc    128-bit  ") + E("LOCKED"));
        yield return TypeLine(D("  [1] payload_0047.enc    128-bit  ") + (hasDecryptKey ? G("KEY AVAILABLE") : E("LOCKED")));
        yield return TypeLine(D("  [2] ECHO_LIST_frag.txt  plaintext ") + G("readable"));
        if (act >= 2)
        {
            yield return TypeLine(D("deep.archive files (GOV_NODE_12):"));
            yield return TypeLine(D("  [3] ECHO_ORDERS.enc     256-bit  ") + E("LOCKED"));
            yield return TypeLine(D("  [4] ECHO_FINANCIALS.enc 256-bit  ") + E("LOCKED"));
        }
    }

    IEnumerator CmdDecrypt()
    {
        if (!hasDecryptKey)
        {
            yield return TypeLine(E("no decrypt key available."));
            yield return TypeLine(D("use ") + C("crack") + D(" (high trace) or ") + C("call maven intel") + D(" to find a key."));
            yield break;
        }
        yield return TypeLine(D("target: payload_0047.enc"));
        yield return TypeLine(D("applying key..."));
        yield return new WaitForSeconds(0.6f);
        yield return TypeLine(G("decrypted. contents: OPERATION ECHO -- pre-arrest orders for 2,849 citizens."));
        AddTrace(15f);
        yield return TypeLine(E(">> trace +15%"));

        if (!echoPieceDecrypt)
        {
            echoPieceDecrypt = true;
            evidence++;
            hasDecryptKey = false;
            yield return TypeLine(G(">> new evidence: pre-arrest orders confirmed. +1 evidence"));
            if (trace < 40f) { favours++; yield return TypeLine(I(">> stealth bonus: trace under 40%. +1 favour")); }
        }
        CheckActProgress();
    }

    IEnumerator CmdRead()
    {
        yield return TypeLine(D("// ECHO_LIST_frag.txt ----------------"));
        yield return TypeLine(C("SUBJECT 4471: Mara Voss   -- ") + E("flagged [HIGH]"));
        yield return TypeLine(C("SUBJECT 4472: Jin Park    -- ") + W("flagged [MED]"));
        yield return TypeLine(C("SUBJECT 4473: Yusuf Ademi -- ") + W("flagged [MED]"));
        yield return TypeLine(D("... [2,847 more entries redacted]"));
        AddTrace(5f);
        yield return TypeLine(E(">> trace +5%"));

        if (!echoPieceRead)
        {
            echoPieceRead = true;
            evidence++;
            yield return TypeLine(G(">> new evidence: citizen flag list confirmed. +1 evidence"));
        }
        CheckActProgress();
    }

    IEnumerator CmdCrack()
    {
        if (act < 2)
        {
            yield return TypeLine(E("no high-security files accessible in act 1."));
            yield break;
        }
        yield return TypeLine(W("force-decrypting ECHO_ORDERS.enc... this will be loud."));
        yield return new WaitForSeconds(0.8f);
        AddTrace(25f);
        yield return TypeLine(E(">> trace +25%"));

        if (!echoPieceCrack)
        {
            echoPieceCrack = true;
            evidence++;
            yield return TypeLine(G("ECHO_ORDERS: field operatives authorised for use of force."));
            yield return TypeLine(G(">> new evidence: force authorisation document. +1 evidence"));
        }
        else
        {
            yield return TypeLine(D("ECHO_ORDERS already in evidence cache."));
        }
        CheckActProgress();
    }

    IEnumerator CmdPing()
    {
        yield return TypeLine(D("pinging node cluster..."));
        yield return new WaitForSeconds(0.3f);
        AddTrace(2f);
        yield return TypeLine(G("GOV_NODE_07: 3 files  [2 locked, 1 readable]"));
        if (act >= 2) yield return TypeLine(W("GOV_NODE_12: 2 files  [both 256-bit locked]  -- heavy guard"));
        yield return TypeLine(E(">> trace +2%"));
    }

    IEnumerator CmdWait()
    {
        if (inDarkMode) { yield return TypeLine(D("already in dark mode.")); yield break; }
        yield return TypeLine(D("holding position..."));
        for (int i = 0; i < 3; i++)
        {
            yield return new WaitForSeconds(2f);
            ReduceTrace(3f);
            heat = Mathf.Min(100f, heat + 0.5f);
            RedrawStatusOnly();
            yield return TypeLine(D($"  trace decaying... {trace:0}%"));
        }
        yield return TypeLine(G("trace reduced. heat ticked slightly."));
    }

    IEnumerator CmdStatus()
    {
        string tc = trace < 40 ? "#7EC850" : trace < 70 ? "#D4903A" : "#C0392B";
        string hc = heat  < 40 ? "#7EC850" : heat  < 70 ? "#D4903A" : "#C0392B";
        yield return TypeLine(I("-- current status --"));
        yield return TypeLine(C("trace    : ") + $"<color={tc}>{trace:0}%</color>");
        yield return TypeLine(C("heat     : ") + $"<color={hc}>{heat:0}%</color>");
        yield return TypeLine(C("favours  : ") + I($"{favours}"));
        yield return TypeLine(C("evidence : ") + G($"{evidence}/5"));
        yield return TypeLine(C("act      : ") + D($"{act}"));
        yield return TypeLine(C("proxy    : ") + (proxyActive ? I("active (next action halved)") : D("inactive")));
        yield return TypeLine(C("key      : ") + (hasDecryptKey ? G("available") : D("none")));
        yield return TypeLine(D("leak requires 3+ evidence and act 3."));
    }

    IEnumerator CmdContacts()
    {
        yield return TypeLine(I("-- contacts --"));
        yield return TypeLine(G("GHOST") + D(" [1 favour]  the fixer."));
        yield return TypeLine(D("  call ghost spoof   ") + C("-- wipe 30% trace"));
        yield return TypeLine(D("  call ghost reroute ") + C("-- halve trace on next action"));
        yield return TypeLine(D("  call ghost dark    ") + C("-- go silent, heat drops over time"));
        Blank();
        yield return TypeLine(W("MAVEN") + D(" [1 favour]  the informant."));
        yield return TypeLine(D("  call maven scan    ") + C("-- scan node with zero trace"));
        yield return TypeLine(D("  call maven intel   ") + C("-- locate decrypt key"));
        yield return TypeLine(D("  call maven warn    ") + C("-- check real heat level"));
        Blank();
        yield return TypeLine(E("ATLAS") + D(" [2 favours]  the backer. use carefully."));
        yield return TypeLine(D("  call atlas publish ") + C("-- partial leak for pressure (no win)"));
        yield return TypeLine(D("  call atlas funds   ") + C("-- emergency +2 favours"));
        yield return TypeLine(D("  call atlas extract ") + C("-- emergency heat reduction"));
        yield return TypeLine(W("warning: using ATLAS 3+ times changes your ending."));
    }

    //  CONTACT SYSTEM
    IEnumerator CallContact(string contact, string action)
    {
        int cost = contact == "atlas" ? 2 : 1;
        if (favours < cost)
        {
            yield return TypeLine(E($"not enough favours. need {cost}, have {favours}."));
            yield break;
        }

        switch (contact)
        {
            case "ghost": yield return CallGhost(action); break;
            case "maven": yield return CallMaven(action); break;
            case "atlas": yield return CallAtlas(action); break;
            default:
                yield return TypeLine(E($"unknown contact: {contact}"));
                yield return TypeLine(D("known contacts: ghost, maven, atlas"));
                break;
        }
    }

    IEnumerator CallGhost(string action)
    {
        favours -= 1;
        yield return TypeLine(G("GHOST: ") + D("..."));
        yield return new WaitForSeconds(0.4f);
        switch (action)
        {
            case "spoof":
                ReduceTrace(30f);
                yield return TypeLine(G("GHOST: ") + C("signal rerouted. you're clean."));
                yield return TypeLine(G(">> trace -30%"));
                break;
            case "reroute":
                proxyActive = true;
                yield return TypeLine(G("GHOST: ") + C("proxy active. next action costs half trace."));
                break;
            case "dark":
                yield return StartCoroutine(GoDark());
                break;
            default:
                favours += 1;
                yield return TypeLine(E("GHOST: ") + D($"unknown request: {action}"));
                yield return TypeLine(D("options: spoof, reroute, dark"));
                break;
        }
    }

    IEnumerator GoDark()
    {
        inDarkMode = true;
        yield return TypeLine(G("GHOST: ") + C("going dark. don't touch anything."));
        RedrawStatusOnly();
        for (int i = 0; i < 4; i++)
        {
            yield return new WaitForSeconds(5f);
            ReduceHeat(5f);
            yield return TypeLine(D($"  [dark] heat decaying... {heat:0}%"));
        }
        inDarkMode = false;
        yield return TypeLine(G("GHOST: ") + C("you're back online. stay careful."));
        RedrawStatusOnly();
    }

    IEnumerator CallMaven(string action)
    {
        favours -= 1;
        yield return TypeLine(W("MAVEN: ") + D("..."));
        yield return new WaitForSeconds(0.5f);
        switch (action)
        {
            case "scan":
                yield return TypeLine(W("MAVEN: ") + C("piggybacking on a maintenance ping."));
                yield return TypeLine(G("GOV_NODE_07: 3 files  [2 locked, 1 readable]"));
                if (act >= 2) yield return TypeLine(W("GOV_NODE_12: 2 files  [both 256-bit, heavy traffic]"));
                yield return TypeLine(W("MAVEN: ") + D("zero trace. you're welcome."));
                break;
            case "intel":
                hasDecryptKey = true;
                yield return TypeLine(W("MAVEN: ") + C("found a contractor's key. sending now."));
                yield return TypeLine(G(">> decrypt key obtained. use [decrypt] on payload_0047.enc"));
                break;
            case "warn":
                string heatStatus = heat < 30 ? "cold. no active search." : heat < 60 ? "warm. patrols elevated." : "hot. agents actively searching.";
                yield return TypeLine(W("MAVEN: ") + C($"heat reading: {heatStatus}"));
                yield return TypeLine(W($"MAVEN: ") + D($"actual heat: {heat:0}%. act fast."));
                break;
            default:
                favours += 1;
                yield return TypeLine(E("MAVEN: ") + D($"don't know that one: {action}"));
                yield return TypeLine(D("options: scan, intel, warn"));
                break;
        }
    }

    IEnumerator CallAtlas(string action)
    {
        favours -= 2;
        atlasUseCount++;
        yield return TypeLine(E("ATLAS: ") + D("..."));
        yield return new WaitForSeconds(0.6f);

        if (atlasUseCount >= 3)
            yield return TypeLine(E("ATLAS: ") + W("you're becoming dependent, SIGNAL. we'll talk after this."));

        switch (action)
        {
            case "publish":
                yield return TypeLine(E("ATLAS: ") + C("pushing partial data to three news networks."));
                yield return TypeLine(W(">> partial pressure applied. no win condition met, but it buys you time."));
                ReduceHeat(15f);
                yield return TypeLine(E(">> heat reduced by 15% (authorities diverted)."));
                break;
            case "funds":
                favours += 2;
                yield return TypeLine(E("ATLAS: ") + C("transferring resources. don't waste them."));
                yield return TypeLine(I(">> +2 favours"));
                break;
            case "extract":
                ReduceHeat(40f);
                ReduceTrace(20f);
                yield return TypeLine(E("ATLAS: ") + C("misdirection package deployed. you have a window."));
                yield return TypeLine(E(">> heat -40%, trace -20%. use it now."));
                break;
            default:
                favours += 2; atlasUseCount--;
                yield return TypeLine(E("ATLAS: ") + D($"unrecognised request: {action}"));
                yield return TypeLine(D("options: publish, funds, extract"));
                break;
        }
    }

    //  ACT PROGRESSION
    void CheckActProgress()
    {
        if (act == 1 && echoPieceScan && echoPieceDecrypt && echoPieceRead)
            StartCoroutine(AdvanceAct(2));
        else if (act == 2 && echoPieceCrack && evidence >= 4)
            StartCoroutine(AdvanceAct(3));
    }

    IEnumerator AdvanceAct(int nextAct)
    {
        yield return new WaitForSeconds(0.5f);
        Blank();
        yield return TypeLine(G($">> act {nextAct - 1} complete. +2 favours."));
        favours += 2;
        ReduceTrace(20f);
        yield return TypeLine(D(">> trace reduced by 20% between acts."));
        Blank();
        yield return StartActBriefing(nextAct);
    }

    //  LEAK (WIN)
    IEnumerator CmdLeak()
    {
        if (act < 3)
        {
            yield return TypeLine(E("not ready to leak yet. complete act 2 first."));
            yield break;
        }
        if (evidence < 3)
        {
            yield return TypeLine(E($"not enough evidence. need 3, have {evidence}."));
            yield break;
        }

        acceptInput = false;
        gameOver    = true;
        Blank();

        // Choose ending
        if (atlasUseCount >= 3 && evidence >= 4)
            yield return EndingAtlasTakeover();
        else if (evidence >= 5)
            yield return EndingFullExposure();
        else if (evidence >= 3)
            yield return EndingPartialTruth();
    }

    IEnumerator EndingFullExposure()
    {
        yield return TypeLine(W("uploading full package to 14 secure drop points..."));
        yield return new WaitForSeconds(1f);
        yield return TypeLine(G("████████████████  transmitted."));
        Blank();
        yield return TypeSlow(G("OPERATION ECHO is public."));
        yield return TypeSlow(C("all 2,849 citizens removed from the flag list."));
        yield return TypeSlow(C("three officials arrested within 48 hours."));
        yield return TypeSlow(G("SIGNAL was never found."));
        Blank();
        yield return TypeLine(D("-- ENDING: FULL EXPOSURE --"));
        yield return TypeLine(D("the system was broken. you broke back."));
    }

    IEnumerator EndingPartialTruth()
    {
        yield return TypeLine(W("uploading partial package..."));
        yield return new WaitForSeconds(0.8f);
        yield return TypeLine(G("████████████░░░░  transmitted (partial)."));
        Blank();
        yield return TypeSlow(W("some of OPERATION ECHO is now public."));
        yield return TypeSlow(C("partial scandal. one official resigns."));
        yield return TypeSlow(C("the full list remains buried — for now."));
        yield return TypeSlow(W("SIGNAL disappears. the work is unfinished."));
        Blank();
        yield return TypeLine(D("-- ENDING: PARTIAL TRUTH --"));
        yield return TypeLine(D("enough to matter. not enough to end it."));
    }

    IEnumerator EndingAtlasTakeover()
    {
        yield return TypeLine(E("ATLAS: ") + C("we'll take it from here, SIGNAL."));
        yield return new WaitForSeconds(0.6f);
        yield return TypeLine(W("uploading to ATLAS distribution network..."));
        yield return new WaitForSeconds(0.8f);
        yield return TypeLine(G("████████████████  transmitted."));
        Blank();
        yield return TypeSlow(E("OPERATION ECHO is public — on ATLAS's terms."));
        yield return TypeSlow(C("the evidence is real. the narrative is theirs."));
        yield return TypeSlow(W("three officials fall. ATLAS gains leverage over two more."));
        yield return TypeSlow(E("you were a tool. a useful one."));
        Blank();
        yield return TypeLine(D("-- ENDING: ATLAS TAKEOVER --"));
        yield return TypeLine(D("the system was broken. someone else fixed it for their own gain."));
    }

    //  LOSS SEQUENCES
    IEnumerator LossTraced()
    {
        Blank();
        yield return TypeLine(E("!! TRACE COMPLETE -- LOCATION EXPOSED"));
        yield return TypeLine(E("GOV_NODE_03 initiating counter-intrusion..."));
        yield return new WaitForSeconds(0.5f);
        yield return TypeLine(E("connection terminated."));
        yield return TypeLine(D("evidence cache wiped."));
        yield return new WaitForSeconds(2f);
        yield return RestartGame("-- LOSS: TRACED --", "they found you. start over.");
    }

    IEnumerator LossBurned()
    {
        Blank();
        yield return TypeLine(E("!! HEAT CRITICAL -- AGENTS ON SITE"));
        yield return TypeLine(E("physical breach imminent. no time to reboot."));
        yield return new WaitForSeconds(0.8f);
        yield return TypeLine(E("connection lost."));
        yield return new WaitForSeconds(2f);
        yield return RestartGame("-- LOSS: BURNED --", "they got there before you could finish.");
    }

    IEnumerator RestartGame(string title, string subtitle)
    {
        yield return TypeLine(D(title));
        yield return TypeLine(D(subtitle));
        yield return new WaitForSeconds(2f);

        outputLines.Clear();
        trace = 0f; heat = 0f; favours = 3; evidence = 0; act = 1;
        proxyActive = false; inDarkMode = false; atlasUseCount = 0;
        hasDecryptKey = false;
        echoPieceScan = echoPieceDecrypt = echoPieceRead = echoPieceCrack = echoPieceBonus = false;
        heatTimer = 0f;
        gameOver = false;

        yield return TypeLine(D("// rebooting secure shell..."));
        yield return new WaitForSeconds(0.5f);
        yield return TypeLine(G("reconnected. the evidence is still out there."));
        Blank();
        yield return StartActBriefing(1);
        acceptInput = true;
    }
}