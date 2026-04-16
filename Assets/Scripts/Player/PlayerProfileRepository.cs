using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

public static class PlayerProfileRepository
{
    private const string RelativeProfilePath = "Scripts/Player/Players/PlayerDates.csv";

    private static readonly Dictionary<PlayerType, PlayerProfile> FallbackProfiles = new Dictionary<PlayerType, PlayerProfile>
    {
        {
            PlayerType.Player1,
            new PlayerProfile
            {
                PlayerType = PlayerType.Player1,
                PlayerKey = "Player1",
                DisplayName = "Player1",
                MaxHp = 100,
                Attack = 20,
                MoveSpeed = 10f,
                ShootSpeed = 1f
            }
        },
        {
            PlayerType.Player2,
            new PlayerProfile
            {
                PlayerType = PlayerType.Player2,
                PlayerKey = "Player2",
                DisplayName = "Player2",
                MaxHp = 120,
                Attack = 15,
                MoveSpeed = 10f,
                ShootSpeed = 1f
            }
        }
    };

    private static Dictionary<PlayerType, PlayerProfile> cachedProfiles;
    private static bool loadAttempted;

    public static PlayerProfile GetProfile(PlayerType playerType)
    {
        EnsureLoaded();
        if (cachedProfiles != null && cachedProfiles.TryGetValue(playerType, out PlayerProfile profile))
        {
            return profile;
        }

        return FallbackProfiles[playerType];
    }

    public static IReadOnlyDictionary<PlayerType, PlayerProfile> GetProfiles()
    {
        EnsureLoaded();
        return cachedProfiles ?? FallbackProfiles;
    }

    private static void EnsureLoaded()
    {
        if (loadAttempted)
        {
            return;
        }

        loadAttempted = true;
        cachedProfiles = LoadProfilesFromWorkbook();
    }

    private static Dictionary<PlayerType, PlayerProfile> LoadProfilesFromWorkbook()
    {
        Dictionary<PlayerType, PlayerProfile> profiles = new Dictionary<PlayerType, PlayerProfile>();
        string workbookPath = Path.Combine(Application.dataPath, RelativeProfilePath);

        if (!File.Exists(workbookPath))
        {
            return CloneFallbackProfiles();
        }

        try
        {
            using (FileStream stream = File.OpenRead(workbookPath))
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                Dictionary<int, string> sharedStrings = ReadSharedStrings(archive);
                List<Dictionary<string, string>> rows = ReadSheetRows(archive, sharedStrings);

                foreach (Dictionary<string, string> row in rows)
                {
                    if (!TryParsePlayerType(GetCell(row, "A"), out PlayerType playerType))
                    {
                        continue;
                    }

                    PlayerProfile fallback = FallbackProfiles[playerType];
                    PlayerProfile profile = new PlayerProfile
                    {
                        PlayerType = playerType,
                        PlayerKey = GetCell(row, "A"),
                        DisplayName = GetCell(row, "B"),
                        MaxHp = ParseInt(GetCell(row, "C"), fallback.MaxHp),
                        Attack = ParseInt(GetCell(row, "D"), fallback.Attack),
                        MoveSpeed = ParseFloat(GetCell(row, "E"), fallback.MoveSpeed),
                        ShootSpeed = ParseFloat(GetCell(row, "F"), fallback.ShootSpeed)
                    };

                    if (string.IsNullOrWhiteSpace(profile.DisplayName))
                    {
                        profile.DisplayName = fallback.DisplayName;
                    }

                    profiles[playerType] = profile;
                }
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Failed to load player workbook: {exception.Message}");
            return CloneFallbackProfiles();
        }

        foreach (KeyValuePair<PlayerType, PlayerProfile> pair in FallbackProfiles)
        {
            if (!profiles.ContainsKey(pair.Key))
            {
                profiles[pair.Key] = pair.Value.Clone();
            }
        }

        return profiles;
    }

    private static Dictionary<int, string> ReadSharedStrings(ZipArchive archive)
    {
        Dictionary<int, string> sharedStrings = new Dictionary<int, string>();
        ZipArchiveEntry sharedStringsEntry = archive.GetEntry("xl/sharedStrings.xml");
        if (sharedStringsEntry == null)
        {
            return sharedStrings;
        }

        using (Stream sharedStream = sharedStringsEntry.Open())
        {
            XDocument document = XDocument.Load(sharedStream);
            XNamespace ns = document.Root?.Name.Namespace ?? XNamespace.None;
            int index = 0;

            foreach (XElement si in document.Descendants(ns + "si"))
            {
                string value = string.Concat(si.Descendants().Where(element => element.Name.LocalName == "t").Select(element => element.Value));
                sharedStrings[index++] = value;
            }
        }

        return sharedStrings;
    }

    private static List<Dictionary<string, string>> ReadSheetRows(ZipArchive archive, Dictionary<int, string> sharedStrings)
    {
        List<Dictionary<string, string>> rows = new List<Dictionary<string, string>>();
        ZipArchiveEntry sheetEntry = archive.GetEntry("xl/worksheets/sheet1.xml");
        if (sheetEntry == null)
        {
            return rows;
        }

        using (Stream sheetStream = sheetEntry.Open())
        {
            XDocument document = XDocument.Load(sheetStream);
            XNamespace ns = document.Root?.Name.Namespace ?? XNamespace.None;

            foreach (XElement row in document.Descendants(ns + "row").Skip(1))
            {
                Dictionary<string, string> cells = new Dictionary<string, string>();
                foreach (XElement cell in row.Elements(ns + "c"))
                {
                    string reference = cell.Attribute("r")?.Value;
                    string column = ExtractColumnName(reference);
                    if (string.IsNullOrEmpty(column))
                    {
                        continue;
                    }

                    string value = ReadCellValue(cell, ns, sharedStrings);
                    cells[column] = value;
                }

                if (cells.Count > 0)
                {
                    rows.Add(cells);
                }
            }
        }

        return rows;
    }

    private static string ReadCellValue(XElement cell, XNamespace ns, Dictionary<int, string> sharedStrings)
    {
        XElement valueElement = cell.Element(ns + "v");
        if (valueElement == null)
        {
            return string.Empty;
        }

        string rawValue = valueElement.Value;
        string cellType = cell.Attribute("t")?.Value;
        if (cellType == "s" && int.TryParse(rawValue, out int sharedIndex) && sharedStrings.TryGetValue(sharedIndex, out string sharedValue))
        {
            return sharedValue;
        }

        return rawValue;
    }

    private static string ExtractColumnName(string reference)
    {
        if (string.IsNullOrEmpty(reference))
        {
            return string.Empty;
        }

        char[] columnChars = reference.TakeWhile(char.IsLetter).ToArray();
        return new string(columnChars);
    }

    private static string GetCell(Dictionary<string, string> row, string column)
    {
        return row.TryGetValue(column, out string value) ? value : string.Empty;
    }

    private static bool TryParsePlayerType(string value, out PlayerType playerType)
    {
        return Enum.TryParse(value, true, out playerType);
    }

    private static int ParseInt(string value, int fallback)
    {
        return int.TryParse(value, out int parsed) ? parsed : fallback;
    }

    private static float ParseFloat(string value, float fallback)
    {
        return float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float parsed)
            ? parsed
            : fallback;
    }

    private static Dictionary<PlayerType, PlayerProfile> CloneFallbackProfiles()
    {
        Dictionary<PlayerType, PlayerProfile> clone = new Dictionary<PlayerType, PlayerProfile>();
        foreach (KeyValuePair<PlayerType, PlayerProfile> pair in FallbackProfiles)
        {
            clone[pair.Key] = pair.Value.Clone();
        }

        return clone;
    }
}
