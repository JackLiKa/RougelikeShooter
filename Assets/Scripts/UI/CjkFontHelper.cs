using UnityEngine;

public static class CjkFontHelper
{
    private static readonly string[] CandidateFonts =
    {
        "Microsoft YaHei UI",
        "Microsoft YaHei",
        "SimHei",
        "Noto Sans CJK SC",
        "Source Han Sans SC",
        "Arial Unicode MS",
        "Arial"
    };

    private static Font cachedFont;

    public static Font GetFont()
    {
        if (cachedFont != null)
        {
            return cachedFont;
        }

        try
        {
            cachedFont = Font.CreateDynamicFontFromOSFont(CandidateFonts, 16);
        }
        catch
        {
            cachedFont = null;
        }

        if (cachedFont == null)
        {
            cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        if (cachedFont == null)
        {
            cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        return cachedFont;
    }
}
