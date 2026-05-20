public class GameState
{
    // Stats
    public float Trace   = 0f;   // 0-100, rises per action
    public float Heat    = 0f;   // 0-100, rises over time
    public int   Favours = 3;    // social currency for contacts
    public int   Evidence = 0;   // pieces gathered (max 5)

    // Node state
    public string ActiveNode   = "";     // currently connected node id
    public bool   Deep12Locked = true;
    public bool   Core19Locked = true;
    public bool   HasCore19Key = false;

    // Modifier flags
    public bool ProxyActive = false;  // next action costs half trace
    public bool InDarkMode  = false;  // sweeps paused, heat decaying

    // Sweep state
    public bool  SweepActive = false;
    public float SweepTimer  = 0f;
    public float HeatTimer   = 0f;

    // Puzzle state
    public string PendingCipherFile   = "";
    public string PendingCipherOutput = "";
    public bool   PendingConfirm      = false;
    public bool   CipherFirstTry      = true;
    public int    BruteProbes         = 0;
    public bool   Deep12BruteLockedOut = false;

    // Contact use
    public int AtlasUseCount = 0;

    // Evidence flags
    public bool Ev1_EchoList     = false;
    public bool Ev2_Budget       = false;
    public bool Ev3_Payload047   = false;
    public bool Ev4_EchoOrders   = false;
    public bool Ev5_EchoMaster   = false;

    // File read flags (for analyze)
    public bool Read_MemoDraft   = false;
    public bool Read_StaffChat   = false;
    public bool Read_AccessLog   = false;
    public bool Read_Budget      = false;
    public bool Read_Directive   = false;
    public bool Read_DirectorLog = false;

    // Mission state
    public int  CurrentMission       = 0;  // index into MissionSystem.Missions
    public bool MissionJustCompleted = false;

    // Game flow
    public bool AcceptInput = false;
    public bool GameOver    = false;

    // Constants
    public const float HEAT_TICK_INTERVAL = 18f;
    public const float HEAT_GAIN_BASE     = 2f;
    public const float SWEEP_INTERVAL_LOW = 45f;  // heat 50-75
    public const float SWEEP_INTERVAL_HIGH= 25f;  // heat 75+
    public const int   MAX_BRUTE_PROBES   = 6;
    public readonly int[] BruteAnswer     = { 5, 7, 9, 6 };

    // Reset
    public void Reset()
    {
        Trace = 0; Heat = 0; Favours = 3; Evidence = 0;
        ActiveNode = ""; Deep12Locked = true; Core19Locked = true; HasCore19Key = false;
        ProxyActive = false; InDarkMode = false;
        SweepActive = false; SweepTimer = 0; HeatTimer = 0;
        PendingCipherFile = ""; PendingCipherOutput = ""; PendingConfirm = false;
        CipherFirstTry = true; BruteProbes = 0; Deep12BruteLockedOut = false;
        AtlasUseCount = 0;
        Ev1_EchoList = Ev2_Budget = Ev3_Payload047 = Ev4_EchoOrders = Ev5_EchoMaster = false;
        Read_MemoDraft = Read_StaffChat = Read_AccessLog = Read_Budget = Read_Directive = Read_DirectorLog = false;
        CurrentMission = 0; MissionJustCompleted = false;
        AcceptInput = false; GameOver = false;
    }
}
