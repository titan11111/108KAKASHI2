#!/bin/zsh
set -eu
project_dir="${0:A:h:h}"
editor_app="$project_dir/.tools/unity-editor/Unity/Unity.app"
if [[ ! -d "$editor_app" ]]; then
  print -u2 'ローカルのUnity Editorが見つかりません。Unity Hubでこのプロジェクトを開いてください。'
  exit 1
fi
export UPM_CACHE_ROOT="$project_dir/.tools/upm-cache"
"$editor_app/Contents/MacOS/Unity" -projectPath "$project_dir" -executeMethod KakashiEditor.KakashiVerification.Play -logFile "$project_dir/Verification/editor.log"
