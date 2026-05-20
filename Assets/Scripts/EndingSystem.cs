using System.Collections;
using UnityEngine;
public class EndingSystem : MonoBehaviour
{
    [HideInInspector] public GameState State;
    [HideInInspector] public Terminal Term;
    [HideInInspector] public MissionSystem Missions;
    
    public IEnumerator Leak()
    {
        if (State.ActiveNode != "core19")
        {
            yield return Term.TypeLine(Term.E("leak must be executed from CORE_19."));
            yield return Term.TypeLine(Term.D("connect core19, then type [leak]."));
            yield break;
        }
        if (State.Evidence < 3)
        {
            yield return Term.TypeLine(Term.E($"not enough evidence. need at least 3, have {State.Evidence}."));
            yield break;
        }

        State.AcceptInput = false;
        State.GameOver    = true;

        if (State.AtlasUseCount >= 3 && State.Evidence >= 4)
            yield return EndingAtlas();
        else if (State.Evidence >= 5)
            yield return EndingFull();
        else
            yield return EndingPartial();
    }
    
    //  WIN ENDINGS
    
    IEnumerator EndingFull()
    {
        yield return Term.TypeLine(Term.W("initiating upload — all 5 evidence packages..."));
        yield return new WaitForSeconds(0.8f);
        yield return Term.Slow(Term.G("[####################] 100% — transmitted."));
        Term.Blank();
        yield return Term.Slow(Term.G("OPERATION ECHO is public."));
        yield return Term.Slow(Term.C("all 2,849 citizens removed from the pre-arrest list."));
        yield return Term.Slow(Term.C("Director Aldric Vane arrested within 48 hours."));
        yield return Term.Slow(Term.C("three officials face formal inquiry. ECHO is suspended."));
        yield return Term.Slow(Term.G("SIGNAL was never found."));
        Term.Blank();
        yield return DrawEndCard("FULL EXPOSURE", "the system was broken. you broke back.", Term.G);
    }

    IEnumerator EndingPartial()
    {
        yield return Term.TypeLine(Term.W($"initiating upload — {State.Evidence} of 5 packages..."));
        yield return new WaitForSeconds(0.8f);
        yield return Term.Slow(Term.W($"[############........] partial — transmitted."));
        Term.Blank();
        yield return Term.Slow(Term.W("part of OPERATION ECHO is now public."));
        yield return Term.Slow(Term.C("partial scandal. one official resigns. the list is redacted."));
        yield return Term.Slow(Term.C("the full picture remains buried — for now."));
        yield return Term.Slow(Term.W("SIGNAL disappears. the work is unfinished."));
        Term.Blank();
        yield return DrawEndCard("PARTIAL TRUTH", "enough to matter. not enough to end it.", Term.W);
    }

    IEnumerator EndingAtlas()
    {
        yield return Term.TypeLine(Term.E("ATLAS: ") + Term.C("we'll handle the upload from here, SIGNAL."));
        yield return new WaitForSeconds(0.5f);
        yield return Term.TypeLine(Term.W("uploading via ATLAS distribution network..."));
        yield return new WaitForSeconds(1f);
        yield return Term.Slow(Term.G("[####################] 100% — transmitted."));
        Term.Blank();
        yield return Term.Slow(Term.E("OPERATION ECHO is public — on ATLAS's terms."));
        yield return Term.Slow(Term.C("the evidence is real. the narrative is theirs."));
        yield return Term.Slow(Term.W("three officials fall. ATLAS gains leverage over two more."));
        yield return Term.Slow(Term.E("you were a useful tool, SIGNAL."));
        Term.Blank();
        yield return DrawEndCard("ATLAS TAKEOVER", "the system was broken. someone else profited.", Term.E);
    }

    
    //  LOSS ENDINGS
    
    public IEnumerator LossTraced()
    {
        Term.Blank();
        yield return Term.TypeLine(Term.E("!! TRACE 100% — LOCATION EXPOSED"));
        yield return Term.TypeLine(Term.E("GOV_NODE_03 counter-intrusion activated."));
        yield return new WaitForSeconds(0.5f);
        yield return Term.TypeLine(Term.E("connection terminated. evidence cache wiped."));
        yield return new WaitForSeconds(2f);
        yield return Restart("LOSS: TRACED", "they found you. start over.");
    }

    public IEnumerator LossBurned()
    {
        Term.Blank();
        yield return Term.TypeLine(Term.E("!! HEAT 100% — AGENTS ON SITE"));
        yield return Term.TypeLine(Term.E("physical breach. no time to reboot."));
        yield return new WaitForSeconds(0.8f);
        yield return Term.TypeLine(Term.E("connection lost. session unrecoverable."));
        yield return new WaitForSeconds(2f);
        yield return Restart("LOSS: BURNED", "they got there first.");
    }    
    IEnumerator Restart(string title, string subtitle)
    {
        yield return DrawEndCard(title, subtitle, Term.E);
        yield return new WaitForSeconds(2.5f);

        State.Reset();

        Term.ClearScreen();
        yield return Term.TypeLine(Term.D("// rebooting secure shell..."));
        yield return new WaitForSeconds(0.5f);
        yield return Term.TypeLine(Term.G("reconnected. the evidence is still out there."));
        Term.Blank();
        Term.Print(Term.I("// network map:"));
        Term.Print(Term.NodeMap());
        Term.Blank();

        yield return Missions.ShowBriefing(0);

        State.AcceptInput = true;
    }

    IEnumerator DrawEndCard(string title, string subtitle, System.Func<string,string> colour)
    {
        string bar = "════════════════════════════════════";
        yield return Term.TypeLine(Term.D(bar));
        yield return Term.TypeLine(colour($"  {title}"));
        yield return Term.TypeLine(Term.D($"  {subtitle}"));
        yield return Term.TypeLine(Term.D(bar));
    }
}
