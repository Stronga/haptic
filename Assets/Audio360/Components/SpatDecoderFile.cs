// Copyright (c) 2018-present, Facebook, Inc. 


using System;
using UnityEngine;
using System.Runtime.InteropServices;
using Facebook.Audio;

namespace TBE
{
    class SpatDecoderFileValidationError : Exception
    {
        public SpatDecoderFile spatDecoderFile;
        public SpatDecoderFileValidationError(string message, SpatDecoderFile o) : base(message)
        {
            spatDecoderFile = o;
        }
    }

    /// <summary>
    /// An object for playing back pre-baked spatial audio files.
    /// </summary>
    public class SpatDecoderFile : MonoBehaviour
    {
        /// File that must be played back
        public string file = string.Empty;
        /// Play when Start() is called
        public bool playOnStart;
        public EventListener events = new EventListener();

        NativeSpatDecoderFile spat_;
        AudioEngineManager.Client audioEngineClient_;
        [SerializeField]
        float volume_ = 1.0f;
        [SerializeField]
        SyncMode syncMode_ = SyncMode.INTERNAL;
        [SerializeField]
        bool focus_ = false;
        [SerializeField]
        bool focusFollowsListener_ = true;
        [SerializeField]
        float offFocusLeveldB_ = -0.0f;
        [SerializeField]
        float focusWidth_ = 90.0f;
        [SerializeField]
        bool loop_ = false;
        [SerializeField]
        bool useObjectRotation_ = true;

        GCHandle thisHandle;
        IOStream streamA;
        IOStream streamB;
        Transform customTransform_ = null;
        private readonly SPSCQ<Event> eventQueue_ = new SPSCQ<Event>(Utils.kEventQueueSize);

        static public SpatDecoderFile AddAsComponent(GameObject gameObject, AudioEngineManager.Client audioEngineClient)
        {
            var spatFile = gameObject.AddComponent<SpatDecoderFile>();
            spatFile.setAudioEngineManager(audioEngineClient);
            return spatFile;
        }

        [AOT.MonoPInvokeCallback(typeof(EventCallback))]
        static void eventCallback(Event e, global::System.IntPtr userData)
        {
            if (userData == IntPtr.Zero)
            {
                return;
            }

            GCHandle gch = GCHandle.FromIntPtr(userData);
            SpatDecoderFile sf = (SpatDecoderFile)gch.Target;
            sf.OnEvent(e);
        }

        private void OnEvent(Event e)
        {
            eventQueue_.Push((ref Event eTarget) => { eTarget = e; });
        }

        public void setAudioEngineManager(AudioEngineManager.Client audioEngineClient)
        {
            audioEngineClient_ = audioEngineClient;
        }

        void Awake()
        {
            init();
        }

        void Start()
        {
            if (validate() && file != null && file.Length > 0)
            {
                if (!isOpen())
                {
                    if (!open(file))
                    {
                        Utils.logError("Failed to open " + Utils.resolvePath(file, PathType.STREAMING_ASSETS), this);
                        return;
                    }
                }

                if (playOnStart)
                {
                    spat_.play();
                }
            }
        }

        AudioEngine getEngine()
        {
            // Use the singleton if the user of this API doesn't provide a an AudioEngine instance
            if (audioEngineClient_ == null && AudioEngineManager.Instance != null)
            {
                return AudioEngineManager.Instance.nativeEngine;
            }

            AudioEngine engine = null;
            if (audioEngineClient_ == null || !audioEngineClient_.GetNativeEngine(out engine))
            {
                return null;
            }
            
            return engine;
        }

        void Update()
        {
            // flush the event queue
            while (eventQueue_.Pop((e) =>
            {
                events.onNewEvent(e);
            })) {}
            
            if (validate())
            {
                TBVector forward = TBVector.forward();
                TBVector up = TBVector.up();
                if (useObjectRotation_)
                {
                    Transform transformToUse = (customTransform != null) ? customTransform : transform;
                    forward = Utils.toTBVector(transformToUse.forward);
                    up = Utils.toTBVector(transformToUse.up);
                }
                spat_.setRotation(forward, up);
            }
        }

        private bool validate()
        {
            var engine = getEngine();
            if (engine == null)
            {
                if (Utils.logVerbosity == LoggerVerbosity.ALL)
                {
                    throw new SpatDecoderFileValidationError("Engine is invalid", this);
                }
                return false;
            }

            return spat_ != null;
        }

        /// Internal. Initialise the native object.
        private void init()
        {
            var engine = getEngine();
            if (engine == null)
            {
                Utils.logError("Engine instance is null.", this);
                return;
            }

            if (spat_ != null)
            {
                return;
            }

            spat_ = engine.createSpatDecoderFile();
            if (spat_ == null)
            {
                Utils.logError("Native SpatDecoderFile is invalid", this);
                return;
            }


            thisHandle = GCHandle.Alloc(this);
            Debug.Assert(thisHandle.IsAllocated, "Failed to created GCHandle for AudioObject");
            spat_.setEventCallback(eventCallback, GCHandle.ToIntPtr(thisHandle));
            updateProps();
        }

        public void updateProps()
        {
            if (validate())
            {
                spat_.setVolume(volume_, 0 /* default ramp */ );
                spat_.setSyncMode(syncMode_);
                spat_.enableFocus(focus_, focusFollowsListener_);
                spat_.setOffFocusLeveldB(offFocusLeveldB_);
                spat_.setFocusWidthDegrees(focusWidth_);
                spat_.enableLooping(loop_);
            }
        }

        /// <summary>
        /// Opens an asset for playback. Currently .wav and .tbe file formats are supported. If no path is specified, 
        /// the asset will be loaded from Assets/StreamingAssets.
        /// While the asset is opened synchronously, it is loaded into the streaming buffer asynchronously. An
        /// event (Event.DECODER_INIT) will be dispatched to the event listener when the streaming buffer is ready for the
        /// asset to play.
        /// </summary>
        /// <param name="fileToplay">Name of the file in StreamAssets or the full path</param>
        /// <param name="map">Channel map/conifiguration of the file. If set to ChannelMap.UNKNOWN, the ChannelMap will be determined by parsing metadata</param>
        /// <returns>true if the file was found and successfully opened</returns>
        public bool open(string fileToplay, ChannelMap map = ChannelMap.UNKNOWN)
        {
            if (validate())
            {
                if (spat_.open(Utils.resolvePath(fileToplay, PathType.STREAMING_ASSETS), map) == EngineError.OK)
                {
                    file = fileToplay;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Open an asset for playback using an IOStream abstraction. SpatDecoderFile uses two IOStream instances
        /// as it cross-fades between two streams during every seek or external synchronization call. 
        /// </summary>
        /// <returns>true if the file was found and successfully opened</returns>
        /// <param name="streamObjectA">Stream object used to open the audio asset.</param>
        /// <param name="streamObjectB">Stream object used to open the audio asset. Must point to the same asset used by <paramref name="streamObjectA"/></param>
        /// <param name="map">Channel map/conifiguration of the file. If set to ChannelMap.UNKNOWN, the ChannelMap will be determined by parsing metadata</param>
        /// <returns>true if the file was found and successfully opened</returns>
        public bool open(IOStream streamObjectA, IOStream streamObjectB, ChannelMap map = ChannelMap.UNKNOWN)
        {
            if (validate() && streamObjectA != null && streamObjectB != null)
            {
                streamA = streamObjectA;
                streamB = streamObjectB;
                if (spat_.open(streamA, streamB, false /* dont own streams */, map) == EngineError.OK)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Opens an asset for playback with additional information, such as the size and offset specified through
        /// assetDescriptor. Currently .wav and .tbe file formats are supported. If no path is specified, 
        /// the asset will be loaded from Assets/StreamingAssets.
        /// While the asset is opened synchronously, it is loaded into the streaming buffer asynchronously. An
        /// event (Event.DECODER_INIT) will be dispatched to the event listener when the streaming buffer is ready for the
        /// asset to play.
        /// </summary>
        /// <param name="fileToplay">Name of the file in StreamAssets or the full path</param>
        /// <param name="assetDescriptor">Custom file offset and size. Any or both values can be zero if unknown.</param>
        /// <param name="map">Channel map/conifiguration of the file. If set to ChannelMap.UNKNOWN, the ChannelMap will be determined by parsing metadata</param>
        /// <returns>true if the file was found and successfully opened</returns>
        public bool open(string fileToplay, AssetDescriptor assetDescriptor, ChannelMap map = ChannelMap.UNKNOWN)
        {
            if (validate())
            {
                if (spat_.open(Utils.resolvePath(fileToplay, PathType.STREAMING_ASSETS), assetDescriptor, map) == EngineError.OK)
                {
                    file = fileToplay;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Close an opened file.
        /// </summary>
        public void close()
        {
            if (validate())
            {
                spat_.close();
                streamA = null;
                streamB = null;
            }
        }

        /// <summary>
        /// Returns true if a file is open and ready.
        /// </summary>
        /// <returns>Returns true if a file is open and ready.</returns>
        public bool isOpen()
        {
            if (validate())
            {
                return spat_.isOpen();
            }
            return false;
        }

        /// <summary>
        /// Begin playback of an opened file.
        /// Any subsequent call to this function or any play function
        /// will disregard this event if it hasn't already been triggered.
        /// </summary>
        public void play()
        {
            if (validate())
            {
                updateProps();
                spat_.play();
            }
        }

        /// <summary>
        /// Schedule playback x milliseconds from now.
        /// Any subsequent call to this function or any play function
        /// will disregard this event if it hasn't already been triggered.
        /// </summary>
        /// <param name="millisecondsFromNow">Time from now in milliseconds</param>
        public void playScheduled(float millisecondsFromNow)
        {
            if (validate())
            {
                updateProps();
                spat_.playScheduled(millisecondsFromNow);
            }
        }

        /// <summary>
        /// Begin playback with a fade.
        /// Any subsequent call to this function or any play function
        /// will disregard this event if it hasn't already been triggered.
        /// </summary>
        /// <param name="fadeDurationInMs">Duration of the fade in milliseconds</param>
        public void playWithFade(float fadeDurationInMs)
        {
            if (validate())
            {
                updateProps();
                spat_.playWithFade(fadeDurationInMs);
            }
        }

        /// <summary>
        /// Stop playback.
        /// Any subsequent call to this function or any stop function
        /// will disregard this event if it hasn't already been triggered.
        /// </summary>
        public void stop()
        {
            if (validate())
            {
                spat_.stop();
            }
        }

        /// <summary>
        /// Schedule to stop playback x milliseconds from now.
        /// Any subsequent call to this function or any stop function
        /// will disregard this event if it hasn't already been triggered.
        /// </summary>
        /// <param name="millisecondsFromNow">Time from now in milliseconds</param>
        public void stopScheduled(float millisecondsFromNow)
        {
            if (validate())
            {
                spat_.stopScheduled(millisecondsFromNow);
            }
        }

        /// <summary>
        /// Fadeout and stop playback.
        /// Any subsequent call to this function or any stop function
        /// will disregard this event if it hasn't already been triggered.
        /// </summary>
        /// <param name="fadeDurationInMs">Duration of the fade in milliseconds.</param>
        public void stopWithFade(float fadeDurationInMs)
        {
            if (validate())
            {
                spat_.stopWithFade(fadeDurationInMs);
            }
        }

        /// <summary>
        ///  Pause playback.
        /// Any subsequent call to this function or any pause function
        /// will disregard this event if it hasn't already been triggered.
        /// </summary>
        public void pause()
        {
            if (validate())
            {
                spat_.pause();
            }
        }

        /// <summary>
        /// Schedule playback to be paused x milliseconds from now.
        /// Any subsequent call to this function or any pause function
        /// will disregard this event if it hasn't already been triggered.
        /// </summary>
        /// <param name="millisecondsFromNow">Time from now in milliseconds</param>
        public void pauseScheduled(float millisecondsFromNow)
        {
            if (validate())
            {
                spat_.pauseScheduled(millisecondsFromNow);
            }
        }

        /// <summary>
        /// Fadeout and pause playback.
        /// Any subsequent call to this function or any pause function
        /// will disregard this event if it hasn't already been triggered.
        /// </summary>
        /// <param name="fadeDurationInMs">Duration of the fade in milliseconds.</param>
        public void pauseWithFade(float fadeDurationInMs)
        {
            if (validate())
            {
                spat_.pauseWithFade(fadeDurationInMs);
            }
        }

        /// <summary>
        /// Returns the current play state (Eg. playing, paused, stopped).
        /// </summary>
        /// <returns>Returns the current play state.</returns>
        public PlayState getPlayState()
        {
            if (validate())
            {
                return spat_.getPlayState();
            }
            return PlayState.INVALID;
        }

        /// <summary>
        /// Seek playback to an absolute point in milliseconds.
        /// </summary>
        /// <param name="ms">Time in milliseconds.</param>
        public void seekToMs(float ms)
        {
            if (validate())
            {
                spat_.seekToMs(ms);
            }
        }

        /// <summary>
        /// /// Seek playback to an absolute point in samples.
        /// </summary>
        /// <param name="sample">Time in samples</param>
        public void seekToSample(uint sample)
        {
            if (validate())
            {
                spat_.seekToSample(sample);
            }
        }

        /// <summary>
        /// Returns elapsed playback time in milliseconds.
        /// </summary>
        /// <returns>Returns elapsed playback time in milliseconds.</returns>
        public double getElapsedTimeInMs()
        {
            if (validate())
            {
                return spat_.getElapsedTimeInMs();
            }
            return 0.0;
        }

        /// <summary>
        /// Returns elapsed playback time in samples.
        /// </summary>
        /// <returns>Returns elapsed playback time in samples.</returns>
        public uint getElapsedTimeInSamples()
        {
            if (validate())
            {
                return spat_.getElapsedTimeInSamples();
            }
            return 0;
        }

        /// <summary>
        /// Returns the total duration of the asset in milliseconds.
        /// </summary>
        /// <returns>Returns the total duration of the asset in milliseconds.</returns>
        public double getAssetDurationInMs()
        {
            if (validate())
            {
                return spat_.getAssetDurationInMs();
            }
            return 0.0;
        }

        /// <summary>
        /// Returns the total duration of the asset in samples.
        /// </summary>
        /// <returns>Returns the total duration of the asset in samples.</returns>
        public uint getAssetDurationInSamples()
        {
            if (validate())
            {
                return spat_.getAssetDurationInSamples();
            }
            return 0;
        }

        void OnDestroy()
        {
            var engine = getEngine();
            if (spat_ != null && engine != null)
            {
                spat_.setEventCallback(null, IntPtr.Zero);
                engine.destroySpatDecoderFile(spat_);
            }
            if (thisHandle.IsAllocated)
            {
                thisHandle.Free();
            }
            audioEngineClient_ = null;
        }

        /// <summary>
        /// Set the external time source in milliseconds. The audio asset will chase this time
        /// source. Only applicable when the SyncMode is set to SyncMode.EXTERNAL
        /// </summary>
        /// <param name="externalClockInMs">External time in milliseconds, to be updated every frame.</param>
        public void setExternalClockInMs(double externalClockInMs)
        {
            if (validate())
            {
                spat_.setExternalClockInMs(externalClockInMs);
            }
        }

        /// <summary>
        /// Set how often the engine tries to synchronise to the external clock. Very low values can result in stutter,
        /// very high values can result in synchronisation latency.
        /// This method along with setResyncThresholdMs() can be used to fine-tune synchronisation.
        /// </summary>
        /// <param name="freewheelInMs"></param>
        public void setFreewheelTimeInMs(double freewheelInMs)
        {
            if (validate())
            {
                spat_.setFreewheelTimeInMs(freewheelInMs);
            }
        }

        /// <summary>
        /// Returns the free wheel time in milliseconds
        /// </summary>
        /// <returns>Returns the free wheel time in milliseconds</returns>
        public double getFreewheelTimeInMs()
        {
            if (validate())
            {
                return spat_.getFreewheelTimeInMs();
            }
            return 0.0;
        }

        /// <summary>
        /// The time threshold after which the engine will synchronise to the external clock.
        /// This method along with setFreewheelTimeInMs() can be used to fine-tune synchronisation.
        /// </summary>
        /// <param name="resyncThresholdMs">Synchronisation threshold in milliseconds</param>
        public void setResyncThresholdMs(double resyncThresholdMs)
        {
            if (validate())
            {
                spat_.setResyncThresholdMs(resyncThresholdMs);
            }
        }

        /// <summary>
        /// Returns the current synchronisation threshold.
        /// </summary>
        /// <returns>Returns the current synchronisation threshold.</returns>
        public double getResyncThresholdMs()
        {
            if (validate())
            {
                return spat_.getResyncThresholdMs();
            }
            return 0.0;
        }

        /// <summary>
        /// Set the volume in linear gain
        /// </summary>
        public float volume
        {
            get
            {
                if (validate())
                {
                    volume_ = spat_.getVolume();
                }
                return volume_;
            }
            set
            {
                volume_ = value;
                if (validate())
                {
                    spat_.setVolume(volume_, 0 /* default ramp */ );
                }
            }
        }

        /// <summary>
        /// Set the volume in decibels
        /// </summary>
        public float volumeDecibels
        {
            get
            {
                return Utils.linearToDecibels(volume);
            }
            set
            {
                volume = Utils.decibelsToLinear(value);
            }
        }

        /// <summary>
        /// Set synchronisation mode. Default is SyncMode.INTERNAL. When set to
        /// SyncMode.EXTERNAL, setExternalClockInMs() can be used to get the audio
        /// asset to chase an external time source.
        /// </summary>
        public SyncMode syncMode
        {
            get
            {
                if (validate())
                {
                    syncMode_ = spat_.getSyncMode();
                }
                return syncMode_;
            }
            set
            {
                syncMode_ = value;
                if (validate())
                {
                    spat_.setSyncMode(syncMode_);
                }
            }
        }

        /// <summary>
        /// Enable mix focus. This gets a specified area of the mix to be more audible than surrounding areas, by reducing
        /// the volume of the area that isn't in focus.
        /// </summary>
        public bool focus
        {
            get
            {
                return focus_;
            }
            set
            {
                focus_ = value;
                if (validate())
                {
                    spat_.enableFocus(focus_, focusFollowsListener_);
                }
            }
        }

        /// <summary>
        /// The attenuation level in dB outside of the focus area. Between 0dB and -24dB.
        /// </summary>
        public float offFocusLeveldB
        {
            get
            {
                return offFocusLeveldB_;
            }
            set
            {
                offFocusLeveldB_ = Mathf.Clamp(value, Utils.kFocusMin, Utils.kFocusMax);
                if (validate())
                {
                    spat_.setOffFocusLeveldB(offFocusLeveldB_);
                }
            }
        }

        /// <summary>
        /// Set the width angle in degrees of the focus area, between 40 and 120 degrees.
        /// </summary>
        public float focusWidth
        {
            get
            {
                return focusWidth_;
            }
            set
            {
                focusWidth_ = Mathf.Clamp(value, Utils.kFocusWidthMin, Utils.kFocusWidthMax);
                if (validate())
                {
                    spat_.setFocusWidthDegrees(focusWidth_);
                }
            }
        }

        /// <summary>
        /// Toggle looping. Use this for sample accurate looping rather than manually 
        /// seeking the file to 0 when it finishes playing.
        /// </summary>
        public bool loop
        {
            get
            {
                return loop_;
            }
            set
            {
                loop_ = value;
                if (validate())
                {
                    spat_.enableLooping(loop_);
                }
            }
        }

        /// <summary>
        /// Use the GameObject's rotation to rotate the ambisonic field
        /// </summary>
        public bool useObjectRotation
        {
            get
            {
                return useObjectRotation_;
            }
            set
            {
                useObjectRotation_ = value;
            }
        }

        /// <summary>
        /// Setting this to a non-null value will cause the component to use this 
        /// Transform, rather that the GameObject it is attached to.
        /// </summary>
        /// <value>The custom transform.</value>
        public Transform customTransform
        {
            get
            {
                return customTransform_;
            }
            set
            {
                customTransform_ = value;
            }
        }

        /// <summary>
        /// Returns the native object interface to access advanced features
        /// </summary>
        /// <value>The native object.</value>
        public NativeSpatDecoderFile nativeObject
        {
            get
            {
                return spat_;
            }
        }

        /// <summary>
        /// Helper method to create an instance of SpatDecoderFile on a GameObject
        /// </summary>
        /// <param name="ga">GameObject instance</param>
        /// <param name="file">File to be played back</param>
        /// <param name="map">Channel map/spatial format of the file</param>
        /// <returns></returns>
        static public TBE.SpatDecoderFile createOnObject(GameObject ga, string file, ChannelMap map = TBE.ChannelMap.TBE_8_2)
        {
            var obj = ga.AddComponent<SpatDecoderFile>();
            if (!obj.open(file, map))
            {
                Utils.logError("Failed to open " + file, ga);
                Destroy(obj);
                return null;
            }
            return obj;
        }
    }
}
