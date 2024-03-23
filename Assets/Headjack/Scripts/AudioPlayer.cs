// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using UnityEngine;
using System.Collections;

namespace Headjack
{
    internal abstract class AudioPlayer : MonoBehaviour
    {
        // Possible states of audioplayer returned by GetState
        internal enum State
        {
            PLAYING,
            PAUSED,
            STOPPED,
            INVALID
        };

        // full path of current tbe file
        internal string Path
        {
            get { return GetPath();  }
        }

        // Volume value between 0-1
        internal float Volume
        {
            get { return GetVolume(); }
            set { SetVolume(value); }
        }

        protected abstract void SetVolume(float vol);
        protected abstract float GetVolume();
        protected abstract string GetPath();

        // Loads tbe file from FullPath (does not play yet)
        internal abstract void Load(string FullPath);

        internal abstract void Pause();

        internal abstract void Play();

        internal abstract void Stop();

        internal abstract void Close();

        // Call sync with video position (in ms) every frame for good audio/video sync
        internal abstract void Sync(float ms);

        internal abstract long GetSeekMs();
        internal abstract void SetSeekMs(long seek);

        internal abstract State GetState();

        // Always call destroy when done with AudioPlayer to clean up Scene
        internal abstract void Destroy();
    }

}
