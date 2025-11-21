using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class BeatmapEditorWindow : EditorWindow
{
    // --- 定数 ---
    private const int NUM_LANES = 4; // レーン数を4に固定
    private const float LANE_HEIGHT = 20f; // 1レーンの縦の高さ
    private const float STEP_WIDTH = 12f;  // 1ステップの横の幅（長さ）
    private const int TOTAL_STEPS_TO_DISPLAY = 1000; // 描画する総ステップ数

    // --- 編集中のアセット ---
    private Beatmap currentBeatmap; 
    
    // --- GUI用 ---
    private Vector2 scrollPosition = Vector2.zero;

    // --- 状態管理 (重要) ---
    private NoteData selectedNoteForResize = null; // 現在リサイズ中のノーツ
    private bool isResizing = false;

    private GameObject previewAudioObj;
    private AudioSource previewAudioSource;
    private bool isPlaying = false;

    [MenuItem("Window/Beatmap Editor")]
    public static void ShowWindow()
    {
        GetWindow<BeatmapEditorWindow>("Beatmap Editor");
    }

    void OnEnable()
    {
        // エディタ用の隠しオブジェクトを作成してAudioSourceを持たせる
        if (previewAudioObj == null)
        {
            previewAudioObj = EditorUtility.CreateGameObjectWithHideFlags("Audio Preview", HideFlags.HideAndDontSave, typeof(AudioSource));
            previewAudioSource = previewAudioObj.GetComponent<AudioSource>();
        }
    }

    void OnDisable()
    {
        // ウィンドウを閉じる時に隠しオブジェクトを破棄 (メモリリーク防止)
        if (previewAudioObj != null)
        {
            DestroyImmediate(previewAudioObj);
        }
    }

    void Update()
    {
        // 再生中は画面を毎フレーム更新して、赤線を滑らかに動かす
        if (isPlaying && previewAudioSource != null && previewAudioSource.isPlaying)
        {
            Repaint();
        }
        else if (isPlaying && previewAudioSource != null && !previewAudioSource.isPlaying)
        {
            // 曲が終わったら停止状態にする
            isPlaying = false;
            Repaint();
        }
    }

    void OnGUI()
    {
        // 編集対象の譜面アセットを選択
        currentBeatmap = (Beatmap)EditorGUILayout.ObjectField("編集する譜面", currentBeatmap, typeof(Beatmap), false);

        if (currentBeatmap == null)
        {
            EditorGUILayout.HelpBox("譜面(Beatmap)アセットを選択してください。", MessageType.Info);
            return;
        }
        
        // Undoを記録 (これがないとCtrl+Zが効かない)
        Undo.RecordObject(currentBeatmap, "Beatmap Editor Change");

        // 曲の設定（ScriptableObjectの値を直接編集）
        DrawSettingsPanel();

        // 再生コントロール
        DrawAudioControls();

        // タイムライン領域
        EditorGUILayout.LabelField("タイムライン", EditorStyles.boldLabel);
        
        // スクロールビューの定義
        // (横幅: 可変, 高さ: 4レーン分 + スクロールバーの余白)
        Rect viewRect = GUILayoutUtility.GetRect(position.width - 20, LANE_HEIGHT * NUM_LANES + 20, GUILayout.ExpandWidth(true));
        // (コンテンツの総サイズ: 1000ステップ分, 4レーン分)
        Rect contentRect = new Rect(0, 0, STEP_WIDTH * TOTAL_STEPS_TO_DISPLAY, LANE_HEIGHT * NUM_LANES);

        scrollPosition = GUI.BeginScrollView(viewRect, scrollPosition, contentRect, false, true);

        // --- タイムラインの背景（グリッド）を描画 ---
        DrawTimelineBackground(contentRect);

        // --- 既存のノーツを描画 ---
        DrawNotes();

        // --- 小節番号を描画 ---
        DrawMeasureNumbers(contentRect);

        // 再生位置の赤線
        DrawPlaybackLine(contentRect);
        
        // --- マウス操作の処理 ---
        HandleMouseInput(contentRect);

        GUI.EndScrollView();
        
        // 変更を保存（重要）
        if (GUI.changed)
        {
            EditorUtility.SetDirty(currentBeatmap);
            // エディタを再描画して変更を即時反映
            Repaint(); 
        }
    }

    // 設定パネルの描画
    void DrawSettingsPanel()
    {
        currentBeatmap.title = EditorGUILayout.TextField("Title", currentBeatmap.title);
        currentBeatmap.audioClip = (AudioClip)EditorGUILayout.ObjectField("AudioClip", currentBeatmap.audioClip, typeof(AudioClip), false);
        currentBeatmap.bpm = EditorGUILayout.DoubleField("BPM", currentBeatmap.bpm);
        currentBeatmap.beatsPerMeasure = EditorGUILayout.IntField("拍子(X/4)", currentBeatmap.beatsPerMeasure);
        currentBeatmap.stepsPerMeasure = EditorGUILayout.IntField("1小節の最大分割数", currentBeatmap.stepsPerMeasure);
        currentBeatmap.firstBeatOffsetSec = EditorGUILayout.DoubleField("オフセット(秒)", currentBeatmap.firstBeatOffsetSec);
    }

    // 再生・停止ボタンの描画
    void DrawAudioControls()
    {
        EditorGUILayout.BeginHorizontal();

        // 5秒戻るボタン
        if (GUILayout.Button("<< -5s", GUILayout.Height(30), GUILayout.Width(60)))
        {
            Seek(-5.0f);
        }

        // 再生/一時停止ボタン
        // ラベルを "Play Music" / "Stop" から "Play" / "Pause" に変更
        if (GUILayout.Button(isPlaying ? "Pause" : "Play", GUILayout.Height(30)))
        {
            if (isPlaying)
            {
                PauseMusic(); // StopMusicから変更
            }
            else
            {
                PlayMusic();
            }
        }

        // 5秒進むボタン
        if (GUILayout.Button("+5s >>", GUILayout.Height(30), GUILayout.Width(60)))
        {
            Seek(5.0f);
        }
        
        // 現在時刻の表示
        if(previewAudioSource != null && previewAudioSource.clip != null)
        {
            // 分:秒 形式で表示
            string currentTimeStr = string.Format("{0:00}:{1:00}", (int)previewAudioSource.time / 60, (int)previewAudioSource.time % 60);
            string totalTimeStr = string.Format("{0:00}:{1:00}", (int)previewAudioSource.clip.length / 60, (int)previewAudioSource.clip.length % 60);
            
            EditorGUILayout.LabelField($"Time: {currentTimeStr} / {totalTimeStr}", GUILayout.Width(150));
        }
        EditorGUILayout.EndHorizontal();
    }

    void PlayMusic()
    {
        if (currentBeatmap.audioClip == null || previewAudioSource == null) return;

        previewAudioSource.clip = currentBeatmap.audioClip;
        
        // もし曲が最後まで再生されていたら、最初に戻してから再生する
        if (Mathf.Approximately(previewAudioSource.time, previewAudioSource.clip.length))
        {
            previewAudioSource.time = 0;
        }

        previewAudioSource.Play();
        isPlaying = true;
    }

    void PauseMusic()
    {
        if (previewAudioSource == null) return;
        
        previewAudioSource.Pause(); // Stop() ではなく Pause() を使う
        
        // previewAudioSource.time = 0; // ← この行を削除（最初に戻さない）
        
        isPlaying = false;
    }

    // 時間を移動する関数
    void Seek(float delta)
    {
        if (previewAudioSource == null || previewAudioSource.clip == null) return;

        // 時間を加算・減算し、0 ～ 曲の長さ の範囲に収める
        float newTime = previewAudioSource.time + delta;
        previewAudioSource.time = Mathf.Clamp(newTime, 0f, previewAudioSource.clip.length);

        // 画面更新（赤線を即座に移動させるため）
        Repaint();
    }

    // 赤い再生ラインの描画
    void DrawPlaybackLine(Rect contentRect)
    {
        if (previewAudioSource == null || currentBeatmap.bpm <= 0) return;

        // 現在の再生時間
        float currentTime = previewAudioSource.time;
        
        // 時間をX座標に変換
        // 1ステップあたりの秒数
        double secondsPerStep = BeatmapUtility.GetSecondsPerStep(currentBeatmap.bpm, currentBeatmap.beatsPerMeasure, currentBeatmap.stepsPerMeasure);
        
        // オフセットを考慮した相対時間
        double relativeTime = currentTime - currentBeatmap.firstBeatOffsetSec;
        if (relativeTime < 0) relativeTime = 0;

        // 現在のステップ数（小数を含む正確な値）
        double currentStep = relativeTime / secondsPerStep;

        // X座標
        float x = (float)(currentStep * STEP_WIDTH);

        // 赤線を描画
        Handles.color = Color.red;
        Handles.DrawLine(new Vector3(x, contentRect.y), new Vector3(x, contentRect.y + contentRect.height));
        
        // 再生中は自動スクロールさせる（オプション）
        if (isPlaying)
        {
            // 画面の中心に来るようにスクロール
            float centerOffset = position.width / 2f;
            if (x > centerOffset)
            {
                scrollPosition.x = x - centerOffset;
            }
        }
    }

    // 背景グリッドの描画
    void DrawTimelineBackground(Rect contentRect)
    {
        // 1. 横線 (レーンの境界)
        Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        for (int i = 1; i < NUM_LANES; i++)
        {
            float y = i * LANE_HEIGHT;
            Handles.DrawLine(new Vector3(0, y), new Vector3(contentRect.width, y));
        }

        // 2. 縦線 (ステップ、拍、小節)
        int stepsPerBeat = currentBeatmap.stepsPerMeasure / currentBeatmap.beatsPerMeasure;
        for (int i = 0; i <= TOTAL_STEPS_TO_DISPLAY; i++)
        {
            float x = i * STEP_WIDTH;
            Color color = new Color(0.5f, 0.5f, 0.5f, 0.3f); // 薄いグレー (ステップ)

            // 拍の線
            if (i % stepsPerBeat == 0)
            {
                color = new Color(0.8f, 0.8f, 0.8f, 0.5f); // やや濃いグレー
            }
            // 小節の線
            if (i % currentBeatmap.stepsPerMeasure == 0)
            {
                color = new Color(1f, 1f, 1f, 0.8f); // 白
            }
            
            // EditorGUI.DrawRectは重いのでHandles.DrawLineに変更
            Handles.color = color;
            Handles.DrawLine(new Vector3(x, 0), new Vector3(x, contentRect.height));
        }
    }
    
    // 既存ノーツの描画
    void DrawNotes()
    {
        if (currentBeatmap.notes == null) return;
        
        foreach (NoteData note in currentBeatmap.notes)
        {
            float x = note.step * STEP_WIDTH;
            float y = note.lane * LANE_HEIGHT;
            // ステップ間に隙間を作るため、幅を少しだけ狭める
            float width = (note.length_in_steps * STEP_WIDTH) - 2f; 
            float height = LANE_HEIGHT - 2f;

            Rect noteRect = new Rect(x, y + 1f, width, height);
            
            // 色の決定
            Color noteColor = Color.cyan;
            if (isResizing && selectedNoteForResize == note)
            {
                noteColor = Color.yellow; // リサイズ中は黄色
            }
            
            EditorGUI.DrawRect(noteRect, noteColor);
            
            // ノーツの端（リサイズ用ハンドル）
            Rect resizeHandleRect = new Rect(x + width - (STEP_WIDTH / 2), y, (STEP_WIDTH / 2), height);
            EditorGUI.DrawRect(resizeHandleRect, Color.blue);
        }
    }

    // マウス操作の処理 (ロジックを大幅に改善)
    void HandleMouseInput(Rect contentRect)
    {
        Event e = Event.current;
        
        // スクロールビュー内のマウス座標を取得
        Vector2 mousePos = e.mousePosition;

        // 座標がタイムラインのコンテンツ領域外なら何もしない
        if (!contentRect.Contains(mousePos))
        {
            // マウスが領域外に出たらリサイズを強制終了
            if (e.type == EventType.MouseUp && isResizing)
            {
                isResizing = false;
                selectedNoteForResize = null;
                GUI.changed = true;
                e.Use();
            }
            return;
        }

        // マウス座標をステップとレーンに変換
        int clickedStep = (int)(mousePos.x / STEP_WIDTH);
        int clickedLane = (int)(mousePos.y / LANE_HEIGHT);
        
        // レーンが 0-3 の範囲外なら無視 (これで4レーンに固定)
        if (clickedLane < 0 || clickedLane >= NUM_LANES) return;

        switch (e.type)
        {
            // --- 1. マウスボタンが押された時 ---
            case EventType.MouseDown:
            {
                // ノーツを探す
                NoteData noteAtClick = FindNoteAt(clickedStep, clickedLane);

                // 【左クリック】
                if (e.button == 0)
                {
                    if (noteAtClick != null)
                    {
                        // ノーツの右端をクリックしたかチェック
                        int endStep = noteAtClick.step + noteAtClick.length_in_steps - 1;
                        if (clickedStep == endStep)
                        {
                            // リサイズ開始
                            isResizing = true;
                            selectedNoteForResize = noteAtClick;
                        }
                        else
                        {
                            // TODO: ノーツの移動処理 (将来の拡張用)
                        }
                    }
                    else
                    {
                        // 新規ノーツの追加 (デフォルトの長さ: 1)
                        NoteData newNote = new NoteData(clickedStep, 1, clickedLane);
                        currentBeatmap.notes.Add(newNote);
                        SortNotes(); // 念のためソート
                        
                        // 今作ったノーツを即座にリサイズ対象にする
                        isResizing = true;
                        selectedNoteForResize = newNote;
                    }
                    GUI.changed = true;
                    e.Use(); // イベントを消費
                }
                // 【右クリック】
                else if (e.button == 1)
                {
                    if (noteAtClick != null)
                    {
                        // ノーツの削除
                        currentBeatmap.notes.Remove(noteAtClick);
                        GUI.changed = true;
                        e.Use();
                    }
                }
                break;
            }

            // --- 2. マウスドラッグ中 ---
            case EventType.MouseDrag:
            {
                if (isResizing && selectedNoteForResize != null)
                {
                    // 新しい長さを計算
                    int newLength = (clickedStep - selectedNoteForResize.step) + 1;
                    
                    // 長さは最低でも1
                    selectedNoteForResize.length_in_steps = Mathf.Max(1, newLength);
                    GUI.changed = true;
                    e.Use();
                }
                break;
            }

            // --- 3. マウスボタンが離された時 ---
            case EventType.MouseUp:
            {
                if (isResizing)
                {
                    // リサイズ終了
                    isResizing = false;
                    selectedNoteForResize = null;
                    GUI.changed = true;
                    e.Use();
                }
                break;
            }
        }
    }

    // --- ヘルパー関数 ---

    /// <summary>
    /// 指定したステップとレーンに「存在する」ノーツを探す (範囲チェック)
    /// </summary>
    private NoteData FindNoteAt(int step, int lane)
    {
        if (currentBeatmap.notes == null) return null;

        // 逆順に探す (手前に描画されているものを優先的に選択するため)
        for (int i = currentBeatmap.notes.Count - 1; i >= 0; i--)
        {
            NoteData note = currentBeatmap.notes[i];
            if (note.lane == lane && 
                step >= note.step && 
                step < (note.step + note.length_in_steps))
            {
                return note;
            }
        }
        return null;
    }

    /// <summary>
    /// ノーツリストを時間順に並べ替える
    /// </summary>
    private void SortNotes()
    {
        currentBeatmap.notes = currentBeatmap.notes.OrderBy(n => n.step).ThenBy(n => n.lane).ToList();
    }

    /// <summary>
    /// タイムラインの上部に小節番号を描画する
    /// </summary>
    void DrawMeasureNumbers(Rect contentRect)
    {
        // 0除算を避ける
        if (currentBeatmap.stepsPerMeasure <= 0) return;

        // スタイルを定義 (読みやすいように白文字)
        GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
        labelStyle.normal.textColor = Color.white;

        // --- パフォーマンス最適化 ---
        // 画面に「見えている」範囲だけを描画対象にする

        // 見えているX座標の開始位置と終了位置
        float startX = scrollPosition.x;
        float endX = scrollPosition.x + position.width; // ウィンドウの幅

        // 見えている最初のステップ番号
        // (例: スクロール位置 120 / 幅 12 = ステップ 10)
        int startStep = (int)(startX / STEP_WIDTH);
        
        // 見えている最後のステップ番号
        int endStep = (int)(endX / STEP_WIDTH) + 2; // +2で余裕を持たせる

        // 描画を開始すべき「小節の先頭ステップ」を計算
        // (例: startStepが20, 1小節が16なら、16から描画)
        int startMeasureStep = (startStep / currentBeatmap.stepsPerMeasure) * currentBeatmap.stepsPerMeasure;
        
        // --- 描画ループ ---
        // 見えている範囲の小節線だけをループ
        for (int i = startMeasureStep; i <= endStep; i += currentBeatmap.stepsPerMeasure)
        {
            // 小節線のX座標
            float x = i * STEP_WIDTH;
            
            // 小節番号 (1始まり)
            int measureNumber = (i / currentBeatmap.stepsPerMeasure) + 1;
            
            // 描画するRect (xは線の位置+2px, yは一番上, 幅40px, 高さ20px)
            Rect labelRect = new Rect(x + 2f, contentRect.y, 40f, 20f);
            
            // 描画
            GUI.Label(labelRect, measureNumber.ToString(), labelStyle);
        }
    }
}