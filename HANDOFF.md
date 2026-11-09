# カカシ工房 Unity版 引き継ぎ

作成日：2026-11-09（JST）

## 現在の結論

**実装を追加し、Unity Editorでプロジェクトが開くところまで進んだ。ゲーム全体の再生確認は未完了。**

ユーザーの「ここまででいい。引き継ぎ書を作って」により、実装・再生確認を中断した。再開の指示が来るまで追加作業はしない。

## ユーザーの依頼と作業先

- 対象は「農家爆受けのカカシを作れ！」。ユーザーに対象を確認済み。
- 原作の参考URL：https://claude.ai/artifact/GFm6reWe6f9Ts8aZbJzhmo
- 原作はサインイン要求が表示され、ソース・実際の挙動は取得できていない。
- ユーザーは「SSD別にみなくていいよ。unity実装して」と指示。その後は本フォルダのコードを土台に実装した。
- 今回の編集先は **`/Users/gosho/Desktop/KakashiUnity`**。SSD上のDay108プロジェクトには変更していない。両者を混同しない。
- 「再生するところまで」との追加依頼があり、ローカルEditorの準備・起動まで実施した。
- 原作との完全一致を確認した状態ではない。既存の独自襲来判定も残っている。

## 追加・修正した内容

| ファイル | 内容 |
|---|---|
| `Assets/Scripts/PartPose.cs` | 部位ごとの位置・傾き・ひねり・サイズの状態と範囲制限 |
| `Assets/Scripts/Scarecrow.cs` | 部位の中心を基準に形状を変更。再構築時も調整を維持。形状による安定度ペナルティ、全調整リセット、再構築前の旧モデル非表示 |
| `Assets/Scripts/GameController.Shape.cs` | 「形を整える」画面、6つのスライダー、部位切替、部位リセット、タップ選択・ドラッグ移動、画面縦横によるパネルとカメラ領域の変更 |
| `Assets/Scripts/GameController.cs` | 形状画面への導線、安定度表示と襲来判定への反映、おまかせ時の調整リセット、早送りラベル、音切替、警告欄のスクロール化 |
| `Assets/Scripts/SafeArea.cs` | ノッチなどを避けるUIルートのアンカー調整 |
| `Assets/Scripts/Env.cs` | UI上から始まったドラッグでカメラが回らないよう修正。旧入力のタッチ対応。稲穂、農家の手・耳・鼻・口・長靴・鍬／かご／スマホを追加 |
| `Assets/Scripts/KakashiAudio.cs` | コード生成のクリック音・衝撃音・カラス音・風音とミュート切替 |
| `Assets/Scripts/UIKit.cs` | ボタンのクリック音を接続 |
| `Assets/Editor/KakashiVerification.cs` | Editor内でゲーム進行を自動操作するスモーク確認メニューとレポート・画像保存 |
| `Packages/manifest.json` | ScreenCaptureモジュール追加。Unityが`packages-lock.json`を生成 |
| `ProjectSettings/`、`Assets/Scenes/Main.unity`、各`.meta` | Unity 6000.3.26f1でプロジェクト初期化時に生成 |
| `Tools/play.sh` | ローカルEditorとローカルUPMキャッシュを使った起動スクリプト |
| `.gitignore`、`README.md` | Editor・キャッシュ類の除外と新機能・確認手順の説明 |

形状の安定度ペナルティはこの移植側で追加した式であり、原作との数値一致は未検証。パーツや動物・農家は引き続きプリミティブによる簡易モデル。

## 実際に確認できたこと

1. C#パーサーで13ファイルを解析し、構文エラー0。結果：`Verification/syntax-check.txt`。これはUnityの型・APIチェックとは別。
2. Unity 6000.3.26f1でパッケージ解決とスクリプトコンパイルが進み、メインシーン生成とゲームのイントロ描画まで到達した。`Verification/editor.log`には`error CS`の記録がない。
3. `Verification/workshop-start.png`を生成し、画像を実際に確認した。**名前に反して内容はイントロ画面**。工房を表示できた証拠として扱わない。
4. 最後のUI確認で、Unityのウィンドウタイトルは`Main - KakashiUnity - Windows, Mac, Linux - Unity 6.3 LTS (6000.3.26f1) <Metal>`、開いているシーンは本フォルダの`Assets/Scenes/Main.unity`だった。

## 未完了・問題点

- **`Verification/play-report.txt`は生成されていない。通しテスト合格ではない。**
- 最後の確認時点で、Playボタンが有効な再生中状態かは確認できていない。
- 形状スライダーの実操作、3Dパーツのドラッグ、安定度変化、各動物・台風、農家3人、結果、再挑戦の通し確認が未完了。
- イントロの画像では横画面で説明文と開始ボタンが重なり、音ボタンがタイトル下の文言に重なる。UI配置の追加調整が必要。
- 最初の起動ログに`UnityEditor.Search.SearchDatabase`内部の`ArgumentOutOfRangeException`がある。ゲームコードの例外とは断定していない。Editorの終了も観測したが、終了原因との因果関係は未確定。
- 自動テストが途中まで実行された後、別のEditorプロセスが本プロジェクトを通常起動していた。再起動用の追加プロセスはパッケージサーバー停止後に終了した。**すでに開いているEditorへ重ねて起動しない。**
- 自動テスト用スクリーンショットは画面遷移と同じフレームで要求する箇所があり、直前の画面が保存され得る。次回は1フレーム以上待ってから保存するように直す。
- iPhone実機、WebGLビルド、配信、原作比較は未実施。公開・Slack送信もしていない。

## Unity環境と起動上の注意

既存Editorは利用できないSSD上にあったため、公式配布の同じバージョンをローカルへ展開した。

```text
Editor:
/Users/gosho/Desktop/KakashiUnity/.tools/unity-editor/Unity/Unity.app

実行ファイル:
/Users/gosho/Desktop/KakashiUnity/.tools/unity-editor/Unity/Unity.app/Contents/MacOS/Unity

今回用のUPMキャッシュ:
/Users/gosho/Desktop/KakashiUnity/.tools/upm-cache
```

- Editorの展開サイズは約9.3GiB。`.tools/`は`.gitignore`で除外している。
- 公式インストーラーはチェックサム一致とUnity TechnologiesのApple配布署名を確認済み。正常展開後に今回の一時インストーラーだけ削除した。
- `~/.upmconfig.toml`の`cacheRoot`が旧SSD上を指すため、そのまま起動するとPackage Managerが失敗することがあった。
- **グローバル設定は変更していない。** 起動するプロセスだけに`UPM_CACHE_ROOT`を設定して回避した。`Tools/play.sh`にも設定済み。
- 既存のUnity Personalライセンスが有効であることを確認した。新規ライセンスの取得やアカウント作成はしていない。
- `Tools/play.sh`はEditorが閉じているときの起動用。すでに本プロジェクトが開いているときは、既存ウィンドウを使う。

## 次回の再開手順

1. 本フォルダを開いているUnityがあるか、ウィンドウのプロジェクト名とシーンパスを確認する。開いていればそのEditorを使う。
2. Consoleでコンパイル・実行時のエラーを確認する。SearchDatabaseのEditor例外とゲーム側の例外を分けて調べる。
3. イントロとHUDの横画面レイアウト、およびテスト画像の保存タイミングを直す。
4. Unityのメニュー **Kakashi → 再生して動作確認** を実行する。コードは形状変更→リセット→おまかせ→襲来→3人の評価→結果→再挑戦を操作する設計。
5. `Verification/play-report.txt`が生成され、`Passed: True`・`Playing: True`になっているか確認する。テストコードに不具合があれば修正して再実行する。レポートだけで視覚品質合格にしない。
6. 保存画像を見て表示を確認し、実際にパーツ選択・ドラッグ・カメラ回転も試す。最後に工房画面をPlay状態で残す。
7. 原作との忠実性は別途照合が必要。今回追加した実装や元からあった簡易モデルを、原作一致済みとは報告しない。

## 証拠ファイル

- `Verification/editor.log`：ローカルUPMキャッシュ指定後の最初の起動・コンパイル・シーン生成・Editor例外のログ
- `Verification/editor-replay.log`：追加の再起動プロセスのログ。これだけではPlayの成功を示さない
- `Verification/workshop-start.png`：描画されたイントロ。名称と実内容が一致していない点に注意
- `Verification/syntax-check.txt`：C#構文解析結果
- `Verification/launcher.log`：起動時標準出力。確認時は空

## 今回適用した記憶

[RECALL_USED: Daily=2026-11-09; Knowledge=unity-game-collaboration-recurring-failure-2026-11-07; Applied=編集先を本フォルダに固定し、ソース保存・Editor表示・再生成功を区別]

[DEPT_ANALYSIS: エンジニアリング部 | 事実=実装追加とEditor表示まで、通しテスト未完了 | 判断=再生完了とは報告しない | 理由=最終Play状態と完走レポートを未確認 | 次回に使う条件=再生検証の再開時]
