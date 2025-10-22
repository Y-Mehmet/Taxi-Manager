using UnityEngine;

public enum HyperCasualColor
{
    Blue,
    Red,
    Green,
    Yellow,
    Orange,
    Purple,
    Pink,
    Cyan,
    Lime,
    White,
    Grey
}

public static class HyperCasualColorUtil
{
    public static Color ToColor(this HyperCasualColor color)
    {
        switch (color)
        {
            case HyperCasualColor.Blue: return new Color(0.2f, 0.5f, 1f);
            case HyperCasualColor.Red: return new Color(1f, 0.2f, 0.2f);
            case HyperCasualColor.Green: return new Color(0.2f, 1f, 0.4f);
            case HyperCasualColor.Yellow: return new Color(1f, 0.92f, 0.2f);
            case HyperCasualColor.Orange: return new Color(1f, 0.6f, 0.2f);
            case HyperCasualColor.Purple: return new Color(0.6f, 0.2f, 1f);
            case HyperCasualColor.Pink: return new Color(1f, 0.4f, 0.7f);
            case HyperCasualColor.Cyan: return new Color(0.2f, 1f, 1f);
            case HyperCasualColor.Lime: return new Color(0.7f, 1f, 0.2f);
            case HyperCasualColor.White: return Color.white;
            case HyperCasualColor.Grey: return Color.grey;
            default: return Color.gray;
        }
    }

    public static HyperCasualColor ToHyperCasualColor(this Color unityColor)
    {
        HyperCasualColor closestColor = HyperCasualColor.White;
        float minDistance = float.MaxValue;

        foreach (HyperCasualColor enumColor in System.Enum.GetValues(typeof(HyperCasualColor)))
        {
            float dist = ColorDistance(enumColor.ToColor(), unityColor);
            if (dist < minDistance)
            {
                minDistance = dist;
                closestColor = enumColor;
            }
        }
        return closestColor;
    }


    private static float ColorDistance(Color c1, Color c2)
    {
        float r = c1.r - c2.r;
        float g = c1.g - c2.g;
        float b = c1.b - c2.b;
        return r * r + g * g + b * b;
    }
}