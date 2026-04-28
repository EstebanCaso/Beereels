using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Construye toda la UI del juego en runtime. Permite previsualizar la UI
/// sin tener que armar prefabs ni canvases en el editor.
///
/// Uso:
///   1. Crea un Empty GameObject en SampleScene (o donde quieras probar).
///   2. A�ade este componente.
///   3. Press Play.
///
/// Construye:
///   - EventSystem + UIPointerEventSystem
///   - HUDCanvas (parent: centerEyeAnchor del OVRCameraRig si existe, si no Camera.main)
///   - PausePanel + GameOverPanel + WristPauseButton
///   - PauseController + GameOverUI (wired)
///   - EditorMousePointer + DesktopDebugInput (para previsualizar sin VR)
///
/// El estilo es "phone-screen vibe" minimal: panel oscuro con bordes,
/// acento neon-pink, secundario cyan, tipograf�a TMP default.
/// </summary>
[DefaultExecutionOrder(-100)]
public class RuntimeUIBootstrap : MonoBehaviour
{
    [Header("Modo")]
    [Tooltip("Si est� activado, usa el bot�n izquierdo del rat�n para clickear (sin VR).")]
    public bool enableDesktopPreview = true;

    [Header("Estilo")]
    public Color panelColor = new Color(0.054f, 0.054f, 0.078f, 0.92f); // #0E0E14 ~92%
    public Color accentColor = new Color(1f, 0.176f, 0.482f, 1f);        // #FF2D7B neon pink
    public Color accentColorHover = new Color(0.133f, 0.827f, 0.933f, 1f); // #22D3EE cyan
    public Color textColor = new Color(0.96f, 0.96f, 0.98f, 1f);

    private Transform _headFollow;
    private Camera _camera;

    void Awake()
    {
        ResolveHeadAndCamera();
        BuildEventSystem();

        GameObject hud = BuildHUD();
        GameObject pausePanel = BuildPausePanel();
        GameObject gameOverPanel = BuildGameOverPanel();
        GameObject wristBtn = BuildWristPauseButton();

        // Pause controller en este mismo GameObject.
        PauseController pause = gameObject.AddComponent<PauseController>();
        pause.pausePanel = pausePanel;
        pause.hudToHide = hud;

        // GameOverUI vive en el bootstrap (no en el panel) para que Start
        // se ejecute al cargar la escena y se suscriba a onGameOver.
        // Si lo poni�ramos en el panel inactivo, Start quedar�a diferido
        // hasta que ShowGameOver activara el panel y entonces Start lo
        // volver�a a desactivar (flash de un frame).
        GameOverUI gameOver = gameObject.AddComponent<GameOverUI>();
        gameOver.gameOverPanel = gameOverPanel;
        gameOver.scoreText = gameOverPanel.transform.Find("Score").GetComponent<TextMeshProUGUI>();
        gameOver.timeText  = gameOverPanel.transform.Find("Time").GetComponent<TextMeshProUGUI>();

        // Bot�n de mu�eca llama a Toggle.
        wristBtn.GetComponentInChildren<Button>().onClick.AddListener(pause.Toggle);

        if (enableDesktopPreview)
        {
            gameObject.AddComponent<EditorMousePointer>();
            gameObject.AddComponent<DesktopDebugInput>();
        }
    }

    private void ResolveHeadAndCamera()
    {
        OVRCameraRig rig = FindObjectOfType<OVRCameraRig>();
        if (rig != null && rig.centerEyeAnchor != null)
        {
            _headFollow = rig.centerEyeAnchor;
            _camera = rig.centerEyeAnchor.GetComponent<Camera>();
        }
        if (_camera == null) _camera = Camera.main;
        if (_camera == null)
        {
            Camera[] all = Camera.allCameras;
            if (all.Length > 0) _camera = all[0];
        }
        if (_headFollow == null && _camera != null) _headFollow = _camera.transform;
    }

    // -------- HUD --------
    private GameObject BuildHUD()
    {
        GameObject canvasGo = MakeWorldCanvas("HUDCanvas", new Vector2(400, 120), 0.001f);
        if (_headFollow != null)
        {
            canvasGo.transform.SetParent(_headFollow, false);
            canvasGo.transform.localPosition = new Vector3(0f, -0.18f, 0.6f);
            canvasGo.transform.localRotation = Quaternion.identity;
        }

        TextMeshProUGUI score = MakeText(canvasGo.transform, "Score", "0", 80, TextAlignmentOptions.Center);
        RectTransform srt = score.rectTransform;
        srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one;
        srt.offsetMin = Vector2.zero; srt.offsetMax = Vector2.zero;

        HUDController hud = canvasGo.AddComponent<HUDController>();
        hud.scoreText = score;
        return canvasGo;
    }

    // -------- Pause panel --------
    private GameObject BuildPausePanel()
    {
        GameObject canvasGo = MakeWorldCanvas("PausePanel", new Vector2(700, 600), 0.0015f);
        PlaceInFrontOfCamera(canvasGo.transform, 1.5f, 0f);

        AddBackground(canvasGo.transform, panelColor);
        AddTitle(canvasGo.transform, "PAUSED");

        Button resume   = AddButton(canvasGo.transform, "Resume",     0,   -40);
        Button restart  = AddButton(canvasGo.transform, "Restart",    0,  -150);
        Button mainMenu = AddButton(canvasGo.transform, "Main Menu",  0,  -260);

        // Wiring se hace en Awake() despu�s, conectando a PauseController.
        // Aqu� solo a�adimos los listeners en una segunda pasada:
        StartCoroutine(WirePauseButtons(resume, restart, mainMenu));

        canvasGo.SetActive(false);
        return canvasGo;
    }

    private System.Collections.IEnumerator WirePauseButtons(Button resume, Button restart, Button mainMenu)
    {
        yield return null;
        PauseController pc = PauseController.Instance;
        if (pc == null) { Debug.LogWarning("[RuntimeUIBootstrap] PauseController missing."); yield break; }
        resume.onClick.AddListener(pc.Resume);
        restart.onClick.AddListener(pc.RestartRun);
        mainMenu.onClick.AddListener(pc.BackToMainMenu);
    }

    // -------- GameOver panel --------
    private GameObject BuildGameOverPanel()
    {
        GameObject canvasGo = MakeWorldCanvas("GameOverPanel", new Vector2(700, 700), 0.0015f);
        PlaceInFrontOfCamera(canvasGo.transform, 1.5f, 0f);

        AddBackground(canvasGo.transform, panelColor);
        AddTitle(canvasGo.transform, "GAME OVER");

        TextMeshProUGUI score = MakeText(canvasGo.transform, "Score", "Score: 0", 60, TextAlignmentOptions.Center);
        PositionRect(score.rectTransform, 0, 80, 600, 70);

        TextMeshProUGUI time = MakeText(canvasGo.transform, "Time", "Tiempo: 0.0s", 50, TextAlignmentOptions.Center);
        PositionRect(time.rectTransform, 0, 0, 600, 60);

        Button restart  = AddButton(canvasGo.transform, "Restart",   0, -130);
        Button mainMenu = AddButton(canvasGo.transform, "Main Menu", 0, -240);

        StartCoroutine(WireGameOverButtons(restart, mainMenu));

        canvasGo.SetActive(false);
        return canvasGo;
    }

    private System.Collections.IEnumerator WireGameOverButtons(Button restart, Button mainMenu)
    {
        yield return null;
        PauseController pc = PauseController.Instance;
        if (pc == null) yield break;
        restart.onClick.AddListener(pc.RestartRun);
        mainMenu.onClick.AddListener(pc.BackToMainMenu);
    }

    // -------- Wrist button --------
    private GameObject BuildWristPauseButton()
    {
        GameObject canvasGo = MakeWorldCanvas("WristPauseButton", new Vector2(120, 60), 0.0008f);
        // Anclar a LeftHandAnchor si existe.
        OVRCameraRig rig = FindObjectOfType<OVRCameraRig>();
        if (rig != null && rig.leftHandAnchor != null)
        {
            canvasGo.transform.SetParent(rig.leftHandAnchor, false);
            canvasGo.transform.localPosition = new Vector3(0f, 0.04f, -0.05f);
            canvasGo.transform.localRotation = Quaternion.Euler(60f, 0f, 0f);
        }
        else if (_headFollow != null)
        {
            // Fallback en escena flat: lo mostramos en una esquina.
            canvasGo.transform.SetParent(_headFollow, false);
            canvasGo.transform.localPosition = new Vector3(-0.35f, -0.22f, 0.7f);
            canvasGo.transform.localRotation = Quaternion.identity;
        }

        AddBackground(canvasGo.transform, new Color(panelColor.r, panelColor.g, panelColor.b, 0.6f));
        Button btn = AddButton(canvasGo.transform, "Pause", 0, 0);
        // Stretch el bot�n al tama�o del panel:
        RectTransform brt = btn.GetComponent<RectTransform>();
        brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
        brt.offsetMin = Vector2.zero; brt.offsetMax = Vector2.zero;
        return canvasGo;
    }

    // -------- Helpers --------

    private void BuildEventSystem()
    {
        EventSystem existing = FindObjectOfType<EventSystem>();
        if (existing != null)
        {
            if (existing.GetComponent<UIPointerEventSystem>() == null)
                existing.gameObject.AddComponent<UIPointerEventSystem>();
            return;
        }
        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<UIPointerEventSystem>();
    }

    private GameObject MakeWorldCanvas(string name, Vector2 sizeDelta, float worldScale)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas c = go.GetComponent<Canvas>();
        c.renderMode = RenderMode.WorldSpace;
        RectTransform rt = (RectTransform)go.transform;
        rt.sizeDelta = sizeDelta;
        rt.localScale = Vector3.one * worldScale;
        return go;
    }

    private void PlaceInFrontOfCamera(Transform t, float distance, float yOffset)
    {
        if (_camera == null) return;
        Vector3 fwd = _camera.transform.forward; fwd.y = 0f; fwd.Normalize();
        if (fwd == Vector3.zero) fwd = Vector3.forward;
        Vector3 pos = _camera.transform.position + fwd * distance + Vector3.up * yOffset;
        t.position = pos;
        t.rotation = Quaternion.LookRotation(fwd, Vector3.up);
    }

    private Image AddBackground(Transform parent, Color color)
    {
        GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(parent, false);
        Image img = bg.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = true;
        RectTransform rt = (RectTransform)bg.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        // Outline accent
        Outline o = bg.AddComponent<Outline>();
        o.effectColor = accentColor;
        o.effectDistance = new Vector2(4, -4);
        return img;
    }

    private void AddTitle(Transform parent, string text)
    {
        TextMeshProUGUI title = MakeText(parent, "Title", text, 90, TextAlignmentOptions.Center);
        title.color = accentColor;
        title.fontStyle = FontStyles.Bold;
        PositionRect(title.rectTransform, 0, 200, 600, 120);
    }

    private Button AddButton(Transform parent, string label, float x, float y)
    {
        GameObject btnGo = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(parent, false);
        Image img = btnGo.GetComponent<Image>();
        img.color = accentColor;
        img.raycastTarget = true;
        Button btn = btnGo.GetComponent<Button>();
        // AddComponent at runtime no llama a Reset(), as� que targetGraphic
        // queda nulo y el ColorBlock se ignora. Lo asignamos a mano.
        btn.targetGraphic = img;
        ColorBlock cb = btn.colors;
        cb.normalColor = accentColor;
        cb.highlightedColor = accentColorHover;
        cb.pressedColor = Color.white;
        cb.selectedColor = accentColorHover;
        btn.colors = cb;

        TextMeshProUGUI lbl = MakeText(btnGo.transform, "Label", label, 40, TextAlignmentOptions.Center);
        lbl.color = textColor;
        RectTransform lrt = lbl.rectTransform;
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;

        PositionRect((RectTransform)btnGo.transform, x, y, 400, 90);
        return btn;
    }

    private TextMeshProUGUI MakeText(Transform parent, string name, string text, float fontSize, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = align;
        tmp.color = textColor;
        tmp.raycastTarget = false;
        return tmp;
    }

    private void PositionRect(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }
}
