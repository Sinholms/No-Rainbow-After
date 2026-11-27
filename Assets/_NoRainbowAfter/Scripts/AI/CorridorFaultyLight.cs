using NoRainbowAfter.Player;
using UnityEngine;

namespace NoRainbowAfter.AI
{
    /// <summary>
    /// Lampu lorong rusak berkedip tak teratur: setiap pergantian daya memicu percikan
    /// partikel singkat, kilatan cahaya (Flash), serta suara korsleting listrik.
    ///
    /// [GRAFIKA KOMPUTER]
    /// Menggunakan MaterialPropertyBlock untuk mengubah _BaseColor &amp; _EmissionColor tabung lampu
    /// secara hemat memori tanpa menduplikasi instance Material.
    /// </summary>
    public sealed class CorridorFaultyLight : MonoBehaviour
    {
        public Light Lamp;
        public Light Flash;
        public Renderer[] Tubes;
        public ParticleSystem Sparks;
        public AudioSource Crackle;
        public int Seed = 1;
        public float RestIntensity = 2.6f;

        public bool Powered { get; private set; }
        public int TransitionCount { get; private set; }
        public int SparkBurstCount { get; private set; }

        private System.Random random;
        private MaterialPropertyBlock block;
        private FirstPersonPlayer player;
        private float remaining;
        private float flashRemaining;
        private int edgesRemaining;

        private void OnEnable()
        {
            random = new System.Random(Seed);
            block = new MaterialPropertyBlock();
            player = FindAnyObjectByType<FirstPersonPlayer>();
            Powered = true;
            TransitionCount = 0;
            SparkBurstCount = 0;
            edgesRemaining = 0;
            remaining = Next(1f, 2f);
            ApplyPower();
            if (Flash != null)
            {
                Flash.enabled = false;
            }
        }

        private float Next(float min, float max) => min + (float)random.NextDouble() * (max - min);

        private void Update()
        {
            if (player != null && player.IsPaused)
            {
                return;
            }
            Tick(Time.deltaTime);
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0)
            {
                return;
            }
            flashRemaining -= deltaTime;
            if (Flash != null && flashRemaining <= 0)
            {
                Flash.enabled = false;
            }
            remaining -= deltaTime;
            if (remaining > 0)
            {
                return;
            }
            if (edgesRemaining == 0)
            {
                edgesRemaining = random.Next(1, 3) * 2;
            }
            SetPowered(!Powered);
            edgesRemaining--;
            remaining = edgesRemaining > 0 ? Next(.09f, .22f) : Next(1f, 2f);
        }

        public void SetPowered(bool powered)
        {
            if (Powered == powered)
            {
                return;
            }
            Powered = powered;
            TransitionCount++;
            ApplyPower();

            if (Sparks != null)
            {
                Sparks.Emit(7);
                SparkBurstCount++;
            }
            if (Flash != null)
            {
                Flash.enabled = true;
                flashRemaining = .045f;
            }
            if (Crackle != null && Crackle.clip != null)
            {
                Crackle.pitch = Next(.88f, 1.14f);
                Crackle.PlayOneShot(Crackle.clip);
            }
        }

        private void ApplyPower()
        {
            if (Lamp != null)
            {
                Lamp.intensity = Powered ? RestIntensity : 0;
            }
            if (block == null)
            {
                block = new MaterialPropertyBlock();
            }
            block.SetColor("_EmissionColor", Powered ? new Color(2.2f, 2.05f, 1.65f) : Color.black);
            block.SetColor("_BaseColor", Powered ? new Color(.84f, .81f, .67f) : new Color(.12f, .13f, .12f));
            if (Tubes != null)
            {
                foreach (var tube in Tubes)
                {
                    if (tube != null)
                    {
                        tube.SetPropertyBlock(block);
                    }
                }
            }
        }

        private void OnDisable()
        {
            if (Sparks != null) Sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (Crackle != null) Crackle.Stop();
            if (Flash != null) Flash.enabled = false;
            Powered = true;
            ApplyPower();
        }
    }
}
