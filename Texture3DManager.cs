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
            return Convert2DTo3D(texture2D.GetPixels32(), texture2D.width, texture2D.height, tileSize, Vector3.zero);
        }

        public static Texture3D Convert2DTo3D(Texture2D texture2D, int tileSize, Vector3 rotation)
        {
            return Convert2DTo3D(texture2D.GetPixels32(), texture2D.width, texture2D.height, tileSize, rotation);
        }

        public static Texture3D Convert2DTo3D(Color32[] pixels, int width, int height, int tileSize)
        {
            return Convert2DTo3D(pixels, width, height, tileSize, Vector3.zero);
        }

        public static Texture3D Convert2DTo3D(Color32[] pixels, int width, int height, int tileSize, Vector3 rotation)
        {
            if (tileSize < 1)
                throw new ArgumentException("textureTileSize must be at least 1");

            int tilesPerColumn = (int)Mathf.Sqrt(tileSize);
            int neededWidth = tileSize * ((tileSize - 1) / tilesPerColumn + 1);
            int neededHeight = tileSize * tilesPerColumn;
            if (width < neededWidth || height < neededHeight)
                Main.Log("The texture is " + width + "x" + height + " but textureTileSize = " + tileSize + " needs " + neededWidth + "x" + neededHeight + ", the nebula will not look right.", LogType.Warning);

            return Create(Rotate(SliceAtlas(pixels, width, height, tileSize), tileSize, tileSize, tileSize, rotation), tileSize, tileSize, tileSize);
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

        public static Color32[] Rotate(Color32[] voxels, int width, int height, int depth, Vector3 eulerAngles)
        {
            Quaternion rotation = Quaternion.Euler(eulerAngles);
            if (Quaternion.Angle(rotation, Quaternion.identity) < 0.01f)
                return voxels;

            Quaternion inverse = Quaternion.Inverse(rotation);
            Color32[] rotated = Rotate(voxels, width, height, depth, inverse * Vector3.right, inverse * Vector3.up, inverse * Vector3.forward);
            Main.Log("Rotated 3D texture by " + eulerAngles.x + ", " + eulerAngles.y + ", " + eulerAngles.z);
            return rotated;
        }

        public static Color32[] Rotate(Color32[] voxels, int width, int height, int depth, Vector3 axisX, Vector3 axisY, Vector3 axisZ)
        {
            Color32[] rotated = new Color32[voxels.Length];

            Parallel.For(0, depth, z =>
            {
                float pz = (z + 0.5f) / depth - 0.5f;
                for (int y = 0; y < height; y++)
                {
                    float py = (y + 0.5f) / height - 0.5f;
                    int target = (z * height + y) * width;
                    for (int x = 0; x < width; x++)
                    {
                        float px = (x + 0.5f) / width - 0.5f;
                        float sx = (px * axisX.x + py * axisY.x + pz * axisZ.x + 0.5f) * width - 0.5f;
                        float sy = (px * axisX.y + py * axisY.y + pz * axisZ.y + 0.5f) * height - 0.5f;
                        float sz = (px * axisX.z + py * axisY.z + pz * axisZ.z + 0.5f) * depth - 0.5f;
                        rotated[target + x] = Sample(voxels, width, height, depth, sx, sy, sz);
                    }
                }
            });

            return rotated;
        }

        static Color32 Sample(Color32[] voxels, int width, int height, int depth, float sx, float sy, float sz)
        {
            int x0 = (int)Math.Floor(sx);
            int y0 = (int)Math.Floor(sy);
            int z0 = (int)Math.Floor(sz);
            if (x0 < -1 || y0 < -1 || z0 < -1 || x0 >= width || y0 >= height || z0 >= depth)
                return new Color32(0, 0, 0, 0);

            float fx = sx - x0;
            float fy = sy - y0;
            float fz = sz - z0;
            float red = 0, green = 0, blue = 0, alpha = 0;
            float plainRed = 0, plainGreen = 0, plainBlue = 0;

            for (int corner = 0; corner < 8; corner++)
            {
                int cx = x0 + (corner & 1);
                int cy = y0 + ((corner >> 1) & 1);
                int cz = z0 + (corner >> 2);
                if (cx < 0 || cy < 0 || cz < 0 || cx >= width || cy >= height || cz >= depth)
                    continue;

                float weight = ((corner & 1) != 0 ? fx : 1f - fx) * (((corner >> 1) & 1) != 0 ? fy : 1f - fy) * ((corner >> 2) != 0 ? fz : 1f - fz);
                Color32 voxel = voxels[(cz * height + cy) * width + cx];
                float weighted = weight * voxel.a;
                red += weighted * voxel.r;
                green += weighted * voxel.g;
                blue += weighted * voxel.b;
                alpha += weighted;
                plainRed += weight * voxel.r;
                plainGreen += weight * voxel.g;
                plainBlue += weight * voxel.b;
            }

            if (alpha > 0)
                return new Color32((byte)(red / alpha + 0.5f), (byte)(green / alpha + 0.5f), (byte)(blue / alpha + 0.5f), (byte)Math.Min(alpha + 0.5f, 255f));
            return new Color32((byte)(plainRed + 0.5f), (byte)(plainGreen + 0.5f), (byte)(plainBlue + 0.5f), 0);
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
