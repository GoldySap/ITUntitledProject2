using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldEvents : MonoBehaviour
{
    [HideInInspector] public GameState State;
    [HideInInspector] public Terminal Term;
    [HideInInspector] public VisualEffects VFX;

    private float eventTimer  = 0f;
    private float nextEventIn = 0f;
    private bool active = false;

    // Agent intercept lines (heat > 50)
    private readonly string[][] agentChatter = {
        new[]{ "agent_04: signal is active somewhere on cluster 7.",
               "dispatch: confirmed. narrow it down." },
        new[]{ "agent_11: we're getting interference on NODE_07.",
               "agent_11: could be the leak. running trace." },
        new[]{ "dispatch: anyone have eyes on the deep archive?",
               "agent_04: negative. whoever it is, they're quiet." },
        new[]{ "agent_11: trace is warm. they're still connected.",
               "dispatch: keep the sweep running." },
        new[]{ "agent_04: lost the signal. they bounced off a proxy.",
               "dispatch: stand by. they'll make a mistake." },
        new[]{ "agent_04: we have a partial fingerprint.",
               "dispatch: good. pull it. do not let them upload anything." },
    };

    // High heat lines (heat > 75)
    private readonly string[][] criticalChatter = {
        new[]{ "dispatch: ALL UNITS — target is on node cluster 7.",
               "dispatch: LOCK IT DOWN." },
        new[]{ "agent_04: I have a solid trace. 90 seconds.",
               "dispatch: move." },
        new[]{ "agent_11: uploading counter-intrusion package now.",
               "dispatch: if they're still connected, terminate the session." },
    };

    private int agentIndex= 0;
    private int criticalIndex = 0;
  
    public void StartEvents() { active = true; ScheduleNext(); }
    public void StopEvents() { active = false; }

    void ScheduleNext()
    {
        float baseInterval = State.Heat > 70f ? 20f : 40f;
        nextEventIn = Random.Range(baseInterval * 0.7f, baseInterval * 1.3f);
        eventTimer  = 0f;
    }
  
    public void Tick()
    {
        if (!active || State.GameOver || !State.AcceptInput) return;
        if (State.Heat < 45f) return; 

        eventTimer += Time.deltaTime;
        if (eventTimer < nextEventIn) return;

        ScheduleNext();
        StartCoroutine(FireEvent());
    }
    public class IndexWrapper
    {
        public int Value;
        public IndexWrapper(int startValue) => Value = startValue;
    }
  
    IEnumerator FireEvent()
    {
        IndexWrapper criticalIndexWrapper = new IndexWrapper(criticalIndex);
        IndexWrapper agentIndexWrapper = new IndexWrapper(agentIndex);

        if (State.Heat >= 75f)
            yield return AgentIntercept(criticalChatter, criticalIndexWrapper, critical: true);
        else
            yield return AgentIntercept(agentChatter, agentIndexWrapper, critical: false);
    }

    IEnumerator AgentIntercept(string[][] pool, IndexWrapper indexObj, bool critical)
    {
        if (pool.Length == 0) yield break;
        string[] lines = pool[indexObj.Value % pool.Length];
        indexObj.Value++;

        Term.Blank();
        yield return Term.TypeLine(Term.D("// intercepted: GOV_NET secure channel"), 0.015f);
        foreach (var line in lines)
        {
            string col = critical ? Term.E(line) : Term.W(line);
            yield return Term.TypeLine(col, 0.02f);
        }
        yield return Term.TypeLine(Term.D("// [end of intercept]"), 0.015f);
        Term.Blank();

        if (critical && VFX != null) VFX.Flicker(2);
    }
  
    public string GhostReaction()
    {
        if (State.Heat > 75f) return "GHOST: you're pushing it. be fast.";
        if (State.Trace > 70f) return "GHOST: you're getting sloppy.";
        if (State.AtlasUseCount >= 2) return "GHOST: careful with ATLAS. they collect debts.";
        return "";
    }

    public string MavenReaction()
    {
        if (State.Heat > 70f) return "MAVEN: they're close. I can feel it.";
        if (State.AtlasUseCount >= 2) return "MAVEN: I don't like that you keep calling ATLAS.";

        return "";
    }

    public string AtlasReaction()
    {
        if (State.AtlasUseCount >= 3) return "ATLAS: you're becoming a liability, SIGNAL.";
        if (State.AtlasUseCount == 2) return "ATLAS: that's twice now. and we expect results.";
        return "";
    }
}
