using System.Collections;
using UnityEngine;

/// <summary>
/// NodeSystem.cs — Network navigation, sweeps, and passive ticks.
/// Handles: scan, connect, disconnect, sever, ls, wait, heat tick, sweep events.
/// </summary>
public class NodeSystem : MonoBehaviour
{
    [HideInInspector] public GameState State;
    [HideInInspector] public Terminal  Term;
    [HideInInspector] public EndingSystem Endings;

    // =================================================================
    //  PASSIVE UPDATE — called from HackerTerminal.Update()
    // =================================================================
    public void Tick()
    {
        if (State.GameOver || !State.AcceptInput) return;

        // Heat tick
        State.HeatTimer += Time.deltaTime;
        if (State.HeatTimer >= GameState.HEAT_TICK_INTERVAL)
        {
            State.HeatTimer = 0f;
            if (!State.InDarkMode)
            {
                float gain = State.Heat > 60f
                    ? GameState.HEAT_GAIN_BASE * 1.5f
                    : GameState.HEAT_GAIN_BASE;
                State.Heat = Mathf.Min(100f, State.Heat + gain);
                Term.UpdateStatusBar();
                if (State.Heat >= 70f && State.Heat < 72f)
                    Term.Print(Term.W(">> agents active. heat is elevated."));
            }
        }

        // Sweep scheduling
        if (!State.SweepActive && State.ActiveNode.Length > 0 && State.Heat > 50f && !State.InDarkMode)
        {
            State.SweepTimer += Time.deltaTime;
            float interval = State.Heat > 75f
                ? GameState.SWEEP_INTERVAL_HIGH
                : GameState.SWEEP_INTERVAL_LOW;
            if (State.SweepTimer >= interval)
            {
                State.SweepTimer = 0f;
                StartCoroutine(RunSweep());
            }
        }

        CheckLoss();
    }

    // =================================================================
    //  SWEEP EVENT
    // =================================================================
    IEnumerator RunSweep()
    {
        State.SweepActive = true;
        Term.UpdateStatusBar();
        string node = State.ActiveNode;
        Term.Print(Term.E($"!! AGENT SWEEP — {node.ToUpper()} — type [sever] in 8 seconds"));

        float countdown = 8f;
        while (countdown > 0f && State.SweepActive)
        {
            yield return new WaitForSeconds(1f);
            countdown -= 1f;
            if (State.SweepActive && countdown > 0)
                Term.Print(Term.D($"   {countdown:0}..."));
        }

        if (State.SweepActive)
        {
            State.SweepActive = false;
            State.ActiveNode  = "";
            AddTrace(25f);
            State.Heat = Mathf.Min(100f, State.Heat + 15f);
            Term.UpdateStatusBar();
            Term.Print(Term.E("!! sweep complete — signal traced. connection terminated."));
            Term.Print(Term.E(">> trace +25%  heat +15%"));
            Term.Redraw();
        }
    }

    // =================================================================
    //  COMMANDS
    // =================================================================
    public IEnumerator Scan()
    {
        yield return Term.TypeLine(Term.D("scanning network..."));
        yield return new WaitForSeconds(0.5f);
        AddTrace(4f);
        Term.Blank();
        Term.Print(Term.I("// network map:"));
        Term.Print(Term.NodeMap());
        Term.Blank();
        yield return Term.TypeLine(Term.G("HUB_03     ") + Term.C("surveillance hub      ") + Term.G("[open]"));
        yield return Term.TypeLine(Term.G("ARCHIVE_07 ") + Term.C("data archive          ") + Term.G("[open]"));
        yield return Term.TypeLine(
            (State.Deep12Locked ? Term.W("DEEP_12    ") : Term.G("DEEP_12    ")) +
            Term.C("deep archive          ") +
            (State.Deep12Locked ? Term.E("[locked — 4-digit bruteforce]") : Term.G("[open]")));
        yield return Term.TypeLine(
            (State.Core19Locked ? Term.W("CORE_19    ") : Term.G("CORE_19    ")) +
            Term.C("command core          ") +
            (State.Core19Locked ? Term.E("[locked — decrypt key required]") : Term.G("[open]")));
        Term.Blank();
        Term.Print(Term.TraceBar());
        yield return Term.TypeLine(Term.E(">> trace +4%"));
    }

    public IEnumerator Connect(string node)
    {
        string n = Normalise(node);
        if (n == "deep12"  && State.Deep12Locked)
        { yield return Term.TypeLine(Term.E("DEEP_12 locked. use [probe deep12 ####] to crack access.")); yield break; }
        if (n == "core19"  && State.Core19Locked)
        { yield return Term.TypeLine(Term.E("CORE_19 locked. a decrypt key is required.")); yield break; }
        if (n != "hub03" && n != "archive07" && n != "deep12" && n != "core19")
        { yield return Term.TypeLine(Term.E($"unknown node: {node}  |  known: hub03, archive07, deep12, core19")); yield break; }

        if (State.ActiveNode.Length > 0)
            yield return Term.TypeLine(Term.D($"// disconnecting from {State.ActiveNode.ToUpper()}..."));

        State.ActiveNode = n;
        float cost = (n == "deep12" || n == "core19") ? 6f : 3f;
        AddTrace(cost);
        yield return Term.TypeLine(Term.G($"connected to {n.ToUpper()}.") + Term.D($"  trace +{cost:0}%"));
        yield return Term.TypeLine(Term.D("type [ls] to list files, [disconnect] or [sever] to leave."));
    }

    public IEnumerator Disconnect()
    {
        if (State.ActiveNode.Length == 0)
        { yield return Term.TypeLine(Term.D("not connected to any node.")); yield break; }
        string was = State.ActiveNode;
        State.ActiveNode = "";
        yield return Term.TypeLine(Term.D($"disconnecting from {was.ToUpper()}..."));
        yield return Term.TypeLine(Term.G("disconnected safely."));
    }

    public IEnumerator Sever()
    {
        if (State.SweepActive)
        {
            State.SweepActive = false;
            string was = State.ActiveNode;
            State.ActiveNode = "";
            State.SweepTimer = 0f;
            Term.UpdateStatusBar();
            yield return Term.TypeLine(Term.G("connection severed."));
            yield return Term.TypeLine(Term.D($"// dropped from {was.ToUpper()}. sweep missed you."));
            yield return Term.TypeLine(Term.D("// type [connect node] to reconnect."));
        }
        else if (State.ActiveNode.Length > 0)
        {
            State.ActiveNode = "";
            yield return Term.TypeLine(Term.W("severed (no sweep active). use [disconnect] for a clean exit next time."));
        }
        else
        {
            yield return Term.TypeLine(Term.D("not connected to any node."));
        }
    }

    public IEnumerator Ls()
    {
        if (State.ActiveNode.Length == 0)
        { yield return Term.TypeLine(Term.E("not connected. use [connect node].")); yield break; }

        switch (State.ActiveNode)
        {
            case "hub03":
                yield return Term.TypeLine(Term.I("// files on HUB_03:"));
                yield return Term.TypeLine(Term.C("  staff_chat   ") + Term.G("[readable]"));
                yield return Term.TypeLine(Term.C("  access_log   ") + Term.G("[readable]"));
                yield return Term.TypeLine(Term.D("  sys_audit    ") + Term.D("[corrupted — unreadable]"));
                break;
            case "archive07":
                yield return Term.TypeLine(Term.I("// files on ARCHIVE_07:"));
                yield return Term.TypeLine(Term.C("  echo_list    ") + Term.G("[readable]"));
                yield return Term.TypeLine(Term.C("  payload_047  ") + Term.W("[encrypted — caesar cipher]"));
                yield return Term.TypeLine(Term.C("  memo_draft   ") + Term.G("[readable]"));
                yield return Term.TypeLine(Term.C("  budget       ") + Term.G("[readable]"));
                break;
            case "deep12":
                yield return Term.TypeLine(Term.I("// files on DEEP_12:"));
                yield return Term.TypeLine(Term.C("  echo_orders  ") + Term.W("[encrypted — caesar cipher]"));
                yield return Term.TypeLine(Term.C("  directive    ") + Term.G("[readable]"));
                yield return Term.TypeLine(Term.C("  core_key     ") + (State.HasCore19Key ? Term.G("[obtained]") : Term.I("[obtainable]")));
                break;
            case "core19":
                yield return Term.TypeLine(Term.I("// files on CORE_19:"));
                yield return Term.TypeLine(Term.C("  echo_master  ") + Term.W("[encrypted — caesar, high security]"));
                yield return Term.TypeLine(Term.C("  director_log ") + Term.G("[readable]"));
                yield return Term.TypeLine(Term.C("  upload_point ") + Term.I("[leak target — use 'leak' command]"));
                break;
        }
        AddTrace(2f);
        yield return Term.TypeLine(Term.E(">> trace +2%"));
    }

    public IEnumerator Wait()
    {
        if (State.ActiveNode.Length > 0)
        { yield return Term.TypeLine(Term.W("disconnect before waiting — idling on a live node raises heat.")); yield break; }
        yield return Term.TypeLine(Term.D("holding position..."));
        for (int i = 0; i < 3; i++)
        {
            yield return new WaitForSeconds(2.5f);
            State.Trace = Mathf.Max(0, State.Trace - 3f);
            State.Heat  = Mathf.Min(100f, State.Heat + 0.3f);
            Term.UpdateStatusBar();
        }
        yield return Term.TypeLine(Term.G(">> trace decayed ~9%."));
        Term.Print(Term.TraceBar());
    }

    // =================================================================
    //  HELPERS
    // =================================================================
    public void AddTrace(float amount)
    {
        float actual = State.ProxyActive ? amount * 0.5f : amount;
        State.ProxyActive = false;
        State.Trace = Mathf.Min(100f, State.Trace + actual);
        Term.UpdateStatusBar();
        CheckLoss();
    }

    public void ReduceTrace(float v) { State.Trace = Mathf.Max(0, State.Trace - v); Term.UpdateStatusBar(); }
    public void ReduceHeat(float v)  { State.Heat  = Mathf.Max(0, State.Heat  - v); Term.UpdateStatusBar(); }

    void CheckLoss()
    {
        if (State.GameOver) return;
        if (State.Trace >= 100f) { State.GameOver = true; StartCoroutine(Endings.LossTraced()); }
        else if (State.Heat >= 100f) { State.GameOver = true; StartCoroutine(Endings.LossBurned()); }
    }

    public static string Normalise(string s) =>
        s.ToLower().Replace("_","").Replace("-","").Replace(" ","");
}
