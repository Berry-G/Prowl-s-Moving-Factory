using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace PMF.EditorTools
{
    /// <summary>
    /// 그레이박스용 흰 사각/원 스프라이트와 색상 타일을 생성한다.
    /// CLAUDE.md 규약: 아트 에셋 금지 — Unity 기본 형태(사각/원)+색상만.
    /// </summary>
    internal static class GreyboxSprites
    {
        private const string ArtRoot = "Assets/_Project/Art/Greybox";

        internal static Sprite GetOrCreateSquare()
        {
            return GetOrCreateSprite("Square", GenerateSquareTexture);
        }

        internal static Sprite GetOrCreateCircle()
        {
            return GetOrCreateSprite("Circle", GenerateCircleTexture);
        }

        internal static Tile GetOrCreateTile(string name, Color color, Sprite square)
        {
            string path = $"{ArtRoot}/{name}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = square;
                tile.color = color;
                AssetDatabase.CreateAsset(tile, path);
            }
            return tile;
        }

        private static Sprite GetOrCreateSprite(string name,
            System.Func<int, Texture2D> generator)
        {
            EnsureFolder();
            string pngPath = $"{ArtRoot}/{name}.png";

            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
            if (existing != null) return existing;

            var tex = generator(name == "Circle" ? 64 : 32);
            File.WriteAllBytes(pngPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(pngPath);

            var importer = (TextureImporter)AssetImporter.GetAtPath(pngPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
        }

        private static Texture2D GenerateSquareTexture(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var fill = new Color(1f, 1f, 1f, 1f);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, fill);
            tex.Apply();
            return tex;
        }

        private static Texture2D GenerateCircleTexture(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float radius = size * 0.5f;
            var clear = new Color(0f, 0f, 0f, 0f);
            var fill = new Color(1f, 1f, 1f, 1f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - radius + 0.5f;
                    float dy = y - radius + 0.5f;
                    bool inside = dx * dx + dy * dy <= radius * radius;
                    tex.SetPixel(x, y, inside ? fill : clear);
                }
            }
            tex.Apply();
            return tex;
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Art"))
                AssetDatabase.CreateFolder("Assets/_Project", "Art");
            if (!AssetDatabase.IsValidFolder(ArtRoot))
                AssetDatabase.CreateFolder("Assets/_Project/Art", "Greybox");
        }
    }
}
