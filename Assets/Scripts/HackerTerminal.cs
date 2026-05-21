using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class HackerTerminal : MonoBehaviour
{
    private GameState state;
    private Terminal term;
    private CommandRouter router;
    private NodeSystem nodes;
    private FileSystem files;
    private PuzzleSystem puzzles;
    private ContactSystem contacts;
    private MissionSystem missions;
    private EndingSystem endings;
    private BootSequence boot;
    private SettingsSystem settings;
    private VisualEffects vfx;
    private WorldEvents worldEvents;
    
    void Awake()
    {
        state = new GameState();

        term = gameObject.AddComponent<Terminal>();
        router = gameObject.AddComponent<CommandRouter>();
        nodes = gameObject.AddComponent<NodeSystem>();
        files = gameObject.AddComponent<FileSystem>();
        puzzles = gameObject.AddComponent<PuzzleSystem>();
        contacts = gameObject.AddComponent<ContactSystem>();
        missions = gameObject.AddComponent<MissionSystem>();
        endings = gameObject.AddComponent<EndingSystem>();
        boot = gameObject.AddComponent<BootSequence>();
        settings = gameObject.AddComponent<SettingsSystem>();
        vfx = gameObject.AddComponent<VisualEffects>();
        worldEvents = gameObject.AddComponent<WorldEvents>();

        term.State = state;
        router.State = state;
        nodes.State = state;
        files.State = state;
        puzzles.State = state;
        contacts.State = state;
        missions.State = state;
        endings.State = state;
        boot.State = state;
        settings.State = state;
        vfx.State = state;
        worldEvents.State = state;

        router.Term = term;
        nodes.Term = term;
        files.Term = term;
        puzzles.Term = term;
        contacts.Term = term;
        missions.Term = term;
        endings.Term = term;
        boot.Term = term;
        settings.Term = term;
        worldEvents.Term = term;

        term.Settings = settings;

        term.Router = router;
        router.Nodes = nodes;
        router.Files = files;
        router.Puzzles = puzzles;
        router.Contacts = contacts;
        router.Missions = missions;
        router.Endings = endings;
        router.Boot = boot;
        router.Settings = settings;
        nodes.Endings = endings;
        nodes.VFX = vfx;
        files.Nodes = nodes;
        puzzles.Nodes = nodes;
        contacts.Nodes = nodes;
        contacts.Events = worldEvents;
        endings.Missions = missions;
        boot.Missions = missions;
        boot.Settings = settings;
        boot.VFX = vfx;
        boot.Events = worldEvents;
        worldEvents.VFX = vfx;
        vfx.Settings = settings;

        term.BuildUI();

        vfx.Init(term.MainCanvas);

        boot.OnGameStart = OnGameStarted;
    }
    
    void Start()
    {
        Keyboard.current.onTextInput += term.OnCharTyped;
        StartCoroutine(boot.Run());
    }

    void OnDestroy()
    {
        if (Keyboard.current != null)
            Keyboard.current.onTextInput -= term.OnCharTyped;
    }
    
    void Update()
    {
        term.HandleInput();
        if (router.GameStarted)
        {
            nodes.Tick();
            worldEvents.Tick();
        }
    }
    
    void OnGameStarted()
    {
        router.GameStarted = true;
        worldEvents.StartEvents();
        StartCoroutine(RunGame());
    }

    IEnumerator RunGame()
    {
        term.Print(term.I("// network map:"));
        term.Print(term.NodeMap());
        term.Blank();

        yield return term.Slow(term.G("welcome back, SIGNAL."));
        yield return term.Slow(term.W("OPERATION ECHO is real. four nodes stand between you and the truth."));
        term.Blank();

        yield return term.TypeLine(
            term.D("contacts: ") +
            term.G("GHOST") + term.D("  ") +
            term.W("MAVEN") + term.D("  ") +
            term.E("ATLAS")
            );
        yield return term.TypeLine(
            term.D("starting favours: ") + term.I("3") +
            term.D("  |  type ") + term.C("help") +
            term.D(" or ") + term.C("missions") +
            term.D(" to begin.")
            );
        term.Blank();

        yield return missions.ShowBriefing(0);

        state.AcceptInput = true;
    }
}
