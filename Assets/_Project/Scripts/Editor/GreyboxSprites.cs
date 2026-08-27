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

        internal static Sprite GetOrCreateHeart()
        {
            return GetOrCreateSprite("Heart", GenerateHeartTexture);
        }

        /// <summary>
        /// 타일 색은 코드(SceneParts)가 진실이므로 이미 존재해도 매번 최신 색으로 갱신한다.
        /// (밸런스 SO 와 달리 디자이너가 인스펙터에서 손대는 대상이 아니다.)
        /// </summary>
        internal static Tile GetOrCreateTile(string name, Color color, Sprite square)
        {
            string path = $"{ArtRoot}/{name}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, path);
            }
            tile.sprite = square;
            tile.color = color;

            // Square 스프라이트는 PPU=100 인데 텍스처가 32px 라 원본 크기가 0.32 유닛뿐이다.
            // 액터용 스프라이트를 그대로 쓰되, 타일만 1유닛 셀을 꽉 채우도록 개별 스케일을 준다.
            // (Square 자체의 PPU 를 올리면 마을/모체 등 액터 크기가 전부 같이 틀어진다.)
            float nativeSize = square.rect.width / square.pixelsPerUnit;
            float fillScale = nativeSize > 0f ? 1f / nativeSize : 1f;
            tile.transform = Matrix4x4.Scale(new Vector3(fillScale, fillScale, 1f));

            EditorUtility.SetDirty(tile);
            return tile;
        }

        private static Sprite GetOrCreateSprite(string name,
            System.Func<int, Texture2D> generator)
        {
            EnsureFolder();
            string pngPath = $"{ArtRoot}/{name}.png";

            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
            if (existing != null) return existing;

            var tex = generator(name == "Circle" || name == "Heart" ? 64 : 32);
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

        /// <summary>고전 하트 음함수 곡선: (x²+y²-1)³ - x²y³ ≤ 0. 원과 동일하게 순수 수식으로 생성.</summary>
        private static Texture2D GenerateHeartTexture(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var clear = new Color(0f, 0f, 0f, 0f);
            var fill = new Color(1f, 1f, 1f, 1f);
            float scale = size * 0.34f;
            float centerX = size * 0.5f;
            float centerY = size * 0.46f;   // 곡선이 y 음수 쪽(뾰족한 끝)으로 더 뻗어서 살짝 내려서 중앙에 맞춘다.

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x + 0.5f - centerX) / scale;
                    float ny = (y + 0.5f - centerY) / scale;
                    float f = Mathf.Pow(nx * nx + ny * ny - 1f, 3f) - nx * nx * ny * ny * ny;
                    tex.SetPixel(x, y, f <= 0f ? fill : clear);
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
