using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public sealed class SessionCardState
{
    public string CardKey;
    public int StackCount;
}

public sealed class SessionEnemyState
{
    public string EnemyKey;
    public float PositionX;
    public float PositionY;
    public int CurrentHp;
    public bool IsElite;
}

public sealed class SessionPickupState
{
    public float PositionX;
    public float PositionY;
    public float ExperienceValue;
    public bool GrantsFreeLevel;
}

public sealed class SessionSnapshotData
{
    public PlayerType PlayerType;
    public WeaponType WeaponType;
    public float ElapsedTime;
    public int CurrentWave;
    public int PlayerLevel;
    public float CurrentExp;
    public float ExpToNextLevel;
    public float PlayerPositionX;
    public float PlayerPositionY;
    public int PlayerHp;
    public int CurrentAmmo;
    public float ReloadRemaining;
    public float SkillCooldownRemaining;
    public int RewindUsesRemaining;
    public int PendingLevelUpChoices;
    public List<SessionCardState> Cards = new List<SessionCardState>();
    public List<SessionEnemyState> Enemies = new List<SessionEnemyState>();
    public List<SessionPickupState> Pickups = new List<SessionPickupState>();
}

public sealed class SavedSessionInfo
{
    public string FilePath;
    public string DisplayName;
    public DateTime SavedAt;
    public bool IsContinueSave;
    public SessionSnapshotData Snapshot;
}

public static class SessionSaveRepository
{
    private const int FileVersion = 1;
    private const string SessionFileName = "roguelike-session-save.bin";
    private const string SnapshotDirectoryName = "RoguelikeSnapshots";
    private const string ManualSaveDirectoryName = "RoguelikeSaves";

    private static string pendingLoadPath;

    public static bool HasSavedSession()
    {
        return File.Exists(GetSavePath());
    }

    public static bool HasAnySavedSession()
    {
        if (HasSavedSession())
        {
            return true;
        }

        string directory = GetManualSaveDirectory();
        return Directory.Exists(directory) && Directory.GetFiles(directory, "*.bin").Length > 0;
    }

    public static bool HasPendingSaveSelection()
    {
        return !string.IsNullOrWhiteSpace(pendingLoadPath) && File.Exists(pendingLoadPath);
    }

    public static bool TryLoadSavedSession(out SessionSnapshotData snapshot)
    {
        return TryReadSnapshot(GetSavePath(), out snapshot);
    }

    public static bool TryLoadRequestedSession(out SessionSnapshotData snapshot)
    {
        string selectedPath = pendingLoadPath;
        pendingLoadPath = null;

        if (!string.IsNullOrWhiteSpace(selectedPath) && TryReadSnapshot(selectedPath, out snapshot))
        {
            return true;
        }

        return TryLoadSavedSession(out snapshot);
    }

    public static void SaveSession(SessionSnapshotData snapshot)
    {
        WriteSnapshot(GetSavePath(), snapshot);
    }

    public static void CreateManualSave(SessionSnapshotData snapshot)
    {
        SaveSession(snapshot);

        string directory = GetManualSaveDirectory();
        Directory.CreateDirectory(directory);
        string fileName = $"save_{DateTime.Now:yyyyMMdd_HHmmss}_wave{Mathf.Max(1, snapshot.CurrentWave)}_{snapshot.PlayerType}_{snapshot.WeaponType}.bin";
        WriteSnapshot(Path.Combine(directory, fileName), snapshot);
    }

    public static void ClearSavedSession()
    {
        string savePath = GetSavePath();
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
        }
    }

    public static void WriteSnapshot(SessionSnapshotData snapshot, int maxSnapshotCount)
    {
        string directory = GetSnapshotDirectory();
        Directory.CreateDirectory(directory);
        string filePath = Path.Combine(directory, $"snapshot_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}.bin");
        WriteSnapshot(filePath, snapshot);

        string[] files = Directory.GetFiles(directory, "*.bin");
        Array.Sort(files, StringComparer.OrdinalIgnoreCase);
        int keepCount = Mathf.Max(1, maxSnapshotCount);
        for (int index = 0; index < files.Length - keepCount; index++)
        {
            File.Delete(files[index]);
        }
    }

    public static bool TryLoadLatestSnapshot(out SessionSnapshotData snapshot)
    {
        string directory = GetSnapshotDirectory();
        snapshot = null;
        if (!Directory.Exists(directory))
        {
            return false;
        }

        string[] files = Directory.GetFiles(directory, "*.bin");
        Array.Sort(files, StringComparer.OrdinalIgnoreCase);
        for (int index = files.Length - 1; index >= 0; index--)
        {
            if (TryReadSnapshot(files[index], out snapshot))
            {
                return true;
            }
        }

        return false;
    }

    public static void ClearSnapshots()
    {
        string directory = GetSnapshotDirectory();
        if (!Directory.Exists(directory))
        {
            return;
        }

        string[] files = Directory.GetFiles(directory, "*.bin");
        for (int index = 0; index < files.Length; index++)
        {
            File.Delete(files[index]);
        }
    }

    public static List<SavedSessionInfo> GetSavedSessions(int maxCount = 12)
    {
        List<SavedSessionInfo> sessions = new List<SavedSessionInfo>();
        AddSavedSessionInfo(sessions, GetSavePath(), true);

        string directory = GetManualSaveDirectory();
        if (Directory.Exists(directory))
        {
            string[] files = Directory.GetFiles(directory, "*.bin");
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            for (int index = files.Length - 1; index >= 0; index--)
            {
                AddSavedSessionInfo(sessions, files[index], false);
            }
        }

        sessions.Sort((left, right) => right.SavedAt.CompareTo(left.SavedAt));
        if (maxCount > 0 && sessions.Count > maxCount)
        {
            sessions.RemoveRange(maxCount, sessions.Count - maxCount);
        }

        return sessions;
    }

    public static void SelectSaveForLoad(string filePath)
    {
        pendingLoadPath = string.IsNullOrWhiteSpace(filePath) ? null : filePath;
    }

    private static void WriteSnapshot(string path, SessionSnapshotData snapshot)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? Application.persistentDataPath);
        using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            writer.Write(FileVersion);
            writer.Write((int)snapshot.PlayerType);
            writer.Write((int)snapshot.WeaponType);
            writer.Write(snapshot.ElapsedTime);
            writer.Write(snapshot.CurrentWave);
            writer.Write(snapshot.PlayerLevel);
            writer.Write(snapshot.CurrentExp);
            writer.Write(snapshot.ExpToNextLevel);
            writer.Write(snapshot.PlayerPositionX);
            writer.Write(snapshot.PlayerPositionY);
            writer.Write(snapshot.PlayerHp);
            writer.Write(snapshot.CurrentAmmo);
            writer.Write(snapshot.ReloadRemaining);
            writer.Write(snapshot.SkillCooldownRemaining);
            writer.Write(snapshot.RewindUsesRemaining);
            writer.Write(snapshot.PendingLevelUpChoices);

            writer.Write(snapshot.Cards.Count);
            for (int index = 0; index < snapshot.Cards.Count; index++)
            {
                writer.Write(snapshot.Cards[index].CardKey ?? string.Empty);
                writer.Write(snapshot.Cards[index].StackCount);
            }

            writer.Write(snapshot.Enemies.Count);
            for (int index = 0; index < snapshot.Enemies.Count; index++)
            {
                SessionEnemyState enemy = snapshot.Enemies[index];
                writer.Write(enemy.EnemyKey ?? string.Empty);
                writer.Write(enemy.PositionX);
                writer.Write(enemy.PositionY);
                writer.Write(enemy.CurrentHp);
                writer.Write(enemy.IsElite);
            }

            writer.Write(snapshot.Pickups.Count);
            for (int index = 0; index < snapshot.Pickups.Count; index++)
            {
                SessionPickupState pickup = snapshot.Pickups[index];
                writer.Write(pickup.PositionX);
                writer.Write(pickup.PositionY);
                writer.Write(pickup.ExperienceValue);
                writer.Write(pickup.GrantsFreeLevel);
            }
        }
    }

    private static bool TryReadSnapshot(string path, out SessionSnapshotData snapshot)
    {
        snapshot = null;
        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                if (reader.ReadInt32() != FileVersion)
                {
                    return false;
                }

                snapshot = new SessionSnapshotData
                {
                    PlayerType = (PlayerType)reader.ReadInt32(),
                    WeaponType = (WeaponType)reader.ReadInt32(),
                    ElapsedTime = reader.ReadSingle(),
                    CurrentWave = reader.ReadInt32(),
                    PlayerLevel = reader.ReadInt32(),
                    CurrentExp = reader.ReadSingle(),
                    ExpToNextLevel = reader.ReadSingle(),
                    PlayerPositionX = reader.ReadSingle(),
                    PlayerPositionY = reader.ReadSingle(),
                    PlayerHp = reader.ReadInt32(),
                    CurrentAmmo = reader.ReadInt32(),
                    ReloadRemaining = reader.ReadSingle(),
                    SkillCooldownRemaining = reader.ReadSingle(),
                    RewindUsesRemaining = reader.ReadInt32(),
                    PendingLevelUpChoices = reader.ReadInt32()
                };

                int cardCount = reader.ReadInt32();
                for (int index = 0; index < cardCount; index++)
                {
                    snapshot.Cards.Add(new SessionCardState
                    {
                        CardKey = reader.ReadString(),
                        StackCount = reader.ReadInt32()
                    });
                }

                int enemyCount = reader.ReadInt32();
                for (int index = 0; index < enemyCount; index++)
                {
                    snapshot.Enemies.Add(new SessionEnemyState
                    {
                        EnemyKey = reader.ReadString(),
                        PositionX = reader.ReadSingle(),
                        PositionY = reader.ReadSingle(),
                        CurrentHp = reader.ReadInt32(),
                        IsElite = reader.ReadBoolean()
                    });
                }

                int pickupCount = reader.ReadInt32();
                for (int index = 0; index < pickupCount; index++)
                {
                    snapshot.Pickups.Add(new SessionPickupState
                    {
                        PositionX = reader.ReadSingle(),
                        PositionY = reader.ReadSingle(),
                        ExperienceValue = reader.ReadSingle(),
                        GrantsFreeLevel = reader.ReadBoolean()
                    });
                }
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Failed to load snapshot '{path}': {exception.Message}");
            snapshot = null;
            return false;
        }

        return snapshot != null;
    }

    private static void AddSavedSessionInfo(List<SavedSessionInfo> sessions, string filePath, bool isContinueSave)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return;
        }

        if (!TryReadSnapshot(filePath, out SessionSnapshotData snapshot) || snapshot == null)
        {
            return;
        }

        DateTime savedAt = File.GetLastWriteTime(filePath);
        string prefix = isContinueSave ? "继续存档" : "手动存档";
        string displayName = $"{prefix}  {savedAt:yyyy-MM-dd HH:mm:ss}  |  第 {Mathf.Max(1, snapshot.CurrentWave)} 波  |  {GameSelectionConfig.GetPlayerDisplayName(snapshot.PlayerType)}  |  {GameSelectionConfig.GetWeaponDisplayName(snapshot.WeaponType)}";

        sessions.Add(new SavedSessionInfo
        {
            FilePath = filePath,
            DisplayName = displayName,
            SavedAt = savedAt,
            IsContinueSave = isContinueSave,
            Snapshot = snapshot
        });
    }

    private static string GetSavePath()
    {
        return Path.Combine(Application.persistentDataPath, SessionFileName);
    }

    private static string GetSnapshotDirectory()
    {
        return Path.Combine(Application.persistentDataPath, SnapshotDirectoryName);
    }

    private static string GetManualSaveDirectory()
    {
        return Path.Combine(Application.persistentDataPath, ManualSaveDirectoryName);
    }
}
