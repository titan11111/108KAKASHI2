#!/bin/zsh
# WebGLビルド。Unityを開いたまま実行 → 場所を自動検出 → 「Unityを閉じて」と出たら閉じる → 自動でビルド開始。出力: Build/WebGL
set -u
project_dir="${0:A:h:h}"
editor_app=""

# 1) 起動中のUnityエディタから場所を取る（Unity Hubは対象外）
line="$(ps -axo command= | grep -m1 '/Unity\.app/Contents/MacOS/Unity' || true)"
if [[ -n "$line" ]]; then
  editor_app="$(print -r -- "$line" | sed -E 's#^(.*/Unity\.app)/Contents/MacOS/Unity.*#\1#')"
fi
# 2) 見つからなければ、よくある場所とSpotlight
if [[ -z "$editor_app" || ! -d "$editor_app" ]]; then
  editor_app=""
  for c in "$project_dir/.tools/unity-editor/Unity/Unity.app" "/Applications/Unity/Hub/Editor/6000.3.26f1/Unity.app" "$HOME/Applications/Unity/Hub/Editor/6000.3.26f1/Unity.app"; do
    [[ -d "$c" ]] && { editor_app="$c"; break; }
  done
fi
if [[ -z "$editor_app" ]]; then
  editor_app="$(mdfind "kMDItemCFBundleIdentifier == 'com.unity3d.UnityEditor5.x'" | head -1)"
fi
if [[ -z "$editor_app" || ! -d "$editor_app" ]]; then
  print -u2 "Unityエディタの場所を特定できませんでした。Unityを開いた状態でもう一度実行してください。"; exit 1
fi
print "Unity: $editor_app"
if [[ ! -d "$editor_app/Contents/PlaybackEngines/WebGLSupport" ]]; then
  print -u2 "このUnityに WebGL Build Support が入っていません（Unity Hub → Installs → ⚙ → モジュールを追加 → WebGL）"; exit 1
fi

# 3) Unityが開いている間は待つ（同じプロジェクトを2重に開けないため）
if pgrep -f "$editor_app/Contents/MacOS/Unity" >/dev/null 2>&1; then
  print "👉 Unityエディタを閉じてください（保存を聞かれたら保存）。閉じたら自動でビルドを始めます…"
  while pgrep -f "$editor_app/Contents/MacOS/Unity" >/dev/null 2>&1; do sleep 3; done
fi

mkdir -p "$project_dir/Verification"
print "ビルド開始（数分かかります）"
"$editor_app/Contents/MacOS/Unity" -batchmode -quit -projectPath "$project_dir" \
  -executeMethod KakashiEditor.KakashiSetup.BuildWebGLBatch \
  -logFile "$project_dir/Verification/webgl-build.log"
rc=$?
if [[ $rc -eq 0 ]]; then
  print "✅ 完了: $project_dir/Build/WebGL"
else
  print -u2 "❌ 失敗(code $rc)。ログ末尾："; tail -40 "$project_dir/Verification/webgl-build.log" 2>/dev/null
  exit $rc
fi
