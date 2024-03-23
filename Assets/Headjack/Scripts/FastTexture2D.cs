// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using UnityEngine;
using System.Collections;
using System.IO;
using System;

internal class FastTexture2D : MonoBehaviour {
    internal delegate void OnEncoded();
	internal delegate void OnDecoded(Texture2D decodedImage);
    internal static void encode(Texture2D texture, string FullFilename, int BlockSize=128,bool HighQuality=true, OnEncoded onEnd=null)
    {
        GameObject instance = new GameObject();
        instance.name = "Encoding...";
        FastTexture2D fastTexture= instance.AddComponent<FastTexture2D>();
        fastTexture.StartCoroutine(fastTexture.Encode(texture, FullFilename, BlockSize, onEnd, HighQuality));
    }

    internal static void decode(string filename,  OnDecoded onEnd, bool GenerateMipMaps=true, int DecodeSpeed=2, string AlsoWriteToFile=null)
    {
        if (!File.Exists(filename))
        {
            onEnd(null);
            return;
        }
        GameObject instance = new GameObject();
        instance.name = "Decoding...";
        FastTexture2D fastTexture= instance.AddComponent<FastTexture2D>();
        fastTexture.StartCoroutine (fastTexture.Decode (filename,onEnd,GenerateMipMaps,DecodeSpeed,AlsoWriteToFile));
    }

    internal IEnumerator Decode(string EncodeFilename, OnDecoded onEnd, bool GenerateMipMaps, int DecodeSpeed = 2, string AlsoWriteToFile = null)
    {
        byte[][] ByteData=new byte[1][];
        Texture2D Decoded;

        if (!File.Exists(EncodeFilename))
        {
            if (onEnd != null)
            {
                onEnd(null);
            }
            Resources.UnloadUnusedAssets();
            yield return null;
            Destroy(gameObject);
            yield break;
        }
        byte[] AllBytes = File.ReadAllBytes(EncodeFilename);
        int GridSize = BitConverter.ToInt32(AllBytes, 0);
        yield return null;
        int NumberOfImages = GridSize * GridSize;
        Color32[] ThisPartColors;

        ////Script can crash here
        bool error = false;
        try
        {
            ByteData = new byte[NumberOfImages][];
        }
        catch (Exception e)
        {
            Debug.LogWarning(e);
            if (onEnd != null)
            {
                onEnd(null);
            }
            error = true;
        }
        if (error)
        {
            Resources.UnloadUnusedAssets();
            yield return null;
            Destroy(gameObject);
            yield break;
        }
        ////Script can crash here

        Texture2D[] Parts = new Texture2D[NumberOfImages];
        int StartAt = 12 + (GridSize * GridSize * 4);
        //int FinalImageCurrent = 0;
        int Width = BitConverter.ToInt32(AllBytes, 4);
        int Height = BitConverter.ToInt32(AllBytes, 8);
        Color32[] FinalImageColors = new Color32[Width * Height];
        int CurrentX = 0;
        int CurrentY = 0;
        for (int i = 0; i < NumberOfImages; ++i)
        {
            if (i % DecodeSpeed == 0)
            {
                yield return null;
            }
            Parts[0] = new Texture2D(2, 2, TextureFormat.ARGB32, false);
            ByteData[i] = new byte[BitConverter.ToInt32(AllBytes, 12 + (i * 4))];
            for (int d = 0; d < ByteData[i].Length; ++d)
            {
                ByteData[i][d] = AllBytes[StartAt + d];
            }
            StartAt += ByteData[i].Length;
            Parts[0].LoadImage(ByteData[i]);
            ThisPartColors = Parts[0].GetPixels32();
            CopyBlockToTexture(ref FinalImageColors, ThisPartColors, Width, CurrentX, CurrentY, Width / GridSize, Height / GridSize);
            CurrentX += Width / GridSize;
            if (CurrentX >= Width)
            {
                CurrentX = 0;
                CurrentY += Height / GridSize;
            }

        }
        yield return null;
        Decoded = new Texture2D(Width, Height, TextureFormat.ARGB32, GenerateMipMaps);
        yield return null;
        Decoded.SetPixels32(FinalImageColors);
        yield return null;
        Decoded.anisoLevel = 0;
        Decoded.wrapMode = TextureWrapMode.Clamp;
        Decoded.filterMode = FilterMode.Bilinear;

        yield return null;
        Decoded.Apply(true, true);

        yield return null;
        Parts = null;
        ByteData = null;

        //        if (AlsoWriteToFile!=null)
        //        {
        //            File.WriteAllBytes(AlsoWriteToFile, Decoded.EncodeToPNG());
        //        }

        if (onEnd != null)
        {
            onEnd(Decoded);
        }
        Resources.UnloadUnusedAssets();
        yield return null;
        Destroy(gameObject);
    }

    private void CopyBlockToTexture(ref Color32[] TargetTexture, Color32[] Block,int ImageWidth,int StartX, int StartY, int Width, int Height)
    {
        int CurrentX = StartX;
        int CurrentY = StartY;
        for (int i = 0; i < Block.Length; ++i)
        {
            TargetTexture[CurrentY * ImageWidth + CurrentX] = Block[i];
            CurrentX += 1;
            if (CurrentX >= StartX + Width)
            {
                CurrentX = StartX;
                CurrentY += 1;
            }
        }
    }

    internal IEnumerator Encode(Texture2D InputTexture, string FullFilename, int BlockSize, OnEncoded onEnd, bool HighQuality)
    {
        int TotalPixels = InputTexture.width * InputTexture.height;
        int BlockSizePixels = BlockSize * BlockSize;
        int GridSize =Mathf.RoundToInt( Mathf.Sqrt( Mathf.CeilToInt((TotalPixels * 1f) / (BlockSizePixels * 1f))));
        int NewX = InputTexture.width+((GridSize-(InputTexture.width % GridSize))%GridSize);
        int NewY = InputTexture.height+((GridSize-(InputTexture.height % GridSize))%GridSize);
        Texture2D ResizedTexture = new Texture2D(NewX, NewY, TextureFormat.ARGB32, false);
        for (int yy = 0; yy < NewY; ++yy)
        {
            for (int xx = 0; xx < NewX; ++xx)
            {
                ResizedTexture.SetPixel(xx, yy, InputTexture.GetPixelBilinear((xx * 1f) / (NewX * 1f), (yy * 1f) / (NewY * 1f)));
            }
            if (yy % 64 == 0)
            {
                yield return null;
            }
        }
        ResizedTexture.Apply();
        byte[][] ByteData;
        Texture2D[] Parts;
        //string[] ByteInfoSplitted;
        yield return null;
        Parts = new Texture2D[GridSize * GridSize];
        Color32[] SourceColors = ResizedTexture.GetPixels32();
        long TotalBytes = 0;

        byte[] ImageInfo = new byte[(GridSize * GridSize * 4) + (3 * 4)];
        int Index = 0;
        WriteIntInByteArray(ref ImageInfo, GridSize,ref Index);
        WriteIntInByteArray(ref ImageInfo, ResizedTexture.width, ref Index);
        WriteIntInByteArray(ref ImageInfo, ResizedTexture.height, ref Index);
        int CurrentX = 0;
        int CurrentY = 0;
        ByteData = new byte[Parts.Length][];
        for (int i = 0; i < Parts.Length; ++i)
        {
            Parts[i] = new Texture2D(ResizedTexture.width / GridSize, ResizedTexture.height / GridSize, TextureFormat.RGBA32, false);
            Color32[] PartsColors = new Color32[SourceColors.Length / Parts.Length];
            CopyTextureToBlock(ref SourceColors,ref PartsColors, ResizedTexture.width, CurrentX, CurrentY, ResizedTexture.width / GridSize, ResizedTexture.height / GridSize);
            CurrentX+=ResizedTexture.width/GridSize;
            if (CurrentX>= ResizedTexture.width)
            {
                CurrentX = 0;
                CurrentY += ResizedTexture.height / GridSize;
            }


            Parts[i].SetPixels32(PartsColors);
            Parts[i].Apply();
            if (HighQuality)
            {
                ByteData[i] = Parts[i].EncodeToPNG();
            }
            else
            {
                ByteData[i] = Parts[i].EncodeToJPG();
            }
            TotalBytes += ByteData[i].LongLength;

            WriteIntInByteArray(ref ImageInfo, ByteData[i].Length, ref Index);

           
            //yield return new WaitForEndOfFrame();
        }

        byte[] FileToSave = new byte[TotalBytes+ImageInfo.Length];
        for (int i = 0; i < ImageInfo.Length; ++i)
        {
            FileToSave[i] = ImageInfo[i];
        }
        yield return null;
        int Pos = ImageInfo.Length;
        for (int i = 0; i < Parts.Length; ++i)
        {
            for (int d = 0; d < ByteData[i].Length; ++d)
            {
                FileToSave[Pos] = ByteData[i][d];
                Pos += 1;
            }
           
        }
        File.WriteAllBytes(FullFilename, FileToSave);
        Parts = new Texture2D[1];
        if (onEnd != null)
        {
            onEnd();
        }
        Destroy(gameObject);
    }

    private void CopyTextureToBlock(ref Color32[] TargetTexture,ref  Color32[] Block,int ImageWidth,int StartX, int StartY, int Width, int Height)
    {
        int CurrentX = StartX;
        int CurrentY = StartY;
        for (int i = 0; i < Block.Length; ++i)
        {
            Block[i] = TargetTexture[CurrentY * ImageWidth + CurrentX];
            CurrentX += 1;
            if (CurrentX >= StartX + Width)
            {
                CurrentX = StartX;
                CurrentY += 1;
            }
        }
    }

    private void WriteIntInByteArray(ref byte[] Target, Int32 Number,ref int StartPosition)
    {
        byte[] IntInBytes = BitConverter.GetBytes(Number);
        for (int i = 0; i < 4; ++i)
        {
            Target[StartPosition + i] = IntInBytes[i];
        }
        StartPosition += 4;
    }
}
