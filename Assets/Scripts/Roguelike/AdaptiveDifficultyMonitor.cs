using System.Collections.Generic;
using UnityEngine;

public sealed class AdaptiveDifficultyMonitor
{
    public enum DifficultyBand
    {
        Skilled,
        Normal,
        Novice
    }

    private struct StatSample
    {
        public float Time;
        public float PlayerLevel;
        public float FireRate;
        public float AttackPower;
    }

    private const float WindowSeconds = 90f;
    private const float SampleInterval = 1f;
    private const float EntropySmoothFactor = 0.12f;
    private const float InitialEntropy = 0.44f;
    private const float SkilledEntropyThreshold = 0.26f;
    private const float NoviceEntropyThreshold = 0.8f;

    private readonly Queue<StatSample> statSamples = new Queue<StatSample>();
    private readonly Queue<float> rewindEvents = new Queue<float>();
    private readonly Queue<float> killEvents = new Queue<float>();

    private float elapsedTime;
    private float sampleTimer;
    private float baseLevel = 1f;
    private float baseFireRate = 1f;
    private float baseAttackPower = 1f;

    public float CurrentEntropy { get; private set; } = InitialEntropy;
    public DifficultyBand CurrentBand { get; private set; } = DifficultyBand.Normal;
    public float AveragePlayerLevel { get; private set; } = 1f;
    public float AverageFireRate { get; private set; } = 1f;
    public float AverageAttackPower { get; private set; } = 1f;
    public float RewindRatePerMinute { get; private set; }
    public float KillEfficiencyPerSecond { get; private set; }

    public float CurrentEnemyDensityMultiplier =>
        CurrentBand == DifficultyBand.Novice ? 0.72f :
        CurrentBand == DifficultyBand.Skilled ? 1.3f : 1.08f;

    public float CurrentEnemyAttackIntervalMultiplier =>
        CurrentBand == DifficultyBand.Novice ? 1.1f :
        CurrentBand == DifficultyBand.Skilled ? 0.95f : 1f;

    public float CurrentEnemyHpMultiplier =>
        CurrentBand == DifficultyBand.Skilled ? 1.28f :
        CurrentBand == DifficultyBand.Normal ? 1.06f : 1f;

    public float CurrentEnemyAttackMultiplier =>
        CurrentBand == DifficultyBand.Skilled ? 1.22f :
        CurrentBand == DifficultyBand.Normal ? 1.06f : 1f;

    public float CurrentEliteRatio =>
        CurrentBand == DifficultyBand.Skilled ? 0.35f : 0f;

    public void Initialize(int playerLevel, float fireRate, float attackPower)
    {
        statSamples.Clear();
        rewindEvents.Clear();
        killEvents.Clear();
        elapsedTime = 0f;
        sampleTimer = 0f;
        baseLevel = Mathf.Max(1f, playerLevel);
        baseFireRate = Mathf.Max(0.1f, fireRate);
        baseAttackPower = Mathf.Max(1f, attackPower);
        CurrentEntropy = InitialEntropy;
        CurrentBand = DifficultyBand.Normal;
        CaptureCurrentState(playerLevel, fireRate, attackPower);
    }

    public void Tick(float deltaTime, int playerLevel, float fireRate, float attackPower)
    {
        float safeDeltaTime = Mathf.Max(0f, deltaTime);
        elapsedTime += safeDeltaTime;
        sampleTimer += safeDeltaTime;

        while (sampleTimer >= SampleInterval)
        {
            sampleTimer -= SampleInterval;
            AddSample(playerLevel, fireRate, attackPower);
        }

        Recalculate();
    }

    public void CaptureCurrentState(int playerLevel, float fireRate, float attackPower)
    {
        AddSample(playerLevel, fireRate, attackPower);
        Recalculate();
    }

    public void RecordKill()
    {
        killEvents.Enqueue(elapsedTime);
        Recalculate();
    }

    public void RecordRewind()
    {
        rewindEvents.Enqueue(elapsedTime);
        Recalculate();
    }

    private void AddSample(int playerLevel, float fireRate, float attackPower)
    {
        statSamples.Enqueue(new StatSample
        {
            Time = elapsedTime,
            PlayerLevel = Mathf.Max(1f, playerLevel),
            FireRate = Mathf.Max(0.1f, fireRate),
            AttackPower = Mathf.Max(1f, attackPower)
        });
    }

    private void Recalculate()
    {
        PruneOldEntries();

        AveragePlayerLevel = AveragePlayerLevel == 0f ? baseLevel : AveragePlayerLevel;
        AverageFireRate = AverageFireRate == 0f ? baseFireRate : AverageFireRate;
        AverageAttackPower = AverageAttackPower == 0f ? baseAttackPower : AverageAttackPower;

        if (statSamples.Count > 0)
        {
            float levelSum = 0f;
            float fireRateSum = 0f;
            float attackPowerSum = 0f;
            foreach (StatSample sample in statSamples)
            {
                levelSum += sample.PlayerLevel;
                fireRateSum += sample.FireRate;
                attackPowerSum += sample.AttackPower;
            }

            float sampleCount = statSamples.Count;
            AveragePlayerLevel = levelSum / sampleCount;
            AverageFireRate = fireRateSum / sampleCount;
            AverageAttackPower = attackPowerSum / sampleCount;
        }

        float effectiveWindow = Mathf.Clamp(elapsedTime, 15f, WindowSeconds);
        RewindRatePerMinute = rewindEvents.Count * 60f / effectiveWindow;
        KillEfficiencyPerSecond = killEvents.Count / effectiveWindow;

        float levelPressure = 1f - Smooth01(Mathf.InverseLerp(baseLevel, baseLevel + 8f, AveragePlayerLevel));
        float fireRatePressure = 1f - Smooth01(Mathf.InverseLerp(baseFireRate, baseFireRate * 1.7f, AverageFireRate));
        float attackPressure = 1f - Smooth01(Mathf.InverseLerp(baseAttackPower, baseAttackPower * 2f, AverageAttackPower));
        float rewindPressure = Smooth01(Mathf.InverseLerp(0f, 0.9f, RewindRatePerMinute));
        float killPressure = 1f - Smooth01(Mathf.InverseLerp(0.02f, 0.25f, KillEfficiencyPerSecond));

        float targetEntropy = Mathf.Clamp01((levelPressure + fireRatePressure + attackPressure + rewindPressure + killPressure) / 5f);
        CurrentEntropy = Mathf.Lerp(CurrentEntropy, targetEntropy, EntropySmoothFactor);
        CurrentBand = ResolveBand(CurrentEntropy);
    }

    private void PruneOldEntries()
    {
        float cutoffTime = elapsedTime - WindowSeconds;

        while (statSamples.Count > 0 && statSamples.Peek().Time < cutoffTime)
        {
            statSamples.Dequeue();
        }

        while (rewindEvents.Count > 0 && rewindEvents.Peek() < cutoffTime)
        {
            rewindEvents.Dequeue();
        }

        while (killEvents.Count > 0 && killEvents.Peek() < cutoffTime)
        {
            killEvents.Dequeue();
        }
    }

    private static DifficultyBand ResolveBand(float entropy)
    {
        if (entropy > NoviceEntropyThreshold)
        {
            return DifficultyBand.Novice;
        }

        if (entropy < SkilledEntropyThreshold)
        {
            return DifficultyBand.Skilled;
        }

        return DifficultyBand.Normal;
    }

    private static float Smooth01(float value)
    {
        float clamped = Mathf.Clamp01(value);
        return clamped * clamped * (3f - (2f * clamped));
    }
}
