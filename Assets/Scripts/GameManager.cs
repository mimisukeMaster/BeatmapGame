using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using unityroom.Api;

public class GameManager : MonoBehaviour
{
    [Header("譜面データ")]
    public Beatmap CurrentBeatmap;

    [Header("レーン設定")]
    public float[] LaneXPositions = new float[] { -3.0f, -1.0f, 1.0f, 3.0f };

    [Header("オブジェクト参照")]
    public Image BackgroundImg;
    public AudioSource BGMSource;
    public GameObject NotePrefab;
    public TextMeshProUGUI GamingTitle;
    public TextMeshProUGUI GamingScore;
    public TextMeshProUGUI ComboText;
    public Image TransitionPanel;
    public Image BackTransitionPanel;
    public Sprite BackTransitionSprite;
    public GameObject ResultCanvas;
    public TextMeshProUGUI ResultTitle;
    public TextMeshProUGUI ResultScore;
    public TextMeshProUGUI ExcellentText;
    public TextMeshProUGUI GoodText;
    public TextMeshProUGUI BadText;
    public TextMeshProUGUI MaxComboText;
    public TextMeshProUGUI HighScoreText;
    public TextMeshProUGUI OptionText;

    [Header("エフェクト")]
    public GameObject ExcellentEffectPrefab;
    public GameObject GoodEffectPrefab;
    public GameObject[] LanePrefabs;

    [Header("ゲーム設定")]
    public float SpawnZ = 50f;       // ノーツが生成されるZ座標
    public float JudgeZ = -3f;   // 判定ラインのZ座標 

    [Header("判定設定")]
    /// <summary>
    /// 予想判定時刻からの許容時間
    /// </summary>
    public float hitTolerance = 0.15f;

    [Header("SE設定")]
    public AudioClip[] SEClips;
    public AudioClip GameStartGingle;
    public AudioClip ScoreAttributeSE;
    public AudioClip ScoreTotalSE;
    public AudioClip ResultShowedGingle;
    public AudioClip FullComboSE;
    public AudioClip HighScoreSE;
    public AudioClip BackSE;

    /// <summary>
    /// 現在のゲーム経過時間のゲッター用変数
    /// </summary>
    public double CurrentGameTime => gameTime;

    /// <summary>
    /// シーン間で選択された曲を渡す
    /// </summary>
    public static Beatmap SelectedBeatmap;

    /// <summary>
    /// シーン間で選択されたノーツSEを渡す
    /// </summary>
    public static int SelectedSEIndex = 0;

    /// <summary>
    /// シーン間で保存されたSEの音量
    /// </summary>
    public static float SelectedSEVolume = 1.0f;

    /// <summary>
    /// シーン間で保存されたBGMの音量
    /// </summary>
    public static float SelectedBGMVolume = 1.0f;

    /// <summary>
    /// シーン間で保存されるハイスコア
    /// </summary>
    private static int highScore = 0;

    /// <summary>
    /// 判定に使用するキー
    /// </summary>
    /// <value>キーボード左順での配列</value>
    private readonly KeyCode[] keys = new KeyCode[] {
        KeyCode.D, KeyCode.F, KeyCode.J, KeyCode.K
    };

    /// <summary>
    /// 各レーンのノーツを順番に管理するキュー
    /// </summary>
    private List<Queue<NoteObject>> laneQueues = new List<Queue<NoteObject>>();

    /// <summary>
    /// 各レーンで長押し中の長押しノーツを保持する配列
    /// </summary>
    private NoteObject[] holdingNotes = new NoteObject[4];

    /// <summary>
    /// 4分音符が相当するステップ数
    /// </summary>
    private int stepsPerQuarterNote;

    /// <summary>
    /// ノーツの流れる速さ（物理的距離に基づく）
    /// </summary>
    private float notesSpeed;

    /// <summary>
    /// ゲームが開始されたか
    /// </summary>
    private bool isGameStarted = false;

    /// <summary>
    /// 音楽再生が開始されたか
    /// </summary>
    private bool isMusicStarted = false;

    /// <summary>
    /// ゲームの経過時間
    /// </summary>
    private double gameTime = 0;

    /// <summary>
    /// 次に生成すべきノーツのインデックス
    /// </summary>
    private int nextNoteIndex = 0;

    /// <summary>
    /// ノーツが生成されてから判定ラインに到達するまでの時間
    /// </summary>
    private double noteTravelTimeInSeconds;

    /// <summary>
    /// ゲームプレイのスコア
    /// </summary>
    private int score = 0;

    /// <summary>
    /// スコア表示が一通り終わったか
    /// </summary>
    private bool isResultShowed = false;

    private int excellentScore = 100;
    private int goodScore = 50;
    private int longBonusScore = 30;
    private int comboBonusScore = 20;
    private int excellentNum = 0;
    private int goodNum = 0;
    private int badNum = 0;
    private int combo = 0;
    private int maxCombo = 0;

    void Start()
    {
        if (SelectedBeatmap != null)
        {
            CurrentBeatmap = SelectedBeatmap;
        }

        if (CurrentBeatmap == null)
        {
            Debug.LogError("譜面が設定されていません");
            return;
        }
        // 背景設定
        BackgroundImg.sprite = CurrentBeatmap.backgroundImg;

        // BGMの音量を設定
        BGMSource.volume = SelectedBGMVolume;

        // 音楽再生準備
        BGMSource.clip = CurrentBeatmap.audioClip;

        // 判定ラインのエフェクトを非アクティブ化
        for (int i = 0; i < LanePrefabs.Length; i++)
        {
            LanePrefabs[i].transform.GetChild(0).gameObject.SetActive(false);
        }

        // シーンのトランジション処理を開始
        StartCoroutine(GameTransition());

        // リザルト要素を非アクティブ化（タイトルとヘッダー以外）
        for (int i = 6; i < ResultCanvas.transform.childCount; i++)
        {
            ResultCanvas.transform.GetChild(i).gameObject.SetActive(false);
        }

        // リザルト画面を非アクティブ化
        ResultCanvas.SetActive(false);

        // コンボ表示を非アクティブ化
        ComboText.gameObject.SetActive(false);

        // 4レーン分のキューを初期化
        for (int i = 0; i < LaneXPositions.Length; i++)
        {
            laneQueues.Add(new Queue<NoteObject>());
            holdingNotes[i] = null;
        }

        // 4分音符のステップ数を計算
        stepsPerQuarterNote = CurrentBeatmap.stepsPerMeasure / CurrentBeatmap.beatsPerMeasure;

        // ノーツの流れる速度を取得
        notesSpeed = CurrentBeatmap.notesSpeed;

        // ノーツが判定ラインに着くまでの移動時間を計算
        float distance = SpawnZ - JudgeZ;
        noteTravelTimeInSeconds = (double)distance / notesSpeed;

        // 譜面データを時間順にソートしておく
        CurrentBeatmap.notes.Sort((a, b) => a.step.CompareTo(b.step));

        gameTime = 0;
        nextNoteIndex = 0;

        // 曲のタイトル表示を設定する
        GamingTitle.text = CurrentBeatmap.title;

        // タイトル表示後、ゲームを開始する
        Invoke(nameof(GameStart), 5.0f);
    }

    void Update()
    {
        if (isGameStarted)
        {
            // 曲が始まっていない時
            if (!isMusicStarted)
            {
                // 開始前は手動で時間を進める
                gameTime += Time.deltaTime;

                // 時間が0になったら音楽スタート
                if (gameTime >= 0)
                {
                    BGMSource.Play();
                    isMusicStarted = true;
                }
            }
            // 再生中
            else
            {
                // 現在の音楽再生時間を取得
                gameTime = BGMSource.time;

                // 曲が終わった時
                if (!BGMSource.isPlaying)
                {
                    isGameStarted = false;

                    // スコア表示コルーチンを開始
                    StartCoroutine(ResultDisplay());

                    return;
                }
            }

            // デバッグ処理 (一時的にQキーで曲が終わる)
            if (Input.GetKeyDown(KeyCode.Q))
            {
                BGMSource.Stop();
            }

            if (nextNoteIndex < CurrentBeatmap.notes.Count)
            {
                NoteData noteToSpawn = CurrentBeatmap.notes[nextNoteIndex];
                double noteHitTime = BeatmapUtility.GetTimeFromStep(CurrentBeatmap, noteToSpawn.step);
                double noteSpawnTime = noteHitTime - noteTravelTimeInSeconds;

                // 現在のゲーム時間が生成時間を超えたか
                if (gameTime >= noteSpawnTime)
                {
                    // どれだけ超えたのかを計算しSpawnNoteに渡す
                    SpawnNote(noteToSpawn, gameTime - noteSpawnTime);

                    nextNoteIndex++; // 次のノーツへ
                }
            }

            // ノーツへのキー入力
            for (int i = 0; i < keys.Length; i++)
            {
                if (Input.GetKeyDown(keys[i]))
                {
                    // 対応するレーンの判定処理を呼ぶ
                    CheckHit(i);

                    // ヒットの可否に関わらずSEを鳴らす
                    BGMSource.PlayOneShot(SEClips[SelectedSEIndex], SelectedSEVolume);
                }

                if (Input.GetKeyUp(keys[i]))
                {
                    CheckRelease(i);
                }
            }
        }
        else
        {
            // スコア表示後の操作受付
            if (isResultShowed)
            {
                // 指示文を点滅
                float alpha = Mathf.Sin(Time.time * 3.0f) * 0.5f + 0.5f;
                OptionText.color = new Color(OptionText.color.r, OptionText.color.g, OptionText.color.b, alpha);

                if (Input.GetKeyDown(KeyCode.D))
                {
                    BGMSource.PlayOneShot(BackSE);
                    StartCoroutine(BackTransition(SceneManager.GetActiveScene().buildIndex - 1));
                }
                else if (Input.GetKeyDown(KeyCode.F))
                {
                    BGMSource.PlayOneShot(BackSE);
                    StartCoroutine(BackTransition(SceneManager.GetActiveScene().buildIndex));
                }
                else if (Input.GetKeyDown(KeyCode.J))
                {
                    // スコアを登録
                    UnityroomApiClient.Instance.SendScore(1, score, ScoreboardWriteMode.HighScoreDesc);

                    // 最大コンボ数を登録
                    UnityroomApiClient.Instance.SendScore(2, maxCombo, ScoreboardWriteMode.HighScoreDesc);
                }
            }
        }
    }

    /// <summary>
    /// 遷移直後のシーントランジション
    /// </summary>
    private IEnumerator GameTransition()
    {
        // 色の設定
        Color[] colors = new Color[]
        {
            Color.black,
            Color.indigo,
            Color.magenta,
            Color.orange,
            Color.white
        };

        // 遅延時間の設定
        float[] delays = new float[] { 0f, 0.4f, 0.6f, 0.75f, 0.84f };

        List<Image> panels = new List<Image>();
        List<Material> materials = new List<Material>();

        TransitionPanel.gameObject.SetActive(true);
        Transform parent = TransitionPanel.transform.parent;

        for (int i = 0; i < colors.Length; i++)
        {
            Image panel;
            // 0番目は既存のもの、それ以外は複製
            if (i == 0) panel = TransitionPanel;
            else panel = Instantiate(TransitionPanel, parent);

            // 奥へ送る
            panel.transform.SetAsFirstSibling();

            // マテリアルを個別に複製
            panel.material = new Material(TransitionPanel.material);
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
        TransitionPanel.gameObject.SetActive(false);

        // 終了直後にスタートジングルを流す
        BGMSource.PlayOneShot(GameStartGingle);
    }

    /// <summary>
    /// ゲームが開始するInvoke関数
    /// </summary>
    private void GameStart()
    {
        isGameStarted = true;
        isMusicStarted = false;

        // 最初のノーツが判定線に達する分だけ時間を前倒しして生成を始める
        gameTime = -noteTravelTimeInSeconds;
    }

    /// <summary>
    /// ノーツを実際にシーンに生成する関数
    /// </summary>
    void SpawnNote(NoteData noteData, double timeSinceSpawn)
    {
        // ノーツが生成され始める時刻と終わる時刻の差分を計算
        double noteDurationInSeconds =
            BeatmapUtility.GetTimeFromStep(CurrentBeatmap, noteData.step + noteData.length_in_steps)
            - BeatmapUtility.GetTimeFromStep(CurrentBeatmap, noteData.step);

        // ノーツが判定ラインに重なるべき正確な時刻
        double targetTime = BeatmapUtility.GetTimeFromStep(CurrentBeatmap, noteData.step);

        // 速さ * 時間 でノーツの長さを計算
        float noteLengthInUnits = notesSpeed * (float)noteDurationInSeconds;

        // この時点で生成時刻を過ぎているので、どの程度移動させてからスポーンすべきかを計算
        // 本来スポーンすべきだったオブジェクトの中心Y座標
        float baseCenterZ = SpawnZ + (noteLengthInUnits / 2f);

        // 遅れた時間の分だけ、下に移動させる
        float distanceToMove = notesSpeed * (float)timeSinceSpawn;

        // 最終的な座標
        Vector3 spawnPos = new Vector3(LaneXPositions[noteData.lane], 0, baseCenterZ - distanceToMove);

        // Prefabをインスタンス化
        GameObject noteObj = Instantiate(NotePrefab, spawnPos, Quaternion.Euler(90, 0, 0));

        // NoteObject スクリプトのコンポーネントを取得
        NoteObject noteScript = noteObj.GetComponent<NoteObject>();

        // 必要な情報を NoteObject に渡す
        noteScript.Speed = notesSpeed;
        noteScript.Controller = this;
        noteScript.Lane = noteData.lane;
        noteScript.TargetTime = targetTime;
        noteScript.TargetEndTime = targetTime + noteDurationInSeconds;

        // ノーツの長さが4分音符より長いかでロングノーツか決まる
        noteScript.IsLongNote = noteData.length_in_steps > stepsPerQuarterNote;

        // スケール変更
        noteObj.transform.localScale = new Vector3(
            noteObj.transform.localScale.x,
            noteLengthInUnits,
            noteObj.transform.localScale.z);

        // 対応するレーンのキューに、今作ったノーツを追加
        laneQueues[noteData.lane].Enqueue(noteScript);
    }

    /// <summary>
    /// 指定されたレーンのキーが押された時の判定処理
    /// </summary>
    /// <param name="laneIndex">判定するレーン番号 (0-3)</param>
    private void CheckHit(int laneIndex)
    {
        // ノーツの有無にかかわらず判定ラインを光らせる
        SpawnLaneEffect(laneIndex, true);

        // そのレーンに判定すべきノーツが存在するか
        if (laneQueues[laneIndex].Count > 0)
        {
            // キューの先頭にあるノーツ（一番下）を取得
            NoteObject note = laneQueues[laneIndex].Peek();

            // 叩かれた瞬間（現在時刻）と、ノーツの予想到達時刻の差
            float timeDiff = (float)System.Math.Abs(gameTime - note.TargetTime);

            // 時刻差分が許容範囲か
            if (timeDiff <= hitTolerance)
            {
                // 許容範囲の半分より正確か
                if (timeDiff <= hitTolerance * 0.5f)
                {
                    // Excellent
                    SpawnHitEffect(ExcellentEffectPrefab, laneIndex);
                    AddScore(excellentScore);
                    excellentNum++;
                }
                else
                {
                    // Good
                    SpawnHitEffect(GoodEffectPrefab, laneIndex);
                    AddScore(goodScore);
                    goodNum++;
                }

                // キューからノーツを削除
                laneQueues[laneIndex].Dequeue();

                // コンボカウント
                combo++;

                // コンボ数が既定値を超えるたびに加算
                if (combo % 10 == 0) AddScore(comboBonusScore);

                // 最大コンボ数は表示に利用するのみ
                if (maxCombo < combo) maxCombo = combo;

                // 5コンボ以上なら画面に表示
                if (combo > 4)
                {
                    ComboText.gameObject.SetActive(true);
                    ComboText.text = $"{combo} COMBO";
                }

                // 長押しノーツ
                if (note.IsLongNote)
                {
                    note.Hold(); // ノーツの色を変える
                    holdingNotes[laneIndex] = note; // 押さえているノーツとして登録
                }
                // 通常ノーツ
                else
                {
                    // 差分時間が経過してから消す（見た目の問題）
                    Invoke(nameof(note.Hit), timeDiff);
                }
            }
            // 許容範囲内ではなかったが、一定範囲以内ならミス
            else if (timeDiff < hitTolerance * 1.5f)
            {
                NoteMissed(note);
                Destroy(note.gameObject);
            }
        }
    }

    /// <summary>
    /// キーを離した時の処理
    /// </summary>
    private void CheckRelease(int laneIndex)
    {
        // 判定ラインを光らせるのをやめる
        SpawnLaneEffect(laneIndex, false);
        // そのレーンで長押し中のノーツがあるか
        if (holdingNotes[laneIndex] != null)
        {
            // ノーツを取得し、消滅させる
            holdingNotes[laneIndex].Hit();

            // 保持状態を解除
            holdingNotes[laneIndex] = null;
        }
    }

    /// <summary>
    /// ノーツを叩き損ねたことを処理する
    /// </summary>
    public void NoteMissed(NoteObject note)
    {
        // ノーツが判定線を通りすぎたにもかかわらずまだ叩かれていない
        if (laneQueues[note.Lane].Count > 0 && laneQueues[note.Lane].Peek() == note)
        {
            // ミスをしたのでBadを加算＆コンボをリセット
            badNum++;
            combo = 0;
            ComboText.gameObject.SetActive(false);

            // キューからそのノーツを削除
            laneQueues[note.Lane].Dequeue();
        }
    }

    /// <summary>
    /// NoteObjectから呼ばれ、上端が判定ラインを通り過ぎて自動成功したことを処理する
    /// </summary>
    public void AutoRelease(int laneIndex)
    {
        // ボーナス点を与える
        AddScore(longBonusScore);

        // UI表示を有効化
        GameObject bonusLane = LanePrefabs[laneIndex].transform.GetChild(1).gameObject;
        bonusLane.SetActive(true);
        StartCoroutine(FadeLongBonusUI(bonusLane.GetComponentInChildren<TextMeshProUGUI>()));

        // 保持状態を解除
        if (holdingNotes[laneIndex] != null)
        {
            holdingNotes[laneIndex] = null;
        }
    }

    private IEnumerator FadeLongBonusUI(TextMeshProUGUI fadeUIText)
    {
        float t = 0f;
        Color initColor = fadeUIText.color;
        while (t < 2.0f)
        {
            t += Time.deltaTime;
            if (t < 1.0f) continue;

            float alpha = (t - 1.0f) / 1.0f;

            // 透明度を上げてフェードアウト
            fadeUIText.color = Color.Lerp(initColor, new Color(0, 0, 0, 0), alpha);
            yield return null;
        }

        // 再び非表示にして透明度を戻す
        fadeUIText.transform.parent.gameObject.SetActive(false);
        fadeUIText.color = initColor;
    }

    /// <summary>
    /// キーが押されたときに判定ラインを光らせる/元に戻す
    /// </summary>
    /// <param name="laneIndex">光らせるレーン</param>
    /// <param name="isTrigger">光らせる/元に戻す</param>
    private void SpawnLaneEffect(int laneIndex, bool isTrigger)
    {
        LanePrefabs[laneIndex].transform.GetChild(0).gameObject.SetActive(isTrigger);
    }

    /// <summary>
    /// ヒットエフェクトを判定ライン上に生成する
    /// </summary>
    /// <param name="prefab">生成するPrefab</param>
    /// <param name="laneIndex">生成するレーン</param>
    private void SpawnHitEffect(GameObject prefab, int laneIndex)
    {
        // 判定ラインの座標にエフェクトを生成
        Instantiate(prefab,
            new Vector3(LaneXPositions[laneIndex], 0, JudgeZ), Quaternion.Euler(90.0f, 0, 0));
    }

    /// <summary>
    /// スコアを加算する
    /// </summary>
    /// <param name="score"></param>
    private void AddScore(int score)
    {
        this.score += score;
        GamingScore.text = this.score.ToString();
    }

    /// <summary>
    /// スコア表示のコルーチン
    /// </summary>
    /// <returns></returns>
    private IEnumerator ResultDisplay()
    {
        // 集計結果の代入
        ResultTitle.text = CurrentBeatmap.title;
        ResultScore.text = score.ToString();
        ExcellentText.text = excellentNum.ToString();
        GoodText.text = goodNum.ToString();
        BadText.text = badNum.ToString();
        MaxComboText.text = maxCombo.ToString();

        // ゲームUIの非表示
        GamingScore.gameObject.SetActive(false);
        ComboText.gameObject.SetActive(false);

        // 画面をフェードアウトさせて暗くする
        float time = 0f;
        Color startColor = new Color(0, 0, 0, 0);
        Color endColor = new Color(0, 0, 0, 0.7f);
        while (time < 2.0f)
        {
            time += Time.deltaTime;
            float alpha = time / 2.0f;

            // シーントランジション用に設定したマテリアルを削除
            TransitionPanel.gameObject.SetActive(true);
            TransitionPanel.material = null;

            // マスクの不透過度を少しずつ上げる
            TransitionPanel.color = Color.Lerp(startColor, endColor, alpha);

            yield return null;
        }
        yield return new WaitForSeconds(1.0f);

        // トランジションによって子の順が入れ替わっているので最下に移動
        ResultCanvas.transform.SetAsLastSibling();

        // リザルトUIの表示
        ResultCanvas.SetActive(true);
        yield return new WaitForSeconds(0.9f);

        // Excellentの表示
        ExcellentText.gameObject.SetActive(true);
        BGMSource.PlayOneShot(ScoreAttributeSE);
        yield return new WaitForSeconds(0.9f);

        // Goodの表示
        GoodText.gameObject.SetActive(true);
        BGMSource.PlayOneShot(ScoreAttributeSE);
        yield return new WaitForSeconds(0.9f);

        // Badの表示
        BadText.gameObject.SetActive(true);
        BGMSource.PlayOneShot(ScoreAttributeSE);
        yield return new WaitForSeconds(0.9f);

        // 最大コンボの表示
        BGMSource.PlayOneShot(ScoreAttributeSE);

        // フルコンボの場合
        if (maxCombo == CurrentBeatmap.notes.Count)
        {
            MaxComboText.fontSize = 110;
            BGMSource.PlayOneShot(FullComboSE);
        }
        MaxComboText.gameObject.SetActive(true);
        yield return new WaitForSeconds(1.2f);

        // スコアの表示

        // ハイスコアの場合
        if (score > highScore)
        {
            highScore = score;
            ResultScore.enableVertexGradient = true;
            ResultScore.fontSize = 180;
            BGMSource.PlayOneShot(ScoreTotalSE);
            ResultScore.gameObject.SetActive(true);

            yield return new WaitForSeconds(0.5f);
            HighScoreText.gameObject.SetActive(true);
            BGMSource.PlayOneShot(HighScoreSE);
        }
        else
        {
            BGMSource.PlayOneShot(ScoreTotalSE);
            ResultScore.gameObject.SetActive(true);
        }
        yield return new WaitForSeconds(1.6f);

        // 進行テキストの表示
        OptionText.gameObject.SetActive(true);
        BGMSource.PlayOneShot(ResultShowedGingle);
        isResultShowed = true;
    }

    /// <summary>
    /// ゲームシーンからのトランジション
    /// </summary>
    /// <returns></returns>
    private IEnumerator BackTransition(int nextSceneIndex)
    {
        BackTransitionPanel.transform.SetAsLastSibling();
        BackTransitionPanel.gameObject.SetActive(true);
        BackTransitionPanel.rectTransform.localScale = Vector3.zero;
        Transform parent = BackTransitionPanel.transform.parent;

        // 色の設定
        Color[] colors = new Color[]
        {
            Color.white,
            Color.navyBlue,
            Color.blueViolet,
            Color.deepSkyBlue,
            Color.black
        };

        // 遅延時間の設定
        float[] delays = new float[] { 0f, 0.2f, 0.4f, 0.45f, 0.5f };

        List<Image> panels = new List<Image>();

        // パネルの生成と設定
        for (int i = 0; i < colors.Length; i++)
        {
            Image panel;
            if (i == 0) panel = BackTransitionPanel;
            else panel = Instantiate(BackTransitionPanel, parent);

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
        SceneManager.LoadScene(nextSceneIndex);
    }
}