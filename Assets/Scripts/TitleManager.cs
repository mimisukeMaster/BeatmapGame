using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.EventSystems;

public class TitleManager : MonoBehaviour
{
    [Header("オブジェクト参照")]
    public GameObject TitleText;
    public GameObject BackgroundFrame;
    public GameObject Instruction;
    public GameObject StartPanel;
    public Transform ScrollViewContent;
    public GameObject SettingPanel;
    public ScrollRect scrollRect;
    public AudioSource TitleBGMAudioSource;
    public AudioSource TitleSEAudioSource;

    [Header("リソース設定")]
    public GameObject musicButtonPrefab;
    public Beatmap[] beatmaps;

    [Header("SE設定")]
    public AudioClip TitleBGM;
    public TextMeshProUGUI SETypeLabelText;
    public TextMeshProUGUI SEText;
    public Button SELeftButton;
    public Button SERightButton;
    public AudioClip[] SEClips;
    public string[] SENames;
    public AudioClip StartPanelSE;
    public AudioClip SettingPanelSE;
    public AudioClip SelectSE;
    public AudioClip CancelSE;

    [Header("SE音量設定")]
    public TextMeshProUGUI SEVolumeLabelText;
    public Transform SEVolumeMeterParent;

    [Header("BGM音量設定")]
    public TextMeshProUGUI BGMVolumeLabelText;
    public Transform BGMVolumeMeterParent;

    [Header("トランジション")]
    public Image TransitionPanel;
    public Image BackTransitionPanel;

    private TextMeshProUGUI instructionText;
    private float flashSpeed = 2.0f;
    private List<Button> musicButtons = new List<Button>();
    private int currentSelectionIndex = 0;
    private int currentSEIndex = 0;
    private int currentSettingRow = 0;
    private int currentSEVolume = 10;
    private int currentBGMVolume = 10;
    private Image[] seVolumeBars;
    private Image[] bgmVolumeBars;
    private Color activeColor = Color.mediumSpringGreen;
    private Color inactiveColor = new Color(1, 1, 1, 0.4f);
    private Color selectedLabelColor = Color.yellow;
    private Color normalLabelColor = Color.white;
    private static bool isFirstSceneLoad = true;

    void Start()
    {
        // 静的変数に保存されている前回の音量設定を復元する
        currentSEVolume = Mathf.RoundToInt(GameManager.SelectedSEVolume * 10f);
        currentBGMVolume = Mathf.RoundToInt(GameManager.SelectedBGMVolume * 10f);

        // 表示/非表示
        StartPanel.SetActive(false);
        SettingPanel.SetActive(false);
        TransitionPanel.gameObject.SetActive(false);

        // 音量の割り当て
        TitleBGMAudioSource.volume = currentBGMVolume / 10.0f;
        TitleSEAudioSource.volume = currentSEVolume / 10.0f;

        // BGMの再生
        TitleBGMAudioSource.clip = TitleBGM;
        TitleBGMAudioSource.Play();

        // 曲リストの生成
        GenerateMusicList();

        // 変数割り当て
        instructionText = Instruction.GetComponent<TextMeshProUGUI>();

        // ボタンにクリックイベントを登録
        SELeftButton.onClick.AddListener(() => ChangeSE(-1));
        SERightButton.onClick.AddListener(() => ChangeSE(1));

        // SE名の表示を更新
        SEText.text = SENames[currentSEIndex];

        // SE音量メータの初期化
        seVolumeBars = SEVolumeMeterParent.GetComponentsInChildren<Image>();
        UpdateSEVolumeMeter();

        // BGM音量メータの初期化
        bgmVolumeBars = BGMVolumeMeterParent.GetComponentsInChildren<Image>();
        UpdateBGMVolumeMeter();

        // ゲームシーンからの遷移の場合はトランジションアニメーション
        if (!isFirstSceneLoad) StartCoroutine(BackTransition());
        isFirstSceneLoad = false;
    }

    void Update()
    {
        // 指示文を点滅させる
        if (Instruction.activeSelf)
        {
            float alpha = Mathf.Sin(Time.time * flashSpeed) * 0.5f + 0.5f;
            instructionText.color = new Color(instructionText.color.r, instructionText.color.g, instructionText.color.b, alpha);
        }

        // スタートパネルも設定パネルも開いていない時
        if (!StartPanel.activeSelf && !SettingPanel.activeSelf)
        {
            // 背景フレームを見せる
            TitleText.SetActive(true);
            BackgroundFrame.SetActive(true);

            // Enterキーではじめる
            if (Input.GetKeyDown(KeyCode.Return))
            {
                Instruction.SetActive(false);
                StartPanel.SetActive(true);
                TitleSEAudioSource.PlayOneShot(StartPanelSE);

                // 一番上を選択状態にする（バグ回避のために時差を付ける）
                currentSelectionIndex = 0;
                scrollRect.verticalNormalizedPosition = 1f;
                Invoke(nameof(UpdateSelectionVisual), 0.1f);
            }

            // Tabキーで設定
            if (Input.GetKeyDown(KeyCode.Tab) && !StartPanel.activeSelf && !SettingPanel.activeSelf)
            {
                Instruction.SetActive(false);
                SettingPanel.SetActive(true);
                TitleSEAudioSource.PlayOneShot(SettingPanelSE);

                currentSettingRow = 0;
                UpdateSettingVisual();
            }

        }

        // スタートパネルが開いている時
        else if (StartPanel.activeSelf)
        {
            // 背景フレームを見せない
            TitleText.SetActive(false);
            BackgroundFrame.SetActive(false);

            // 上移動
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                currentSelectionIndex--;
                if (currentSelectionIndex < 0) currentSelectionIndex = 0;
                TitleSEAudioSource.PlayOneShot(SelectSE);
                UpdateSelectionVisual();
            }

            // 下移動
            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            {
                currentSelectionIndex++;
                if (currentSelectionIndex >= musicButtons.Count) currentSelectionIndex = musicButtons.Count - 1;
                TitleSEAudioSource.PlayOneShot(SelectSE);
                UpdateSelectionVisual();
            }

            // 決定
            if (Input.GetKeyDown(KeyCode.Return))
            {
                // 現在選択中のボタンのクリックイベントを実行
                if (musicButtons.Count > 0)
                {
                    TitleSEAudioSource.PlayOneShot(StartPanelSE);
                    musicButtons[currentSelectionIndex].onClick.Invoke();
                }
            }

            // BackSpaceで戻る
            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                StartPanel.SetActive(false);
                Instruction.SetActive(true);
                TitleSEAudioSource.Stop();
                TitleSEAudioSource.PlayOneShot(CancelSE);
                TitleBGMAudioSource.UnPause();
            }
        }

        // 設定パネルが開いている時
        else if (SettingPanel.activeSelf)
        {
            HandleSettingInput();
        }
    }

    // 設定画面の入力をまとめた関数
    void HandleSettingInput()
    {
        // 背景フレームを見せない
        TitleText.SetActive(false);
        BackgroundFrame.SetActive(false);

        // 項目の移動
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            currentSettingRow--;
            if (currentSettingRow < 0) currentSettingRow = 0;
            UpdateSettingVisual();
            TitleSEAudioSource.PlayOneShot(SelectSE);
        }
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            currentSettingRow++;
            if (currentSettingRow > 2) currentSettingRow = 2;
            UpdateSettingVisual();
            TitleSEAudioSource.PlayOneShot(SelectSE);
        }

        // 値の変更
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (currentSettingRow == 0) // SE選択
            {
                StartCoroutine(SEButton(SELeftButton));
                ChangeSE(-1);
            }
            else if (currentSettingRow == 1) // SE音量
            {
                ChangeSEVolume(-1);
            }
            else if (currentSettingRow == 2) // BGM音量
            {
                ChangeBGMVolume(-1);
            }
        }
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (currentSettingRow == 0) // SE選択
            {
                StartCoroutine(SEButton(SERightButton));
                ChangeSE(1);
            }
            else if (currentSettingRow == 1) // SE音量
            {
                ChangeSEVolume(1);
            }
            else if (currentSettingRow == 2) // BGM音量
            {
                ChangeBGMVolume(1);
            }
        }

        // Enterキーの処理
        if (Input.GetKeyDown(KeyCode.Return))
        {
            // 一番下の項目なら戻る、それ以外なら下の項目へ
            if (currentSettingRow == 2)
            {
                StartPanel.SetActive(false);
                SettingPanel.SetActive(false);
                Instruction.SetActive(true);
                TitleSEAudioSource.PlayOneShot(CancelSE);
            }
            else
            {
                currentSettingRow++;
                UpdateSettingVisual();
                TitleSEAudioSource.PlayOneShot(SelectSE);
            }
        }

        // BackSpaceなら即戻る
        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            StartPanel.SetActive(false);
            SettingPanel.SetActive(false);
            Instruction.SetActive(true);
            TitleSEAudioSource.PlayOneShot(CancelSE);
        }
    }

    // SEを変更する関数
    public void ChangeSE(int direction)
    {
        currentSEIndex += direction;

        // 範囲外になったらループさせる
        if (currentSEIndex < 0) currentSEIndex = SEClips.Length - 1;
        if (currentSEIndex >= SEClips.Length) currentSEIndex = 0;

        // SE名の表示を更新
        SEText.text = SENames[currentSEIndex];

        // 変更したSEをプレビュー再生
        if (direction != 0) TitleSEAudioSource.PlayOneShot(SEClips[currentSEIndex]);

        // クリックのハイライト状態を削除
        EventSystem.current.SetSelectedGameObject(null);
    }

    // SEの音量を変更する関数
    public void ChangeSEVolume(int direction)
    {
        currentSEVolume += direction;
        // 0 ~ 10 の範囲に制限
        if (currentSEVolume < 0) currentSEVolume = 0;
        if (currentSEVolume > seVolumeBars.Length) currentSEVolume = seVolumeBars.Length;

        // 表示更新
        UpdateSEVolumeMeter();

        // 変更を反映する
        TitleSEAudioSource.volume = currentSEVolume / 10.0f;
        TitleSEAudioSource.PlayOneShot(SEClips[currentSEIndex]);

    }

    // BGMの音量を変更する関数
    public void ChangeBGMVolume(int direction)
    {
        currentBGMVolume += direction;
        // 0 ~ 10 の範囲に制限
        if (currentBGMVolume < 0) currentBGMVolume = 0;
        if (currentBGMVolume > bgmVolumeBars.Length) currentBGMVolume = bgmVolumeBars.Length;

        // 表示更新
        UpdateBGMVolumeMeter();

        // 変更を反映する
        TitleBGMAudioSource.volume = currentBGMVolume / 10.0f;
    }

    // SE音量メーターの見た目を更新
    void UpdateSEVolumeMeter()
    {
        for (int i = 0; i < seVolumeBars.Length; i++)
        {
            if (i < currentSEVolume) seVolumeBars[i].color = activeColor;
            else seVolumeBars[i].color = inactiveColor;
        }
    }

    // BGM音量メーターの見た目を更新
    void UpdateBGMVolumeMeter()
    {
        for (int i = 0; i < bgmVolumeBars.Length; i++)
        {
            if (i < currentBGMVolume) bgmVolumeBars[i].color = activeColor;
            else bgmVolumeBars[i].color = inactiveColor;
        }
    }

    // 選択中の行の見た目を更新
    void UpdateSettingVisual()
    {
        SETypeLabelText.color = (currentSettingRow == 0) ? selectedLabelColor : normalLabelColor;
        SEVolumeLabelText.color = (currentSettingRow == 1) ? selectedLabelColor : normalLabelColor;
        BGMVolumeLabelText.color = (currentSettingRow == 2) ? selectedLabelColor : normalLabelColor;
    }

    // ボタン押下時のアニメーション
    private IEnumerator SEButton(Button btn)
    {
        float duration = 0.1f;
        Vector3 pressedScale = new Vector3(0.8f, 0.8f, 1f);

        // 縮む
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            btn.transform.localScale = Vector3.Lerp(Vector3.one, pressedScale, elapsed / duration);
            yield return null;
        }

        // 戻る
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            btn.transform.localScale = Vector3.Lerp(pressedScale, Vector3.one, elapsed / duration);
            yield return null;
        }
        btn.transform.localScale = Vector3.one;
    }

    // 譜面リストからボタンを生成する
    void GenerateMusicList()
    {
        musicButtons.Clear();

        foreach (Beatmap beatmap in beatmaps)
        {
            // ボタンをContentの子として生成
            GameObject btnObj = Instantiate(musicButtonPrefab, ScrollViewContent);

            // 曲名を設定
            TextMeshProUGUI btnText = btnObj.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            btnText.text = beatmap.title;
            
            // 難易度の表示
            TextMeshProUGUI difficultyText = btnObj.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            string[] difficultyStrings = new string[3]{"Easy", "Normal", "Hard"}; 
            Color[] difficultyColor = new Color[3]{Color.paleGreen, Color.softYellow, Color.indianRed};

            difficultyText.text = difficultyStrings[beatmap.difficulty];
            difficultyText.color = difficultyColor[beatmap.difficulty];

            // ボタンクリック時の動作を登録しておく
            Button btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(() => OnMusicSelected(beatmap));

            // 曲選択リストに追加
            musicButtons.Add(btn);
        }
    }

    // 選択状態の見た目を更新する関数
    void UpdateSelectionVisual()
    {
        TitleBGMAudioSource.Pause();
        
        // 選択した曲を再生
        TitleSEAudioSource.clip = beatmaps[currentSelectionIndex].audioClip;
        TitleSEAudioSource.Play();

        // EventSystemにより選択状態にする
        musicButtons[currentSelectionIndex].Select();

        // スクロール処理
        AutoScrollToSelection();
    }

    // 選択項目が画面内に収まるようにスクロールさせる
    void AutoScrollToSelection()
    {
        // ターゲットとなるボタンのRectTransform
        RectTransform target = musicButtons[currentSelectionIndex].GetComponent<RectTransform>();
        RectTransform content = scrollRect.content;
        RectTransform viewport = scrollRect.viewport;

        // ビューポートの高さとコンテンツ全体の高さ
        float viewportHeight = viewport.rect.height;
        float contentHeight = content.rect.height;

        // スクロール可能な余地がない場合は何もしない
        if (contentHeight <= viewportHeight) return;

        // ターゲットの中心位置を取得
        float targetY = -target.anchoredPosition.y;

        // 現在のスクロール位置を上からの距離に変換
        float scrollRange = contentHeight - viewportHeight;
        float currentScrollY = (1f - scrollRect.verticalNormalizedPosition) * scrollRange;

        // ターゲットを表示するために必要な範囲
        float itemHalfHeight = target.rect.height / 2f;
        float targetTop = targetY - itemHalfHeight;
        float targetBottom = targetY + itemHalfHeight;

        // 画面外にはみ出ている場合修正
        if (targetTop < currentScrollY)
        {
            float newScrollY = targetTop;
            scrollRect.verticalNormalizedPosition = 1f - (newScrollY / scrollRange);
        }
        else if (targetBottom > currentScrollY + viewportHeight)
        {
            float newScrollY = targetBottom - viewportHeight;
            scrollRect.verticalNormalizedPosition = 1f - (newScrollY / scrollRange);
        }
    }

    // 曲が押されたとき
    void OnMusicSelected(Beatmap beatmap)
    {
        // 選んだ曲を静的変数に保存
        GameManager.SelectedBeatmap = beatmap;

        // 設定項目を静的変数に保存
        GameManager.SelectedSEIndex = currentSEIndex;
        GameManager.SelectedSEVolume = currentSEVolume / 10.0f;
        GameManager.SelectedBGMVolume = currentBGMVolume / 10.0f;

        // シーンのトランジションを開始
        StartCoroutine(TitleTransition());
    }

    /// <summary>
    /// ゲームシーンへのトランジション
    /// </summary>
    /// <returns></returns>
    private IEnumerator TitleTransition()
    {
        TransitionPanel.gameObject.SetActive(true);

        TransitionPanel.rectTransform.localScale = Vector3.zero;
        Transform parent = TransitionPanel.transform.parent;

        // 色の設定
        Color[] colors = new Color[]
        {
            Color.white,
            Color.orangeRed,
            Color.lightGray,
            Color.dimGray,
            Color.black
        };

        // 遅延時間の設定
        float[] delays = new float[] { 0f, 0.4f, 0.6f, 0.75f, 0.84f };

        List<Image> panels = new List<Image>();

        // パネルの生成と設定
        for (int i = 0; i < colors.Length; i++)
        {
            Image panel;
            if (i == 0) panel = TransitionPanel;
            else panel = Instantiate(TransitionPanel, parent);

            panel.color = colors[i];
            panel.rectTransform.localScale = Vector3.zero;
            panels.Add(panel);
        }

        float duration = 0.8f;
        float maxScale = 30.0f;
        float time = 0f;

        while (time < duration + delays[delays.Length - 1])
        {
            time += Time.deltaTime;

            for (int i = 0; i < panels.Count; i++)
            {
                // 進捗率を時差付きで計算
                float t = Mathf.Clamp01((time - delays[i]) / duration);

                // 4次関数でイージング
                float scale = Mathf.Pow(t, 4.0f) * maxScale;

                // サイズ適用
                panels[i].rectTransform.localScale = new Vector3(scale, scale, 1f);
            }
            yield return null;
        }

        // シーン遷移
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    /// <summary>
    /// ゲームシーンからのシーントランジション
    /// </summary>
    private IEnumerator BackTransition()
    {
        // 色の設定
        Color[] colors = new Color[]
        {
            Color.black,
            Color.seaGreen,
            Color.lawnGreen,
            Color.limeGreen,
            Color.white
        };

        // 遅延時間の設定
        float[] delays = new float[] { 0f, 0.4f, 0.6f, 0.75f, 0.84f };

        List<Image> panels = new List<Image>();
        List<Material> materials = new List<Material>();

        BackTransitionPanel.gameObject.SetActive(true);
        Transform parent = BackTransitionPanel.transform.parent;

        for (int i = 0; i < colors.Length; i++)
        {
            Image panel;
            // 0番目は既存のもの、それ以外は複製
            if (i == 0) panel = BackTransitionPanel;
            else panel = Instantiate(BackTransitionPanel, parent);

            // 奥へ送る
            panel.transform.SetAsFirstSibling();

            // マテリアルを個別に複製
            panel.material = new Material(BackTransitionPanel.material);
            panel.material.SetColor("_Color", Color.white);
            panel.material.SetFloat("_Radius", 0f); // 最初は穴なし
            panel.color = colors[i];

            panels.Add(panel);
            materials.Add(panel.material);
        }

        // 順番を正しく並べ替える
        for (int i = panels.Count - 1; i >= 0; i--)
        {
            panels[i].transform.SetAsLastSibling();
        }

        float duration = 0.8f;
        float time = 0f;

        while (time < duration + delays[delays.Length - 1])
        {
            time += Time.deltaTime;

            // 各パネルの進捗率を計算
            for (int i = 0; i < panels.Count; i++)
            {
                float t = Mathf.Clamp01((time - delays[i]) / duration);

                // イージング
                float progress = 1f - Mathf.Pow(1f - t, 4f);

                // 半径を広げる
                materials[i].SetFloat("_Radius", progress * 1.2f);
            }

            yield return null;
        }

        // 複製したパネルを削除/非表示にする
        for (int i = 1; i < panels.Count; i++)
        {
            Destroy(panels[i].gameObject);
        }
        BackTransitionPanel.gameObject.SetActive(false);
    }
}