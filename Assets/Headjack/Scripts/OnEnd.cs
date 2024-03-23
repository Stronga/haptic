// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

namespace Headjack
{
    public delegate void PushEvent(SimpleJSON.JSONArray message);
    public delegate void OnEnd(bool succes, string error);
    public delegate void OnImageProgress(ImageLoadingState imageLoadingState, float processed, float total);
    public enum ImageLoadingState
    {
        Downloading,
        Decoding
    }
}
