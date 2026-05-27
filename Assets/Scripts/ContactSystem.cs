using System.Collections;
using UnityEngine;
public class ContactSystem : MonoBehaviour
{
    [HideInInspector] public GameState State;
    [HideInInspector] public Terminal Term;
    [HideInInspector] public NodeSystem  Nodes;
    [HideInInspector] public WorldEvents Events;
    
    public IEnumerator ShowAll()
    {
        yield return Term.TypeLine(Term.I("-- contacts --"));
        yield return Term.TypeLine(Term.G("GHOST") + Term.D(" [1 favour]  the fixer."));
        yield return Term.TypeLine(Term.L("  call ghost spoof    ") + Term.C("wipe 30% trace"));
        yield return Term.TypeLine(Term.L("  call ghost reroute  ") + Term.C("halve trace cost on next action"));
        yield return Term.TypeLine(Term.L("  call ghost dark     ") + Term.C("go silent — sweeps paused, heat decays"));
        Term.Blank();
        yield return Term.TypeLine(Term.W("MAVEN") + Term.D(" [1 favour]  the informant."));
        yield return Term.TypeLine(Term.L("  call maven scan     ") + Term.C("recon all nodes, zero trace"));
        yield return Term.TypeLine(Term.L("  call maven hint     ") + Term.C("get a puzzle hint (cipher or bruteforce)"));
        yield return Term.TypeLine(Term.L("  call maven warn     ") + Term.C("check true heat level"));
        Term.Blank();
        yield return Term.TypeLine(Term.E("ATLAS") + Term.D(" [2 favours]  the backer. use carefully."));
        yield return Term.TypeLine(Term.L("  call atlas publish  ") + Term.C("partial leak — buys time, not a win"));
        yield return Term.TypeLine(Term.L("  call atlas funds    ") + Term.C("emergency +2 favours"));
        yield return Term.TypeLine(Term.L("  call atlas extract  ") + Term.C("emergency heat & trace reduction"));
    }
    
    public IEnumerator Call(string contact, string action)
    {
        int cost = contact == "atlas" ? 2 : 1;
        if (State.Favours < cost)
        {
            yield return Term.TypeLine(Term.E($"not enough favours. need {cost}, have {State.Favours}."));
            yield break;
        }
        switch (contact)
        {
            case "ghost": yield return Ghost(action); break;
            case "maven": yield return Maven(action); break;
            case "atlas": yield return Atlas(action); break;
            default:
                yield return Term.TypeLine(Term.E($"unknown contact: {contact}"));
                yield return Term.TypeLine(Term.D("known contacts: ghost, maven, atlas"));
                break;
        }
    }
    
    IEnumerator Ghost(string action)
    {
        State.Favours--;
        yield return Term.TypeLine(Term.G("GHOST: ") + Term.D("..."));
        yield return new WaitForSeconds(0.4f);
        string ghostReact = Events?.GhostReaction() ?? "";
        if (ghostReact.Length > 0) yield return Term.TypeLine(Term.D(ghostReact));

        switch (action)
        {
            case "spoof":
                Nodes.ReduceTrace(30f);
                yield return Term.TypeLine(Term.G("GHOST: ") + Term.C("signal scrubbed. you're clean."));
                yield return Term.TypeLine(Term.G(">> trace -30%"));
                Term.Print(Term.TraceBar());
                break;

            case "reroute":
                State.ProxyActive = true;
                Term.UpdateStatusBar();
                yield return Term.TypeLine(Term.G("GHOST: ") + Term.C("proxy armed. next action costs half trace."));
                break;

            case "dark":
                if (State.ActiveNode.Length > 0)
                {
                    State.Favours++;
                    yield return Term.TypeLine(Term.G("GHOST: ") + Term.W("disconnect from the node first."));
                    yield break;
                }
                yield return GoSilent();
                break;

            default:
                State.Favours++;
                yield return Term.TypeLine(Term.E($"GHOST: don't know that: {action}"));
                yield return Term.TypeLine(Term.D("options: spoof, reroute, dark"));
                break;
        }
    }

    IEnumerator GoSilent()
    {
        State.InDarkMode = true;
        Term.UpdateStatusBar();
        yield return Term.TypeLine(Term.G("GHOST: ") + Term.C("going dark. sweeps paused. heat decaying."));
        for (int i = 0; i < 4; i++)
        {
            yield return new WaitForSeconds(5f);
            Nodes.ReduceHeat(6f);
            Term.Print(Term.D($"  [dark] heat: {State.Heat:0}%"));
        }
        State.InDarkMode = false;
        Term.UpdateStatusBar();
        yield return Term.TypeLine(Term.G("GHOST: ") + Term.C("back online. be careful."));
    }
    
    IEnumerator Maven(string action)
    {
        State.Favours--;
        yield return Term.TypeLine(Term.W("MAVEN: ") + Term.D("..."));
        yield return new WaitForSeconds(0.5f);
        string mavenReact = Events?.MavenReaction() ?? "";
        if (mavenReact.Length > 0) yield return Term.TypeLine(Term.D(mavenReact));

        switch (action)
        {
            case "scan":
                yield return Term.TypeLine(Term.W("MAVEN: ") + Term.C("piggybacking a maintenance ping — zero trace."));
                yield return Term.TypeLine(Term.G("HUB_03     ") + Term.D("open   — 2 readable files"));
                yield return Term.TypeLine(Term.G("ARCHIVE_07 ") + Term.D("open   — 2 readable, 1 encrypted (caesar)"));
                yield return Term.TypeLine(
                    (State.Deep12Locked ? Term.E("DEEP_12    ") : Term.G("DEEP_12    ")) +
                    Term.D("bruteforce gate, 1 encrypted (caesar)"));
                yield return Term.TypeLine(
                    (State.Core19Locked ? Term.E("CORE_19    ") : Term.G("CORE_19    ")) +
                    Term.D("key-gated, 1 encrypted (hardest caesar)"));
                break;

            case "hint":
                yield return Term.TypeLine(Term.W("MAVEN: ") + Term.C("let me think..."));
                if (!State.Ev3_Payload047)
                    yield return Term.TypeLine(Term.W("MAVEN: ") + Term.D("payload_047 uses caesar. the shift is tied to a date in memo_draft. look for numbers."));
                else if (!State.Ev4_EchoOrders)
                    yield return Term.TypeLine(Term.W("MAVEN: ") + Term.D("echo_orders uses caesar too. the shift value is named explicitly in directive.txt."));
                else if (!State.Ev5_EchoMaster)
                    yield return Term.TypeLine(Term.W("MAVEN: ") + Term.D("echo_master — same shift as echo_orders. Vane used the same key for everything."));
                else if (State.Deep12Locked)
                    yield return Term.TypeLine(Term.W("MAVEN: ") + Term.D("DEEP_12 code — access_log shows 3 digits. the fourth follows logically. use probe feedback."));
                else
                    yield return Term.TypeLine(Term.W("MAVEN: ") + Term.D("you have everything you need. leak it from CORE_19."));
                break;

            case "warn":
                string status = State.Heat < 30
                    ? "cold. no active search."
                    : State.Heat < 60
                    ? "warm. patrols elevated."
                    : "hot. agents on nearby nodes.";
                yield return Term.TypeLine(Term.W("MAVEN: ") + Term.C($"heat status: {status}"));
                yield return Term.TypeLine(Term.W("MAVEN: ") + Term.D($"actual reading: {State.Heat:0}%. ") +
                    (State.Heat > 60 ? Term.E("be careful.") : Term.G("you're ok for now.")));
                break;

            default:
                State.Favours++;
                yield return Term.TypeLine(Term.E($"MAVEN: don't know that: {action}"));
                yield return Term.TypeLine(Term.D("options: scan, hint, warn"));
                break;
        }
    }
    
    IEnumerator Atlas(string action)
    {
        State.Favours -= 2;
        State.AtlasUseCount++;
        yield return Term.TypeLine(Term.E("ATLAS: ") + Term.D("..."));
        yield return new WaitForSeconds(0.6f);

        if (State.AtlasUseCount >= 3)
            yield return Term.TypeLine(Term.E("ATLAS: ") + Term.W("you keep calling, SIGNAL. we'll settle accounts when this is over."));

        switch (action)
        {
            case "publish":
                yield return Term.TypeLine(Term.E("ATLAS: ") + Term.C("pushing partial data to press contacts."));
                yield return Term.TypeLine(Term.W(">> partial pressure applied. authorities diverted temporarily."));
                Nodes.ReduceHeat(15f);
                yield return Term.TypeLine(Term.E(">> heat -15%"));
                break;

            case "funds":
                State.Favours += 2;
                yield return Term.TypeLine(Term.E("ATLAS: ") + Term.C("resources transferred. don't waste them."));
                yield return Term.TypeLine(Term.I(">> +2 favours"));
                break;

            case "extract":
                Nodes.ReduceHeat(40f);
                Nodes.ReduceTrace(20f);
                yield return Term.TypeLine(Term.E("ATLAS: ") + Term.C("misdirection package deployed. move now."));
                yield return Term.TypeLine(Term.E(">> heat -40%  trace -20%"));
                Term.Print(Term.TraceBar());
                break;

            default:
                State.Favours += 2;
                State.AtlasUseCount--;
                yield return Term.TypeLine(Term.E($"ATLAS: unrecognised: {action}"));
                yield return Term.TypeLine(Term.D("options: publish, funds, extract"));
                break;
        }
    }
}
