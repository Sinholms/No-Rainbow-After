using UnityEngine;
using UnityEngine.UI;

namespace NoRainbowAfter.AI
{
    /// <summary>
    /// Pemunculan monster bayangan beserta rig animasinya, audio detak jantung kejaran,
    /// serta UI penghitung mundur 2 menit samar pada ChapterThreeFlow.
    /// </summary>
    public sealed partial class ChapterThreeFlow
    {
        private void SpawnMonster()
        {
            if (monsterObject != null)
            {
                Destroy(monsterObject);
            }

            Vector3 spawnPos = arena.MonsterSpawn != null
                ? arena.MonsterSpawn.position
                : arena.transform.position + new Vector3(0f, 0.12f, 19.5f);
            Quaternion spawnRot = arena.MonsterSpawn != null
                ? arena.MonsterSpawn.rotation
                : Quaternion.Euler(0f, 180f, 0f);

            monsterObject = new GameObject("C3_ShadowMonster", typeof(CharacterController));
            monsterObject.transform.SetPositionAndRotation(spawnPos, spawnRot);
            monsterController = monsterObject.GetComponent<CharacterController>();
            monsterController.radius = 0.34f;
            monsterController.height = 2.0f;
            monsterController.center = new Vector3(0f, 1.0f, 0f);

            // Prioritas visual: prefab khusus Chapter 3 (di dalam VisualWrapper agar ShadowMotionPlayback
            // mendapat facingRoot tepat ke monsterObject), lalu fallback ke scene Chapter 1 atau Resources.
            Transform visualWrapper = null;
            if (monsterVisualPrefab != null)
            {
                var wrapper = new GameObject("VisualWrapper");
                wrapper.transform.SetParent(monsterObject.transform, false);
                var cloned = Instantiate(monsterVisualPrefab, wrapper.transform);
                cloned.SetActive(true);
                visualWrapper = wrapper.transform;
            }
            else
            {
                var existingController = FindAnyObjectByType<ShadowEncounterController>(FindObjectsInactive.Include);
                if (existingController != null && existingController.transform.childCount > 0)
                {
                    var cloned = Instantiate(existingController.transform.GetChild(0).gameObject, monsterObject.transform);
                    cloned.SetActive(true);
                    visualWrapper = cloned.transform;
                }
                else
                {
                    var motionPrefab = Resources.Load<GameObject>("C1ShadowMotion");
                    if (motionPrefab != null)
                    {
                        var cloned = Instantiate(motionPrefab, monsterObject.transform);
                        visualWrapper = cloned.transform;
                    }
                }
            }

            if (visualWrapper != null)
            {
                monsterVisual = monsterObject.AddComponent<ShadowPlaceholderVisual>();
                monsterVisual.Configure(visualWrapper, null);
                monsterVisual.SetVisible(true);
                monsterVisual.ResetPostureImmediate();
            }
        }

        private void StartChaseAudio()
        {
            if (chaseAudio == null)
            {
                chaseAudio = gameObject.AddComponent<AudioSource>();
                chaseAudio.playOnAwake = false;
                chaseAudio.loop = true;
                chaseAudio.spatialBlend = 0f;
                chaseAudio.volume = 0.14f;

                const int rate = 22050;
                const float dur = 0.85f;
                int count = Mathf.RoundToInt(rate * dur);
                var data = new float[count];
                for (int i = 0; i < count; i++)
                {
                    float t = (float)i / rate;
                    float beat1 = Mathf.Exp(-Mathf.Pow((t - 0.12f) / 0.04f, 2f)) * Mathf.Sin(2f * Mathf.PI * 50f * t);
                    float beat2 = Mathf.Exp(-Mathf.Pow((t - 0.30f) / 0.05f, 2f)) * Mathf.Sin(2f * Mathf.PI * 44f * t) * 0.72f;
                    data[i] = Mathf.Clamp((beat1 + beat2) * 0.85f, -1f, 1f);
                }
                chaseClip = AudioClip.Create("Chapter3ChasePulse", count, 1, rate, false);
                chaseClip.SetData(data, 0);
                chaseAudio.clip = chaseClip;
            }
            if (!chaseAudio.isPlaying)
            {
                chaseAudio.Play();
            }
        }

        private void BuildTimerUi()
        {
            if (timerCanvas != null)
            {
                timerCanvas.enabled = true;
                return;
            }

            var go = new GameObject("ChapterThreeTimerUI", typeof(Canvas), typeof(CanvasScaler));
            go.transform.SetParent(transform, false);
            timerCanvas = go.GetComponent<Canvas>();
            timerCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            timerCanvas.sortingOrder = 14;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var labelGo = new GameObject("FaintCountdown", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(go.transform, false);
            var rect = (RectTransform)labelGo.transform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -26f);
            rect.sizeDelta = new Vector2(220f, 44f);

            faintTimerLabel = labelGo.GetComponent<Text>();
            faintTimerLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            faintTimerLabel.fontSize = 24;
            faintTimerLabel.alignment = TextAnchor.MiddleCenter;
            // Teks countdown sengaja dibuat samar dan pucat sesuai desain horor Chapter 3.
            faintTimerLabel.color = new Color(0.68f, 0.76f, 0.76f, 0.36f);
            faintTimerLabel.raycastTarget = false;
        }

        private void RefreshTimerUi()
        {
            if (faintTimerLabel == null)
            {
                return;
            }
            int total = Mathf.CeilToInt(Mathf.Max(0f, RemainingSeconds));
            int mins = total / 60;
            int secs = total % 60;
            faintTimerLabel.text = $"{mins:00}:{secs:00}";
        }
    }
}
