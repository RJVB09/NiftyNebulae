using System;
using UnityEngine;

namespace NiftyNebulae
{
    public class DDSTexture
    {
        public int width;
        public int height;
        public int depth;
        public string format;
        public bool hasAlpha;
        public Color32[] pixels;
    }

    public static class DDSLoader
    {
        const uint Magic = 0x20534444;
        const int HeaderSize = 128;
        const int DX10HeaderSize = 20;

        const uint DDSD_DEPTH = 0x800000;
        const uint DDPF_ALPHAPIXELS = 0x1;
        const uint DDPF_FOURCC = 0x4;
        const uint DDPF_LUMINANCE = 0x20000;
        const uint DDSCAPS2_VOLUME = 0x200000;
        const uint DX10_TEXTURE3D = 4;

        enum Encoding
        {
            Masks,
            BC1,
            BC2,
            BC3,
            BC7
        }

        static readonly byte[] ColorBits = { 4, 6, 5, 7, 5, 7, 7, 5 };
        static readonly byte[] AlphaBits = { 0, 0, 0, 0, 6, 8, 7, 5 };
        static readonly int[] Weights2 = { 0, 21, 43, 64 };
        static readonly int[] Weights3 = { 0, 9, 18, 27, 37, 46, 55, 64 };
        static readonly int[] Weights4 = { 0, 4, 9, 13, 17, 21, 26, 30, 34, 38, 43, 47, 51, 55, 60, 64 };
        const int ModesWithPBits = 0xCB;

        static readonly byte[] Partitions2 =
        {
            128,   0,   1,   1,   0,   0,   1,   1,   0,   0,   1,   1,   0,   0,   1, 129,
            128,   0,   0,   1,   0,   0,   0,   1,   0,   0,   0,   1,   0,   0,   0, 129,
            128,   1,   1,   1,   0,   1,   1,   1,   0,   1,   1,   1,   0,   1,   1, 129,
            128,   0,   0,   1,   0,   0,   1,   1,   0,   0,   1,   1,   0,   1,   1, 129,
            128,   0,   0,   0,   0,   0,   0,   1,   0,   0,   0,   1,   0,   0,   1, 129,
            128,   0,   1,   1,   0,   1,   1,   1,   0,   1,   1,   1,   1,   1,   1, 129,
            128,   0,   0,   1,   0,   0,   1,   1,   0,   1,   1,   1,   1,   1,   1, 129,
            128,   0,   0,   0,   0,   0,   0,   1,   0,   0,   1,   1,   0,   1,   1, 129,
            128,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   1,   0,   0,   1, 129,
            128,   0,   1,   1,   0,   1,   1,   1,   1,   1,   1,   1,   1,   1,   1, 129,
            128,   0,   0,   0,   0,   0,   0,   1,   0,   1,   1,   1,   1,   1,   1, 129,
            128,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   1,   0,   1,   1, 129,
            128,   0,   0,   1,   0,   1,   1,   1,   1,   1,   1,   1,   1,   1,   1, 129,
            128,   0,   0,   0,   0,   0,   0,   0,   1,   1,   1,   1,   1,   1,   1, 129,
            128,   0,   0,   0,   1,   1,   1,   1,   1,   1,   1,   1,   1,   1,   1, 129,
            128,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   1,   1,   1, 129,
            128,   0,   0,   0,   1,   0,   0,   0,   1,   1,   1,   0,   1,   1,   1, 129,
            128,   1, 129,   1,   0,   0,   0,   1,   0,   0,   0,   0,   0,   0,   0,   0,
            128,   0,   0,   0,   0,   0,   0,   0, 129,   0,   0,   0,   1,   1,   1,   0,
            128,   1, 129,   1,   0,   0,   1,   1,   0,   0,   0,   1,   0,   0,   0,   0,
            128,   0, 129,   1,   0,   0,   0,   1,   0,   0,   0,   0,   0,   0,   0,   0,
            128,   0,   0,   0,   1,   0,   0,   0, 129,   1,   0,   0,   1,   1,   1,   0,
            128,   0,   0,   0,   0,   0,   0,   0, 129,   0,   0,   0,   1,   1,   0,   0,
            128,   1,   1,   1,   0,   0,   1,   1,   0,   0,   1,   1,   0,   0,   0, 129,
            128,   0, 129,   1,   0,   0,   0,   1,   0,   0,   0,   1,   0,   0,   0,   0,
            128,   0,   0,   0,   1,   0,   0,   0, 129,   0,   0,   0,   1,   1,   0,   0,
            128,   1, 129,   0,   0,   1,   1,   0,   0,   1,   1,   0,   0,   1,   1,   0,
            128,   0, 129,   1,   0,   1,   1,   0,   0,   1,   1,   0,   1,   1,   0,   0,
            128,   0,   0,   1,   0,   1,   1,   1, 129,   1,   1,   0,   1,   0,   0,   0,
            128,   0,   0,   0,   1,   1,   1,   1, 129,   1,   1,   1,   0,   0,   0,   0,
            128,   1, 129,   1,   0,   0,   0,   1,   1,   0,   0,   0,   1,   1,   1,   0,
            128,   0, 129,   1,   1,   0,   0,   1,   1,   0,   0,   1,   1,   1,   0,   0,
            128,   1,   0,   1,   0,   1,   0,   1,   0,   1,   0,   1,   0,   1,   0, 129,
            128,   0,   0,   0,   1,   1,   1,   1,   0,   0,   0,   0,   1,   1,   1, 129,
            128,   1,   0,   1,   1,   0, 129,   0,   0,   1,   0,   1,   1,   0,   1,   0,
            128,   0,   1,   1,   0,   0,   1,   1, 129,   1,   0,   0,   1,   1,   0,   0,
            128,   0, 129,   1,   1,   1,   0,   0,   0,   0,   1,   1,   1,   1,   0,   0,
            128,   1,   0,   1,   0,   1,   0,   1, 129,   0,   1,   0,   1,   0,   1,   0,
            128,   1,   1,   0,   1,   0,   0,   1,   0,   1,   1,   0,   1,   0,   0, 129,
            128,   1,   0,   1,   1,   0,   1,   0,   1,   0,   1,   0,   0,   1,   0, 129,
            128,   1, 129,   1,   0,   0,   1,   1,   1,   1,   0,   0,   1,   1,   1,   0,
            128,   0,   0,   1,   0,   0,   1,   1, 129,   1,   0,   0,   1,   0,   0,   0,
            128,   0, 129,   1,   0,   0,   1,   0,   0,   1,   0,   0,   1,   1,   0,   0,
            128,   0, 129,   1,   1,   0,   1,   1,   1,   1,   0,   1,   1,   1,   0,   0,
            128,   1, 129,   0,   1,   0,   0,   1,   1,   0,   0,   1,   0,   1,   1,   0,
            128,   0,   1,   1,   1,   1,   0,   0,   1,   1,   0,   0,   0,   0,   1, 129,
            128,   1,   1,   0,   0,   1,   1,   0,   1,   0,   0,   1,   1,   0,   0, 129,
            128,   0,   0,   0,   0,   1, 129,   0,   0,   1,   1,   0,   0,   0,   0,   0,
            128,   1,   0,   0,   1,   1, 129,   0,   0,   1,   0,   0,   0,   0,   0,   0,
            128,   0, 129,   0,   0,   1,   1,   1,   0,   0,   1,   0,   0,   0,   0,   0,
            128,   0,   0,   0,   0,   0, 129,   0,   0,   1,   1,   1,   0,   0,   1,   0,
            128,   0,   0,   0,   0,   1,   0,   0, 129,   1,   1,   0,   0,   1,   0,   0,
            128,   1,   1,   0,   1,   1,   0,   0,   1,   0,   0,   1,   0,   0,   1, 129,
            128,   0,   1,   1,   0,   1,   1,   0,   1,   1,   0,   0,   1,   0,   0, 129,
            128,   1, 129,   0,   0,   0,   1,   1,   1,   0,   0,   1,   1,   1,   0,   0,
            128,   0, 129,   1,   1,   0,   0,   1,   1,   1,   0,   0,   0,   1,   1,   0,
            128,   1,   1,   0,   1,   1,   0,   0,   1,   1,   0,   0,   1,   0,   0, 129,
            128,   1,   1,   0,   0,   0,   1,   1,   0,   0,   1,   1,   1,   0,   0, 129,
            128,   1,   1,   1,   1,   1,   1,   0,   1,   0,   0,   0,   0,   0,   0, 129,
            128,   0,   0,   1,   1,   0,   0,   0,   1,   1,   1,   0,   0,   1,   1, 129,
            128,   0,   0,   0,   1,   1,   1,   1,   0,   0,   1,   1,   0,   0,   1, 129,
            128,   0, 129,   1,   0,   0,   1,   1,   1,   1,   1,   1,   0,   0,   0,   0,
            128,   0, 129,   0,   0,   0,   1,   0,   1,   1,   1,   0,   1,   1,   1,   0,
            128,   1,   0,   0,   0,   1,   0,   0,   0,   1,   1,   1,   0,   1,   1, 129
        };

        static readonly byte[] Partitions3 =
        {
            128,   0,   1, 129,   0,   0,   1,   1,   0,   2,   2,   1,   2,   2,   2, 130,
            128,   0,   0, 129,   0,   0,   1,   1, 130,   2,   1,   1,   2,   2,   2,   1,
            128,   0,   0,   0,   2,   0,   0,   1, 130,   2,   1,   1,   2,   2,   1, 129,
            128,   2,   2, 130,   0,   0,   2,   2,   0,   0,   1,   1,   0,   1,   1, 129,
            128,   0,   0,   0,   0,   0,   0,   0, 129,   1,   2,   2,   1,   1,   2, 130,
            128,   0,   1, 129,   0,   0,   1,   1,   0,   0,   2,   2,   0,   0,   2, 130,
            128,   0,   2, 130,   0,   0,   2,   2,   1,   1,   1,   1,   1,   1,   1, 129,
            128,   0,   1,   1,   0,   0,   1,   1, 130,   2,   1,   1,   2,   2,   1, 129,
            128,   0,   0,   0,   0,   0,   0,   0, 129,   1,   1,   1,   2,   2,   2, 130,
            128,   0,   0,   0,   1,   1,   1,   1, 129,   1,   1,   1,   2,   2,   2, 130,
            128,   0,   0,   0,   1,   1, 129,   1,   2,   2,   2,   2,   2,   2,   2, 130,
            128,   0,   1,   2,   0,   0, 129,   2,   0,   0,   1,   2,   0,   0,   1, 130,
            128,   1,   1,   2,   0,   1, 129,   2,   0,   1,   1,   2,   0,   1,   1, 130,
            128,   1,   2,   2,   0, 129,   2,   2,   0,   1,   2,   2,   0,   1,   2, 130,
            128,   0,   1, 129,   0,   1,   1,   2,   1,   1,   2,   2,   1,   2,   2, 130,
            128,   0,   1, 129,   2,   0,   0,   1, 130,   2,   0,   0,   2,   2,   2,   0,
            128,   0,   0, 129,   0,   0,   1,   1,   0,   1,   1,   2,   1,   1,   2, 130,
            128,   1,   1, 129,   0,   0,   1,   1, 130,   0,   0,   1,   2,   2,   0,   0,
            128,   0,   0,   0,   1,   1,   2,   2, 129,   1,   2,   2,   1,   1,   2, 130,
            128,   0,   2, 130,   0,   0,   2,   2,   0,   0,   2,   2,   1,   1,   1, 129,
            128,   1,   1, 129,   0,   1,   1,   1,   0,   2,   2,   2,   0,   2,   2, 130,
            128,   0,   0, 129,   0,   0,   0,   1, 130,   2,   2,   1,   2,   2,   2,   1,
            128,   0,   0,   0,   0,   0, 129,   1,   0,   1,   2,   2,   0,   1,   2, 130,
            128,   0,   0,   0,   1,   1,   0,   0, 130,   2, 129,   0,   2,   2,   1,   0,
            128,   1,   2, 130,   0, 129,   2,   2,   0,   0,   1,   1,   0,   0,   0,   0,
            128,   0,   1,   2,   0,   0,   1,   2, 129,   1,   2,   2,   2,   2,   2, 130,
            128,   1,   1,   0,   1,   2, 130,   1, 129,   2,   2,   1,   0,   1,   1,   0,
            128,   0,   0,   0,   0,   1, 129,   0,   1,   2, 130,   1,   1,   2,   2,   1,
            128,   0,   2,   2,   1,   1,   0,   2, 129,   1,   0,   2,   0,   0,   2, 130,
            128,   1,   1,   0,   0, 129,   1,   0,   2,   0,   0,   2,   2,   2,   2, 130,
            128,   0,   1,   1,   0,   1,   2,   2,   0,   1, 130,   2,   0,   0,   1, 129,
            128,   0,   0,   0,   2,   0,   0,   0, 130,   2,   1,   1,   2,   2,   2, 129,
            128,   0,   0,   0,   0,   0,   0,   2, 129,   1,   2,   2,   1,   2,   2, 130,
            128,   2,   2, 130,   0,   0,   2,   2,   0,   0,   1,   2,   0,   0,   1, 129,
            128,   0,   1, 129,   0,   0,   1,   2,   0,   0,   2,   2,   0,   2,   2, 130,
            128,   1,   2,   0,   0, 129,   2,   0,   0,   1, 130,   0,   0,   1,   2,   0,
            128,   0,   0,   0,   1,   1, 129,   1,   2,   2, 130,   2,   0,   0,   0,   0,
            128,   1,   2,   0,   1,   2,   0,   1, 130,   0, 129,   2,   0,   1,   2,   0,
            128,   1,   2,   0,   2,   0,   1,   2, 129, 130,   0,   1,   0,   1,   2,   0,
            128,   0,   1,   1,   2,   2,   0,   0,   1,   1, 130,   2,   0,   0,   1, 129,
            128,   0,   1,   1,   1,   1, 130,   2,   2,   2,   0,   0,   0,   0,   1, 129,
            128,   1,   0, 129,   0,   1,   0,   1,   2,   2,   2,   2,   2,   2,   2, 130,
            128,   0,   0,   0,   0,   0,   0,   0, 130,   1,   2,   1,   2,   1,   2, 129,
            128,   0,   2,   2,   1, 129,   2,   2,   0,   0,   2,   2,   1,   1,   2, 130,
            128,   0,   2, 130,   0,   0,   1,   1,   0,   0,   2,   2,   0,   0,   1, 129,
            128,   2,   2,   0,   1,   2, 130,   1,   0,   2,   2,   0,   1,   2,   2, 129,
            128,   1,   0,   1,   2,   2, 130,   2,   2,   2,   2,   2,   0,   1,   0, 129,
            128,   0,   0,   0,   2,   1,   2,   1, 130,   1,   2,   1,   2,   1,   2, 129,
            128,   1,   0, 129,   0,   1,   0,   1,   0,   1,   0,   1,   2,   2,   2, 130,
            128,   2,   2, 130,   0,   1,   1,   1,   0,   2,   2,   2,   0,   1,   1, 129,
            128,   0,   0,   2,   1, 129,   1,   2,   0,   0,   0,   2,   1,   1,   1, 130,
            128,   0,   0,   0,   2, 129,   1,   2,   2,   1,   1,   2,   2,   1,   1, 130,
            128,   2,   2,   2,   0, 129,   1,   1,   0,   1,   1,   1,   0,   2,   2, 130,
            128,   0,   0,   2,   1,   1,   1,   2, 129,   1,   1,   2,   0,   0,   0, 130,
            128,   1,   1,   0,   0, 129,   1,   0,   0,   1,   1,   0,   2,   2,   2, 130,
            128,   0,   0,   0,   0,   0,   0,   0,   2,   1, 129,   2,   2,   1,   1, 130,
            128,   1,   1,   0,   0, 129,   1,   0,   2,   2,   2,   2,   2,   2,   2, 130,
            128,   0,   2,   2,   0,   0,   1,   1,   0,   0, 129,   1,   0,   0,   2, 130,
            128,   0,   2,   2,   1,   1,   2,   2, 129,   1,   2,   2,   0,   0,   2, 130,
            128,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   2, 129,   1, 130,
            128,   0,   0, 130,   0,   0,   0,   1,   0,   0,   0,   2,   0,   0,   0, 129,
            128,   2,   2,   2,   1,   2,   2,   2,   0,   2,   2,   2, 129,   2,   2, 130,
            128,   1,   0, 129,   2,   2,   2,   2,   2,   2,   2,   2,   2,   2,   2, 130,
            128,   1,   1, 129,   2,   0,   1,   1, 130,   2,   0,   1,   2,   2,   2,   0
        };

        public static bool IsDDS(byte[] data)
        {
            return data != null && data.Length >= 4 && ReadUInt32(data, 0) == Magic;
        }

        public static DDSTexture Load(byte[] data)
        {
            if (!IsDDS(data))
                throw new FormatException("not a DDS file");
            if (data.Length < HeaderSize)
                throw new FormatException("the DDS header is incomplete");

            uint flags = ReadUInt32(data, 8);
            long height = ReadUInt32(data, 12);
            long width = ReadUInt32(data, 16);
            long depth = ReadUInt32(data, 24);
            uint pixelFlags = ReadUInt32(data, 80);
            uint fourCC = ReadUInt32(data, 84);
            int bitCount = (int)ReadUInt32(data, 88);
            uint rMask = ReadUInt32(data, 92);
            uint gMask = ReadUInt32(data, 96);
            uint bMask = ReadUInt32(data, 100);
            uint aMask = ReadUInt32(data, 104);
            uint caps2 = ReadUInt32(data, 112);

            bool volume = (flags & DDSD_DEPTH) != 0 || (caps2 & DDSCAPS2_VOLUME) != 0;
            int dataOffset = HeaderSize;
            Encoding encoding = Encoding.Masks;
            bool luminance = false;
            bool alphaOnly = false;
            string format;

            if ((pixelFlags & DDPF_FOURCC) != 0)
            {
                format = FourCCName(fourCC);
                switch (format)
                {
                    case "DXT1":
                        encoding = Encoding.BC1;
                        break;
                    case "DXT2":
                    case "DXT3":
                        encoding = Encoding.BC2;
                        break;
                    case "DXT4":
                    case "DXT5":
                        encoding = Encoding.BC3;
                        break;
                    case "DX10":
                        if (data.Length < HeaderSize + DX10HeaderSize)
                            throw new FormatException("the DX10 header is incomplete");
                        uint dxgiFormat = ReadUInt32(data, 128);
                        volume = ReadUInt32(data, 132) == DX10_TEXTURE3D;
                        dataOffset += DX10HeaderSize;
                        switch (dxgiFormat)
                        {
                            case 70:
                            case 71:
                            case 72:
                                encoding = Encoding.BC1;
                                format = "BC1";
                                break;
                            case 73:
                            case 74:
                            case 75:
                                encoding = Encoding.BC2;
                                format = "BC2";
                                break;
                            case 76:
                            case 77:
                            case 78:
                                encoding = Encoding.BC3;
                                format = "BC3";
                                break;
                            case 97:
                            case 98:
                            case 99:
                                encoding = Encoding.BC7;
                                format = "BC7";
                                break;
                            case 27:
                            case 28:
                            case 29:
                                format = "RGBA8";
                                bitCount = 32;
                                rMask = 0x000000FF;
                                gMask = 0x0000FF00;
                                bMask = 0x00FF0000;
                                aMask = 0xFF000000;
                                break;
                            case 87:
                            case 90:
                            case 91:
                                format = "BGRA8";
                                bitCount = 32;
                                rMask = 0x00FF0000;
                                gMask = 0x0000FF00;
                                bMask = 0x000000FF;
                                aMask = 0xFF000000;
                                break;
                            case 88:
                            case 92:
                            case 93:
                                format = "BGRX8";
                                bitCount = 32;
                                rMask = 0x00FF0000;
                                gMask = 0x0000FF00;
                                bMask = 0x000000FF;
                                aMask = 0;
                                break;
                            case 49:
                                format = "RG8";
                                bitCount = 16;
                                rMask = 0x00FF;
                                gMask = 0xFF00;
                                bMask = 0;
                                aMask = 0;
                                break;
                            case 61:
                                format = "R8";
                                bitCount = 8;
                                rMask = 0xFF;
                                gMask = 0;
                                bMask = 0;
                                aMask = 0;
                                break;
                            case 65:
                                format = "A8";
                                bitCount = 8;
                                rMask = 0;
                                gMask = 0;
                                bMask = 0;
                                aMask = 0xFF;
                                alphaOnly = true;
                                break;
                            default:
                                throw new NotSupportedException("DXGI format " + dxgiFormat + " is not supported (use DXT1, DXT3, DXT5, BC7 or an uncompressed format)");
                        }
                        break;
                    default:
                        throw new NotSupportedException("DDS format " + format + " is not supported (use DXT1, DXT3, DXT5, BC7 or an uncompressed format)");
                }
            }
            else
            {
                if (bitCount != 8 && bitCount != 16 && bitCount != 24 && bitCount != 32)
                    throw new NotSupportedException("uncompressed DDS with " + bitCount + " bits per pixel is not supported");
                if (bitCount < 32)
                {
                    uint usedBits = (1u << bitCount) - 1;
                    rMask &= usedBits;
                    gMask &= usedBits;
                    bMask &= usedBits;
                    aMask &= usedBits;
                }
                luminance = (pixelFlags & DDPF_LUMINANCE) != 0;
                if (luminance)
                {
                    if (rMask == 0)
                        rMask = 0xFF;
                    if (aMask == 0 && bitCount == 16 && (pixelFlags & DDPF_ALPHAPIXELS) != 0)
                        aMask = 0xFF00;
                }
                if ((rMask | gMask | bMask | aMask) == 0)
                    throw new NotSupportedException("the DDS pixel format has no channel masks");
                alphaOnly = !luminance && (rMask | gMask | bMask) == 0;
                format = (luminance ? "L" : alphaOnly ? "A" : "RGB") + (aMask != 0 && !alphaOnly ? "A" : "") + " " + bitCount + "-bit";
            }

            if (!volume || depth < 1)
                depth = 1;
            if (width < 1 || height < 1)
                throw new FormatException("the DDS has no pixels");
            if (width * height * depth > int.MaxValue / 4)
                throw new NotSupportedException("the DDS is too large (" + width + "x" + height + "x" + depth + ")");

            DDSTexture texture = new DDSTexture();
            texture.width = (int)width;
            texture.height = (int)height;
            texture.depth = (int)depth;
            texture.format = format;
            texture.hasAlpha = encoding == Encoding.Masks ? aMask != 0 : encoding != Encoding.BC1;
            texture.pixels = new Color32[width * height * depth];

            long sliceBytes;
            if (encoding == Encoding.Masks)
                sliceBytes = width * height * (bitCount / 8);
            else
                sliceBytes = ((width + 3) / 4) * ((height + 3) / 4) * (encoding == Encoding.BC1 ? 8 : 16);
            if (data.Length - dataOffset < sliceBytes * depth)
                throw new FormatException("the DDS is truncated (" + (data.Length - dataOffset) + " bytes of pixel data, " + (sliceBytes * depth) + " needed)");

            int slicePixels = texture.width * texture.height;
            for (int z = 0; z < texture.depth; z++)
            {
                int source = dataOffset + (int)(sliceBytes * z);
                if (encoding == Encoding.Masks)
                    DecodeMasks(data, source, texture.pixels, slicePixels * z, slicePixels, bitCount / 8, rMask, gMask, bMask, aMask, luminance, alphaOnly);
                else
                    DecodeBlocks(data, source, texture.pixels, slicePixels * z, texture.width, texture.height, encoding);
            }
            return texture;
        }

        static string FourCCName(uint fourCC)
        {
            char[] name = new char[4];
            for (int i = 0; i < 4; i++)
            {
                uint c = (fourCC >> (8 * i)) & 0xFF;
                name[i] = c >= 32 && c < 127 ? (char)c : '?';
            }
            return new string(name);
        }

        static uint ReadUInt32(byte[] data, int offset)
        {
            return (uint)(data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24));
        }

        static ulong ReadUInt64(byte[] data, int offset)
        {
            return ReadUInt32(data, offset) | ((ulong)ReadUInt32(data, offset + 4) << 32);
        }

        struct Channel
        {
            public uint mask;
            public int shift;
            public ulong max;
            public int byteIndex;
            public byte missing;

            public Channel(uint mask, byte missing)
            {
                this.mask = mask;
                this.missing = missing;
                shift = 0;
                max = 0;
                byteIndex = -1;
                if (mask == 0)
                    return;
                while (((mask >> shift) & 1) == 0)
                    shift++;
                max = mask >> shift;
                if (max == 0xFF && shift % 8 == 0)
                    byteIndex = shift / 8;
            }

            public byte Read(uint value)
            {
                if (mask == 0)
                    return missing;
                ulong v = (value & mask) >> shift;
                return (byte)((v * 255 + max / 2) / max);
            }
        }

        static void DecodeMasks(byte[] data, int source, Color32[] pixels, int target, int count, int bytesPerPixel, uint rMask, uint gMask, uint bMask, uint aMask, bool luminance, bool alphaOnly)
        {
            byte missingColor = alphaOnly ? (byte)255 : (byte)0;
            Channel red = new Channel(rMask, missingColor);
            Channel green = new Channel(gMask, missingColor);
            Channel blue = new Channel(bMask, missingColor);
            Channel alpha = new Channel(aMask, 255);

            if (luminance)
            {
                green = red;
                blue = red;
            }

            bool direct = (red.mask == 0 || red.byteIndex >= 0) && (green.mask == 0 || green.byteIndex >= 0) && (blue.mask == 0 || blue.byteIndex >= 0) && (alpha.mask == 0 || alpha.byteIndex >= 0);
            if (direct)
            {
                int ri = red.byteIndex;
                int gi = green.byteIndex;
                int bi = blue.byteIndex;
                int ai = alpha.byteIndex;
                for (int i = 0; i < count; i++)
                {
                    byte r = ri >= 0 ? data[source + ri] : missingColor;
                    byte g = gi >= 0 ? data[source + gi] : missingColor;
                    byte b = bi >= 0 ? data[source + bi] : missingColor;
                    byte a = ai >= 0 ? data[source + ai] : (byte)255;
                    pixels[target + i] = new Color32(r, g, b, a);
                    source += bytesPerPixel;
                }
                return;
            }

            for (int i = 0; i < count; i++)
            {
                uint value = 0;
                for (int b = 0; b < bytesPerPixel; b++)
                    value |= (uint)data[source + b] << (8 * b);
                pixels[target + i] = new Color32(red.Read(value), green.Read(value), blue.Read(value), alpha.Read(value));
                source += bytesPerPixel;
            }
        }

        static void DecodeBlocks(byte[] data, int source, Color32[] pixels, int target, int width, int height, Encoding encoding)
        {
            Color32[] block = new Color32[16];
            Color32[] palette = new Color32[4];
            byte[] alphas = new byte[8];
            int[] endpoints = new int[24];
            int[] indices = new int[16];

            for (int y = 0; y < height; y += 4)
            {
                for (int x = 0; x < width; x += 4)
                {
                    switch (encoding)
                    {
                        case Encoding.BC1:
                            DecodeColorBlock(data, source, block, palette, false);
                            source += 8;
                            break;
                        case Encoding.BC2:
                            DecodeColorBlock(data, source + 8, block, palette, true);
                            DecodeExplicitAlpha(data, source, block);
                            source += 16;
                            break;
                        case Encoding.BC3:
                            DecodeColorBlock(data, source + 8, block, palette, true);
                            DecodeInterpolatedAlpha(data, source, block, alphas);
                            source += 16;
                            break;
                        default:
                            DecodeBC7Block(data, source, block, endpoints, indices);
                            source += 16;
                            break;
                    }

                    for (int i = 0; i < 4 && y + i < height; i++)
                    {
                        int row = target + (y + i) * width + x;
                        for (int j = 0; j < 4 && x + j < width; j++)
                            pixels[row + j] = block[i * 4 + j];
                    }
                }
            }
        }

        static void DecodeColorBlock(byte[] data, int offset, Color32[] block, Color32[] palette, bool opaqueOnly)
        {
            int c0 = data[offset] | (data[offset + 1] << 8);
            int c1 = data[offset + 2] | (data[offset + 3] << 8);

            int r0 = (c0 >> 11) & 0x1F;
            int g0 = (c0 >> 5) & 0x3F;
            int b0 = c0 & 0x1F;
            int r1 = (c1 >> 11) & 0x1F;
            int g1 = (c1 >> 5) & 0x3F;
            int b1 = c1 & 0x1F;

            palette[0] = new Color32((byte)((r0 * 527 + 23) >> 6), (byte)((g0 * 259 + 33) >> 6), (byte)((b0 * 527 + 23) >> 6), 255);
            palette[1] = new Color32((byte)((r1 * 527 + 23) >> 6), (byte)((g1 * 259 + 33) >> 6), (byte)((b1 * 527 + 23) >> 6), 255);

            if (c0 > c1 || opaqueOnly)
            {
                palette[2] = new Color32((byte)(((2 * r0 + r1) * 351 + 61) >> 7), (byte)(((2 * g0 + g1) * 2763 + 1039) >> 11), (byte)(((2 * b0 + b1) * 351 + 61) >> 7), 255);
                palette[3] = new Color32((byte)(((r0 + 2 * r1) * 351 + 61) >> 7), (byte)(((g0 + 2 * g1) * 2763 + 1039) >> 11), (byte)(((b0 + 2 * b1) * 351 + 61) >> 7), 255);
            }
            else
            {
                palette[2] = new Color32((byte)(((r0 + r1) * 1053 + 125) >> 8), (byte)(((g0 + g1) * 4145 + 1019) >> 11), (byte)(((b0 + b1) * 1053 + 125) >> 8), 255);
                palette[3] = new Color32(0, 0, 0, 0);
            }

            uint selectors = ReadUInt32(data, offset + 4);
            for (int i = 0; i < 16; i++)
            {
                block[i] = palette[selectors & 3];
                selectors >>= 2;
            }
        }

        static void DecodeExplicitAlpha(byte[] data, int offset, Color32[] block)
        {
            for (int i = 0; i < 16; i++)
            {
                int nibble = (data[offset + i / 2] >> (4 * (i & 1))) & 0x0F;
                block[i].a = (byte)(nibble * 17);
            }
        }

        static void DecodeInterpolatedAlpha(byte[] data, int offset, Color32[] block, byte[] alphas)
        {
            int a0 = data[offset];
            int a1 = data[offset + 1];
            alphas[0] = (byte)a0;
            alphas[1] = (byte)a1;
            if (a0 > a1)
            {
                alphas[2] = (byte)((6 * a0 + a1) / 7);
                alphas[3] = (byte)((5 * a0 + 2 * a1) / 7);
                alphas[4] = (byte)((4 * a0 + 3 * a1) / 7);
                alphas[5] = (byte)((3 * a0 + 4 * a1) / 7);
                alphas[6] = (byte)((2 * a0 + 5 * a1) / 7);
                alphas[7] = (byte)((a0 + 6 * a1) / 7);
            }
            else
            {
                alphas[2] = (byte)((4 * a0 + a1) / 5);
                alphas[3] = (byte)((3 * a0 + 2 * a1) / 5);
                alphas[4] = (byte)((2 * a0 + 3 * a1) / 5);
                alphas[5] = (byte)((a0 + 4 * a1) / 5);
                alphas[6] = 0;
                alphas[7] = 255;
            }

            ulong selectors = ReadUInt64(data, offset) >> 16;
            for (int i = 0; i < 16; i++)
            {
                block[i].a = alphas[selectors & 7];
                selectors >>= 3;
            }
        }

        struct BitReader
        {
            ulong low;
            ulong high;

            public BitReader(byte[] data, int offset)
            {
                low = ReadUInt64(data, offset);
                high = ReadUInt64(data, offset + 8);
            }

            public int Read(int count)
            {
                if (count <= 0)
                    return 0;
                ulong mask = (1UL << count) - 1;
                int bits = (int)(low & mask);
                low >>= count;
                low |= (high & mask) << (64 - count);
                high >>= count;
                return bits;
            }
        }

        static int Interpolate(int a, int b, int weight)
        {
            return (a * (64 - weight) + b * weight + 32) >> 6;
        }

        static void DecodeBC7Block(byte[] data, int offset, Color32[] block, int[] endpoints, int[] indices)
        {
            BitReader bits = new BitReader(data, offset);

            int mode = 0;
            while (mode < 8 && bits.Read(1) == 0)
                mode++;
            if (mode >= 8)
            {
                for (int i = 0; i < 16; i++)
                    block[i] = new Color32(0, 0, 0, 0);
                return;
            }

            int partition = 0;
            int subsets = 1;
            int rotation = 0;
            int indexSelection = 0;

            if (mode == 0 || mode == 1 || mode == 2 || mode == 3 || mode == 7)
            {
                subsets = (mode == 0 || mode == 2) ? 3 : 2;
                partition = bits.Read(mode == 0 ? 4 : 6);
            }
            int endpointCount = subsets * 2;

            if (mode == 4 || mode == 5)
            {
                rotation = bits.Read(2);
                if (mode == 4)
                    indexSelection = bits.Read(1);
            }

            int colorBits = ColorBits[mode];
            int alphaBits = AlphaBits[mode];
            for (int c = 0; c < 3; c++)
            {
                for (int e = 0; e < endpointCount; e++)
                    endpoints[e * 4 + c] = bits.Read(colorBits);
            }
            for (int e = 0; e < endpointCount; e++)
                endpoints[e * 4 + 3] = bits.Read(alphaBits);

            int pBit = (ModesWithPBits >> mode) & 1;
            if (pBit != 0)
            {
                for (int i = 0; i < endpointCount * 4; i++)
                    endpoints[i] <<= 1;

                if (mode == 1)
                {
                    int p0 = bits.Read(1);
                    int p1 = bits.Read(1);
                    for (int c = 0; c < 3; c++)
                    {
                        endpoints[c] |= p0;
                        endpoints[4 + c] |= p0;
                        endpoints[8 + c] |= p1;
                        endpoints[12 + c] |= p1;
                    }
                }
                else
                {
                    for (int e = 0; e < endpointCount; e++)
                    {
                        int p = bits.Read(1);
                        for (int c = 0; c < 4; c++)
                            endpoints[e * 4 + c] |= p;
                    }
                }
            }

            int colorPrecision = colorBits + pBit;
            int alphaPrecision = alphaBits + pBit;
            for (int e = 0; e < endpointCount; e++)
            {
                for (int c = 0; c < 3; c++)
                {
                    int value = endpoints[e * 4 + c] << (8 - colorPrecision);
                    endpoints[e * 4 + c] = value | (value >> colorPrecision);
                }
                if (alphaBits > 0)
                {
                    int value = endpoints[e * 4 + 3] << (8 - alphaPrecision);
                    endpoints[e * 4 + 3] = value | (value >> alphaPrecision);
                }
                else
                    endpoints[e * 4 + 3] = 255;
            }

            int indexBits = (mode == 0 || mode == 1) ? 3 : (mode == 6 ? 4 : 2);
            int indexBits2 = mode == 4 ? 3 : (mode == 5 ? 2 : 0);
            int[] weights = indexBits == 2 ? Weights2 : (indexBits == 3 ? Weights3 : Weights4);
            int[] weights2 = indexBits2 == 2 ? Weights2 : Weights3;
            byte[] partitions = subsets == 3 ? Partitions3 : Partitions2;
            int partitionOffset = partition * 16;

            for (int i = 0; i < 16; i++)
            {
                bool anchor = subsets == 1 ? i == 0 : (partitions[partitionOffset + i] & 0x80) != 0;
                indices[i] = bits.Read(anchor ? indexBits - 1 : indexBits);
            }

            for (int i = 0; i < 16; i++)
            {
                int subset = subsets == 1 ? 0 : partitions[partitionOffset + i] & 3;
                int e0 = subset * 8;
                int e1 = e0 + 4;
                int colorWeight = weights[indices[i]];
                int alphaWeight = colorWeight;

                if (indexBits2 != 0)
                {
                    int weight2 = weights2[bits.Read(i == 0 ? indexBits2 - 1 : indexBits2)];
                    if (indexSelection == 0)
                        alphaWeight = weight2;
                    else
                        colorWeight = weight2;
                }

                int r = Interpolate(endpoints[e0], endpoints[e1], colorWeight);
                int g = Interpolate(endpoints[e0 + 1], endpoints[e1 + 1], colorWeight);
                int b = Interpolate(endpoints[e0 + 2], endpoints[e1 + 2], colorWeight);
                int a = Interpolate(endpoints[e0 + 3], endpoints[e1 + 3], alphaWeight);

                int swap;
                switch (rotation)
                {
                    case 1:
                        swap = a;
                        a = r;
                        r = swap;
                        break;
                    case 2:
                        swap = a;
                        a = g;
                        g = swap;
                        break;
                    case 3:
                        swap = a;
                        a = b;
                        b = swap;
                        break;
                }

                block[i] = new Color32((byte)r, (byte)g, (byte)b, (byte)a);
            }
        }
    }
}
