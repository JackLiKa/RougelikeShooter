using UnityEngine;

public enum WeaponType
{
    Ak47,
    AssaultRifle
}

public static class GameSelectionConfig
{
    private const string PlayerTypeKey = "AI_SelectedPlayerType";
    private const string WeaponTypeKey = "AI_SelectedWeaponType";

    public static readonly PlayerType[] AvailablePlayers =
    {
        PlayerType.Player1,
        PlayerType.Player2,
        PlayerType.Player3,
        PlayerType.Player4
    };

    public static readonly WeaponType[] AvailableWeapons =
    {
        WeaponType.Ak47,
        WeaponType.AssaultRifle
    };

    public static PlayerType CurrentPlayerType
    {
        get
        {
            int value = PlayerPrefs.GetInt(PlayerTypeKey, (int)PlayerType.Player1);
            return (PlayerType)Mathf.Clamp(value, (int)PlayerType.Player1, (int)PlayerType.Player4);
        }
        set
        {
            PlayerPrefs.SetInt(PlayerTypeKey, (int)value);
            PlayerPrefs.Save();
        }
    }

    public static WeaponType CurrentWeaponType
    {
        get
        {
            int value = PlayerPrefs.GetInt(WeaponTypeKey, (int)WeaponType.Ak47);
            return (WeaponType)Mathf.Clamp(value, (int)WeaponType.Ak47, (int)WeaponType.AssaultRifle);
        }
        set
        {
            PlayerPrefs.SetInt(WeaponTypeKey, (int)value);
            PlayerPrefs.Save();
        }
    }

    public static PlayerType NextPlayer(PlayerType currentPlayerType)
    {
        int index = IndexOf(AvailablePlayers, currentPlayerType);
        return AvailablePlayers[(index + 1) % AvailablePlayers.Length];
    }

    public static PlayerType PreviousPlayer(PlayerType currentPlayerType)
    {
        int index = IndexOf(AvailablePlayers, currentPlayerType);
        return AvailablePlayers[(index - 1 + AvailablePlayers.Length) % AvailablePlayers.Length];
    }

    public static WeaponType NextWeapon(WeaponType currentWeaponType)
    {
        int index = IndexOf(AvailableWeapons, currentWeaponType);
        return AvailableWeapons[(index + 1) % AvailableWeapons.Length];
    }

    public static WeaponType PreviousWeapon(WeaponType currentWeaponType)
    {
        int index = IndexOf(AvailableWeapons, currentWeaponType);
        return AvailableWeapons[(index - 1 + AvailableWeapons.Length) % AvailableWeapons.Length];
    }

    public static string GetPlayerObjectName(PlayerType playerType)
    {
        return playerType.ToString();
    }

    public static string GetPlayerPreviewResourceFolder(PlayerType playerType)
    {
        switch (playerType)
        {
            case PlayerType.Player2:
                return "Player/player2";
            case PlayerType.Player3:
                return "Player/player3";
            case PlayerType.Player4:
                return "Player/player4";
            case PlayerType.Player1:
            default:
                return "Player/player1";
        }
    }

    public static string GetPlayerDisplayName(PlayerType playerType)
    {
        return PlayerProfileRepository.GetProfile(playerType).DisplayName;
    }

    public static string GetWeaponObjectName(WeaponType weaponType)
    {
        return weaponType.ToString();
    }

    public static string GetWeaponDisplayName(WeaponType weaponType)
    {
        switch (weaponType)
        {
            case WeaponType.AssaultRifle:
                return "突击步枪";
            case WeaponType.Ak47:
            default:
                return "AK47";
        }
    }

    private static int IndexOf<T>(T[] source, T target)
    {
        for (int index = 0; index < source.Length; index++)
        {
            if (Equals(source[index], target))
            {
                return index;
            }
        }

        return 0;
    }
}
