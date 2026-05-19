using System.Collections;
using UnityEngine;

/// <summary>
/// CommandRouter.cs — Parses typed commands and routes to the right system.
/// No game logic here. Adding a new command = one new case + one method elsewhere.
/// </summary>
public class CommandRouter : MonoBehaviour
{
    [HideInInspector] public GameState      State;
    [HideInInspector] public Terminal       Term;
    [HideInInspector] public NodeSystem     Nodes;
    [HideInInspector] public FileSystem     Files;
    [HideInInspector] public PuzzleSystem   Puzzles;
    [HideInInspector] public ContactSystem  Contacts;
    [HideInInspector] public MissionSystem  Missions;
    [HideInInspector] public EndingSystem   Endings;

    public IEnumerator Route(string cmd)
    {
        // Priority commands — always work regardless of state
        if (cmd == "sever")   { yield return Nodes.Sever();   }
        else if (cmd == "confirm") { yield return Puzzles.Confirm(); }

        // Call contact: "call ghost spoof" etc
        else if (cmd.StartsWith("call "))
        {
            string[] p = cmd.Split(' ');
            if (p.Length >= 3)
                yield return Contacts.Call(p[1], string.Join(" ", p, 2, p.Length - 2));
            else
                yield return Term.TypeLine(Term.E("usage: call [ghost|maven|atlas] [action]"));
        }

        // Parameterised commands
        else if (cmd.StartsWith("connect "))
            yield return Nodes.Connect(cmd.Substring(8).Trim());
        else if (cmd.StartsWith("probe "))
            yield return Puzzles.Probe(cmd.Substring(6).Trim());
        else if (cmd.StartsWith("decrypter "))
            yield return Puzzles.Decrypter(cmd.Substring(10).Trim());
        else if (cmd.StartsWith("analyze "))
            yield return Files.Analyze(cmd.Substring(8).Trim());
        else if (cmd.StartsWith("read "))
            yield return Files.Read(cmd.Substring(5).Trim());

        // Simple commands
        else switch (cmd)
        {
            case "help":       yield return ShowHelp();          break;
            case "scan":       yield return Nodes.Scan();        break;
            case "ls":         yield return Nodes.Ls();          break;
            case "disconnect": yield return Nodes.Disconnect();  break;
            case "wait":       yield return Nodes.Wait();        break;
            case "status":     yield return ShowStatus();        break;
            case "contacts":   yield return Contacts.ShowAll();  break;
            case "missions":   yield return Missions.ShowAll();  break;
            case "map":
                Term.Print(Term.I("// network map:"));
                Term.Print(Term.NodeMap());
                break;
            case "leak":       yield return Endings.Leak();      break;
            case "decrypter":  yield return Puzzles.Decrypter(""); break;
            case "clear":      Term.ClearScreen();               break;
            default:
                yield return Term.TypeLine(Term.E("command not found: ") + Term.C(cmd));
                yield return Term.TypeLine(Term.D("type ") + Term.C("help") + Term.D(" for available commands."));
                break;
        }

        Term.Blank();
        Missions.CheckObjectives();
        if (!State.GameOver) State.AcceptInput = true;
    }

    // =================================================================
    //  HELP
    // =================================================================
    IEnumerator ShowHelp()
    {
        yield return Term.TypeLine(Term.I("-- commands --"));
        yield return Term.TypeLine(Term.C("  scan                ") + Term.D("scan the network, show node map"));
        yield return Term.TypeLine(Term.C("  connect [node]      ") + Term.D("connect to hub03 / archive07 / deep12 / core19"));
        yield return Term.TypeLine(Term.C("  disconnect          ") + Term.D("safely leave current node"));
        yield return Term.TypeLine(Term.C("  ls                  ") + Term.D("list files on connected node"));
        yield return Term.TypeLine(Term.C("  read [file]         ") + Term.D("read a file by name"));
        yield return Term.TypeLine(Term.C("  analyze [file]      ") + Term.D("run anomaly scan on a previously read file"));
        yield return Term.TypeLine(Term.C("  decrypter --shift N ") + Term.D("[text]  apply caesar decryption"));
        yield return Term.TypeLine(Term.C("  confirm             ") + Term.D("confirm a decrypter output as correct"));
        yield return Term.TypeLine(Term.C("  probe [node] [####] ") + Term.D("bruteforce a locked node access code"));
        yield return Term.TypeLine(Term.C("  sever               ") + Term.D("cut connection immediately (use during sweep)"));
        yield return Term.TypeLine(Term.C("  wait                ") + Term.D("hold — trace decays, heat ticks slowly"));
        yield return Term.TypeLine(Term.C("  map                 ") + Term.D("show network diagram"));
        yield return Term.TypeLine(Term.C("  status              ") + Term.D("detailed stat and evidence readout"));
        yield return Term.TypeLine(Term.C("  missions            ") + Term.D("show current mission objectives"));
        yield return Term.TypeLine(Term.C("  contacts            ") + Term.D("show contacts and call syntax"));
        yield return Term.TypeLine(Term.C("  call [c] [a]        ") + Term.D("call a contact (ghost / maven / atlas)"));
        yield return Term.TypeLine(Term.C("  leak                ") + Term.D("publish evidence from CORE_19 — win condition"));
        yield return Term.TypeLine(Term.C("  clear               ") + Term.D("clear the screen"));
    }

    // =================================================================
    //  STATUS
    // =================================================================
    IEnumerator ShowStatus()
    {
        string tc = State.Trace < 40 ? "#7EC850" : State.Trace < 70 ? "#D4903A" : "#C0392B";
        string hc = State.Heat  < 40 ? "#7EC850" : State.Heat  < 70 ? "#D4903A" : "#C0392B";
        yield return Term.TypeLine(Term.I("-- status --"));
        yield return Term.TypeLine(Term.C("trace    : ") + $"<color={tc}>{State.Trace:0}%</color>");
        Term.Print(Term.TraceBar());
        yield return Term.TypeLine(Term.C("heat     : ") + $"<color={hc}>{State.Heat:0}%</color>");
        yield return Term.TypeLine(Term.C("favours  : ") + Term.I($"{State.Favours}"));
        yield return Term.TypeLine(Term.C("evidence : ") + Term.G($"{State.Evidence}/5"));
        yield return Term.TypeLine(Term.C("node     : ") + (State.ActiveNode.Length > 0 ? Term.G(State.ActiveNode.ToUpper()) : Term.D("none")));
        yield return Term.TypeLine(Term.C("proxy    : ") + (State.ProxyActive ? Term.I("active") : Term.D("inactive")));
        yield return Term.TypeLine(Term.C("DEEP_12  : ") + (State.Deep12Locked ? Term.E("locked") : Term.G("open")));
        yield return Term.TypeLine(Term.C("CORE_19  : ") + (State.Core19Locked ? Term.E("locked") : Term.G("open")));
        Term.Blank();
        yield return Term.TypeLine(Term.D("evidence checklist:"));
        yield return Term.TypeLine((State.Ev1_EchoList   ? Term.G("  [x]") : Term.D("  [ ]")) + Term.C(" ECHO citizen list"));
        yield return Term.TypeLine((State.Ev2_Budget     ? Term.G("  [x]") : Term.D("  [ ]")) + Term.C(" financial trail"));
        yield return Term.TypeLine((State.Ev3_Payload047 ? Term.G("  [x]") : Term.D("  [ ]")) + Term.C(" pre-arrest orders"));
        yield return Term.TypeLine((State.Ev4_EchoOrders ? Term.G("  [x]") : Term.D("  [ ]")) + Term.C(" force authorisation"));
        yield return Term.TypeLine((State.Ev5_EchoMaster ? Term.G("  [x]") : Term.D("  [ ]")) + Term.C(" director sign-off"));
    }
}
