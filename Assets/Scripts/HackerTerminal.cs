using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// HackerTerminal.cs — Bootstrap only. Wires all modules together.
/// Attach this (and only this) to an empty GameObject in the scene.
/// All other scripts are added automatically at runtime.
///
/// SETUP:
///   1. New Unity 2D project
///   2. Window > TextMeshPro > Import TMP Essential Resources > Import
///   3. Create empty GameObject, attach HackerTerminal.cs, hit Play
/// </summary>
public class HackerTerminal : MonoBehaviour
{
    // ── Module references ─────────────────────────────────────────────
    private GameState      state;
    private Terminal       term;
    private CommandRouter  router;
    private NodeSystem     nodes;
    private FileSystem     files;
    private PuzzleSystem   puzzles;
    private ContactSystem  contacts;
    private MissionSystem  missions;
    private EndingSystem   endings;

    // =================================================================
    //  AWAKE — wire everything together
    // =================================================================
    void Awake()
    {
        // Create shared state (plain C# class, not MonoBehaviour)
        state = new GameState();

        // Add all MonoBehaviour modules to this same GameObject
        term     = gameObject.AddComponent<Terminal>();
        router   = gameObject.AddComponent<CommandRouter>();
        nodes    = gameObject.AddComponent<NodeSystem>();
        files    = gameObject.AddComponent<FileSystem>();
        puzzles  = gameObject.AddComponent<PuzzleSystem>();
        contacts = gameObject.AddComponent<ContactSystem>();
        missions = gameObject.AddComponent<MissionSystem>();
        endings  = gameObject.AddComponent<EndingSystem>();

        // Inject state into every module
        term.State     = state;
        router.State   = state;
        nodes.State    = state;
        files.State    = state;
        puzzles.State  = state;
        contacts.State = state;
        missions.State = state;
        endings.State  = state;

        // Inject terminal into every module
        router.Term   = term;
        nodes.Term    = term;
        files.Term    = term;
        puzzles.Term  = term;
        contacts.Term = term;
        missions.Term = term;
        endings.Term  = term;

        // Inject cross-module dependencies
        term.Router          = router;
        router.Nodes         = nodes;
        router.Files         = files;
        router.Puzzles       = puzzles;
        router.Contacts      = contacts;
        router.Missions      = missions;
        router.Endings       = endings;
        nodes.Endings        = endings;
        files.Nodes          = nodes;
        puzzles.Nodes        = nodes;
        contacts.Nodes       = nodes;
        endings.Missions     = missions;

        // Build the UI (Terminal handles this)
        term.BuildUI();
    }

    // =================================================================
    //  START — subscribe to keyboard, run boot sequence
    // =================================================================
    void Start()
    {
        Keyboard.current.onTextInput += term.OnCharTyped;
        StartCoroutine(Boot());
    }

    void OnDestroy()
    {
        if (Keyboard.current != null)
            Keyboard.current.onTextInput -= term.OnCharTyped;
    }

    // =================================================================
    //  UPDATE — delegate to modules
    // =================================================================
    void Update()
    {
        term.HandleInput();
        nodes.Tick();
    }

    // =================================================================
    //  BOOT SEQUENCE
    // =================================================================
    IEnumerator Boot()
    {
        // ASCII logo
        term.Print(term.E(" _  _ ___ _  _ _   _ ___"));
        term.Print(term.E("| \\| | __| \\/ | | | / __|"));
        term.Print(term.E("|    | _|  >  < | |_| \\__ \\"));
        term.Print(term.E("|_|\\_|___/_/\\_|\\___/|___/"));
        term.Print(term.D("// NEXUS SECURE SHELL v4.1"));
        term.Blank();

        yield return new WaitForSeconds(0.5f);
        yield return term.TypeLine(term.D("// encrypted tunnel established"), 0.025f);
        yield return term.TypeLine(term.D("// identity masked — location spoofed"), 0.025f);
        yield return new WaitForSeconds(0.3f);
        term.Blank();

        yield return term.Slow(term.G("welcome back, SIGNAL."));
        yield return term.Slow(term.W("OPERATION ECHO is real. four nodes stand between you and the truth."));
        term.Blank();

        yield return term.TypeLine(term.D("contacts online: ") +
            term.G("GHOST") + term.D("  ") +
            term.W("MAVEN") + term.D("  ") +
            term.E("ATLAS"));
        yield return term.TypeLine(
            term.D("starting favours: ") + term.I("3") +
            term.D("  |  type ") + term.C("help") +
            term.D(" to begin, ") + term.C("missions") +
            term.D(" for objectives."));
        term.Blank();

        // Network map
        term.Print(term.I("// network map:"));
        term.Print(term.NodeMap());
        term.Blank();

        // First mission briefing
        yield return missions.ShowBriefing(0);

        state.AcceptInput = true;
    }
}
