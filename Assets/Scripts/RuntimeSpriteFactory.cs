using System.IO;
using UnityEngine;

public static class RuntimeSpriteFactory
{
    private const float AlphaThreshold = 0.05f;
    private const float PlayerPixelsPerUnit = 1000f;
    private const string PlayerResourcePath = "Police/charater_police";
    private const string BackgroundResourcePath = "Police/player_background";

    private static Sprite whiteSprite;
    private static Sprite playerSprite;
    private static Sprite backgroundSprite;

    public static Sprite WhiteSprite
    {
        get
        {
            if (whiteSprite != null)
            {
                return whiteSprite;
            }

            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.name = "RuntimeWhiteTexture";
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            whiteSprite.name = "RuntimeWhiteSprite";
            return whiteSprite;
        }
    }

    public static Sprite PlayerSprite
    {
        get
        {
            if (playerSprite != null)
            {
                return playerSprite;
            }

            Sprite[] importedSprites = Resources.LoadAll<Sprite>(PlayerResourcePath);
            for (int i = 0; i < importedSprites.Length; i++)
            {
                if (importedSprites[i] != null && importedSprites[i].name == "charater_police_1")
                {
                    Sprite importedSprite = importedSprites[i];
                    Rect rect = importedSprite.rect;
                    Vector2 pivot = new Vector2(importedSprite.pivot.x / rect.width, importedSprite.pivot.y / rect.height);
                    playerSprite = Sprite.Create(importedSprite.texture, rect, pivot, PlayerPixelsPerUnit);
                    playerSprite.name = "RuntimePoliceSprite";
                    return playerSprite;
                }
            }

            Texture2D texture = LoadTexture(PlayerResourcePath, "Charater/Police/charater_police.png", "RuntimePoliceTexture");
            if (texture == null)
            {
                return null;
            }

            Rect spriteRect = TrimTransparentBounds(texture, new Rect(58f, 31f, 973f, 889f));
            playerSprite = Sprite.Create(texture, spriteRect, new Vector2(0.5f, 0.5f), PlayerPixelsPerUnit);
            playerSprite.name = "RuntimePoliceSprite";
            return playerSprite;
        }
    }

    public static Sprite BackgroundSprite
    {
        get
        {
            if (backgroundSprite != null)
            {
                return backgroundSprite;
            }

            Sprite[] importedSprites = Resources.LoadAll<Sprite>(BackgroundResourcePath);
            if (importedSprites.Length > 0 && importedSprites[0] != null)
            {
                backgroundSprite = importedSprites[0];
                return backgroundSprite;
            }

            Texture2D texture = LoadTexture(BackgroundResourcePath, "Background_Player/Police/player_background.png", "RuntimeBackgroundTexture");
            if (texture == null)
            {
                return null;
            }

            backgroundSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            backgroundSprite.name = "RuntimeBackgroundSprite";
            return backgroundSprite;
        }
    }

    private static Texture2D LoadTexture(string resourcePath, string editorRelativePath, string textureName)
    {
        Texture2D resourceTexture = Resources.Load<Texture2D>(resourcePath);
        if (resourceTexture != null)
        {
            return resourceTexture;
        }

        string spritePath = Path.Combine(Application.dataPath, editorRelativePath);
        if (!File.Exists(spritePath))
        {
            return null;
        }

        byte[] imageBytes = File.ReadAllBytes(spritePath);
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.name = textureName;

        if (!texture.LoadImage(imageBytes))
        {
            Object.Destroy(texture);
            return null;
        }

        return texture;
    }

    private static Rect TrimTransparentBounds(Texture2D texture, Rect sourceRect)
    {
        if (!texture.isReadable)
        {
            return sourceRect;
        }

        int minX = Mathf.RoundToInt(sourceRect.xMin);
        int minY = Mathf.RoundToInt(sourceRect.yMin);
        int maxX = Mathf.RoundToInt(sourceRect.xMax) - 1;
        int maxY = Mathf.RoundToInt(sourceRect.yMax) - 1;

        int trimmedMinX = maxX;
        int trimmedMinY = maxY;
        int trimmedMaxX = minX;
        int trimmedMaxY = minY;
        bool foundOpaquePixel = false;

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                if (texture.GetPixel(x, y).a <= AlphaThreshold)
                {
                    continue;
                }

                foundOpaquePixel = true;
                trimmedMinX = Mathf.Min(trimmedMinX, x);
                trimmedMinY = Mathf.Min(trimmedMinY, y);
                trimmedMaxX = Mathf.Max(trimmedMaxX, x);
                trimmedMaxY = Mathf.Max(trimmedMaxY, y);
            }
        }

        if (!foundOpaquePixel)
        {
            return sourceRect;
        }

        return new Rect(
            trimmedMinX,
            trimmedMinY,
            (trimmedMaxX - trimmedMinX) + 1,
            (trimmedMaxY - trimmedMinY) + 1);
    }
}
