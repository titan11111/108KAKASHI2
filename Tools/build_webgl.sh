#!/bin/zsh
# WebGLビルド（Unityは閉じてから実行）。出力: Build/WebGL
set -eu
project_dir="${0:A:h:h}"
editor_app="$project_dir/.tools/unity-editor/Unity/Unity.app"
if [[ ! -d "$editor_app/Contents/PlaybackEngines/WebGLSupport" ]]; then
  print -u2 'WebGL Build Support が未インストールです。Unity Hub →「Editorを追加（ディスクから）」で .tools のEditorを登録し、WebGL Build Support を追加してください。'
  exit 1
fi
export UPM_CACHE_ROOT="$project_dir/.tools/upm-cache"
"$editor_app/Contents/MacOS/Unity" -batchmode -quit -projectPath "$project_dir" -executeMethod KakashiEditor.KakashiSetup.BuildWebGLBatch -logFile "$project_dir/Verification/webgl-build.log"
print "完了: $project_dir/Build/WebGL"
