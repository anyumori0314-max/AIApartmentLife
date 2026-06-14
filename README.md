# AI Apartment Life MVP

Unity 6.3 LTS / Universal 3D 向けの、住人観察シミュレーションゲーム MVP です。
外部アセットは使わず、部屋や住人は Cube / Capsule / Sphere などのプリミティブで仮実装しています。

## セットアップ

1. Unity 6.3 LTS でこのプロジェクトを開きます。
2. `Assets/Scenes/MainScene.unity` を開きます。
3. Play を押します。

`MainScene` に手動で GameObject や Script をアタッチする必要はありません。
`ApartmentLifeBootstrap` が再生開始時に自動でゲーム本体、アパート、住人、カメラ、ライト、uGUI を生成します。

## 操作方法

- `時間を進める`: ランダムな日常イベントを1つ発生させます。
- `セーブ`: 現在の日数、住人状態、関係値、イベントログを JSON で保存します。
- `ロード`: 保存済み JSON を読み込みます。
- 住人をクリック: 住人の詳細と他住人との関係値を表示します。

セーブファイルは Unity の `Application.persistentDataPath` 配下に `apartment-life-save.json` として作成されます。

## MVP内容

- 4部屋の簡易アパートを自動生成
- 各部屋に1人ずつ住人を配置
- 住人データ:
  - id
  - name
  - personality
  - hobby
  - catchphrase
  - mood
  - energy
- 住人同士の relationship 値を管理
- ランダムイベント:
  - 雑談
  - 喧嘩
  - 仲直り
  - 趣味の共有
  - 相談
- イベント結果に応じて mood / energy / relationship を増減
- イベントログ表示
- 住人クリックによる詳細パネル表示
- JSON セーブ / ロード
- アパート全体を斜め上から見下ろすカメラ
- 部屋と住人が見えるライト

## ファイル構成

```text
Assets/Scripts/ApartmentLife/
  Data/
    GameSaveData.cs        保存用データの入れ物
    RelationshipData.cs    住人同士の関係値
    ResidentData.cs        住人の基本データ
  Runtime/
    ApartmentBuilder.cs    3Dアパートと住人の自動生成
    ApartmentLifeBootstrap.cs
                           Play開始時の自動起動
    ApartmentLifeGame.cs   ゲーム進行、イベント、セーブ/ロード
    ResidentView.cs        住人クリック受付
  UI/
    ApartmentLifeUI.cs     uGUI の自動生成と表示更新
```

## 開発メモ

- 今回は動く MVP を優先し、Scene ファイルの直接編集は避けています。
- 見た目を作り込む場合は、`ApartmentBuilder` のプリミティブ生成部分を Prefab 参照に置き換えると拡張しやすいです。
- イベント種類や住人初期データを増やす場合は、`ApartmentLifeGame` の `CreateDefaultData` と `ApplyEvent` を編集してください。
