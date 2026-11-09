using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace KakashiEditor
{
    // A small Editor-side smoke runner. It leaves the game playing after checks.
    [InitializeOnLoad]
    public static class KakashiVerification
    {
        static readonly List<string> errors = new List<string>();
        static int stage;
        static double next;
        static string pendingShot;
        static string dir => Path.GetFullPath("Verification");

        static KakashiVerification()
        {
            Application.logMessageReceived += (message, stack, type) =>
            {
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message + "\n" + stack);
            };
            EditorApplication.update += Tick;
        }

        [MenuItem("Kakashi/再生して動作確認")]
        public static void Play()
        {
            Directory.CreateDirectory(dir);
            errors.Clear(); stage = 0;
            SessionState.SetBool("Kakashi.Verify", true);
            if (EditorApplication.isPlaying) { next = EditorApplication.timeSinceStartup + 3; return; }
            if (!File.Exists("Assets/Scenes/Main.unity")) KakashiSetup.CreateScene();
            else EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
            next = EditorApplication.timeSinceStartup + 3;
            EditorApplication.isPlaying = true;
        }

        static bool HasActive(string name)
        {
            foreach (var button in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None))
                if (button.name == name && button.gameObject.activeInHierarchy) return true;
            return false;
        }

        static void Click(string name)
        {
            foreach (var button in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None))
                if (button.name == name && button.gameObject.activeInHierarchy) { button.onClick.Invoke(); return; }
            throw new Exception("Active button missing: " + name);
        }

        static void Tick()
        {
            if (!SessionState.GetBool("Kakashi.Verify", false) || !EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.timeSinceStartup < next) return;
            try
            {
                if (pendingShot != null)
                {
                    // Capture one tick after the click, so the new screen is the one saved.
                    ScreenCapture.CaptureScreenshot(pendingShot); pendingShot = null;
                    next = EditorApplication.timeSinceStartup + 0.5; return;
                }
                switch (stage)
                {
                    case 0:
                        CheckData(); Click("start");
                        pendingShot = Path.Combine(dir, "workshop-start.png"); break;
                    case 1:
                        Click("shape");
                        pendingShot = Path.Combine(dir, "shape.png"); break;
                    case 2:
                        var slider = GameObject.Find("UI/SafeArea/ShapePanel/傾き/slider").GetComponent<Slider>();
                        slider.value = 20;
                        var scarecrow = UnityEngine.Object.FindFirstObjectByType<Kakashi.Scarecrow>();
                        Require(scarecrow.ShapePenalty > 0, "Shape changes stability");
                        Click("reset"); Require(scarecrow.ShapePenalty == 0, "Shape reset");
                        Click("done"); break;
                    case 3:
                        Click("rand"); Click("go"); Click("skipBtn"); break;
                    case 4:
                        // Wait for the entire animal and storm sequence, with a bounded deadline.
                        if (!HasActive("next"))
                        {
                            if (EditorApplication.timeSinceStartup > SessionState.GetFloat("Kakashi.Deadline", 0)) throw new Exception("Test sequence timed out | frame=" + Time.frameCount + " gameTime=" + Time.time.ToString("0.0") + " timeScale=" + Time.timeScale + " focused=" + UnityEditorInternal.InternalEditorUtility.isApplicationActive);
                            next = EditorApplication.timeSinceStartup + 1; return;
                        }
                        Click("next"); break;
                    case 5: Click("next"); break;
                    case 6:
                        Click("next");
                        pendingShot = Path.Combine(dir, "results.png"); break;
                    case 7:
                        Require(GameObject.Find("UI/SafeArea/ResultPanel") != null, "Results visible");
                        Click("again"); break;
                    case 8:
                        Require(GameObject.Find("UI/SafeArea/BuildPanel") != null, "Return to workshop");
                        Require(Mathf.Approximately(Time.timeScale, 1), "Speed reset");
                        pendingShot = Path.Combine(dir, "workshop.png");
                        break;
                    case 9:
                        Finish(errors.Count == 0); return;
                }
                stage++;
                if (stage == 4) SessionState.SetFloat("Kakashi.Deadline", (float)EditorApplication.timeSinceStartup + 300);
                next = EditorApplication.timeSinceStartup + (stage == 1 ? 3 : 1.5);
            }
            catch (Exception exception) { errors.Add(exception.ToString()); Finish(false); }
        }

        static void Require(bool condition, string label) { if (!condition) throw new Exception(label); }

        static void CheckData()
        {
            var selection = new Dictionary<string, string>();
            foreach (var slot in Kakashi.Data.Slots) selection[slot.id] = slot.opts[0].id;
            var score = Kakashi.Data.Calc(selection);
            Require(score.Cost <= Kakashi.Data.Budget, "Initial build within budget");
            var events = Kakashi.Data.Simulate(selection, score);
            Require(events.Count >= 5, "All encounters exist");
            foreach (var farmer in Kakashi.Data.Farmers)
            {
                var result = Kakashi.Data.Judge(farmer, score, selection, events);
                Require(result.Total >= 0 && result.Total <= 100 && !string.IsNullOrEmpty(result.Comment), "Farmer result");
            }
        }

        static void Finish(bool passed)
        {
            SessionState.SetBool("Kakashi.Verify", false);
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "play-report.txt"),
                "Passed: " + passed + "\nEditor: " + Application.unityVersion + "\nProject: " + Application.dataPath +
                "\nStage: " + stage + "\nPlaying: " + EditorApplication.isPlaying + "\nErrors: " + errors.Count + "\n" + string.Join("\n", errors));
            Debug.Log("[Kakashi] Play verification " + (passed ? "passed" : "FAILED") + ". See Verification/play-report.txt");
        }
    }
}
