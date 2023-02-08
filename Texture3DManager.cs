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
            Texture3D texture3D = new Texture3D(tileSize, tileSize, tileSize, TextureFormat.RGBA32, false);
            texture3D.wrapMode = TextureWrapMode.Clamp;
            Color[] colors = new Color[tileSize * tileSize * tileSize];

            for (int z = 0; z < tileSize; z++)
            {
                int zOffset = z * tileSize * tileSize;
                for (int y = 0; y < tileSize; y++)
                {
                    int yOffset = y * tileSize;
                    for (int x = 0; x < tileSize; x++)
                    {
                        colors[x + yOffset + zOffset] = texture2D.GetPixel(tileSize * Mathf.FloorToInt(z / (int)Mathf.Sqrt(tileSize)) + x, tileSize * (z % (int)Mathf.Sqrt(tileSize)) + y);
                    }
                }
            }

            texture3D.SetPixels(colors);
            texture3D.Apply();

            return texture3D;
        }
    }
}
