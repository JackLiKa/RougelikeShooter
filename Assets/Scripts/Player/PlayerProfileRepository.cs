using System.Collections.Generic;

public static class PlayerProfileRepository
{
    public static PlayerProfile GetProfile(PlayerType playerType)
    {
        return RoguelikeDataRepository.GetPlayerProfile(playerType);
    }

    public static IReadOnlyDictionary<PlayerType, PlayerProfile> GetProfiles()
    {
        Dictionary<PlayerType, PlayerProfile> profiles = new Dictionary<PlayerType, PlayerProfile>();
        foreach (PlayerType playerType in GameSelectionConfig.AvailablePlayers)
        {
            profiles[playerType] = GetProfile(playerType);
        }

        return profiles;
    }
}
