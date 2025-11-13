using UnityEngine;

public class NoteObject : MonoBehaviour
{

    // 消える位置
    private float despawnY = -20f;

    // 速さ
    public float speed;

    // 自身を管理するコントローラー
    public RhythmGameController controller;

    // 自身のレーン番号 (0-3)
    public int lane;
    
    // 自分が長押しノーツか
    public bool isLongNote = false;
    
    // 自分が現在押さえられているか
    private bool isHolding = false;

    // 色を変えるためのレンダラー
    private Renderer objRenderer;

    void Awake()
    {
        // 自分のレンダラーを起動時に取得
        objRenderer = GetComponent<Renderer>();

        if (objRenderer != null && objRenderer.material != null)
        {
            objRenderer.material = new Material(objRenderer.material);
        }
    }
    void Update()
    {
        // 毎フレーム、真下に移動させる
        transform.Translate(Vector3.down * speed * Time.deltaTime);

        // ノーツの上端のY座標を計算
        float noteTopY = transform.position.y + (transform.localScale.y / 2.0f);

        // 画面外に出たら自身を破棄する
        if (noteTopY < despawnY)
        {
            if (controller != null && !isHolding)
            {
                controller.NoteMissed(this);
            }
            Destroy(gameObject);
        }
        
        // 押さえられていてかつノーツの上端が判定ラインを通過したら
        if (isHolding && noteTopY < controller.judgementY)
        {
            // 成功として自動で消滅（リリース）
            controller.AutoRelease(lane); // コントローラーに通知
            Hit(); // 自分を消す
        }
    }

    /// <summary>
    /// 叩かれた（Hit）時にコントローラーから呼ばれる
    /// </summary>
    public void Hit()
    {
        // （ここにエフェクト再生などを追加できる）
        Destroy(gameObject); // 自身を破棄する
    }

    /// <summary>
    /// 長押し開始時にコントローラーから呼ばれる
    /// </summary>
    public void Hold()
    {
        isHolding = true;

        // 色を半透明にする
        if (objRenderer != null && objRenderer.material != null)
        {
            Color color = objRenderer.material.color;
            color.a = 0.5f;
            objRenderer.material.color = color;
        }
    }
}