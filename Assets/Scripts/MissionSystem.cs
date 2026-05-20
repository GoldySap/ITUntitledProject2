using System.Collections;
using UnityEngine;

public class MissionSystem : MonoBehaviour
{
    [HideInInspector] public GameState State;
    [HideInInspector] public Terminal  Term;
    
    public struct Objective
    {
        public string Description;
        public System.Func<GameState, bool> IsComplete;
    }

    public struct Mission
    {
        public string Title;
        public string Briefing;
        public Objective[] Objectives;
        public int FavourReward;
        public string CompletionMessage;
    }

    public static Mission[] Missions;

    void Awake()
    {
        Missions = new Mission[]
        {
            // MISSION 1: First Contact
            new Mission
            {
                Title    = "M1: FIRST CONTACT",
                Briefing = "You've intercepted a fragmented reference to OPERATION ECHO.\n" +
                           "Get into the network and find out what it is.",
                Objectives = new Objective[]
                {
                    new Objective { Description = "Scan the network",
                        IsComplete = s => s.Trace > 0 },   // any scan raises trace
                    new Objective { Description = "Read echo_list on ARCHIVE_07",
                        IsComplete = s => s.Ev1_EchoList },
                    new Objective { Description = "Read memo_draft on ARCHIVE_07",
                        IsComplete = s => s.Read_MemoDraft },
                },
                FavourReward      = 2,
                CompletionMessage = "GHOST: good start. you know what ECHO is now. dig deeper.",
            },

            // MISSION 2: Follow the Money
            new Mission
            {
                Title    = "M2: FOLLOW THE MONEY",
                Briefing = "ECHO is funded off the books. Find the financial trail\n" +
                           "and the kill orders — they're the hard evidence.",
                Objectives = new Objective[]
                {
                    new Objective { Description = "Read budget_report on ARCHIVE_07",
                        IsComplete = s => s.Ev2_Budget },
                    new Objective { Description = "Decrypt payload_047 on ARCHIVE_07",
                        IsComplete = s => s.Ev3_Payload047 },
                    new Objective { Description = "Analyze memo_draft (find the cipher hint)",
                        IsComplete = s => s.Read_MemoDraft },
                },
                FavourReward      = 2,
                CompletionMessage = "MAVEN: now we're talking. the orders are real. get deeper in.",
            },

            // MISSION 3: Break the Gate
            new Mission
            {
                Title    = "M3: BREAK THE GATE",
                Briefing = "DEEP_12 holds the force authorisation documents.\n" +
                           "Crack the access code and get inside.",
                Objectives = new Objective[]
                {
                    new Objective { Description = "Read access_log on HUB_03 (get probe hints)",
                        IsComplete = s => s.Read_AccessLog },
                    new Objective { Description = "Unlock DEEP_12 via bruteforce probe",
                        IsComplete = s => !s.Deep12Locked },
                    new Objective { Description = "Decrypt echo_orders on DEEP_12",
                        IsComplete = s => s.Ev4_EchoOrders },
                },
                FavourReward      = 3,
                CompletionMessage = "GHOST: four pieces. one more and we have everything. push to the core.",
            },

            // MISSION 4: The Director
            new Mission
            {
                Title    = "M4: THE DIRECTOR",
                Briefing = "CORE_19 has the final document — the director's sign-off.\n" +
                           "Get the key from DEEP_12, breach the core, and decrypt it.\n" +
                           "Then leak everything.",
                Objectives = new Objective[]
                {
                    new Objective { Description = "Obtain core_key from DEEP_12",
                        IsComplete = s => s.HasCore19Key },
                    new Objective { Description = "Decrypt echo_master on CORE_19",
                        IsComplete = s => s.Ev5_EchoMaster },
                    new Objective { Description = "Leak all evidence from CORE_19",
                        IsComplete = s => s.GameOver },  // set true on win
                },
                FavourReward      = 0,
                CompletionMessage = "you did it.",
            },
        };
    }
    
    public void CheckObjectives()
    {
        if (State.CurrentMission >= Missions.Length) return;
        var mission = Missions[State.CurrentMission];

        bool allDone = true;
        foreach (var obj in mission.Objectives)
            if (!obj.IsComplete(State)) { allDone = false; break; }

        if (allDone && !State.MissionJustCompleted)
        {
            State.MissionJustCompleted = true;
            StartCoroutine(CompleteMission(State.CurrentMission));
        }
    }

    IEnumerator CompleteMission(int index)
    {
        var mission = Missions[index];
        yield return new WaitForSeconds(0.3f);
        Term.Blank();
        yield return Term.TypeLine(Term.G($">> {mission.Title} — COMPLETE"));
        yield return Term.TypeLine(Term.C(mission.CompletionMessage));
        if (mission.FavourReward > 0)
        {
            State.Favours += mission.FavourReward;
            Term.UpdateStatusBar();
            yield return Term.TypeLine(Term.I($">> reward: +{mission.FavourReward} favours"));
        }
        Term.Blank();
        State.CurrentMission++;
        State.MissionJustCompleted = false;
        if (State.CurrentMission < Missions.Length)
        {
            yield return new WaitForSeconds(0.5f);
            yield return ShowBriefing(State.CurrentMission);
        }
    }

    
    //  SHOW MISSIONS — "missions" command
    
    public IEnumerator ShowAll()
    {
        yield return Term.TypeLine(Term.I("-- missions --"));
        for (int i = 0; i < Missions.Length; i++)
        {
            var m = Missions[i];
            if (i < State.CurrentMission)
            {
                yield return Term.TypeLine(Term.G($"  [DONE] {m.Title}"));
            }
            else if (i == State.CurrentMission)
            {
                yield return Term.TypeLine(Term.W($"  [ACTIVE] {m.Title}"));
                foreach (var obj in m.Objectives)
                {
                    bool done = obj.IsComplete(State);
                    yield return Term.TypeLine(
                        (done ? Term.G("    [x] ") : Term.D("    [ ] ")) +
                        Term.C(obj.Description));
                }
                yield return Term.TypeLine(Term.I($"  reward: +{m.FavourReward} favours on completion"));
            }
            else
            {
                yield return Term.TypeLine(Term.D($"  [???] {m.Title} — locked"));
            }
        }
    }

    public IEnumerator ShowBriefing(int index)
    {
        if (index >= Missions.Length) yield break;
        var m = Missions[index];
        yield return Term.TypeLine(Term.I($"-- {m.Title} --"));
        foreach (var line in m.Briefing.Split('\n'))
            yield return Term.TypeLine(Term.C(line));
        Term.Blank();
        yield return Term.TypeLine(Term.D("type [missions] to track objectives."));
    }
}
