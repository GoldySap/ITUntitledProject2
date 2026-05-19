using System.Collections;
using UnityEngine;

/// <summary>
/// FileSystem.cs — All readable files and their content.
/// Adding a new file = new case in Read() and optionally Analyze().
/// </summary>
public class FileSystem : MonoBehaviour
{
    [HideInInspector] public GameState  State;
    [HideInInspector] public Terminal   Term;
    [HideInInspector] public NodeSystem Nodes;

    // =================================================================
    //  READ
    // =================================================================
    public IEnumerator Read(string file)
    {
        if (State.ActiveNode.Length == 0)
        { yield return Term.TypeLine(Term.E("not connected to a node. use [connect node].")); yield break; }

        string f = Normalise(file);

        // ── HUB_03 ────────────────────────────────────────────────────
        if (f == "staffchat" && State.ActiveNode == "hub03")
        {
            State.Read_StaffChat = true;
            yield return Term.TypeLine(Term.D("// staff_chat ──────────────────────────────"));
            yield return Term.TypeLine(Term.C("employee_331: new passphrase policy is a nightmare"));
            yield return Term.TypeLine(Term.C("employee_447: yeah, still using ECHO as the base word"));
            yield return Term.TypeLine(Term.C("employee_331: same as always. when does the shift change?"));
            yield return Term.TypeLine(Term.C("employee_447: maintenance log says the ") + Term.W("12th") + Term.C(". mark it."));
            yield return Term.TypeLine(Term.C("employee_331: got it. see you then."));
            yield return Term.TypeLine(Term.D("// [end of log]"));
            Nodes.AddTrace(3f);
            yield return Term.TypeLine(Term.E(">> trace +3%"));
            yield return Term.TypeLine(Term.D("tip: [analyze staff_chat] to flag anomalies."));
        }
        else if (f == "accesslog" && State.ActiveNode == "hub03")
        {
            State.Read_AccessLog = true;
            yield return Term.TypeLine(Term.D("// access_log ──────────────────────────────"));
            yield return Term.TypeLine(Term.C("DEEP_12 access attempts — last 72h:"));
            yield return Term.TypeLine(Term.W("  failed:  5 _ _ _"));
            yield return Term.TypeLine(Term.W("  failed:  5 7 _ _"));
            yield return Term.TypeLine(Term.W("  failed:  5 7 3 _"));
            yield return Term.TypeLine(Term.E("  granted: [REDACTED]"));
            yield return Term.TypeLine(Term.D("  session closed — authorised user."));
            yield return Term.TypeLine(Term.D("// [end of log]"));
            Nodes.AddTrace(3f);
            yield return Term.TypeLine(Term.E(">> trace +3%"));
            yield return Term.TypeLine(Term.D("tip: [analyze access_log] to flag anomalies."));
        }

        // ── ARCHIVE_07 ────────────────────────────────────────────────
        else if (f == "echolist" && State.ActiveNode == "archive07")
        {
            yield return Term.TypeLine(Term.D("// echo_list ───────────────────────────────"));
            yield return Term.TypeLine(Term.C("OPERATION ECHO — citizen surveillance list"));
            yield return Term.TypeLine(Term.C("SUBJECT 4471: Mara Voss   — ") + Term.E("flagged [HIGH]"));
            yield return Term.TypeLine(Term.C("SUBJECT 4472: Jin Park    — ") + Term.W("flagged [MED]"));
            yield return Term.TypeLine(Term.C("SUBJECT 4473: Yusuf Ademi — ") + Term.W("flagged [MED]"));
            yield return Term.TypeLine(Term.D("... [2,847 more entries]"));
            yield return Term.TypeLine(Term.D("// [end of file]"));
            Nodes.AddTrace(3f);
            yield return Term.TypeLine(Term.E(">> trace +3%"));
            if (!State.Ev1_EchoList)
            {
                State.Ev1_EchoList = true;
                State.Evidence = Mathf.Min(5, State.Evidence + 1);
                Term.UpdateStatusBar();
                yield return Term.TypeLine(Term.G(">> new evidence: ECHO citizen list confirmed. +1 evidence"));
            }
        }
        else if (f == "memodraft" && State.ActiveNode == "archive07")
        {
            State.Read_MemoDraft = true;
            yield return Term.TypeLine(Term.D("// memo_draft ──────────────────────────────"));
            yield return Term.TypeLine(Term.C("FROM: Deputy Director Harlan"));
            yield return Term.TypeLine(Term.C("TO:   [REDACTED]"));
            yield return Term.TypeLine(Term.C("RE:   Subject 4471 (Voss case)"));
            yield return Term.TypeLine(Term.C(""));
            yield return Term.TypeLine(Term.C("The Voss situation is becoming complicated."));
            yield return Term.TypeLine(Term.C("Her son works at the Ministry. If this leaks"));
            yield return Term.TypeLine(Term.C("before the sweep on the ") + Term.W("7th") + Term.C(", we have a serious problem."));
            yield return Term.TypeLine(Term.C("Standard caesar encoding confirmed for all field docs."));
            yield return Term.TypeLine(Term.C("Authorisation shift remains at protocol value."));
            yield return Term.TypeLine(Term.D("// [draft — not sent]"));
            Nodes.AddTrace(3f);
            yield return Term.TypeLine(Term.E(">> trace +3%"));
            yield return Term.TypeLine(Term.D("tip: [analyze memo_draft] to flag anomalies."));
        }
        else if (f == "budget" && State.ActiveNode == "archive07")
        {
            State.Read_Budget = true;
            yield return Term.TypeLine(Term.D("// budget_report ───────────────────────────"));
            yield return Term.TypeLine(Term.C("OPERATION ECHO — Q3 resource allocation"));
            yield return Term.TypeLine(Term.C("  surveillance infrastructure:  $4.2M"));
            yield return Term.TypeLine(Term.C("  pre-arrest operations:        $1.8M"));
            yield return Term.TypeLine(Term.C("  field operatives (contracted): $0.9M"));
            yield return Term.TypeLine(Term.W("  black budget reference:       FINANCIALS_CORE"));
            yield return Term.TypeLine(Term.C("  total allocated:              $6.9M"));
            yield return Term.TypeLine(Term.D("// auditor copy — distribution restricted"));
            Nodes.AddTrace(3f);
            yield return Term.TypeLine(Term.E(">> trace +3%"));
            if (!State.Ev2_Budget)
            {
                State.Ev2_Budget = true;
                State.Evidence = Mathf.Min(5, State.Evidence + 1);
                Term.UpdateStatusBar();
                yield return Term.TypeLine(Term.G(">> new evidence: ECHO financial trail confirmed. +1 evidence"));
            }
        }

        // ── DEEP_12 ───────────────────────────────────────────────────
        else if (f == "directive" && State.ActiveNode == "deep12")
        {
            State.Read_Directive = true;
            yield return Term.TypeLine(Term.D("// directive ───────────────────────────────"));
            yield return Term.TypeLine(Term.C("FIELD DIRECTIVE — OPERATION ECHO"));
            yield return Term.TypeLine(Term.C("All field operatives are authorised to use"));
            yield return Term.TypeLine(Term.C("pre-emptive force on subjects rated HIGH."));
            yield return Term.TypeLine(Term.C("Encoding: standard caesar, shift value ") + Term.W("12") + Term.C("."));
            yield return Term.TypeLine(Term.C("Core node passkey stored in keyfile."));
            yield return Term.TypeLine(Term.D("// [classified — DEEP_12 only]"));
            Nodes.AddTrace(5f);
            yield return Term.TypeLine(Term.E(">> trace +5%"));
            yield return Term.TypeLine(Term.D("tip: [analyze directive] to flag anomalies."));
        }
        else if (f == "corekey" && State.ActiveNode == "deep12")
        {
            if (!State.HasCore19Key)
            {
                State.HasCore19Key = true;
                State.Core19Locked = false;
                yield return Term.TypeLine(Term.G("// core_key — decryption key extracted."));
                yield return Term.TypeLine(Term.G(">> CORE_19 is now accessible. use [connect core19]."));
                Nodes.AddTrace(6f);
                yield return Term.TypeLine(Term.E(">> trace +6%"));
            }
            else yield return Term.TypeLine(Term.D("key already obtained. CORE_19 is unlocked."));
        }

        // ── CORE_19 ───────────────────────────────────────────────────
        else if (f == "directorlog" && State.ActiveNode == "core19")
        {
            State.Read_DirectorLog = true;
            yield return Term.TypeLine(Term.D("// director_log ────────────────────────────"));
            yield return Term.TypeLine(Term.C("Director Aldric Vane — personal log"));
            yield return Term.TypeLine(Term.C("ECHO has exceeded its original mandate."));
            yield return Term.TypeLine(Term.C("What began as threat assessment is now"));
            yield return Term.TypeLine(Term.C("pre-emptive political suppression. I signed"));
            yield return Term.TypeLine(Term.C("the authorisation. I know what that means."));
            yield return Term.TypeLine(Term.W("If this reaches the public, I am finished."));
            yield return Term.TypeLine(Term.W("-- Vane."));
            yield return Term.TypeLine(Term.D("// [personal device — unencrypted]"));
            Nodes.AddTrace(5f);
            yield return Term.TypeLine(Term.E(">> trace +5%"));
            yield return Term.TypeLine(Term.G(">> key detail: Director Aldric Vane identified."));
        }
        else
        {
            yield return Term.TypeLine(Term.E($"file not found on {State.ActiveNode.ToUpper()}: {file}"));
            yield return Term.TypeLine(Term.D("use [ls] to list available files."));
        }
    }

    // =================================================================
    //  ANALYZE
    // =================================================================
    public IEnumerator Analyze(string file)
    {
        string f = Normalise(file);
        bool found = false;

        yield return Term.TypeLine(Term.I($"// anomaly scan: {file} ─────────────────"));
        yield return new WaitForSeconds(0.4f);

        if (f == "memodraft" && State.Read_MemoDraft)
        {
            found = true;
            yield return Term.TypeLine(Term.I("flagged: ") + Term.W("[7th]")    + Term.I("  — numeric in non-numeric context"));
            yield return Term.TypeLine(Term.I("flagged: ") + Term.W("[4471]")   + Term.I("  — cross-ref: subject ID in ECHO_LIST"));
            yield return Term.TypeLine(Term.I("flagged: ") + Term.W("[caesar]") + Term.I("  — cipher family named explicitly"));
            yield return Term.TypeLine(Term.D("// interpretation is yours, SIGNAL."));
        }
        if (f == "staffchat" && State.Read_StaffChat)
        {
            found = true;
            yield return Term.TypeLine(Term.I("flagged: ") + Term.W("[ECHO]")  + Term.I("  — classified op name in casual context"));
            yield return Term.TypeLine(Term.I("flagged: ") + Term.W("[12th]")  + Term.I("  — date reference, schedule or cipher?"));
            yield return Term.TypeLine(Term.I("flagged: ") + Term.W("[shift]") + Term.I("  — ambiguous: shift as in schedule, or cipher shift?"));
            yield return Term.TypeLine(Term.D("// interpretation is yours, SIGNAL."));
        }
        if (f == "accesslog" && State.Read_AccessLog)
        {
            found = true;
            yield return Term.TypeLine(Term.I("flagged: ") + Term.W("[5 _ _ _]") + Term.I("  — digit 1 confirmed"));
            yield return Term.TypeLine(Term.I("flagged: ") + Term.W("[5 7 _ _]") + Term.I("  — digit 2 confirmed"));
            yield return Term.TypeLine(Term.I("flagged: ") + Term.W("[5 7 3 _]") + Term.I("  — digit 3 confirmed"));
            yield return Term.TypeLine(Term.I("flagged: ") + Term.W("[REDACTED]")+ Term.I("  — digit 4 suppressed"));
            yield return Term.TypeLine(Term.D("// three of four digits confirmed. reason through the fourth."));
        }
        if (f == "directive" && State.Read_Directive)
        {
            found = true;
            yield return Term.TypeLine(Term.I("flagged: ") + Term.W("[12]")     + Term.I("  — explicit shift value in field document"));
            yield return Term.TypeLine(Term.I("flagged: ") + Term.W("[ATLAS]")  + Term.I("  — organisation name in classified context"));
            yield return Term.TypeLine(Term.I("flagged: ") + Term.W("[keyfile]")+ Term.I("  — cross-ref: core_key on DEEP_12"));
            yield return Term.TypeLine(Term.D("// interpretation is yours, SIGNAL."));
        }

        if (!found)
        {
            yield return Term.TypeLine(Term.E($"cannot analyze: {file}"));
            yield return Term.TypeLine(Term.D("read the file first, then analyze it."));
            yield break;
        }
        Nodes.AddTrace(2f);
        yield return Term.TypeLine(Term.E(">> trace +2%"));
    }

    static string Normalise(string s) =>
        s.ToLower().Replace("_","").Replace("-","").Replace(".txt","").Replace(".dat","").Replace(" ","");
}
