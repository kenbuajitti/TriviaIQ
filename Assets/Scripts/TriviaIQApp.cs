using System;
using System.Collections.Generic;
using System.Text;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

[Serializable] public class TriviaPack
{
    public string id, title, edition, difficulty, factScope;
    public int hintIntervalSeconds = 30;
    public TriviaQuestion[] puzzles;
}
[Serializable] public class TriviaQuestion
{
    public string id, answer;
    public string[] aliases, hints, sources;
}

// Packs are content, not code: add JSON TextAssets under Resources/TriviaPacks.
public class TriviaIQApp : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] static extern void TriviaOpenGames();
#endif
    readonly List<TriviaPack> packs = new List<TriviaPack>();
    readonly List<int> visible = new List<int>();
    readonly List<TMP_Text> hints = new List<TMP_Text>();
    static string selectedPack;
    TriviaPack pack;
    RectTransform root;
    CanvasScaler scaler;
    TMP_Text clock, counter, feedback, record, filterLabel, startLabel;
    TMP_InputField answer;
    Button start, submit, reveal, previous, next, filter;
    bool running, finished, onlyUnsolved, gameplay;
    int position, shown, wrong, lastWidth, lastHeight;
    double startedAt;
    float finalTime;
    TriviaQuestion Current => visible.Count == 0 ? null : pack.puzzles[visible[position]];
    float Elapsed => running ? (float)(Time.realtimeSinceStartupAsDouble - startedAt) : finalTime;
    string Key(TriviaQuestion q) => "TriviaIQ." + pack.id + "." + q.id;
    bool Solved(TriviaQuestion q) => PlayerPrefs.GetInt(Key(q) + ".solved", 0) == 1;
    static Color Ink => new Color(.025f, .075f, .09f);
    static Color Gold => new Color(1f, .79f, .35f);

    public void Initialize(bool isGame)
    {
        gameplay = isGame;
        foreach (var asset in Resources.LoadAll<TextAsset>("TriviaPacks"))
        {
            try
            {
                var candidate = JsonUtility.FromJson<TriviaPack>(asset.text);
                Validate(candidate);
                packs.Add(candidate);
            }
            catch (Exception ex) { Debug.LogError("Invalid trivia pack " + asset.name + ": " + ex.Message); }
        }
        packs.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
        BuildCanvas();
        if (packs.Count == 0) { Label(root, "No valid trivia packs found.", .1f, .2f, .8f, .5f, 32); return; }
        pack = packs.Find(p => p.id == selectedPack) ?? packs[0];
        selectedPack = pack.id;
        if (gameplay) BuildGame(); else BuildMenu();
    }
    static void Validate(TriviaPack p)
    {
        if (p == null || string.IsNullOrEmpty(p.id) || string.IsNullOrEmpty(p.title) || p.hintIntervalSeconds < 1 || p.puzzles == null || p.puzzles.Length == 0)
            throw new Exception("Missing pack identity, interval or puzzles.");
        var ids = new HashSet<string>();
        foreach (var q in p.puzzles)
        {
            if (q == null || string.IsNullOrEmpty(q.id) || !ids.Add(q.id) || string.IsNullOrEmpty(q.answer) || q.hints == null || q.hints.Length != 4)
                throw new Exception("Each puzzle needs a unique ID, answer and exactly four hints.");
            foreach (string h in q.hints) if (string.IsNullOrWhiteSpace(h)) throw new Exception("Empty hint.");
        }
    }
    void BuildCanvas()
    {
        // Screen-space overlay UI still needs a rendering camera to suppress
        // Unity's Game view "No cameras rendering" overlay in both scenes.
        if (Camera.allCamerasCount == 0)
        {
            var cameraObject = new GameObject("TriviaIQ Camera", typeof(Camera));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0, 0, -10);
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.025f, .14f, .13f);
            camera.orthographic = true;
            camera.cullingMask = 0; // The overlay canvas renders independently.
            camera.targetDisplay = 0;
            camera.enabled = true;
        }
        var go = new GameObject("TriviaIQ Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root = go.GetComponent<RectTransform>();
        go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        Resize();
        if (EventSystem.current == null)
        {
            var events = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            events.AddComponent<InputSystemUIInputModule>();
#else
            events.AddComponent<StandaloneInputModule>();
#endif
        }
        if (gameplay)
        {
            // Gameplay keeps the football-field treatment. The menu uses the
            // responsive cover art instead, matching the other IQ games.
            Panel(root, "Field", 0, 0, 1, 1, new Color(.025f, .14f, .13f));
            for (int i = 1; i < 10; i++) Panel(root, "Yard line", i / 10f, 0, .001f, 1, new Color(1, 1, 1, .035f));
        }
        else
        {
            go.AddComponent<IQResponsiveMenuBackground>();
        }

        if (FindFirstObjectByType<AudioListener>() == null) gameObject.AddComponent<AudioListener>();
        IQMusic.GetPlayer();
        if (gameplay)
        {
            var sound = MakeButton(root, "SOUND", .79f, .025f, .17f, .045f, () => IQMusic.GetPlayer().ToggleSound());
            // Keep the clickable area, but render only the speaker icon.
            sound.GetComponent<Image>().color = Color.clear;
            sound.transition = Selectable.Transition.None;
            var icon = Rect(sound.transform, "Speaker", .36f, .15f, .28f, .7f);
            var speaker = icon.gameObject.AddComponent<IQSpeakerGraphic>();
            speaker.color = Color.white;
            speaker.IsOn = IQMusic.GetPlayer().SoundEnabled;
            speaker.raycastTarget = false;
            sound.GetComponentInChildren<TMP_Text>().text = "";
            sound.onClick.AddListener(() => { speaker.IsOn = IQMusic.GetPlayer().SoundEnabled; speaker.SetVerticesDirty(); });
        }
    }
    void Resize()
    {
        lastWidth = Screen.width; lastHeight = Screen.height;
        // A wide layout and a tall layout share proportional rows without cropping.
        scaler.referenceResolution = Screen.width >= Screen.height ? new Vector2(1100, 800) : new Vector2(720, 1080);
        scaler.matchWidthOrHeight = .5f;
    }
    void BuildMenu()
    {
        // Keep the cover itself clean: only the three standard IQ Games actions.
        MakeButton(root, "PLAY", .2f, .61f, .6f, .075f, () => SceneManager.LoadScene("TspGameScene"));
        MakeButton(root, "HOW TO PLAY", .2f, .705f, .6f, .075f, ShowHelp);
        MakeButton(root, "OTHER IQ GAMES", .2f, .80f, .6f, .075f, OpenAllGames);
    }
    void OpenAllGames()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        TriviaOpenGames();
#else
        Application.OpenURL("https://iqgamesonline.com/?iqreturn=1");
#endif
    }
    void ShowHelp()
    {
        var overlay = Panel(root, "How to play", .04f, .10f, .92f, .80f, Ink);
        Label(overlay, "HOW TO PLAY", .06f, .04f, .88f, .08f, 38, Gold);
        Label(overlay,
            "Press START to see the first clue and begin the clock.\n\nA new clue appears every " + pack.hintIntervalSeconds + " seconds. Earlier clues stay visible.\n\nType a full name or surname, then press GUESS or Enter. Capitalization and punctuation do not matter.\n\nWrong guesses add 10 seconds to your result. Hints still arrive on the real-time clock.\n\nREVEAL ends the attempt without a solved record. Your best successful time is saved on this device/browser.\n\nUse PREV / NEXT to browse. All puzzles / Unsolved filters the pack. Replays are practice; the first clue always starts hidden.\n\n" + pack.factScope,
            .06f, .15f, .88f, .68f, 25);
        MakeButton(overlay, "BACK", .25f, .88f, .5f, .075f, () => Destroy(overlay.gameObject));
    }
    void BuildGame()
    {
        Label(root, "TRIVIAIQ  /  " + pack.title, .04f, .09f, .92f, .055f, 34, Color.white);
        Label(root, pack.edition + "  •  " + pack.difficulty, .04f, .148f, .92f, .035f, 20, Gold);
        previous = MakeButton(root, "PREV", .04f, .20f, .18f, .05f, () => Move(-1));
        counter = Label(root, "", .24f, .20f, .22f, .05f, 20, Color.white, TextAlignmentOptions.Center);
        next = MakeButton(root, "NEXT", .48f, .20f, .18f, .05f, () => Move(1));
        filter = MakeButton(root, "ALL PUZZLES", .69f, .20f, .27f, .05f, ToggleFilter);
        filterLabel = filter.GetComponentInChildren<TMP_Text>();
        clock = Label(root, "", .04f, .27f, .92f, .06f, 26, Gold, TextAlignmentOptions.Center);
        for (int i = 0; i < 4; i++)
        {
            var card = Panel(root, "Clue " + (i + 1), .04f, .35f + i * .09f, .92f, .08f, new Color(.07f, .23f, .22f));
            hints.Add(Label(card, "", .025f, .08f, .95f, .84f, 27));
        }
        var inputRoot = Panel(root, "Your guess", .04f, .725f, .66f, .06f, Color.white);
        var viewport = Panel(inputRoot, "Viewport", .025f, .05f, .95f, .90f, Color.clear);
        viewport.gameObject.AddComponent<RectMask2D>();
        var value = Label(viewport, "", 0, 0, 1, 1, 26, Ink);
        var placeholder = Label(viewport, "Full name or surname", 0, 0, 1, 1, 24, new Color(.4f, .45f, .45f));
        answer = inputRoot.gameObject.AddComponent<TMP_InputField>();
        answer.textViewport = viewport; answer.textComponent = value; answer.placeholder = placeholder;
        answer.targetGraphic = inputRoot.GetComponent<Image>(); answer.characterLimit = 80;
        answer.lineType = TMP_InputField.LineType.SingleLine;
        answer.onSubmit.AddListener(_ => Guess());
        submit = MakeButton(root, "GUESS", .73f, .725f, .23f, .06f, Guess);
        feedback = Label(root, "", .04f, .795f, .92f, .065f, 24, Gold, TextAlignmentOptions.Center);
        start = MakeButton(root, "START", .04f, .875f, .28f, .055f, StartPuzzle);
        startLabel = start.GetComponentInChildren<TMP_Text>();
        reveal = MakeButton(root, "REVEAL", .36f, .875f, .28f, .055f, Reveal);
        MakeButton(root, "MENU", .68f, .875f, .28f, .055f, () => SceneManager.LoadScene("TspMenuScene"));
        record = Label(root, "", .04f, .945f, .92f, .03f, 18, Color.white, TextAlignmentOptions.Center);
        Rebuild(); ShowPuzzle();
    }
    void Rebuild()
    {
        visible.Clear();
        for (int i = 0; i < pack.puzzles.Length; i++) if (!onlyUnsolved || !Solved(pack.puzzles[i])) visible.Add(i);
        position = Mathf.Clamp(position, 0, Mathf.Max(0, visible.Count - 1));
    }
    void ToggleFilter() { if (running) return; onlyUnsolved = !onlyUnsolved; position = 0; Rebuild(); ShowPuzzle(); }
    void Move(int direction)
    {
        if (running) return;
        var old = Current;
        int oldPosition = position;
        Rebuild();
        int retained = old == null ? -1 : visible.IndexOf(Array.IndexOf(pack.puzzles, old));
        if (visible.Count > 0) position = retained >= 0 ? (retained + direction + visible.Count) % visible.Count
            : (oldPosition + (direction < 0 ? -1 : 0) + visible.Count) % visible.Count;
        ShowPuzzle();
    }
    void ShowPuzzle()
    {
        running = finished = false; shown = wrong = 0; finalTime = 0;
        answer.text = "";
        bool exists = Current != null;
        counter.text = exists ? (position + 1) + " / " + visible.Count : "0 / 0";
        filterLabel.text = onlyUnsolved ? "UNSOLVED" : "ALL PUZZLES";
        feedback.text = exists ? "Who is this quarterback? Press START." : "Pack complete! Select ALL PUZZLES to replay.";
        record.text = exists && Solved(Current) ? "Solved  •  Best adjusted time: " + Format(PlayerPrefs.GetFloat(Key(Current) + ".best")) : "Best times are saved on this device/browser.";
        startLabel.text = "START";
        UpdateClues(); UpdateControls(); UpdateClock();
    }
    void StartPuzzle()
    {
        if (running || Current == null) return;
        wrong = 0; shown = 1; finalTime = 0; finished = false; answer.text = "";
        startedAt = Time.realtimeSinceStartupAsDouble; running = true;
        feedback.text = "Type your guess whenever you are ready.";
        UpdateClues(); UpdateControls(); answer.ActivateInputField();
    }
    void Update()
    {
        if (scaler != null && (lastWidth != Screen.width || lastHeight != Screen.height)) Resize();
        if (!gameplay || clock == null || Current == null || !running) return;
        int count = Mathf.Min(4, 1 + Mathf.FloorToInt(Elapsed / pack.hintIntervalSeconds));
        if (count != shown) { shown = count; UpdateClues(); }
        UpdateClock();
    }
    public static string Normalize(string input)
    {
        var b = new StringBuilder();
        foreach (char c in (input ?? "").Normalize(NormalizationForm.FormD)) if (char.IsLetterOrDigit(c)) b.Append(char.ToLowerInvariant(c));
        return b.ToString();
    }
    public static bool Matches(TriviaQuestion q, string input)
    {
        string n = Normalize(input);
        if (n.Length == 0) return false;
        if (n == Normalize(q.answer)) return true;
        if (q.aliases != null) foreach (string a in q.aliases) if (n == Normalize(a)) return true;
        return false;
    }
    void Guess()
    {
        if (!running || Current == null) return;
        if (string.IsNullOrEmpty(Normalize(answer.text))) { feedback.text = "Enter a name first."; return; }
        if (Matches(Current, answer.text))
        {
            StopClock();
            float adjusted = finalTime + wrong * 10;
            string key = Key(Current);
            float best = PlayerPrefs.GetFloat(key + ".best", float.MaxValue);
            if (adjusted < best) PlayerPrefs.SetFloat(key + ".best", adjusted);
            PlayerPrefs.SetInt(key + ".solved", 1); PlayerPrefs.Save();
            feedback.text = "Correct! " + Current.answer + "  •  " + Format(adjusted) + " adjusted";
            record.text = "Best: " + Format(Mathf.Min(best, adjusted)) + "  •  " + wrong + " wrong guesses (+" + wrong * 10 + "s)";
            shown = 4; UpdateClues();
        }
        else
        {
            wrong++; feedback.text = "Not quite. +10 seconds to your result. Try again.";
            answer.text = ""; answer.ActivateInputField(); UpdateClock();
        }
    }
    void Reveal()
    {
        if (!running || Current == null) return;
        StopClock(); shown = 4; UpdateClues();
        feedback.text = Current.answer + "  •  Revealed; this attempt is not scored.";
        record.text = "Use NEXT for another puzzle, or REPLAY to practise.";
    }
    void StopClock() { finalTime = Elapsed; running = false; finished = true; startLabel.text = "REPLAY"; UpdateControls(); UpdateClock(); }
    void UpdateControls()
    {
        bool exists = Current != null;
        start.interactable = exists && !running;
        submit.interactable = reveal.interactable = answer.interactable = exists && running;
        previous.interactable = next.interactable = !running && exists;
        filter.interactable = !running;
    }
    void UpdateClues()
    {
        for (int i = 0; i < hints.Count; i++) hints[i].text = Current != null && i < shown ? (i + 1) + ". " + Current.hints[i]
            : "CLUE " + (i + 1) + "  /  " + (i == 0 ? "Available at START" : "Unlocks at " + i * pack.hintIntervalSeconds + " seconds");
    }
    void UpdateClock()
    {
        clock.text = "TIME " + Format(Elapsed) + "  |  PENALTY +" + wrong * 10 + "s";
        if (running) clock.text += shown < 4 ? "  |  NEXT CLUE " + Mathf.CeilToInt(shown * pack.hintIntervalSeconds - Elapsed) + "s" : "  |  ALL CLUES OPEN";
        else if (finished) clock.text += "  |  FINISHED";
    }
    static string Format(float seconds) => Mathf.FloorToInt(seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00.0");
    static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
        var r = go.GetComponent<RectTransform>(); r.anchorMin = new Vector2(x, 1 - y - h); r.anchorMax = new Vector2(x + w, 1 - y);
        r.offsetMin = r.offsetMax = Vector2.zero; return r;
    }
    static RectTransform Panel(Transform parent, string name, float x, float y, float w, float h, Color color)
    { var r = Rect(parent, name, x, y, w, h); r.gameObject.AddComponent<Image>().color = color; return r; }
    static TMP_Text Label(Transform parent, string text, float x, float y, float w, float h, float size, Color? color = null, TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
    {
        var r = Rect(parent, "Label", x, y, w, h); var t = r.gameObject.AddComponent<TextMeshProUGUI>();
        t.font = TMP_Settings.defaultFontAsset; t.text = text; t.color = color ?? Color.white;
        t.fontSize = size; t.enableAutoSizing = true; t.fontSizeMin = 12; t.fontSizeMax = size;
        t.alignment = alignment; t.raycastTarget = false; t.richText = false; return t;
    }
    static Button MakeButton(Transform parent, string text, float x, float y, float w, float h, UnityAction action)
    {
        var r = Panel(parent, text, x, y, w, h, Color.white); var b = r.gameObject.AddComponent<Button>(); b.targetGraphic = r.GetComponent<Image>();
        b.onClick.AddListener(action); Label(r, text, .04f, .06f, .92f, .88f, 26, Ink, TextAlignmentOptions.Center); return b;
    }
}
