using UnityEngine;

public class SettingsSystem : MonoBehaviour
{
    [HideInInspector] public GameState State;
    [HideInInspector] public Terminal  Term;

    private const string KEY_TEXTSPEED = "textspeed";
    private const string KEY_SCANLINES = "scanlines";
    private const string KEY_FLICKER = "flicker";

    public float TypeDelay  { get; private set; } = 0.018f;
    public bool  Scanlines  { get; private set; } = true;
    public bool  Flicker    { get; private set; } = true;

    public static readonly float[] SpeedPresets = { 0.04f, 0.018f, 0.008f, 0f };
    public static readonly string[] SpeedNames  = { "slow", "normal", "fast", "instant" };
    private int speedIndex = 1;

    void Awake() => Load();
    
    public void Load()
    {
        speedIndex = PlayerPrefs.GetInt(KEY_TEXTSPEED, 1);
        speedIndex = Mathf.Clamp(speedIndex, 0, SpeedPresets.Length - 1);
        TypeDelay  = SpeedPresets[speedIndex];
        Scanlines  = PlayerPrefs.GetInt(KEY_SCANLINES, 1) == 1;
        Flicker    = PlayerPrefs.GetInt(KEY_FLICKER,   1) == 1;
    }

    void Save()
    {
        PlayerPrefs.SetInt(KEY_TEXTSPEED, speedIndex);
        PlayerPrefs.SetInt(KEY_SCANLINES, Scanlines ? 1 : 0);
        PlayerPrefs.SetInt(KEY_FLICKER,   Flicker   ? 1 : 0);
        PlayerPrefs.Save();
    }
    
    public System.Collections.IEnumerator ShowSettings()
    {
        yield return Term.TypeLine(Term.I("-- settings --"));
        yield return Term.TypeLine(Term.C("  textspeed ") + Term.D($"current: ") + Term.W(SpeedNames[speedIndex]));
        yield return Term.TypeLine(Term.D("    set textspeed [slow|normal|fast|instant]"));
        yield return Term.TypeLine(Term.C("  scanlines ") + Term.D($"current: ") + (Scanlines ? Term.G("on") : Term.D("off")));
        yield return Term.TypeLine(Term.D("    set scanlines [on|off]"));
        yield return Term.TypeLine(Term.C("  flicker   ") + Term.D($"current: ") + (Flicker ? Term.G("on") : Term.D("off")));
        yield return Term.TypeLine(Term.D("    set flicker [on|off]"));
        yield return Term.TypeLine(Term.D("  settings are saved automatically."));
    }

    public System.Collections.IEnumerator ApplySetting(string key, string value)
    {
        switch (key)
        {
            case "textspeed":
                int idx = System.Array.IndexOf(SpeedNames, value);
                if (idx < 0)
                {
                    yield return Term.TypeLine(Term.E($"unknown speed: {value}. options: slow, normal, fast, instant"));
                    yield break;
                }
                speedIndex = idx;
                TypeDelay  = SpeedPresets[idx];
                yield return Term.TypeLine(Term.G($"text speed set to: {SpeedNames[idx]}"));
                break;

            case "scanlines":
                if (value != "on" && value != "off")
                { yield return Term.TypeLine(Term.E("options: on, off")); yield break; }
                Scanlines = value == "on";
                yield return Term.TypeLine(Term.G($"scanlines: {value}"));
                break;

            case "flicker":
                if (value != "on" && value != "off")
                { yield return Term.TypeLine(Term.E("options: on, off")); yield break; }
                Flicker = value == "on";
                yield return Term.TypeLine(Term.G($"flicker: {value}"));
                break;

            default:
                yield return Term.TypeLine(Term.E($"unknown setting: {key}"));
                yield return Term.TypeLine(Term.D("available: textspeed, scanlines, flicker"));
                yield break;
        }
        Save();
        var vfx = GetComponent<VisualEffects>();
        if (vfx != null) vfx.ApplySettings(Scanlines, Flicker);
    }
}
