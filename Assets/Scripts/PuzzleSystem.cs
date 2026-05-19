using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

public class PuzzleSystem : MonoBehaviour
{
    [HideInInspector] public GameState  State;
    [HideInInspector] public Terminal   Term;
    [HideInInspector] public NodeSystem Nodes;

    // Encoded strings for each cipher puzzle
    // All use caesar with the shift value hidden in lore files
    // payload_047 shift=7:  "KILL ORDERS READY" -> encoded
    // echo_orders shift=12: "FORCE AUTH SIGNED" -> encoded
    // echo_master shift=12: "VANE SIGNED ALL"   -> encoded
    private const string ENC_PAYLOAD  = "RPSS VYKLYZ YLHKF"; // shift 7  -> KILL ORDERS READY
    private const string ENC_ORDERS   = "RUBAC MDFZ UECBSR"; // shift 12 -> FORCE AUTH SIGNED
    private const string ENC_MASTER   = "HMZQ EUASTQ MXX";   // shift 12 -> VANE SIGNED ALL

    public IEnumerator Decrypter(string args)
    {
        if (args.Length == 0)
        {
            yield return Term.TypeLine(Term.I("// built-in caesar decrypter"));
            yield return Term.TypeLine(Term.C("  usage:   decrypter --shift [N] [ENCODED TEXT]"));
            yield return Term.TypeLine(Term.C("  example: decrypter --shift 7 RPSS VYKLYZ YLHKF"));
            yield return Term.TypeLine(Term.D("  find the shift value in lore files."));
            yield break;
        }

        var match = Regex.Match(args, @"--shift\s+(\d+)\s+(.+)");
        if (!match.Success)
        {
            yield return Term.TypeLine(Term.E("invalid syntax. usage: decrypter --shift [N] [TEXT]"));
            yield break;
        }

        int    shift   = int.Parse(match.Groups[1].Value);
        string encoded = match.Groups[2].Value.Trim().ToUpper();
        string decoded = CaesarDecrypt(encoded, shift);

        yield return Term.TypeLine(Term.I($"// decrypter — caesar shift {shift}"));
        yield return Term.TypeLine(Term.D($"input:  {encoded}"));
        yield return new WaitForSeconds(0.3f);
        yield return AnimateDecrypt(encoded, decoded);
        Term.Blank();

        bool matched = false;
        if (State.ActiveNode == "archive07" && !State.Ev3_Payload047 && IsValidPlaintext(decoded))
        {
            State.PendingCipherFile   = "payload_047";
            State.PendingCipherOutput = decoded;
            State.PendingConfirm      = true;
            State.CipherFirstTry      = true;
            matched = true;
        }
        else if (State.ActiveNode == "deep12" && !State.Ev4_EchoOrders && IsValidPlaintext(decoded))
        {
            State.PendingCipherFile   = "echo_orders";
            State.PendingCipherOutput = decoded;
            State.PendingConfirm      = true;
            State.CipherFirstTry      = true;
            matched = true;
        }
        else if (State.ActiveNode == "core19" && !State.Ev5_EchoMaster && IsValidPlaintext(decoded))
        {
            State.PendingCipherFile   = "echo_master";
            State.PendingCipherOutput = decoded;
            State.PendingConfirm      = true;
            State.CipherFirstTry      = true;
            matched = true;
        }

        if (matched)
        {
            yield return Term.TypeLine(Term.W("// output appears to be valid plaintext."));
            yield return Term.TypeLine(Term.W("// type [confirm] if this is the correct decryption."));
            yield return Term.TypeLine(Term.D("// wrong confirm costs trace. first-try correct = bonus."));
        }
        else
        {
            if (State.PendingConfirm) State.CipherFirstTry = false;
            yield return Term.TypeLine(Term.D("// output does not appear to be valid plaintext. try a different shift."));
            Nodes.AddTrace(5f);
            yield return Term.TypeLine(Term.E(">> trace +5% (bad attempt)"));
        }
    }

    public IEnumerator Confirm()
    {
        if (!State.PendingConfirm)
        {
            yield return Term.TypeLine(Term.E("nothing to confirm. run decrypter first."));
            yield break;
        }

        State.PendingConfirm = false;
        bool first = State.CipherFirstTry;

        switch (State.PendingCipherFile)
        {
            case "payload_047":
                if (!State.Ev3_Payload047)
                {
                    State.Ev3_Payload047 = true;
                    State.Evidence = Mathf.Min(5, State.Evidence + 1);
                    Term.UpdateStatusBar();
                    yield return Term.TypeLine(Term.G($"confirmed: {State.PendingCipherOutput}"));
                    yield return Term.TypeLine(Term.G(">> evidence: pre-arrest kill orders. +1 evidence"));
                    float cost = first ? 5f : 15f;
                    Nodes.AddTrace(cost);
                    yield return Term.TypeLine(first
                        ? Term.G(">> first-try bonus: trace +5% instead of +15%")
                        : Term.E($">> trace +15%"));
                }
                else yield return Term.TypeLine(Term.D("payload_047 already logged as evidence."));
                break;

            case "echo_orders":
                if (!State.Ev4_EchoOrders)
                {
                    State.Ev4_EchoOrders = true;
                    State.Evidence = Mathf.Min(5, State.Evidence + 1);
                    Term.UpdateStatusBar();
                    yield return Term.TypeLine(Term.G($"confirmed: {State.PendingCipherOutput}"));
                    yield return Term.TypeLine(Term.G(">> evidence: field force authorisation. +1 evidence"));
                    float cost = first ? 5f : 15f;
                    Nodes.AddTrace(cost);
                    yield return Term.TypeLine(first
                        ? Term.G(">> first-try bonus: trace +5% instead of +15%")
                        : Term.E($">> trace +15%"));
                }
                else yield return Term.TypeLine(Term.D("echo_orders already logged as evidence."));
                break;

            case "echo_master":
                if (!State.Ev5_EchoMaster)
                {
                    State.Ev5_EchoMaster = true;
                    State.Evidence = Mathf.Min(5, State.Evidence + 1);
                    Term.UpdateStatusBar();
                    yield return Term.TypeLine(Term.G($"confirmed: {State.PendingCipherOutput}"));
                    yield return Term.TypeLine(Term.G(">> evidence: director sign-off. +1 evidence — full picture assembled."));
                    float cost = first ? 5f : 20f;
                    Nodes.AddTrace(cost);
                    yield return Term.TypeLine(first
                        ? Term.G(">> first-try bonus: trace +5% instead of +20%")
                        : Term.E($">> trace +20%"));
                }
                else yield return Term.TypeLine(Term.D("echo_master already logged as evidence."));
                break;
        }
        State.PendingCipherFile = "";
    }

    public IEnumerator Probe(string args)
    {
        string[] parts = args.Split(' ');
        if (parts.Length < 5)
        {
            yield return Term.TypeLine(Term.E("usage: probe [node] [d1] [d2] [d3] [d4]"));
            yield return Term.TypeLine(Term.D("example: probe deep12 5 2 8 1"));
            yield break;
        }

        string node = NodeSystem.Normalise(parts[0]);
        if (node != "deep12")
        { yield return Term.TypeLine(Term.E("only DEEP_12 requires bruteforce access.")); yield break; }
        if (!State.Deep12Locked)
        { yield return Term.TypeLine(Term.D("DEEP_12 is already unlocked.")); yield break; }
        if (State.Deep12BruteLockedOut)
        { yield return Term.TypeLine(Term.E("DEEP_12 is in lockout. wait for it to expire.")); yield break; }

        int[] guess = new int[4];
        bool ok = true;
        for (int i = 0; i < 4; i++)
            if (!int.TryParse(parts[i + 1], out guess[i])) { ok = false; break; }
        if (!ok)
        { yield return Term.TypeLine(Term.E("digits must be numbers 0-9.")); yield break; }

        State.BruteProbes++;
        Nodes.AddTrace(4f);
        yield return Term.TypeLine(Term.D($"probing DEEP_12 [{guess[0]} {guess[1]} {guess[2]} {guess[3]}]..."));
        yield return new WaitForSeconds(0.4f);

        int correctPos = 0, wrongPos = 0;
        bool[] ansUsed   = new bool[4];
        bool[] guessUsed = new bool[4];

        for (int i = 0; i < 4; i++)
            if (guess[i] == State.BruteAnswer[i]) { correctPos++; ansUsed[i] = guessUsed[i] = true; }

        for (int i = 0; i < 4; i++)
        {
            if (guessUsed[i]) continue;
            for (int j = 0; j < 4; j++)
            {
                if (!ansUsed[j] && guess[i] == State.BruteAnswer[j])
                { wrongPos++; ansUsed[j] = true; break; }
            }
        }

        if (correctPos == 4)
        {
            State.Deep12Locked = false;
            State.BruteProbes  = 0;
            yield return Term.TypeLine(Term.G("ACCESS GRANTED. DEEP_12 unlocked."));
            yield return Term.TypeLine(Term.G(">> use [connect deep12] to access files."));
        }
        else
        {
            yield return Term.TypeLine(Term.W($"correct position: {correctPos}  |  wrong position: {wrongPos}  |  trace +4%"));
            int rem = GameState.MAX_BRUTE_PROBES - State.BruteProbes;
            yield return Term.TypeLine(Term.D($"probes remaining: {rem}"));
            if (rem <= 0)
            {
                Nodes.AddTrace(20f);
                State.BruteProbes = 0;
                State.Deep12BruteLockedOut = true;
                yield return Term.TypeLine(Term.E("!! probe limit reached. DEEP_12 locked out for 60 seconds. trace +20%"));
                StartCoroutine(LockoutTimer());
            }
        }
    }

    IEnumerator LockoutTimer()
    {
        yield return new WaitForSeconds(60f);
        State.Deep12BruteLockedOut = false;
        Term.Print(Term.D("// DEEP_12 lockout expired. probes reset."));
    }

    IEnumerator AnimateDecrypt(string encoded, string decoded)
    {
        char[] chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();
        var result = encoded.ToCharArray();
        var lines  = new System.Collections.Generic.List<string>();

        for (int pass = 0; pass < 3; pass++)
        {
            for (int i = 0; i < result.Length; i++)
                if (result[i] != ' ') result[i] = chars[Random.Range(0, chars.Length)];
            Term.Print(Term.W(new string(result)));
            lines.Add("");
            yield return new WaitForSeconds(0.12f);
            if (lines.Count > 0) { /* remove last printed line */ }
        }

        // Partial reveal
        char[] final = decoded.ToCharArray();
        for (int i = 0; i < Mathf.Min(result.Length, final.Length); i++)
            result[i] = (final[i] != ' ' && Random.value > 0.5f) ? chars[Random.Range(0, chars.Length)] : final[i];
        Term.Print(Term.W(new string(result)));
        yield return new WaitForSeconds(0.18f);

        Term.Print(Term.G(decoded));
    }

    static string CaesarDecrypt(string text, int shift)
    {
        var sb = new StringBuilder();
        foreach (char ch in text.ToUpper())
        {
            if (ch >= 'A' && ch <= 'Z')
                sb.Append((char)(((ch - 'A' - (shift % 26) + 26) % 26) + 'A'));
            else sb.Append(ch);
        }
        return sb.ToString();
    }

    static bool IsValidPlaintext(string text)
    {
        int letters = 0, total = 0;
        foreach (char c in text) { if (c != ' ') { total++; if (char.IsLetter(c)) letters++; } }
        return total > 0 && (float)letters / total > 0.85f;
    }
}
