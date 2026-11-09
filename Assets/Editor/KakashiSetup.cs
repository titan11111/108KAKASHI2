using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KakashiEditor
{
    /// <summary>
    /// プロジェクトを開くと、メインシーンを自動で作る。
    /// メニュー「Kakashi」からシーン再生成・WebGLビルドもできる。
    /// </summary>
    [InitializeOnLoad]
    public static class KakashiSetup
    {
        const string ScenePath = "Assets/Scenes/Main.unity";

        static KakashiSetup()
        {
            EditorApplication.delayCall += AutoSetup;
        }

        static void AutoSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!File.Exists(ScenePath)) CreateScene();
        }

        [MenuItem("Kakashi/シーンを作る・設定する")]
        public static void CreateScene()
        {
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("KakashiGame").AddComponent<Kakashi.GameController>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            PlayerSettings.productName = "カカシ工房";
            PlayerSettings.companyName = "titan";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            // コードで Shader.Find するシェーダーは、ビルドから外れないよう登録しておく
            EnsureShader("Standard");
            EnsureShader("Skybox/Procedural");
            EnsureShader("Unlit/Texture");
            EnsureShader("Universal Render Pipeline/Lit");
            AssetDatabase.SaveAssets();
            Debug.Log("[Kakashi] Assets/Scenes/Main.unity を作りました。▶ を押すと遊べます。");
        }

        static void EnsureShader(string shaderName)
        {
            var sh = Shader.Find(shaderName);
            if (sh == null) return;
            var gs = AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/GraphicsSettings.asset");
            if (gs == null) return;
            var so = new SerializedObject(gs);
            var arr = so.FindProperty("m_AlwaysIncludedShaders");
            if (arr == null) return;
            for (int i = 0; i < arr.arraySize; i++)
                if (arr.GetArrayElementAtIndex(i).objectReferenceValue == sh) return;
            arr.arraySize++;
            arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = sh;
            so.ApplyModifiedProperties();
        }

        /// <summary>コマンドライン用：Tools/build_webgl.sh から呼ばれる。失敗時は終了コード1。</summary>
        public static void BuildWebGLBatch()
        {
            EditorApplication.Exit(BuildWebGLInternal() ? 0 : 1);
        }

        [MenuItem("Kakashi/iOS設定を適用（縦横自動回転・Bundle ID）")]
        public static void ConfigureIOS()
        {
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.iOS, "com.titan.kakashi");
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            AssetDatabase.SaveAssets();
            Debug.Log("[Kakashi] iOS設定を適用しました（com.titan.kakashi / 縦横自動回転）。署名チームはXcode側で選択してください。");
        }

        static void ConfigureWebGL()
        {
            // GitHub Pages は Content-Encoding ヘッダを付けられないので、ブラウザ側で解凍するフォールバックを使う
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.initialMemorySize = 256;      // iPhone Safariでも落ちにくい控えめ設定
            PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.Geometric;
            PlayerSettings.WebGL.template = "APPLICATION:Default";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        }

        static bool BuildWebGLInternal()
        {
            if (!File.Exists(ScenePath)) CreateScene();
            ConfigureWebGL();
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Build/WebGL",
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(opts);
            Debug.Log("[Kakashi] WebGLビルド結果: " + report.summary.result);
            return report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded;
        }

        [MenuItem("Kakashi/WebGLビルド（Build/WebGL）")]
        public static void BuildWebGL()
        {
            BuildWebGLInternal();
        }
    }
}
