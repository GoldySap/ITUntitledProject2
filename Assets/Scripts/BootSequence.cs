using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class BootSequence : MonoBehaviour
{
    [HideInInspector] public Terminal       Term;
    [HideInInspector] public GameState      State;
    [HideInInspector] public SettingsSystem Settings;
    [HideInInspector] public MissionSystem  Missions;
    [HideInInspector] public VisualEffects  VFX;
    [HideInInspector] public WorldEvents    Events;

    public System.Action OnGameStart;

    private bool menuActive  = false;
    private bool connecting  = false;
    
    public IEnumerator Run()
    {
        yield return BiosPost();
        yield return new WaitForSeconds(0.3f);
        yield return LoadNexusShell();
        yield return new WaitForSeconds(0.2f);
        ShowMenuPrompt();
        menuActive = true;
        State.AcceptInput = true;
    }
    
    IEnumerator BiosPost()
    {
        float d = 0.025f;
        Term.Print(Term.D("NEXUS BIOS v2.1  --  POST sequence"));
        yield return new WaitForSeconds(0.3f);
        yield return FakeLine("CPU check ...................", "[OK]", d, green: true);
        yield return FakeLine("RAM 64MB ....................", "[OK]", d, green: true);
        yield return FakeLine("Network interface ...........", "[OK]", d, green: true);
        yield return FakeLine("Encrypted volume ............", "[OK]", d, green: true);
        yield return FakeLine("Secure boot verification ....", "[OK]", d, green: true);
        yield return new WaitForSeconds(0.2f);
        yield return FakeLine("Loading kernel modules ......", "[OK]", d, green: true);
        yield return FakeLine("Mounting filesystems ........", "[OK]", d, green: true);
        yield return FakeLine("Starting network stack ......", "[OK]", d, green: true);
        yield return new WaitForSeconds(0.3f);
        yield return Term.TypeLine(Term.G("NEXUS OS ready."), 0.03f);
        Term.Blank();
    }

    IEnumerator FakeLine(string label, string result, float delay, bool green)
    {
        for (int i = 0; i < label.Length; i++)
        {
            yield return new WaitForSeconds(delay * 0.3f);
        }
        string col = green ? Term.G(result) : Term.E(result);
        Term.Print(Term.D(label) + col);
    }
    
    IEnumerator LoadNexusShell()
    {
        yield return Term.TypeLine(Term.D("// NEXUS SECURE SHELL v4.1"), 0.025f);
        yield return Term.TypeLine(Term.D("// no active connection"), 0.025f);
        yield return Term.TypeLine(Term.D("// type [help] for available commands"), 0.025f);
        Term.Blank();
        yield return Term.TypeLine(Term.E(" _  _ ___ _  _ _   _  ___"), 0.01f);
        yield return Term.TypeLine(Term.E("| \\| | __| \\/ | | | / __|"), 0.01f);
        yield return Term.TypeLine(Term.E("|     | _|  > < | |_| \\__ \\"), 0.01f);
        yield return Term.TypeLine(Term.E("|_|\\_|___/_/\\_|\\___/|___/"), 0.01f);
        Term.Blank();
    }
    
    void ShowMenuPrompt()
    {
        Term.Print(Term.C("  start       ") + Term.D("establish connection and begin"));
        Term.Print(Term.C("  lore        ") + Term.D("background on OPERATION ECHO"));
        Term.Print(Term.C("  howtoplay   ") + Term.D("game mechanics overview"));
        Term.Print(Term.C("  settings    ") + Term.D("text speed, scanlines, flicker"));
        Term.Print(Term.C("  credits     ") + Term.D("who made this"));
        Term.Print(Term.C("  quit        ") + Term.D("exit application"));
        Term.Blank();
    }
        
    public bool IsMenuCommand(string cmd) =>
        menuActive && !connecting &&
        (cmd == "start" || cmd == "lore" || cmd == "howtoplay" ||
         cmd == "settings" || cmd.StartsWith("set ") ||
         cmd == "credits" || cmd == "quit" || cmd == "help" || cmd == "clear");

    public IEnumerator HandleMenuCommand(string cmd)
    {
        if (cmd == "start")         { yield return StartConnection(); }
        else if (cmd == "lore")     { yield return ShowLore(); }
        else if (cmd == "howtoplay"){ yield return ShowHowToPlay(); }
        else if (cmd == "settings") { yield return Settings.ShowSettings(); }
        else if (cmd.StartsWith("set "))
        {
            string[] p = cmd.Split(' ');
            if (p.Length >= 3) yield return Settings.ApplySetting(p[1], p[2]);
            else yield return Term.TypeLine(Term.E("usage: set [setting] [value]"));
        }
        else if (cmd == "credits")  { yield return ShowCredits(); }
        else if (cmd == "quit")     { QuitGame(); }
        else if (cmd == "help")
        {
            Term.Blank();
            ShowMenuPrompt();
        }
        else if (cmd == "clear")    { Term.ClearScreen(); ShowMenuPrompt(); }
        else
        {
            yield return Term.TypeLine(Term.E($"command not found: {cmd}"));
            yield return Term.TypeLine(Term.D("type [start] to connect, or [help] for menu commands."));
        }
    }
        
    IEnumerator StartConnection()
    {
        connecting  = true;
        menuActive  = false;
        State.AcceptInput = false;

        Term.Blank();
        yield return Term.TypeLine(Term.C("initiating secure connection..."), 0.025f);
        yield return new WaitForSeconds(0.2f);

        yield return FakeStep("routing: LOCAL → TOR_EXIT_4 → TOR_EXIT_9 → TARGET", "[OK]", 0.02f);
        yield return FakeStep("spoofing geolocation ...................", "[OK]", 0.02f);
        yield return FakeStep("masking device fingerprint .............", "[OK]", 0.02f);
        yield return FakeStep("randomising MAC address ................", "[OK]", 0.02f);
        yield return FakeStep("establishing encrypted tunnel ..........", "[OK]", 0.02f);
        yield return new WaitForSeconds(0.4f);

        if (VFX != null) VFX.Flicker(1);
        yield return Term.TypeLine(Term.G("connection established. welcome, SIGNAL."), 0.035f);
        yield return new WaitForSeconds(0.5f);
        Term.Blank();

        connecting = false;
        OnGameStart?.Invoke();
    }

    IEnumerator FakeStep(string label, string result, float delay)
    {
        for (int i = 0; i < label.Length; i++)
            yield return new WaitForSeconds(delay * 0.15f);
        Term.Print(Term.D(label) + Term.G(result));
    }
        
    IEnumerator ShowLore()
    {
        yield return Term.TypeLine(Term.I("-- BACKGROUND: OPERATION ECHO --"), 0.015f);
        Term.Blank();
        yield return Term.TypeLine(Term.C("OPERATION ECHO began as a legitimate national security"), 0.012f);
        yield return Term.TypeLine(Term.C("programme — threat assessment for known extremist networks."), 0.012f);
        yield return Term.TypeLine(Term.C("Within two years it had expanded beyond its mandate."), 0.012f);
        Term.Blank();
        yield return Term.TypeLine(Term.C("Citizens are now flagged not for crimes committed,"), 0.012f);
        yield return Term.TypeLine(Term.C("but for beliefs held, associations made, and words written."), 0.012f);
        yield return Term.TypeLine(Term.C("The pre-arrest list has 2,849 names on it."), 0.012f);
        yield return Term.TypeLine(Term.C("None of them know."), 0.012f);
        Term.Blank();
        yield return Term.TypeLine(Term.W("Director Aldric Vane signed the authorisation."), 0.015f);
        yield return Term.TypeLine(Term.W("ATLAS wants the documents for leverage."), 0.015f);
        yield return Term.TypeLine(Term.W("MAVEN wants the programme shut down."), 0.015f);
        yield return Term.TypeLine(Term.W("GHOST just wants to get paid."), 0.015f);
        Term.Blank();
        yield return Term.TypeLine(Term.D("// type [start] when you're ready."), 0.015f);
    }
        
    IEnumerator ShowHowToPlay()
    {
        yield return Term.TypeLine(Term.I("-- HOW TO PLAY --"), 0.015f);
        Term.Blank();
        yield return Term.TypeLine(Term.C("You are SIGNAL — an anonymous hacker."), 0.012f);
        yield return Term.TypeLine(Term.C("Your goal: gather 5 pieces of evidence and leak them."), 0.012f);
        Term.Blank();
        yield return Term.TypeLine(Term.W("TRACE"), 0.015f);
        yield return Term.TypeLine(Term.D("  rises with every action. hits 100% = game over."), 0.012f);
        yield return Term.TypeLine(Term.D("  use [wait], call GHOST [spoof], or stay careful."), 0.012f);
        Term.Blank();
        yield return Term.TypeLine(Term.W("HEAT"), 0.015f);
        yield return Term.TypeLine(Term.D("  rises passively over time. triggers agent sweeps."), 0.012f);
        yield return Term.TypeLine(Term.D("  call GHOST [dark] or ATLAS [extract] to reduce it."), 0.012f);
        Term.Blank();
        yield return Term.TypeLine(Term.W("FAVOURS"), 0.015f);
        yield return Term.TypeLine(Term.D("  earned by completing missions. spent on contacts."), 0.012f);
        yield return Term.TypeLine(Term.D("  GHOST costs 1. MAVEN costs 1. ATLAS costs 2."), 0.012f);
        Term.Blank();
        yield return Term.TypeLine(Term.W("PUZZLES"), 0.015f);
        yield return Term.TypeLine(Term.D("  encrypted files use caesar ciphers. find the shift"), 0.012f);
        yield return Term.TypeLine(Term.D("  in lore files, use [decrypter --shift N TEXT]."), 0.012f);
        yield return Term.TypeLine(Term.D("  DEEP_12 uses a 4-digit code — hints in access_log."), 0.012f);
        Term.Blank();
        yield return Term.TypeLine(Term.W("SWEEPS"), 0.015f);
        yield return Term.TypeLine(Term.D("  when heat is high and you're connected to a node,"), 0.012f);
        yield return Term.TypeLine(Term.D("  agent sweeps fire. type [sever] to escape in time."), 0.012f);
        Term.Blank();
        yield return Term.TypeLine(Term.W("SCROLL"), 0.015f);
        yield return Term.TypeLine(Term.D("  use PageUp / PageDown to scroll the terminal."), 0.012f);
        yield return Term.TypeLine(Term.D("  any new command snaps back to the bottom."), 0.012f);
        Term.Blank();
        yield return Term.TypeLine(Term.D("// type [start] when you're ready."), 0.015f);
    }
    
    IEnumerator ShowCredits()
    {
        yield return Term.TypeLine(Term.I("-- CREDITS --"), 0.015f);
        Term.Blank();
        yield return Term.TypeLine(Term.C("game design, code, writing"), 0.015f);
        yield return Term.TypeLine(Term.G("  you"), 0.015f);
        Term.Blank();
        yield return Term.TypeLine(Term.C("built with Unity + TextMeshPro"), 0.015f);
        Term.Blank();
        yield return Term.TypeLine(Term.D("// type [start] to begin."), 0.015f);
    }
        
    void QuitGame()
    {
        StartCoroutine(DoQuit());
    }

    IEnumerator DoQuit()
    {
        State.AcceptInput = false;
        yield return Term.TypeLine(Term.D("// closing secure session..."), 0.025f);
        yield return Term.TypeLine(Term.D("// wiping local cache..."), 0.025f);
        yield return Term.TypeLine(Term.G("// goodbye, SIGNAL."), 0.03f);
        yield return new WaitForSeconds(1f);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
