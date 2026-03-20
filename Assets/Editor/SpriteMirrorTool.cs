using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor tool: select a sprite in the Project window, run this tool,
/// and it creates a new mirrored (left + flipped right) version.
///
/// Place this file in Assets/Editor/ folder.
///
/// Usage: Select sprite → Tools → Mirror Sprite Horizontally
/// </summary>
public class SpriteMirrorTool : EditorWindow
{
    [MenuItem("Tools/Mirror Sprite Horizontally")]
    public static void MirrorSelectedSprite()
    {
        // 1. Get selected texture
        Object selected = Selection.activeObject;
        if (selected == null || !(selected is Texture2D))
        {
            EditorUtility.DisplayDialog("Mirror Sprite",
                "Please select a sprite/texture in the Project window first.", "OK");
            return;
        }

        Texture2D source = (Texture2D)selected;
        string sourcePath = AssetDatabase.GetAssetPath(source);

        // 2. Make sure the source is readable
        TextureImporter importer = AssetImporter.GetAtPath(sourcePath) as TextureImporter;
        if (importer != null && !importer.isReadable)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();
            // Re-fetch after reimport
            source = AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePath);
        }

        int srcWidth = source.width;
        int srcHeight = source.height;

        // 3. Create new texture: original left half + mirrored right half
        //    Full width = source width * 2 (left original + right mirrored)
        int newWidth = srcWidth * 2;
        int newHeight = srcHeight;

        Texture2D mirrored = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, false);

        Color[] srcPixels = source.GetPixels();

        for (int y = 0; y < newHeight; y++)
        {
            for (int x = 0; x < newWidth; x++)
            {
                Color pixel;

                if (x < srcWidth)
                {
                    // Left half: original pixels
                    pixel = srcPixels[y * srcWidth + x];
                }
                else
                {
                    // Right half: mirror of original (read from right to left)
                    int mirroredX = newWidth - 1 - x;
                    pixel = srcPixels[y * srcWidth + mirroredX];
                }

                mirrored.SetPixel(x, y, pixel);
            }
        }

        mirrored.Apply();

        // 4. Save as PNG next to the original
        string directory = Path.GetDirectoryName(sourcePath);
        string filename = Path.GetFileNameWithoutExtension(sourcePath);
        string newPath = Path.Combine(directory, filename + "_mirrored.png");

        byte[] pngData = mirrored.EncodeToPNG();
        File.WriteAllBytes(newPath, pngData);

        AssetDatabase.Refresh();

        // 5. Copy import settings from source
        TextureImporter newImporter = AssetImporter.GetAtPath(newPath) as TextureImporter;
        if (newImporter != null && importer != null)
        {
            newImporter.textureType = importer.textureType;
            newImporter.spriteImportMode = importer.spriteImportMode;
            newImporter.spritePixelsPerUnit = importer.spritePixelsPerUnit;
            newImporter.filterMode = importer.filterMode;
            newImporter.textureCompression = importer.textureCompression;
            newImporter.isReadable = false;

            // Set 9-slice borders: mirror the L/R borders
            Vector4 srcBorder = importer.spriteBorder;
            // Original left border → new left border
            // Original left border → also new right border (mirrored)
            newImporter.spriteBorder = new Vector4(
                srcBorder.x,  // Left stays
                srcBorder.y,  // Bottom stays
                srcBorder.x,  // Right = same as Left (symmetric now)
                srcBorder.w   // Top stays
            );

            newImporter.SaveAndReimport();
        }

        // 6. Select the new sprite
        Object newAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(newPath);
        Selection.activeObject = newAsset;
        EditorGUIUtility.PingObject(newAsset);

        EditorUtility.DisplayDialog("Mirror Sprite",
            $"Created: {newPath}\n\nSize: {newWidth}x{newHeight}\n9-slice borders auto-set (symmetric).",
            "OK");
    }
}
