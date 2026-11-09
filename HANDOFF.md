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


---
# Day110 追記（2026-11-09 JST）

## 今回の変更（コード編集のみ。Unityでのコンパイル・再生は**未確認**）
- `Env.cs`：猿の腕を別枠化（armRaise/hand）、農家を「肩・ひじ・腰・首」の関節つきに作り直し（歩く・身振り・口パク）、カラスの羽・尾羽・足を追加、OrbitCamに画角(fov)と自動まわりこみ(autoYaw)
- `FarmerRig.cs`（新規）：農家の歩き／身振り手振り／うなずき・首振り／口パク／カカシへの視線
- `Weather.cs`（新規）：夕暮れ⇄夜の切り替え（月明かり・霧・空）、大雨パーティクル、稲妻
- `KakashiAudio.cs`：雷鳴・雨音・猿の声を追加
- `GameController.cs`：①猿が飛びかかって奪う（ジャンプ＋腕を伸ばし＋戦利品を抱えて逃走）②カメラが動物とカカシを同時に捉える（縦画面の携帯でイノシシが見切れない）③台風は夜＋大雨＋稲妻 ④「農家の人たちが集まってきたよ！」→小屋からぞろぞろ歩いてかかしを囲む ⑤審査は「手前＝農家（正面）、奥＝カカシ」の構図で、1人ずつ歩いてきて身振り手振りで批評（口パク＋タイプ表示）
- バックアップ：`../backup_scripts_before_day110_edit.tgz`（編集前のScripts/Editor）

## 未確認（次回最初にやること）
- Unityを開いてConsoleのコンパイルエラーを確認 → ▶再生 → Kakashi→再生して動作確認
- 縦画面（携帯のゲームビュー）で猪・猿・農家の構図を目視確認
- WebGLビルド・公開・実機確認は未実施

---
# Day110 追記2（2026-11-09 21時台）バランス・倒れ方・雨雷
- `Data.cs`：襲来の発生条件を見直し（猿＝警戒度 D*0.4+V*0.4+音 が 50/28 未満で飛びかかる、猪＝守り45(好物あり62)で引き返す・安定度50(58)で耐える、鹿＝守り40(55)・安定度45(52)、台風＝安定度55、帽子は75未満で飛ぶ）。支柱の安定度基礎値：枝36/木組み52/丸太68
- `GameController.cs`：倒れ方を「地面に当たる角度まで」で止める（横木や頭がめり込まない）。加速して倒れ→弾んで停止、起き上がりもなめらか
- `Weather.cs`：雨2層（雨筋＋霧雨）・地面の波紋・風で斜めに降る・ギザギザの雷光と枝分かれ・近い/遠い雷
- `KakashiAudio.cs`：雷鳴を「バリッ＋ゴロゴロ」に
- 編集前の控え：`../backup_scripts_before_balance_edit.tgz`
- 未確認：Unityコンパイル・再生・縦画面での見た目（前回と同じ）

## Day110 追記3：里山と木々（見本段階）
- 新規 `Assets/Scripts/Scenery.cs`：手作りメッシュの木（根元太→先細の幹、途中から枝分かれ、葉は大小の塊を枝先に不規則配置＝枝と空が透ける、左右非対称、足元に草・低木）と、尾根/谷のある山肌＋手前斜面の雑木林（大きさ/高さ/緑違い）＋岩・土が少し。遠景(layer1)は林なしで霞ませ簡略化。
- `Env.cs`：`Scenery.Enabled`（false で従来に戻る＝変更前後比較用）、`Scenery.Expand`（false=見本のみ：右側の木(17,0,-6)と手前山 layer0 の i==3 だけ新方式。true で全木・全山＋追加の木）。
- 既存の樹木素材は無し（全部プリミティブ生成）。
- 【未検証】Unity未実行のためコンパイル・見た目は未確認。変更前後のGameビュー画像は未取得。テストプレイで見本の品質を確認 → OKなら `Expand = true` で展開。
- 確認ポイント：右の木の幹/枝/葉の透け感、山の輪郭、夕方の暖色、人物より目立たないか、スマホ負荷。
- バックアップ：`day110/backup_scripts_before_scenery_edit.tgz`
- GitHub更新は未実施（テストプレイ問題なし後）。

## Day110 追記4：①山の輪郭（2026-11-09 夜）※コード変更のみ・Unity未確認
- バックアップ：`day110/backup_scripts_before_scenery_expand.tgz`（この作業の直前のScripts全体）
- `Scenery.cs`：`ExpandHills=true`（山は全部新方式）／`ExpandTrees=false`（木は②まで見本の1本のまま）。`Expand`は従来の一括スイッチ（今はfalse）。`Enabled=false`で変更前に戻る
- 林の塊：小さめ多数＋大は稀（べき乗）、密度ノイズで密/疎を作り等間隔を避ける、緑5色（深緑〜夕日の黄緑）、塊ごとに縦横比と位置をばらす、稜線に小さく背の高い木をまばらに追加
- 遠い山：layer1は林なし・起伏を半分に（soft=0.5）・赤紫の霞色(#c98f8e, haze 0.70)。近い山はばら色(#b8857c, haze 0.30)で明暗差を弱めた
- 軽量化：新方式の山を`FarHills`にまとめ`StaticBatchingUtility.Combine`で同材質を結合（影なし）。`Env.cs`の山ループの呼び出し先を`farHills`に変更
- 【未確認】Unityのコンパイル、見た目（コブの並び・稜線・霞の色）、実機の負荷。括弧の整合のみ確認済み
- テスト点：山の稜線が丸いコブの繰り返しに見えないか／遠い山が赤みで薄く見えるか／人物・カカシより目立たないか／FarHillsのドローコール数
- GitHub更新・プレイURL：未実施

## Day110 追記5：②木の見せ方（2026-11-09 夜）※コード変更のみ・Unity未確認
- ①は「OK」をもらった（ただし①の実機テストプレイでの確認結果は未受領＝Unityでの確認は未実施のまま）
- バックアップ：`day110/backup_scripts_before_tree_edit.tgz`
- 見本の木の位置：(17,0,-6)→`Scenery.SampleTree`=(7.5,0,-19.5)（畑の奥・柵の向こう。カカシ正面カメラの視野に入る。縦画面は端ぎりぎり）。`Enabled=false`なら従来の(17,0,-6)の球の木に戻る
- 木の形：幹の根元をより太く広げて先へ細く／主枝5〜7本で長め／葉の塊を小さく・衛星を0〜2個・中間の葉を減らす・葉のない枝先を2割残す → 枝と空が見える
- 幹色を少し明るく（#765232）
- 足元の丸影：`FootShadow`（青みの暗色・ふち柔らかい板1枚・太陽方向へ少し長く・Sprites/Default・全部の木で材質共有）
- 展開時の追加木(5,-19)は見本と重なるので(15,-17)へ移動。木の展開(`ExpandTrees`)は未実施（見本確認後）
- 【未確認】コンパイル、木の見た目、丸影が地面になじむか、旧い木(9,-15)との重なり。括弧整合のみ確認
- GitHub更新・プレイURL：未実施

## Day110 追記6：③夕方感（2026-11-09 夜）※コード変更のみ・Unity未確認
- バックアップ：`day110/backup_scripts_before_evening_edit.tgz`
- 空：`Scenery.SkyDome`＝頂点カラーだけの空ドーム1枚（Sprites/Default・ライト/フォグ無し・renderQueue=1000で背景として最初に描く）。地平線近く明るい黄(#ffe9a8)→橙→朱→赤紫(#5d3d7e)。ゲーム画面に映るのは地平線の上10〜20度ほどなので、変化はその範囲に詰めた。太陽方向に薄い輝き。作れた時だけ`Env.cs`でカメラを SolidColor にして標準Skyboxの描画を止める（Skyboxが上描きするのを防ぐため）。標準Skybox材質は`Weather.cs`が読むので残してある
- 夜・稲妻：`Weather.cs`のUpdateに`Scenery.SkyNight(k, flash)`を1行追加。夜は空ドームが暗い青に、稲妻で一瞬明るくなる
- 霞：フォグ色 #f0a878→#f6bd86（地平線の空寄り）。フォグ距離は従来(24〜150)のまま。山側は①でばら色の霞色にしてある
- 畝の影：太陽の高さ30°→25°（影が約3割長くなる）、環境光(影の中)を(0.52,0.48,0.58)→(0.47,0.47,0.64)と青寄りに。※太陽の向き/環境光はカカシ・農家の見え方にも少し効く（人物の影側が少し青く、影が少し長くなる）
- 戻し方：`Scenery.EveningSky=false`（または`Enabled=false`）で空・フォグ・太陽・環境光が従来値に戻る
- 【未確認】コンパイル、空の見た目（黄→赤紫の帯が見えるか／境目の霞）、夜・稲妻で空が暗く/光るか、雨粒が空に隠れないか、畝の影が青く長く見えるか、人物が暗くなりすぎないか
- 括弧整合のみ確認済み。GitHub更新・プレイURL：未実施

## Day110 追記7：GitHub更新（2026-11-09 21:44頃）
- タイタンの「プッシュして」を受けて `titan11111/108KAKASHI2` の main へ push（コミット c3f70f7）。Assets/Scripts 11ファイル＋HANDOFF.md（Day110の農家リグ・天候・里山/木/夕焼け空を含む）。README.md はGitHub側が新しいため未反映
- 【重要】`docs/`（GitHub Pagesのプレイ用WebGL）は今日11時台のビルドのまま＝**今日の変更はプレイURLに未反映**。Macで Tools/build_webgl.sh → docs/ へ入れて push が必要
- Unityでのコンパイル・見た目の確認結果は、こちらでは未受領

## Day110 追記8：木の全展開（2026-11-09 21:50頃）※コード変更のみ・Unity未確認
- タイタンから「テストプレイ問題なし。進めて」（見本の木・山・空は問題なしとの報告）。それを受けて `Scenery.ExpandTrees=true`
- バックアップ：`day110/backup_scripts_before_trees_expand.tgz`
- 背景の木4本（13,-10）（見本7.5,-19.5）（-19,-3）（9,-15）が新方式になり、追加の木4本（21,-12）（-23,-7）（15,-17）（-6,-18）も出る。合計8本
- 軽量化：新方式の木を`Trees`にまとめて静的バッチ（同色の材質は共有→幹/葉3色/草/丸影が本数によらず少ないドローコール）
- 戻し方：`ExpandTrees=false`で見本の1本だけに戻る。`Enabled=false`で全部従来
- 【未確認】コンパイル、8本の見た目（人物より目立たないか／小屋・藁山・柵と重ならないか／縦画面で端に出すぎないか）。括弧整合のみ
- GitHub更新：この変更は未push（タイタンのテストプレイOK待ち）。プレイURLも今日の変更は未反映（WebGL再ビルド待ち）

## Day110 追記9：GitHub更新（木の全展開・2026-11-09 21:5x）
- 「最後までやって、携帯でチェックしたい」の依頼で、木の全展開(Env.cs/Scenery.cs)を `titan11111/108KAKASHI2` main へ push
- 【未実施】WebGL再ビルド：Linux側ではUnity(Mac)を動かせないため。`docs/`(Pages)は今日11時台のビルドのまま＝**携帯で開いても今日の変更は出ない**
- 次の最初の操作（Mac）：①Unityを閉じる → ②`Tools/build_webgl.sh` → ③`Build/WebGL` の中身を `docs/` へ上書き（Compression Format=Disabled、`.nojekyll`維持）→ ④push → ⑤`https://titan11111.github.io/108KAKASHI2/?fresh=1` を携帯で開く

## Day110 追記10：WebGL再ビルド＆Pages反映（2026-11-09 22:0x）
- Unityヘッドレスビルド（Tools/build_webgl_hub.sh 新規：起動中Unityから本体を自動検出→閉じたら自動ビルド）。Unity=`.../Unity/Editors/6000.3.26f1/Unity.app`
- **コンパイル：合格**（ログにerror CSなし。Scenery.cs の CS0162 到達不能コード警告のみ＝定数スイッチ由来）。WebGLビルド：完了（22:07 JST）
- `Build/WebGL/Build/*` → `docs/Build/*` に反映（形式は従来と同じ .unityweb。docs/index.html・TemplateData・.nojekyll は変更なし）
- 未確認：Pagesでの起動、携帯での見た目・操作・負荷、Editor Play。プレイURL: https://titan11111.github.io/108KAKASHI2/?fresh=1
