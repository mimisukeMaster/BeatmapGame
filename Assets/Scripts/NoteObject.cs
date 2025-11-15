using UnityEngine;

public class NoteObject : MonoBehaviour
{

    /// <summary>
    /// 消える位置
    /// </summary>
    private float DespawnY = -20f;

    /// <summary>
    /// 速さ
    /// </summary>
    public float Speed;

    /// <summary>
    /// 自身を管理するコントローラ
    /// </summary>
    public RhythmGameController Controller;

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
        // 下に移動
        transform.Translate(Vector3.down * Speed * Time.deltaTime);

        // ノーツの上端のY座標を計算
        float noteTopY = transform.position.y + (transform.localScale.y / 2.0f);

        // 画面外に出たら自身を破棄する
        if (noteTopY < DespawnY)
        {
            if (Controller != null && !isHolding)
            {
                Controller.NoteMissed(this);
            }
            Destroy(gameObject);
        }

        // 押さえられていてかつノーツの上端が判定ラインを通過したら
        if (isHolding && noteTopY < Controller.JudgementY)
        {
            // 成功として自動で消滅
            Controller.AutoRelease(Lane);
            Hit(); // 自分を消す
        }
    }

    /// <summary>
    /// 叩かれた時
    /// </summary>
    public void Hit()
    {
        // （ここにエフェクト再生などを追加できる）

        Destroy(gameObject);
    }

    /// <summary>
    /// 長押し開始時
    /// </summary>
    public void Hold()
    {
        isHolding = true;

        // 色をシアンにする
        objRenderer.material.color = Color.cyan;
    }
}