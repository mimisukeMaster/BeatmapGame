# Aeterna -エテルナ-
[<img src="https://img.shields.io/github/stars/mimisukeMaster/BeatmapGame">](https://github.com/mimisukeMaster/BeatmapGame/stargazers)
[<img  src="https://img.shields.io/hexpm/l/plug?color=red&logo=apache">](https://www.apache.org/licenses/)
<img src="https://img.shields.io/badge/made with-Unity6000.1.x-blue.svg?&logo=unity&color=8000FF"><br>
[<img src="https://img.shields.io/badge/issues-welcome-green">](https://github.com/mimisukeMaster/BeatmapGame/issues)
<img src="https://img.shields.io/github/repo-size/mimisukeMaster/BeatmapGame?color=ff69b4&logo=gitlfs">
[<img src="https://img.shields.io/badge/deployed%20to-unityroom-blue?logo=unity">](https://unityroom.com/games/aeterna)

## 概要
Unityを使用しチームで制作したリズムゲームです。プレイヤーは音楽に合わせて表示されるノーツをタイミングよく叩き、ハイスコアやフルコンボを目指します。 
デモプレイのリンク先に詳細なゲーム紹介があります。

## デモプレイ
<p align="center">
    <img src="Assets/Images/GameIcon.png" width="100">
</p>
<h3>
    <p align="center">
        <a href="https://unityroom.com/games/aeterna">
        ▶ プレイはこちらから
        </a>
    </p>
</h3>

## 特徴
- **統一的な操作**: 全てキーボード操作で完結することで、マウスも同時に使用する煩雑さを無くしました。
- **多様な楽曲**: チームメンバーの制作したオリジナル曲に加え、著作権フリーの有名な楽曲も用意しています。
- **スコアランキング**: Unityroomでハイスコア・最大コンボ数のランキングを登録することで、プレイヤーとの間でスコアを競い合うことができます。
- **譜面作成用のエディタ拡張**: ノーツがいつどのレーンにどのくらいの長さで出現するかをScriptableObjectで管理しています。この譜面を作成するための専用エディタを作り、直感的なDAWライクの操作ができます。（開発者向け）

## 遊び方
操作方法は画面の指示にしたがって行います。
1.  ゲームを起動し、プレイしたい楽曲を選択します。
1.  音楽が流れると同時に、画面上部からノーツが流れ始めます。
1.  ノーツが判定ラインに到達するタイミングで、レーンに対応するキーを押します。
2.  正確なタイミングで叩くと、高得点が得られます。
3.  楽曲の最後までプレイし、最終スコアを確認します。

## 開発者向け情報

### プロジェクトのセットアップ

1.  Unity Hub を開き、「Add project from disk」を選択します。
2.  このリポジトリをクローンしたフォルダを選択します。
3.  Unity Editor でプロジェクトを開きます。

### 主要なディレクトリ

- `Assets/Beatmap`: 譜面のScriptableObject
- `Assets/Scripts`: C#スクリプト
- `Assets/Musics`: 楽曲の音源、SE

### 製作者
チームにより開発されたものです。
- プログラム: `mimisukeMaster`
- グラフィック: `togenashiuniuni`
- サウンド: `banetu`, `bunchooo`
- 譜面作成: `konnnyakuimo2000`
### 貢献

バグ報告や機能改善の提案は、GitHubのIssuesでお気軽にお寄せください。