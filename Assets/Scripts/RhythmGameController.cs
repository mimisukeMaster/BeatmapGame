using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RhythmGameController : MonoBehaviour
{
    [Header("譜面データ")]
    public Beatmap CurrentBeatmap;

    [Header("レーン設定")]
    public float[] LaneXPositions = new float[] { -1.5f, -0.5f, 0.5f, 1.5f };

    [Header("オブジェクト参照")]
    public AudioSource BGMSource;
    public GameObject NotePrefab;
    public TextMeshProUGUI GamingTitle;
    public TextMeshProUGUI GamingScore;
    public TextMeshProUGUI ComboText;
    public GameObject ResultCanvas;
    public TextMeshProUGUI ResultTitle;
    public TextMeshProUGUI ResultScore;
    public TextMeshProUGUI ExcellentText;
    public TextMeshProUGUI GoodText;
    public TextMeshProUGUI BadText;
    public TextMeshProUGUI MaxComboText;
    public GameObject BackButton;
    public GameObject RetryButton;
    public GameObject RankingButton;

    [Header("エフェクト")]
    public GameObject ExcellentEffectPrefab;
    public GameObject GoodEffectPrefab;
    public GameObject[] LineEffectPrefabs;

    [Header("ゲーム設定")]
    public float NoteSpeed = 10f;
    public float SpawnY = 10f;       // ノーツが生成されるY座標
    public float JudgeY = -3f;   // 判定ラインのY座標 

    [Header("判定設定")]
    /// <summary>
    /// 判定ラインからの許容距離
    /// </summary>
    public float hitTolerance = 0.5f;

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

    private int excellentScore = 100;
    private int goodScore = 50;
    private int longBonusScore = 30;
    private int excellentNum = 0;
    private int goodNum = 0;
    private int badNum = 0;
    private int combo = 0;
    private int maxCombo = 0;

    void Start()
    {
        if (CurrentBeatmap == null)
        {
            Debug.LogError("譜面が設定されていません");
            return;
        }

        // 判定ラインのエフェクトを非アクティブ化
        for(int i = 0; i < LineEffectPrefabs.Length; i++)
        {
            LineEffectPrefabs[i].SetActive(false);
        }

        // リザルト要素を非アクティブ化（タイトルとヘッダー以外）
        for (int i = 2; i < ResultCanvas.transform.childCount; i++) 
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

        // 音楽再生準備
        BGMSource.clip = CurrentBeatmap.audioClip;

        // ノーツが判定ラインに着くまでの移動時間を計算
        float distance = SpawnY - JudgeY;
        noteTravelTimeInSeconds = (double)distance / NoteSpeed;

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
        if (!isGameStarted) return;

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

                // 最大コンボ数をそのままスコアに加算
                AddScore(maxCombo);

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
            }

            if (Input.GetKeyUp(keys[i]))
            {
                CheckRelease(i);
            }
        }
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

        // 速さ * 時間 でノーツの長さを計算
        float noteLengthInUnits = NoteSpeed * (float)noteDurationInSeconds;

        // この時点で生成時刻を過ぎているので、どの程度移動させてからスポーンすべきかを計算
        // 本来スポーンすべきだったオブジェクトの中心Y座標
        float baseCenterY = SpawnY + (noteLengthInUnits / 2f);

        // 遅れた時間の分だけ、下に移動させる
        float distanceToMove = NoteSpeed * (float)timeSinceSpawn;

        // 最終的な座標
        Vector3 spawnPos = new Vector3(LaneXPositions[noteData.lane], baseCenterY - distanceToMove, 0);

        // Prefabをインスタンス化
        GameObject noteObj = Instantiate(NotePrefab, spawnPos, Quaternion.identity);

        // NoteObject スクリプトのコンポーネントを取得
        NoteObject noteScript = noteObj.GetComponent<NoteObject>();

        // 必要な情報を NoteObject に渡す
        noteScript.Speed = NoteSpeed;
        noteScript.Controller = this;
        noteScript.Lane = noteData.lane;

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

            //　ノーツの下端のY座標と判定ラインとの距離を計算
            float distance = Mathf.Abs(
                note.transform.position.y - (note.transform.localScale.y / 2.0f) - JudgeY);

            // 距離が許容範囲付近か
            if (distance <= hitTolerance + 1)
            {
                // 許容範囲内ならExcellent、それ以外ならGood
                if (distance <= hitTolerance)
                {
                    SpawnHitEffect(ExcellentEffectPrefab, laneIndex);
                    AddScore(excellentScore);
                    excellentNum++;
                }
                else
                {
                    SpawnHitEffect(GoodEffectPrefab, laneIndex);
                    AddScore(goodScore);
                    goodNum++;
                }

                // キューからノーツを削除
                laneQueues[laneIndex].Dequeue();

                // コンボカウント
                combo++;
                if (maxCombo < combo) maxCombo = combo;

                // 3コンボ以上なら画面に表示
                if (combo > 2)
                {
                    ComboText.gameObject.SetActive(true);
                    ComboText.text = $"{combo}combo!";
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
                    note.Hit(); // ノーツを消滅させる
                }
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
    /// NoteObjectから呼ばれ、ノーツが判定ラインを過ぎたことを処理する
    /// </summary>
    public void NoteMissed(NoteObject note)
    {
        // ノーツがdespawnYを通りすぎたにもかかわらずまだ叩かれていない
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

        // 保持状態を解除
        if (holdingNotes[laneIndex] != null)
        {
            holdingNotes[laneIndex] = null;
        }
    }

    /// <summary>
    /// キーが押されたときに判定ラインを光らせる/元に戻す
    /// </summary>
    /// <param name="laneIndex">光らせるレーン</param>
    /// <param name="isTrigger">光らせる/元に戻す</param>
    private void SpawnLaneEffect(int laneIndex, bool isTrigger)
    {
        LineEffectPrefabs[laneIndex].SetActive(isTrigger);
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
            new Vector3(LaneXPositions[laneIndex], JudgeY, 0), Quaternion.identity);
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
        ExcellentText.text = $"Excellent: {excellentNum}";
        GoodText.text = $"Good: {goodNum}";
        BadText.text = $"Bad: {badNum}";
        MaxComboText.text = $"Max combo bonus: {maxCombo}";

        // ゲームUIの非表示
        GamingScore.gameObject.SetActive(false);

        // TODO:画面がちょっと暗くなる
        yield return new WaitForSeconds(1.0f);

        // リザルトUIの表示
        ResultCanvas.SetActive(true);
        yield return new WaitForSeconds(0.9f);

        // Excellentの表示
        ExcellentText.gameObject.SetActive(true);
        yield return new WaitForSeconds(0.9f);

        // Goodの表示
        GoodText.gameObject.SetActive(true);
        yield return new WaitForSeconds(0.9f);

        // Badの表示
        BadText.gameObject.SetActive(true);
        yield return new WaitForSeconds(0.9f);

        // 最大コンボの表示
        MaxComboText.gameObject.SetActive(true);
        yield return new WaitForSeconds(1.2f);

        // スコアの表示
        ResultScore.gameObject.SetActive(true);
        yield return new WaitForSeconds(1.6f);

        // 各ボタンの表示
        BackButton.SetActive(true);
        RetryButton.SetActive(true);
        RankingButton.SetActive(true);
    }
}