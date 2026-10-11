using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace NiftyNebulae
{
    class Texture3DManager
    {
        public static Texture3D Convert2DTo3D(Texture2D texture2D, int tileSize)
        {
            return Convert2DTo3D(texture2D.GetPixels32(), texture2D.width, texture2D.height, tileSize);
        }

        public static Texture3D Convert2DTo3D(Color32[] pixels, int width, int height, int tileSize)
        {
            if (tileSize < 1)
                throw new ArgumentException("textureTileSize must be at least 1");

            int tilesPerColumn = (int)Mathf.Sqrt(tileSize);
            int neededWidth = tileSize * ((tileSize - 1) / tilesPerColumn + 1);
            int neededHeight = tileSize * tilesPerColumn;
            if (width < neededWidth || height < neededHeight)
                Main.Log("The texture is " + width + "x" + height + " but textureTileSize = " + tileSize + " needs " + neededWidth + "x" + neededHeight + ", the nebula will not look right.", LogType.Warning);

            return Create(SliceAtlas(pixels, width, height, tileSize), tileSize, tileSize, tileSize);
        }

        public static Color32[] SliceAtlas(Color32[] pixels, int width, int height, int tileSize)
        {
            int tilesPerColumn = (int)Mathf.Sqrt(tileSize);
            Color32[] voxels = new Color32[tileSize * tileSize * tileSize];

            for (int z = 0; z < tileSize; z++)
            {
                int tileX = tileSize * (z / tilesPerColumn);
                int tileY = tileSize * (z % tilesPerColumn);
                for (int y = 0; y < tileSize; y++)
                {
                    int target = (z * tileSize + y) * tileSize;
                    int row = ((tileY + y) % height) * width;
                    if (tileX + tileSize <= width)
                        Array.Copy(pixels, row + tileX, voxels, target, tileSize);
                    else
                    {
                        for (int x = 0; x < tileSize; x++)
                            voxels[target + x] = pixels[row + (tileX + x) % width];
                    }
                }
            }

            return voxels;
        }

        public static Texture3D Create(Color32[] voxels, int width, int height, int depth)
        {
            Texture3D texture3D = new Texture3D(width, height, depth, TextureFormat.RGBA32, false);
            texture3D.wrapMode = TextureWrapMode.Clamp;
            texture3D.SetPixels32(voxels);
            texture3D.Apply();

            return texture3D;
        }
    }
}
