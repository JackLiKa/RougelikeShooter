using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public sealed class WeaponProfile
{
    public WeaponType WeaponType;
    public string DisplayName;
    public int Damage;
    public float ProjectileSpeed;
    public float ShootRate;
    public float BulletLifetime;
    public float ProjectileScale;
    public int PoolSize;
    public int MaxAmmo;
    public float ReloadDuration;
    public float SpreadAngle;
    public int Pierce;
    public string PreviewResource;
}

public sealed class EnemyProfile
{
    public string EnemyKey;
    public string DisplayName;
    public string TemplateName;
    public bool IsElite;
    public int MaxHp;
    public int Attack;
    public float MoveSpeed;
    public float AttackInterval;
    public float ContactRange;
    public float ExperienceDrop;
    public float Scale;
    public int PoolSize;
}

public sealed class WaveProfile
{
    public int WaveIndex;
    public string EnemyKey;
    public int SpawnCount;
    public float SpawnInterval;
    public string EliteEnemyKey;
    public int EliteCount;
}

public sealed class PowerCardData
{
    public string CardKey;
    public string Title;
    public string Description;
    public int Weight;
    public int MaxStacks;
    public int BonusHp;
    public int BonusAttack;
    public float BonusMoveSpeed;
    public float BonusShootRate;
    public float BonusBulletSpeed;
    public int BonusProjectileCount;
    public int BonusVolleyCount;
    public int BonusBurstCount;
    public int BonusPierce;
    public float BonusPickupRadius;
    public int BonusHealOnPickup;
}

[Serializable]
public sealed class InGameConfigData
{
    public float WaveDuration = 30f;
    public int BaseExpToLevel = 40;
    public int ExpGrowthPerLevel = 18;
    public float PickupRadius = 2.2f;
    public int RewindUsesPerRun = 3;
    public float SnapshotInterval = 12f;
    public int SnapshotKeepCount = 6;
    public int RewardGoldPerWave = 18;
    public int RewardExpPerWave = 24;
    public float SpawnMinDistance = 14f;
    public float SpawnMaxDistance = 32f;
    public int CardChoiceCount = 2;
}

[Serializable]
public sealed class MapConfigData
{
    public float MinX = 80f;
    public float MaxX = 1840f;
    public float MinY = 80f;
    public float MaxY = 1000f;
    public float SafeMargin = 48f;
}

[Serializable]
public sealed class SaveConfigData
{
    public string SaveSlot = "slot0";
    public float AutoSaveSeconds = 20f;
    public float SnapshotInterval = 12f;
    public int SnapshotKeepCount = 6;
    public string BinaryExtension = "bin";
}

[Serializable]
public sealed class UserConfigData
{
    public int StartingCoins;
    public int StartingLevel = 1;
    public int StartingExp;
    public int BaseExpToLevel = 80;
    public int ExpGrowthPerLevel = 35;
    public int UpgradeCostBase = 60;
    public int UpgradeCostGrowth = 30;
    public float UpgradeStatPercent = 0.08f;
}

[Serializable]
public sealed class PlayerUpgradeState
{
    public string PlayerKey;
    public int UpgradeLevel;
}

[Serializable]
public sealed class UserProgressData
{
    public int Coins;
    public int Level = 1;
    public int CurrentExp;
    public List<PlayerUpgradeState> PlayerUpgrades = new List<PlayerUpgradeState>();
}

public static class RoguelikeDataRepository
{
    private const string DataFolder = "Datas";
    private const string PlayerFile = "PlayersData.csv";
    private const string WeaponFile = "WeaponsData.csv";
    private const string EnemyFile = "EnemysData.csv";
    private const string WaveFile = "WavaData.csv";
    private const string PowerFile = "PowerInGameData.csv";
    private const string InGameFile = "InGameData.csv";
    private const string MapFile = "MapData.csv";
    private const string SaveFile = "SaveGameData.csv";
    private const string UserFile = "UserData.csv";

    private static readonly Dictionary<PlayerType, PlayerProfile> FallbackPlayers = new Dictionary<PlayerType, PlayerProfile>
    {
        {
            PlayerType.Player1,
            new PlayerProfile
            {
                PlayerType = PlayerType.Player1,
                PlayerKey = "Player1",
                DisplayName = "圣堂骑士",
                MaxHp = 120,
                Attack = 18,
                MoveSpeed = 18.5f,
                ShootSpeed = 3f
            }
        },
        {
            PlayerType.Player2,
            new PlayerProfile
            {
                PlayerType = PlayerType.Player2,
                PlayerKey = "Player2",
                DisplayName = "游侠斥候",
                MaxHp = 95,
                Attack = 14,
                MoveSpeed = 20.5f,
                ShootSpeed = 4.2f
            }
        },
        {
            PlayerType.Player3,
            new PlayerProfile
            {
                PlayerType = PlayerType.Player3,
                PlayerKey = "Player3",
                DisplayName = "迅捷战士",
                MaxHp = 120,
                Attack = 20,
                MoveSpeed = 18f,
                ShootSpeed = 3.5f
            }
        },
        {
            PlayerType.Player4,
            new PlayerProfile
            {
                PlayerType = PlayerType.Player4,
                PlayerKey = "Player4",
                DisplayName = "中分球皇",
                MaxHp = 100,
                Attack = 25,
                MoveSpeed = 21.5f,
                ShootSpeed = 2.5f
            }
        }
    };

    private static readonly Dictionary<WeaponType, WeaponProfile> FallbackWeapons = new Dictionary<WeaponType, WeaponProfile>
    {
        {
            WeaponType.Ak47,
            new WeaponProfile
            {
                WeaponType = WeaponType.Ak47,
                DisplayName = "AK47",
                Damage = 20,
                ProjectileSpeed = 28f,
                ShootRate = 4f,
                BulletLifetime = 1.4f,
                ProjectileScale = 1f,
                PoolSize = 48,
                MaxAmmo = 24,
                ReloadDuration = 1.3f,
                SpreadAngle = 6f,
                Pierce = 0,
                PreviewResource = "Ak47"
            }
        },
        {
            WeaponType.AssaultRifle,
            new WeaponProfile
            {
                WeaponType = WeaponType.AssaultRifle,
                DisplayName = "突击步枪",
                Damage = 14,
                ProjectileSpeed = 32f,
                ShootRate = 5.2f,
                BulletLifetime = 1.2f,
                ProjectileScale = 0.95f,
                PoolSize = 64,
                MaxAmmo = 30,
                ReloadDuration = 1.1f,
                SpreadAngle = 4f,
                Pierce = 0,
                PreviewResource = "AssaultRifle"
            }
        }
    };

    private static readonly Dictionary<string, EnemyProfile> FallbackEnemies = new Dictionary<string, EnemyProfile>(StringComparer.OrdinalIgnoreCase)
    {
        {
            "Enemy1",
            new EnemyProfile
            {
                EnemyKey = "Enemy1",
                DisplayName = "史莱姆",
                TemplateName = "Enemy1",
                IsElite = false,
                MaxHp = 40,
                Attack = 6,
                MoveSpeed = 3.5f,
                AttackInterval = 0.9f,
                ContactRange = 1.1f,
                ExperienceDrop = 12f,
                Scale = 8.5f,
                PoolSize = 48
            }
        },
        {
            "DireEnemy1",
            new EnemyProfile
            {
                EnemyKey = "DireEnemy1",
                DisplayName = "灾厄精英",
                TemplateName = "DireEnemy1",
                IsElite = true,
                MaxHp = 180,
                Attack = 16,
                MoveSpeed = 5.8f,
                AttackInterval = 0.7f,
                ContactRange = 1.35f,
                ExperienceDrop = 0f,
                Scale = 12.5f,
                PoolSize = 18
            }
        }
    };

    private static Dictionary<PlayerType, PlayerProfile> cachedPlayers;
    private static Dictionary<WeaponType, WeaponProfile> cachedWeapons;
    private static Dictionary<string, EnemyProfile> cachedEnemies;
    private static List<WaveProfile> cachedWaves;
    private static List<PowerCardData> cachedPowerCards;
    private static InGameConfigData cachedInGameConfig;
    private static MapConfigData cachedMapConfig;
    private static SaveConfigData cachedSaveConfig;
    private static UserConfigData cachedUserConfig;

    public static PlayerProfile GetPlayerProfile(PlayerType playerType)
    {
        EnsurePlayersLoaded();
        PlayerProfile profile = cachedPlayers.TryGetValue(playerType, out PlayerProfile loadedProfile)
            ? loadedProfile.Clone()
            : (FallbackPlayers.TryGetValue(playerType, out PlayerProfile fallbackProfile)
                ? fallbackProfile.Clone()
                : FallbackPlayers[PlayerType.Player1].Clone());

        float multiplier = UserProgressRepository.GetUpgradeMultiplier(playerType);
        profile.MaxHp = Mathf.Max(1, Mathf.RoundToInt(profile.MaxHp * multiplier));
        profile.Attack = Mathf.Max(1, Mathf.RoundToInt(profile.Attack * multiplier));
        profile.MoveSpeed = Mathf.Max(0.1f, profile.MoveSpeed * multiplier);
        profile.ShootSpeed = Mathf.Max(0.1f, profile.ShootSpeed * multiplier);
        return profile;
    }

    public static WeaponProfile GetWeaponProfile(WeaponType weaponType)
    {
        EnsureWeaponsLoaded();
        if (cachedWeapons.TryGetValue(weaponType, out WeaponProfile profile))
        {
            return profile;
        }

        return FallbackWeapons[weaponType];
    }

    public static EnemyProfile GetEnemyProfile(string enemyKey)
    {
        EnsureEnemiesLoaded();
        if (!string.IsNullOrWhiteSpace(enemyKey) && cachedEnemies.TryGetValue(enemyKey, out EnemyProfile profile))
        {
            return profile;
        }

        return FallbackEnemies["Enemy1"];
    }

    public static EnemyProfile GetDefaultEliteProfile()
    {
        EnsureEnemiesLoaded();
        foreach (EnemyProfile profile in cachedEnemies.Values)
        {
            if (profile.IsElite)
            {
                return profile;
            }
        }

        return FallbackEnemies["DireEnemy1"];
    }

    public static List<WaveProfile> GetWaveProfilesFor(int waveIndex)
    {
        EnsureWavesLoaded();
        List<WaveProfile> profiles = new List<WaveProfile>();
        foreach (WaveProfile profile in cachedWaves)
        {
            if (profile.WaveIndex == waveIndex)
            {
                profiles.Add(profile);
            }
        }

        if (profiles.Count > 0)
        {
            return profiles;
        }

        int fallbackWave = 1;
        for (int index = 0; index < cachedWaves.Count; index++)
        {
            fallbackWave = Mathf.Max(fallbackWave, cachedWaves[index].WaveIndex);
        }

        foreach (WaveProfile template in cachedWaves)
        {
            if (template.WaveIndex != fallbackWave)
            {
                continue;
            }

            int extraWaveCount = Mathf.Max(0, waveIndex - fallbackWave);
            profiles.Add(new WaveProfile
            {
                WaveIndex = waveIndex,
                EnemyKey = template.EnemyKey,
                SpawnCount = template.SpawnCount + (extraWaveCount * 2),
                SpawnInterval = Mathf.Max(0.35f, template.SpawnInterval - (extraWaveCount * 0.03f)),
                EliteEnemyKey = template.EliteEnemyKey,
                EliteCount = waveIndex % 5 == 0 ? Mathf.Max(1, template.EliteCount) : 0
            });
        }

        if (profiles.Count == 0)
        {
            profiles.Add(new WaveProfile
            {
                WaveIndex = waveIndex,
                EnemyKey = "Enemy1",
                SpawnCount = 6 + (waveIndex * 2),
                SpawnInterval = Mathf.Max(0.5f, 2f - (waveIndex * 0.05f)),
                EliteEnemyKey = "DireEnemy1",
                EliteCount = waveIndex % 5 == 0 ? 1 : 0
            });
        }

        return profiles;
    }

    public static List<PowerCardData> GetPowerCards()
    {
        EnsureCardsLoaded();
        return cachedPowerCards;
    }

    public static InGameConfigData GetInGameConfig()
    {
        if (cachedInGameConfig != null)
        {
            return cachedInGameConfig;
        }

        List<Dictionary<string, string>> rows = ReadCsvRows(InGameFile);
        cachedInGameConfig = new InGameConfigData();
        if (rows.Count == 0)
        {
            return cachedInGameConfig;
        }

        Dictionary<string, string> row = rows[0];
        cachedInGameConfig.WaveDuration = GetFloat(row, "WaveDuration", cachedInGameConfig.WaveDuration);
        cachedInGameConfig.BaseExpToLevel = GetInt(row, "BaseExpToLevel", cachedInGameConfig.BaseExpToLevel);
        cachedInGameConfig.ExpGrowthPerLevel = GetInt(row, "ExpGrowthPerLevel", cachedInGameConfig.ExpGrowthPerLevel);
        cachedInGameConfig.PickupRadius = GetFloat(row, "PickupRadius", cachedInGameConfig.PickupRadius);
        cachedInGameConfig.RewindUsesPerRun = GetInt(row, "RewindUsesPerRun", cachedInGameConfig.RewindUsesPerRun);
        cachedInGameConfig.SnapshotInterval = GetFloat(row, "SnapshotInterval", cachedInGameConfig.SnapshotInterval);
        cachedInGameConfig.SnapshotKeepCount = GetInt(row, "SnapshotKeepCount", cachedInGameConfig.SnapshotKeepCount);
        cachedInGameConfig.RewardGoldPerWave = GetInt(row, "RewardGoldPerWave", cachedInGameConfig.RewardGoldPerWave);
        cachedInGameConfig.RewardExpPerWave = GetInt(row, "RewardExpPerWave", cachedInGameConfig.RewardExpPerWave);
        cachedInGameConfig.SpawnMinDistance = GetFloat(row, "SpawnMinDistance", cachedInGameConfig.SpawnMinDistance);
        cachedInGameConfig.SpawnMaxDistance = GetFloat(row, "SpawnMaxDistance", cachedInGameConfig.SpawnMaxDistance);
        cachedInGameConfig.CardChoiceCount = Mathf.Max(1, GetInt(row, "CardChoiceCount", cachedInGameConfig.CardChoiceCount));
        return cachedInGameConfig;
    }

    public static MapConfigData GetMapConfig()
    {
        if (cachedMapConfig != null)
        {
            return cachedMapConfig;
        }

        List<Dictionary<string, string>> rows = ReadCsvRows(MapFile);
        cachedMapConfig = new MapConfigData();
        if (rows.Count == 0)
        {
            return cachedMapConfig;
        }

        Dictionary<string, string> row = rows[0];
        cachedMapConfig.MinX = GetFloat(row, "MinX", cachedMapConfig.MinX);
        cachedMapConfig.MaxX = GetFloat(row, "MaxX", cachedMapConfig.MaxX);
        cachedMapConfig.MinY = GetFloat(row, "MinY", cachedMapConfig.MinY);
        cachedMapConfig.MaxY = GetFloat(row, "MaxY", cachedMapConfig.MaxY);
        cachedMapConfig.SafeMargin = GetFloat(row, "SafeMargin", cachedMapConfig.SafeMargin);
        return cachedMapConfig;
    }

    public static SaveConfigData GetSaveConfig()
    {
        if (cachedSaveConfig != null)
        {
            return cachedSaveConfig;
        }

        List<Dictionary<string, string>> rows = ReadCsvRows(SaveFile);
        cachedSaveConfig = new SaveConfigData();
        if (rows.Count == 0)
        {
            return cachedSaveConfig;
        }

        Dictionary<string, string> row = rows[0];
        cachedSaveConfig.SaveSlot = GetString(row, "SaveSlot", cachedSaveConfig.SaveSlot);
        cachedSaveConfig.AutoSaveSeconds = GetFloat(row, "AutoSaveSeconds", cachedSaveConfig.AutoSaveSeconds);
        cachedSaveConfig.SnapshotInterval = GetFloat(row, "SnapshotInterval", cachedSaveConfig.SnapshotInterval);
        cachedSaveConfig.SnapshotKeepCount = GetInt(row, "SnapshotKeepCount", cachedSaveConfig.SnapshotKeepCount);
        cachedSaveConfig.BinaryExtension = GetString(row, "BinaryExtension", cachedSaveConfig.BinaryExtension);
        return cachedSaveConfig;
    }

    public static UserConfigData GetUserConfig()
    {
        if (cachedUserConfig != null)
        {
            return cachedUserConfig;
        }

        List<Dictionary<string, string>> rows = ReadCsvRows(UserFile);
        cachedUserConfig = new UserConfigData();
        if (rows.Count == 0)
        {
            return cachedUserConfig;
        }

        Dictionary<string, string> row = rows[0];
        cachedUserConfig.StartingCoins = GetInt(row, "StartingCoins", cachedUserConfig.StartingCoins);
        cachedUserConfig.StartingLevel = Mathf.Max(1, GetInt(row, "StartingLevel", cachedUserConfig.StartingLevel));
        cachedUserConfig.StartingExp = GetInt(row, "StartingExp", cachedUserConfig.StartingExp);
        cachedUserConfig.BaseExpToLevel = GetInt(row, "BaseExpToLevel", cachedUserConfig.BaseExpToLevel);
        cachedUserConfig.ExpGrowthPerLevel = GetInt(row, "ExpGrowthPerLevel", cachedUserConfig.ExpGrowthPerLevel);
        cachedUserConfig.UpgradeCostBase = GetInt(row, "UpgradeCostBase", cachedUserConfig.UpgradeCostBase);
        cachedUserConfig.UpgradeCostGrowth = GetInt(row, "UpgradeCostGrowth", cachedUserConfig.UpgradeCostGrowth);
        cachedUserConfig.UpgradeStatPercent = GetFloat(row, "UpgradeStatPercent", cachedUserConfig.UpgradeStatPercent);
        return cachedUserConfig;
    }

    private static void EnsurePlayersLoaded()
    {
        if (cachedPlayers != null)
        {
            return;
        }

        cachedPlayers = new Dictionary<PlayerType, PlayerProfile>();
        List<Dictionary<string, string>> rows = ReadCsvRows(PlayerFile);
        for (int index = 0; index < rows.Count; index++)
        {
            Dictionary<string, string> row = rows[index];
            if (!Enum.TryParse(GetString(row, "PlayerType", string.Empty), true, out PlayerType playerType))
            {
                continue;
            }

            if (!FallbackPlayers.TryGetValue(playerType, out PlayerProfile fallback))
            {
                fallback = FallbackPlayers[PlayerType.Player1];
            }

            cachedPlayers[playerType] = new PlayerProfile
            {
                PlayerType = playerType,
                PlayerKey = playerType.ToString(),
                DisplayName = GetString(row, "DisplayName", fallback.DisplayName),
                MaxHp = GetInt(row, "MaxHp", fallback.MaxHp),
                Attack = GetInt(row, "Attack", fallback.Attack),
                MoveSpeed = GetFloat(row, "MoveSpeed", fallback.MoveSpeed),
                ShootSpeed = GetFloat(row, "ShootSpeed", fallback.ShootSpeed)
            };
        }

        foreach (KeyValuePair<PlayerType, PlayerProfile> pair in FallbackPlayers)
        {
            if (!cachedPlayers.ContainsKey(pair.Key))
            {
                cachedPlayers[pair.Key] = pair.Value.Clone();
            }
        }
    }

    private static void EnsureWeaponsLoaded()
    {
        if (cachedWeapons != null)
        {
            return;
        }

        cachedWeapons = new Dictionary<WeaponType, WeaponProfile>();
        List<Dictionary<string, string>> rows = ReadCsvRows(WeaponFile);
        for (int index = 0; index < rows.Count; index++)
        {
            Dictionary<string, string> row = rows[index];
            if (!Enum.TryParse(GetString(row, "WeaponType", string.Empty), true, out WeaponType weaponType))
            {
                continue;
            }

            WeaponProfile fallback = FallbackWeapons[weaponType];
            cachedWeapons[weaponType] = new WeaponProfile
            {
                WeaponType = weaponType,
                DisplayName = GetString(row, "DisplayName", fallback.DisplayName),
                Damage = GetInt(row, "Damage", fallback.Damage),
                ProjectileSpeed = GetFloat(row, "ProjectileSpeed", fallback.ProjectileSpeed),
                ShootRate = GetFloat(row, "ShootRate", fallback.ShootRate),
                BulletLifetime = GetFloat(row, "BulletLifetime", fallback.BulletLifetime),
                ProjectileScale = GetFloat(row, "ProjectileScale", fallback.ProjectileScale),
                PoolSize = GetInt(row, "PoolSize", fallback.PoolSize),
                MaxAmmo = GetInt(row, "MaxAmmo", fallback.MaxAmmo),
                ReloadDuration = GetFloat(row, "ReloadDuration", fallback.ReloadDuration),
                SpreadAngle = GetFloat(row, "SpreadAngle", fallback.SpreadAngle),
                Pierce = GetInt(row, "Pierce", fallback.Pierce),
                PreviewResource = GetString(row, "PreviewResource", fallback.PreviewResource)
            };
        }

        foreach (KeyValuePair<WeaponType, WeaponProfile> pair in FallbackWeapons)
        {
            if (!cachedWeapons.ContainsKey(pair.Key))
            {
                cachedWeapons[pair.Key] = pair.Value;
            }
        }
    }

    private static void EnsureEnemiesLoaded()
    {
        if (cachedEnemies != null)
        {
            return;
        }

        cachedEnemies = new Dictionary<string, EnemyProfile>(StringComparer.OrdinalIgnoreCase);
        List<Dictionary<string, string>> rows = ReadCsvRows(EnemyFile);
        for (int index = 0; index < rows.Count; index++)
        {
            Dictionary<string, string> row = rows[index];
            string enemyKey = GetString(row, "EnemyKey", string.Empty);
            if (string.IsNullOrWhiteSpace(enemyKey))
            {
                continue;
            }

            EnemyProfile fallback = FallbackEnemies.ContainsKey(enemyKey) ? FallbackEnemies[enemyKey] : FallbackEnemies["Enemy1"];
            cachedEnemies[enemyKey] = new EnemyProfile
            {
                EnemyKey = enemyKey,
                DisplayName = GetString(row, "DisplayName", fallback.DisplayName),
                TemplateName = GetString(row, "TemplateName", fallback.TemplateName),
                IsElite = GetBool(row, "IsElite", fallback.IsElite),
                MaxHp = GetInt(row, "MaxHp", fallback.MaxHp),
                Attack = GetInt(row, "Attack", fallback.Attack),
                MoveSpeed = GetFloat(row, "MoveSpeed", fallback.MoveSpeed),
                AttackInterval = GetFloat(row, "AttackInterval", fallback.AttackInterval),
                ContactRange = GetFloat(row, "ContactRange", fallback.ContactRange),
                ExperienceDrop = GetFloat(row, "ExperienceDrop", fallback.ExperienceDrop),
                Scale = GetFloat(row, "Scale", fallback.Scale),
                PoolSize = GetInt(row, "PoolSize", fallback.PoolSize)
            };
        }

        foreach (KeyValuePair<string, EnemyProfile> pair in FallbackEnemies)
        {
            if (!cachedEnemies.ContainsKey(pair.Key))
            {
                cachedEnemies[pair.Key] = pair.Value;
            }
        }
    }

    private static void EnsureWavesLoaded()
    {
        if (cachedWaves != null)
        {
            return;
        }

        cachedWaves = new List<WaveProfile>();
        List<Dictionary<string, string>> rows = ReadCsvRows(WaveFile);
        for (int index = 0; index < rows.Count; index++)
        {
            Dictionary<string, string> row = rows[index];
            int waveIndex = GetInt(row, "WaveIndex", -1);
            if (waveIndex <= 0)
            {
                continue;
            }

            cachedWaves.Add(new WaveProfile
            {
                WaveIndex = waveIndex,
                EnemyKey = GetString(row, "EnemyKey", "Enemy1"),
                SpawnCount = Mathf.Max(1, GetInt(row, "SpawnCount", 6)),
                SpawnInterval = Mathf.Max(0.2f, GetFloat(row, "SpawnInterval", 1.5f)),
                EliteEnemyKey = GetString(row, "EliteEnemyKey", "DireEnemy1"),
                EliteCount = Mathf.Max(0, GetInt(row, "EliteCount", 0))
            });
        }

        if (cachedWaves.Count == 0)
        {
            for (int wave = 1; wave <= 10; wave++)
            {
                cachedWaves.Add(new WaveProfile
                {
                    WaveIndex = wave,
                    EnemyKey = "Enemy1",
                    SpawnCount = 4 + (wave * 2),
                    SpawnInterval = Mathf.Max(0.5f, 2.1f - (wave * 0.1f)),
                    EliteEnemyKey = "DireEnemy1",
                    EliteCount = wave % 5 == 0 ? 1 : 0
                });
            }
        }
    }

    private static void EnsureCardsLoaded()
    {
        if (cachedPowerCards != null)
        {
            return;
        }

        cachedPowerCards = new List<PowerCardData>();
        List<Dictionary<string, string>> rows = ReadCsvRows(PowerFile);
        for (int index = 0; index < rows.Count; index++)
        {
            Dictionary<string, string> row = rows[index];
            string cardKey = GetString(row, "CardKey", string.Empty);
            if (string.IsNullOrWhiteSpace(cardKey))
            {
                continue;
            }

            cachedPowerCards.Add(new PowerCardData
            {
                CardKey = cardKey,
                Title = GetString(row, "Title", cardKey),
                Description = GetString(row, "Description", string.Empty),
                Weight = Mathf.Max(1, GetInt(row, "Weight", 1)),
                MaxStacks = Mathf.Max(1, GetInt(row, "MaxStacks", 1)),
                BonusHp = GetInt(row, "BonusHp", 0),
                BonusAttack = GetInt(row, "BonusAttack", 0),
                BonusMoveSpeed = GetFloat(row, "BonusMoveSpeed", 0f),
                BonusShootRate = GetFloat(row, "BonusShootRate", 0f),
                BonusBulletSpeed = GetFloat(row, "BonusBulletSpeed", 0f),
                BonusProjectileCount = GetInt(row, "BonusProjectileCount", 0),
                BonusVolleyCount = GetInt(row, "BonusVolleyCount", 0),
                BonusBurstCount = GetInt(row, "BonusBurstCount", 0),
                BonusPierce = GetInt(row, "BonusPierce", 0),
                BonusPickupRadius = GetFloat(row, "BonusPickupRadius", 0f),
                BonusHealOnPickup = GetInt(row, "BonusHealOnPickup", 0)
            });
        }

        if (cachedPowerCards.Count == 0)
        {
            cachedPowerCards.Add(new PowerCardData
            {
                CardKey = "hp_boost",
                Title = "钢铁之心",
                Description = "最大生命值 +25",
                Weight = 10,
                MaxStacks = 5,
                BonusHp = 25
            });
            cachedPowerCards.Add(new PowerCardData
            {
                CardKey = "attack_boost",
                Title = "重装弹头",
                Description = "攻击力 +4",
                Weight = 10,
                MaxStacks = 5,
                BonusAttack = 4
            });
            cachedPowerCards.Add(new PowerCardData
            {
                CardKey = "rapid_trigger",
                Title = "高速扳机",
                Description = "射速 +0.6/秒",
                Weight = 8,
                MaxStacks = 6,
                BonusShootRate = 0.6f
            });
        }
    }

    private static List<Dictionary<string, string>> ReadCsvRows(string fileName)
    {
        string fullPath = Path.Combine(Application.dataPath, DataFolder, fileName);
        List<Dictionary<string, string>> rows = new List<Dictionary<string, string>>();
        if (!File.Exists(fullPath))
        {
            return rows;
        }

        string[] lines = File.ReadAllLines(fullPath);
        if (lines.Length <= 1)
        {
            return rows;
        }

        string[] headers = SplitCsvLine(lines[0]);
        for (int lineIndex = 1; lineIndex < lines.Length; lineIndex++)
        {
            string line = lines[lineIndex].Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            string[] values = SplitCsvLine(lines[lineIndex]);
            Dictionary<string, string> row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int columnIndex = 0; columnIndex < headers.Length; columnIndex++)
            {
                string header = headers[columnIndex].Trim();
                string value = columnIndex < values.Length ? values[columnIndex].Trim() : string.Empty;
                row[header] = value;
            }

            rows.Add(row);
        }

        return rows;
    }

    private static string[] SplitCsvLine(string line)
    {
        List<string> result = new List<string>();
        bool inQuotes = false;
        int startIndex = 0;

        for (int index = 0; index < line.Length; index++)
        {
            if (line[index] == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (line[index] != ',' || inQuotes)
            {
                continue;
            }

            result.Add(line.Substring(startIndex, index - startIndex).Trim().Trim('"'));
            startIndex = index + 1;
        }

        result.Add(line.Substring(startIndex).Trim().Trim('"'));
        return result.ToArray();
    }

    private static string GetString(Dictionary<string, string> row, string key, string fallback)
    {
        return row.TryGetValue(key, out string value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;
    }

    private static int GetInt(Dictionary<string, string> row, string key, int fallback)
    {
        return row.TryGetValue(key, out string value) && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedValue)
            ? parsedValue
            : fallback;
    }

    private static float GetFloat(Dictionary<string, string> row, string key, float fallback)
    {
        return row.TryGetValue(key, out string value) && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedValue)
            ? parsedValue
            : fallback;
    }

    private static bool GetBool(Dictionary<string, string> row, string key, bool fallback)
    {
        if (!row.TryGetValue(key, out string value))
        {
            return fallback;
        }

        if (bool.TryParse(value, out bool parsedValue))
        {
            return parsedValue;
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedInt))
        {
            return parsedInt != 0;
        }

        return fallback;
    }
}

public static class UserProgressRepository
{
    private const string ProgressFileName = "roguelike-user-progress.json";

    private static UserProgressData cachedProgress;

    public static UserProgressData GetProgress()
    {
        if (cachedProgress != null)
        {
            return cachedProgress;
        }

        string path = GetProgressPath();
        if (File.Exists(path))
        {
            try
            {
                cachedProgress = JsonUtility.FromJson<UserProgressData>(File.ReadAllText(path));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Failed to read user progress: {exception.Message}");
            }
        }

        if (cachedProgress == null)
        {
            UserConfigData config = RoguelikeDataRepository.GetUserConfig();
            cachedProgress = new UserProgressData
            {
                Coins = config.StartingCoins,
                Level = config.StartingLevel,
                CurrentExp = config.StartingExp,
                PlayerUpgrades = new List<PlayerUpgradeState>()
            };
        }

        NormalizeProgress();
        return cachedProgress;
    }

    public static int GetUpgradeLevel(PlayerType playerType)
    {
        PlayerUpgradeState state = GetUpgradeState(playerType, false);
        return state != null ? state.UpgradeLevel : 0;
    }

    public static int GetPlayerUpgradeCap()
    {
        return Mathf.Max(1, GetProgress().Level);
    }

    public static bool IsPlayerUpgradeAtCap(PlayerType playerType)
    {
        return GetUpgradeLevel(playerType) >= GetPlayerUpgradeCap();
    }

    public static float GetUpgradeMultiplier(PlayerType playerType)
    {
        UserConfigData config = RoguelikeDataRepository.GetUserConfig();
        return 1f + (GetUpgradeLevel(playerType) * config.UpgradeStatPercent);
    }

    public static int GetNextUpgradeCost(PlayerType playerType)
    {
        UserConfigData config = RoguelikeDataRepository.GetUserConfig();
        int currentLevel = GetUpgradeLevel(playerType);
        return config.UpgradeCostBase + (currentLevel * config.UpgradeCostGrowth);
    }

    public static bool TryUpgradePlayer(PlayerType playerType)
    {
        UserProgressData progress = GetProgress();
        if (IsPlayerUpgradeAtCap(playerType))
        {
            return false;
        }

        int cost = GetNextUpgradeCost(playerType);
        if (progress.Coins < cost)
        {
            return false;
        }

        progress.Coins -= cost;
        PlayerUpgradeState state = GetUpgradeState(playerType, true);
        state.UpgradeLevel++;
        Save();
        return true;
    }

    public static void AddMatchRewards(int goldReward, int expReward)
    {
        UserProgressData progress = GetProgress();
        UserConfigData config = RoguelikeDataRepository.GetUserConfig();

        progress.Coins += Mathf.Max(0, goldReward);
        progress.CurrentExp += Mathf.Max(0, expReward);

        int requiredExp = GetRequiredExpForLevel(progress.Level, config);
        while (progress.CurrentExp >= requiredExp)
        {
            progress.CurrentExp -= requiredExp;
            progress.Level++;
            requiredExp = GetRequiredExpForLevel(progress.Level, config);
        }

        Save();
    }

    public static void Save()
    {
        NormalizeProgress();
        string path = GetProgressPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? Application.persistentDataPath);
        File.WriteAllText(path, JsonUtility.ToJson(cachedProgress, true));
    }

    public static int GetRequiredExpForLevel(int level)
    {
        return GetRequiredExpForLevel(level, RoguelikeDataRepository.GetUserConfig());
    }

    private static int GetRequiredExpForLevel(int level, UserConfigData config)
    {
        return config.BaseExpToLevel + (Mathf.Max(1, level) - 1) * config.ExpGrowthPerLevel;
    }

    private static PlayerUpgradeState GetUpgradeState(PlayerType playerType, bool createIfMissing)
    {
        UserProgressData progress = GetProgress();
        string key = playerType.ToString();
        for (int index = 0; index < progress.PlayerUpgrades.Count; index++)
        {
            if (string.Equals(progress.PlayerUpgrades[index].PlayerKey, key, StringComparison.OrdinalIgnoreCase))
            {
                return progress.PlayerUpgrades[index];
            }
        }

        if (!createIfMissing)
        {
            return null;
        }

        PlayerUpgradeState state = new PlayerUpgradeState
        {
            PlayerKey = key,
            UpgradeLevel = 0
        };
        progress.PlayerUpgrades.Add(state);
        return state;
    }

    private static void NormalizeProgress()
    {
        if (cachedProgress == null)
        {
            return;
        }

        if (cachedProgress.PlayerUpgrades == null)
        {
            cachedProgress.PlayerUpgrades = new List<PlayerUpgradeState>();
        }

        cachedProgress.Level = Mathf.Max(1, cachedProgress.Level);
        cachedProgress.CurrentExp = Mathf.Max(0, cachedProgress.CurrentExp);
        cachedProgress.Coins = Mathf.Max(0, cachedProgress.Coins);
    }

    private static string GetProgressPath()
    {
        return Path.Combine(Application.persistentDataPath, ProgressFileName);
    }
}
