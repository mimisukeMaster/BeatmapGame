using UnityEngine;

public class NoteObject : MonoBehaviour
{
    /// <summary>
    /// 速さ
    /// </summary>
    public float Speed;

    /// <summary>
    /// 判定ライン到達予定時刻 (秒)
    /// </summary>
    public double TargetTime;

    /// <summary>
    /// (ロングノーツ用) 終了予定時刻 (秒)
    /// </summary>
    public double TargetEndTime;

    /// <summary>
    /// 自身を管理するコントローラ
    /// </summary>
    public GameManager Controller;

    /// <summary>
    /// 自身のレーン番号 (0-3)
    /// </summary>
    public int Lane;

    /// <summary>
    /// 自分が長押しノーツか
    /// </summary>
    public bool IsLongNote = false;

    /// <summary>
    /// 自分が現在押さえられているか
    /// </summary>
    private bool isHolding = false;

    /// <summary>
    /// 自身のレンダラ
    /// </summary>
    private SpriteRenderer objRenderer;

    void Awake()
    {
        // 自分のレンダラを起動時に取得
        objRenderer = GetComponent<SpriteRenderer>();
    }
    void Update()
    {
        // 奥から手前に移動
        // CurrentGameTimeに基づきその瞬間の正しい位置を計算して配置し直す
        // 判定線の位置 + 中心補正 + (速度 × 判定までの残り時間)
        
        // スケールYが長さ（SpawnNoteの実装に基づく）なので、その半分を中心補正とする
        float halfLength = transform.localScale.y / 2.0f;
        
        // 到達までの残り時間（過ぎている場合はマイナスになる）
        float timeDiff = (float)(TargetTime - Controller.CurrentGameTime);

        // 新しいZ座標を計算
        float newZ = Controller.JudgeZ + halfLength + (Speed * timeDiff);

        // 位置を更新
        transform.position = new Vector3(transform.position.x, transform.position.y, newZ);

        // 時間ベースで判定する
        // 現在のゲーム時間を取得
        double currentGameTime = Controller.CurrentGameTime;

        // ミス判定 (判定時間を過ぎ、かつ許容範囲も超えた場合)
        if (currentGameTime > TargetTime + Controller.hitTolerance && !isHolding && !IsLongNote)
        {
            Controller.NoteMissed(this);
            Destroy(gameObject);
        }
        // ロングノーツの場合開始から末尾が過ぎるまで待つ
        else if (currentGameTime > TargetEndTime + Controller.hitTolerance && !isHolding && IsLongNote)
        {
            Controller.NoteMissed(this);
            Destroy(gameObject);
        }

        // ロングノーツは終了時間を過ぎたら成功とみなす
        if (isHolding && currentGameTime >= TargetEndTime)
        {
            Controller.AutoRelease(Lane);
            Hit();
        }
    }

    /// <summary>
    /// 叩かれた時
    /// </summary>
    public void Hit()
    {
        Destroy(gameObject);
    }

    /// <summary>
    /// 長押し開始時
    /// </summary>
    public void Hold()
    {
        isHolding = true;

        // 色を濃くする
        Color.RGBToHSV(objRenderer.material.color, out float h, out float s, out float v);
        objRenderer.material.color = Color.gray4;
    }
}