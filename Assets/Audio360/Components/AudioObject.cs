// Copyright (c) 2018-present, Facebook, Inc.


using System;
using UnityEngine;
using System.Runtime.InteropServices;
using Facebook.Audio;

namespace TBE
{
    /// <summary>
    /// Spatialise a sound in space
    /// </summary>
    [ExecuteInEditMode]
    public class AudioObject : MonoBehaviour
    {
        public string file = string.Empty;
        public bool playOnStart = false;
        public AudioObjectEventListener events = new AudioObjectEventListener();

        private const float KDefaultVolume = 1.0f;
        private const bool KDefaultLoop = false;
        private const float KDefaultPitch = 1.0f;
        private const float KDefaultMinDistance = 1.0f;
        private const float KDefaultMaxDistance = 1000.0f;
        private const float KDefaultAttenFactor = 1.0f;
        private const AttenuationMode KDefaultAttenMode = AttenuationMode.LOGARITHMIC;
        private const bool KDefaultDirectionality = false;
        private const float KDefaulyDirectionalityLevel = 1.0f;
        private const float KDefaulyDirectionalityCone = 150.0f;
        private const bool KDefaultSpatialize = true;
        private const bool KDefaultMaxDistanceMute = false;

        [SerializeField] float volume_ = KDefaultVolume;
        [SerializeField] bool loop_ = KDefaultLoop;
        [SerializeField] float pitch_ = KDefaultPitch;
        [SerializeField] float minDistance_ = KDefaultMinDistance;
        [SerializeField] float maxDistance_ = KDefaultMaxDistance;
        [SerializeField] float attenFactor_ = KDefaultAttenFactor;
        [SerializeField] AttenuationMode attenMode_ = KDefaultAttenMode;
        [SerializeField] bool directionality_ = KDefaultDirectionality;
        [SerializeField] float directionalityLevel_ = KDefaulyDirectionalityLevel;
        [SerializeField] float directionalityConeArea_ = KDefaulyDirectionalityCone;
        [SerializeField] bool spatialise_ = KDefaultSpatialize;

        [SerializeField] private bool maxDistanceMute_ = KDefaultMaxDistanceMute;

        NativeAudioObject nativeObj_;
        AudioEngineManager.Client audioEngineClient_;
        AttenuationProps attenProps_ = new AttenuationProps();
        DirectionalProps directionalProps_ = new DirectionalProps();
        GCHandle thisHandle;
        IOStream stream;
        Transform customTransform_ = null;
        private readonly SPSCQ<Event> eventQueue_ = new SPSCQ<Event>(Utils.kEventQueueSize);
        private SPSCQ<Event>.Reader dispatchEvent_;
        private bool attenuationPropsChanged_ = true;
        private bool directionalityPropsChanged_ = true;
        private bool valid_;

        public static AudioObject AddAsComponent(GameObject gameObject, AudioEngineManager.Client audioEngineClient)
        {
            Debug.Assert(audioEngineClient != null);
            var audioObject = gameObject.AddComponent<AudioObject>();
            audioObject.setAudioEngineManager(audioEngineClient);
            return audioObject;
        }

        private Vector3 previousPosition_;
        private Quaternion previousRotation_;
        private TBVector forwardVector_ = null;
        private TBVector upVector_ = null;
        private TBVector positionVector_ = null;

        [AOT.MonoPInvokeCallback(typeof(EventCallback))]
        static void eventCallback(Event e, global::System.IntPtr userData)
        {
            if (userData == IntPtr.Zero)
            {
                return;
            }

            GCHandle gch = GCHandle.FromIntPtr(userData);
            AudioObject ob = (AudioObject) gch.Target;
            ob.OnEvent(e);
        }

        private void DispatchEvent(Event e)
        {
            events.onNewEvent(e, this);
        }

        private void OnEvent(Event e)
        {
            eventQueue_.Push((ref Event eTarget) => { eTarget = e; });
        }

        public void FlushEvents()
        {
            while (eventQueue_.Pop(dispatchEvent_))
            {
            }
        }

        public void setAudioEngineManager(AudioEngineManager.Client audioEngineClient)
        {
            Debug.Assert(audioEngineClient_ == null);
            Debug.Assert(audioEngineClient != null);
            audioEngineClient_ = audioEngineClient;
            if (audioEngineClient_ != null)
            {
                audioEngineClient_.OnInvalid += AudioEngineClientOnInvalid;
            }
        }

        private void AudioEngineClientOnInvalid(object sender, EventArgs e)
        {
            // If the engine is destroyed, then the native audio object would be invalid too
            valid_ = false;
        }

        void Awake()
        {
            init();
            ForceUpdateProps();
        }

        void Start()
        {
            if (nativeObj_ != null && file != null && file.Length > 0)
            {
                if (!isOpen())
                {
                    if (!open(file))
                    {
                        Utils.logError("Failed to open " + file, this);
                        return;
                    }
                }

                if (playOnStart)
                {
                    play();
                }
            }
        }

        AudioEngine getEngine()
        {
            // Use the singleton if the user of this API doesn't provide a an AudioEngine instance
            if (audioEngineClient_ == null && AudioEngineManager.Instance != null)
            {
                setAudioEngineManager(AudioEngineManager.Instance.NewClient());
            }

            AudioEngine engine = null;
            if (audioEngineClient_ == null || !audioEngineClient_.GetNativeEngine(out engine))
            {
                return null;
            }

            return engine;
        }

        private void init()
        {
            var engine = getEngine();
            if (engine == null)
            {
                // Error log only in play mode
                if (Application.isPlaying) 
                {
                    Utils.logError("Engine instance is null.", this);
                }
                return;
            }

            if (nativeObj_ != null)
            {
                return;
            }

            nativeObj_ = engine.createAudioObject();
            if (nativeObj_ == null)
            {
                Utils.logError("Native audio object is invalid", this);
                return;
            }

            thisHandle = GCHandle.Alloc(this);
            Debug.Assert(thisHandle.IsAllocated, "Failed to created GCHandle for AudioObject");
            nativeObj_.setEventCallback(eventCallback, GCHandle.ToIntPtr(thisHandle));
            dispatchEvent_ = DispatchEvent;

            //Set object to correct location on init
            Transform transformToUse = (customTransform != null) ? customTransform : transform;
            //Alloc three TBVectors
            forwardVector_ = Utils.toTBVector(transformToUse.forward);
            upVector_ = Utils.toTBVector(transformToUse.up);
            positionVector_ = Utils.toTBVector(transformToUse.position);
            nativeObj_.setRotation(forwardVector_, upVector_);
            nativeObj_.setPosition(positionVector_);
            previousPosition_ = transformToUse.position;
            previousRotation_ = transformToUse.rotation;
            valid_ = true;
        }

        void Update()
        {
            if (!valid_)
            {
                return;
            }

            // flush the event queue
            FlushEvents();

            UpdateTransform(false);
            UpdateAttenuationProps(false);
            UpdateDirectionalityProps(false);
        }

        public void ForceUpdateProps()
        {
            if (!valid_)
            {
                return;
            }

            nativeObj_.setVolume(volume_, 0 /* default ramp */);
            nativeObj_.enableLooping(loop_);
            nativeObj_.setPitch(pitch_);
            nativeObj_.setAttenuationMode(attenMode_);
            nativeObj_.setDirectionalityEnabled(directionality_);
            nativeObj_.shouldSpatialise(spatialise_);

            UpdateTransform(true);
            UpdateAttenuationProps(true);
            UpdateDirectionalityProps(true);
        }

        private void UpdateAttenuationProps(bool force)
        {
            if (force || attenuationPropsChanged_)
            {
                attenProps_.minimumDistance = minDistance;
                attenProps_.maximumDistance = maxDistance;
                attenProps_.factor = attenFactor;
                attenProps_.maxDistanceMute = maxDistanceMute_;
                nativeObj_.setAttenuationProperties(attenProps_);
                attenuationPropsChanged_ = false;
            }
        }

        private void UpdateDirectionalityProps(bool force)
        {
            if (force || directionalityPropsChanged_)
            {
                directionalProps_.set(directionalityLevel_, directionalityConeArea_);
                nativeObj_.setDirectionalProperties(directionalProps_);
                directionalityPropsChanged_ = false;
            }
        }

        private void UpdateTransform(bool force)
        {
            var transformToUse = (customTransform != null) ? customTransform : transform;
            if (force || transformToUse.rotation != previousRotation_)
            {
                Vector3 up = transformToUse.up;
                upVector_.set(up.x, up.y, up.z);
                Vector3 forward = transformToUse.forward;
                forwardVector_.set(forward.x, forward.y, forward.z);
                nativeObj_.setRotation(forwardVector_, upVector_);
                previousRotation_ = transformToUse.rotation;
            }

            if (force || transformToUse.position != previousPosition_)
            {
                Vector3 position = transformToUse.position;
                positionVector_.set(position.x, position.y, position.z);
                nativeObj_.setPosition(positionVector_);
                previousPosition_ = transformToUse.position;
            }
        }

        public bool IsPlaying()
        {
            return valid_ && nativeObj_.getPlayState() == PlayState.PLAYING;
        }

        /// <summary>
        /// Opens an asset for playback. Currently .wav and .opus formats are supported. If no path is specified,
        /// the asset will be loaded from Assets/StreamingAssets.
        /// While the asset is opened synchronously, it is loaded into the streaming buffer asynchronously. An
        /// event (Event.DECODER_INIT) will be dispatched to the event listener when the streaming buffer is ready for the
        /// asset to play.
        /// </summary>
        /// <param name="fileToplay">Name of the file in StreamAssets or the full path</param>
        /// <returns>true if the file was found and successfully opened</returns>
        public bool open(string fileToplay)
        {
            if (!valid_)
            {
                return false;
            }

            if (nativeObj_.open(Utils.resolvePath(fileToplay, PathType.STREAMING_ASSETS)) == EngineError.OK)
            {
                file = fileToplay;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Open an asset for playback using an IOStream abstraction.
        /// </summary>
        /// <returns>true if the file was found and successfully opened</returns>
        /// <param name="streamObject">Stream object used to open the audio asset.</param>
        public bool open(IOStream streamObject)
        {
            if (!valid_)
            {
                return false;
            }

            if (streamObject == null)
            {
                return false;
            }


            stream = streamObject;
            return nativeObj_.open(stream, false /* don't own the stream */) == EngineError.OK;
        }

        /// <summary>
        /// Open a stream with a specific gain value at a specific position
        /// </summary>
        /// <param name="streamObject"></param>
        /// <param name="newPosition"></param>
        /// <param name="newVolume"></param>
        /// <returns>true if the file was found and successfully opened</returns>
        public bool open(IOStream streamObject, Vector3 newPosition, float newLinearGain)
        {
            if (!valid_)
            {
                return false;
            }

            if (streamObject == null)
            {
                return false;
            }

            ForceUpdateProps();

            positionVector_.set(newPosition.x, newPosition.y, newPosition.z);
            volume_ = newLinearGain;
            stream = streamObject;
            return nativeObj_.open(stream, false /* don't own the stream */, positionVector_, volume_) ==
                   EngineError.OK;
        }

        /// <summary>
        /// Close an opened file.
        /// </summary>
        public void close()
        {
            if (valid_)
            {
                nativeObj_.close();
            }

            stream = null;
        }

        /// <summary>
        /// Returns true if a file is open and ready.
        /// </summary>
        /// <returns>Returns true if a file is open and ready.</returns>
        public bool isOpen()
        {
            return valid_ && nativeObj_.isOpen();
        }

        /// <summary>
        /// Begin playback of an opened file.
        /// Any subsequent call to this function or any play function
        /// will disregard this event if it hasn't already been triggered.
        /// </summary>
        public void play()
        {
            if (!valid_)
            {
                return;
            }

            ForceUpdateProps();
            nativeObj_.play();
        }

        /// <summary>
        /// Schedule playback x milliseconds from now.
        /// Any subsequent call to this function or any play function
        /// will disregard this event if it hasn't already been triggered.
        /// </summary>
        /// <param name="millisecondsFromNow">Time from now in milliseconds</param>
        public void playScheduled(float millisecondsFromNow)
        {
            if (!valid_)
            {
                return;
            }

            ForceUpdateProps();
            nativeObj_.playScheduled(millisecondsFromNow);
        }

        /// <summary>
        /// Schedule playback x milliseconds from now with a fade in.
        /// Any subsequent call to this function or any play function
        /// will disregard this event if it hasn't already been triggered.
        /// </summary>
        /// <param name="millisecondsFromNow">Time from now in milliseconds</param>
        /// <param name="fadeTimeInMs">Fade in time in ms</param>
        public void playScheduled(float millisecondsFromNow, float fadeTimeInMs)
        {
            if (!valid_)
            {
                return;
            }

            ForceUpdateProps();
            nativeObj_.playScheduled(millisecondsFromNow, fadeTimeInMs);
        }

        /// <summary>
        /// Begin playback with a fade.
        /// Any subsequent call to this function or any play function
        /// will disregard this event if it hasn't already been triggered.
        /// </summary>
        /// <param name="fadeDurationInMs">Duration of the fade in milliseconds</param>
        public void playWithFade(float fadeDurationInMs)
        {
            if (!valid_)
            {
                return;
            }

            ForceUpdateProps();
            nativeObj_.playWithFade(fadeDurationInMs);
        }

        /// <summary>
        /// Stop playback.
        /// Any subsequent call to this function or any stop function
        /// will disregard this event if it hasn't already been triggered.
        /// </summary>
        public void stop()
        {
            if (!valid_)
            {
                return;
            }

            nativeObj_.stop();
        }

        /// <summary>
        /// Schedule to stop playback x milliseconds from now.
        /// Any subsequent call to this function or any stop function
        /// will disregard this event if it hasn't already been triggered.
        /// </summary>
        /// <param name="millisecondsFromNow">Time from now in milliseconds</param>
        public void stopScheduled(float millisecondsFromNow)
        {
            if (!valid_)
            {
                return;
            }

            nativeObj_.stopScheduled(millisecondsFromNow);
        }

        /// <summary>
        /// Schedule to stop playback x milliseconds from now.
        /// Any subsequent call to this function or any stop function
        /// will disregard this event if it hasn't already been triggered.
        /// </summary>
        /// <param name="millisecondsFromNow">Time from now in milliseconds</param>
        /// <param name="fadeTimeInMs">Fade out time in ms</param>
        public void stopScheduled(float millisecondsFromNow, float fadeTimeInMs)
        {
            if (!valid_)
            {
                return;
            }

            nativeObj_.stopScheduled(millisecondsFromNow, fadeTimeInMs);
        }

        /// <summary>
        /// Fadeout and stop playback.
        /// Any subsequent call to this function or any stop function
        /// will disregard this event if it hasn't already been triggered.
        /// </summary>
        /// <param name="fadeDurationInMs">Duration of the fade in milliseconds.</param>
        public void stopWithFade(float fadeDurationInMs)
        {
            if (!valid_)
            {
                return;
            }

            nativeObj_.stopWithFade(fadeDurationInMs);
        }

        /// <summary>
        ///  Pause playback.
        /// Any subsequent call to this function or any pause function
        /// will disregard this event if it hasn't already been triggered.
        /// </summary>
        public void pause()
        {
            if (!valid_)
            {
                return;
            }

            nativeObj_.pause();
        }

        /// <summary>
        /// Schedule playback to be paused x milliseconds from now.
        /// Any subsequent call to this function or any pause function
        /// will disregard this event if it hasn't already been triggered.
        /// </summary>
        /// <param name="millisecondsFromNow">Time from now in milliseconds</param>
        public void pauseScheduled(float millisecondsFromNow)
        {
            if (!valid_)
            {
                return;
            }

            nativeObj_.pauseScheduled(millisecondsFromNow);
        }

        /// <summary>
        /// Schedule playback to be paused x milliseconds from now.
        /// Any subsequent call to this function or any pause function
        /// will disregard this event if it hasn't already been triggered.
        /// </summary>
        /// <param name="millisecondsFromNow">Time from now in milliseconds</param>
        /// <param name="fadeTimeInMs">Fade out time in ms</param>
        public void pauseScheduled(float millisecondsFromNow, float fadeTimeInMs)
        {
            if (!valid_)
            {
                return;
            }

            nativeObj_.pauseScheduled(millisecondsFromNow, fadeTimeInMs);
        }

        /// <summary>
        /// Fadeout and pause playback.
        /// Any subsequent call to this function or any pause function
        /// will disregard this event if it hasn't already been triggered.
        /// </summary>
        /// <param name="fadeDurationInMs">Duration of the fade in milliseconds.</param>
        public void pauseWithFade(float fadeDurationInMs)
        {
            if (!valid_)
            {
                return;
            }

            nativeObj_.pauseWithFade(fadeDurationInMs);
        }

        /// <summary>
        /// Seek playback to an absolute point in milliseconds.
        /// </summary>
        /// <param name="ms">Time in milliseconds.</param>
        public void seekToMs(float ms)
        {
            if (!valid_)
            {
                return;
            }

            nativeObj_.seekToMs(ms);
        }

        /// <summary>
        /// Returns elapsed playback time in milliseconds.
        /// </summary>
        /// <returns>Returns elapsed playback time in milliseconds.</returns>
        public double getElapsedTimeInMs()
        {
            if (!valid_)
            {
                return 0.0;
            }

            return nativeObj_.getElapsedTimeInMs();
        }

        /// <summary>
        /// Returns the total duration of the asset in milliseconds.
        /// </summary>
        /// <returns>Returns the total duration of the asset in milliseconds.</returns>
        [Obsolete("getDurationInMs() is deprecated, please use getAssetDurationInMs().")]
        public double getDurationInMs()
        {
            if (!valid_)
            {
                return 0.0;
            }

            return nativeObj_.getAssetDurationInMs();
        }

        /// <summary>
        /// Returns the total duration of the asset in milliseconds.
        /// </summary>
        /// <returns>Returns the total duration of the asset in milliseconds.</returns>
        public double getAssetDurationInMs()
        {
            if (!valid_)
            {
                return 0.0;
            }

            return nativeObj_.getAssetDurationInMs();
        }

        /// <summary>
        /// Gets the play back state.
        /// </summary>
        /// <returns>The play back state.</returns>
        public PlayState getPlayState()
        {
            if (!valid_)
            {
                return PlayState.STOPPED;
            }

            return nativeObj_.getPlayState();
        }

        /// <summary>
        /// Cancels any scheduled play states on the object
        /// </summary>
        public void cancelScheduledParams()
        {
            if (!valid_)
            {
                return;
            }

            nativeObj_.cancelScheduledParams();
        }

        public void resetState()
        {
            volume_ = KDefaultVolume;
            loop_ = KDefaultLoop;
            pitch_ = KDefaultPitch;
            minDistance_ = KDefaultMinDistance;
            maxDistance_ = KDefaultMaxDistance;
            attenFactor_ = KDefaultAttenFactor;
            attenMode_ = KDefaultAttenMode;
            directionality_ = KDefaultDirectionality;
            directionalityLevel_ = KDefaulyDirectionalityLevel;
            directionalityConeArea_ = KDefaulyDirectionalityCone;
            spatialise_ = KDefaultSpatialize;
            maxDistanceMute_ = KDefaultMaxDistanceMute;

            var engine = getEngine();
            if (engine != null)
            {
                engine.connectToMasterBus(nativeObj_);
            }

            ForceUpdateProps();
        }

        void OnDestroy()
        {
            valid_ = false;

            var engine = getEngine();
            if (nativeObj_ != null && engine != null)
            {
                nativeObj_.setEventCallback(null, IntPtr.Zero);
                engine.destroyAudioObject(nativeObj_);
            }

            if (thisHandle.IsAllocated)
            {
                thisHandle.Free();
            }

            if (audioEngineClient_ != null)
            {
                audioEngineClient_.OnInvalid -= AudioEngineClientOnInvalid;
                audioEngineClient_ = null;
            }
        }

        /// <summary>
        /// Set the volume in linear gain
        /// </summary>
        public float volume
        {
            get
            {
                if (valid_)
                {
                    volume_ = nativeObj_.getVolume();
                }

                return volume_;
            }
            set
            {
                if (Math.Abs(volume_ - value) > Mathf.Epsilon)
                {
                    volume_ = value;
                    if (valid_)
                    {
                        nativeObj_.setVolume(volume_, 0 /* default ramp */);
                    }
                }
            }
        }

        /// <summary>
        /// Set the volume in decibels
        /// </summary>
        public float volumeDecibels
        {
            get { return Utils.linearToDecibels(volume); }
            set { volume = Utils.decibelsToLinear(value); }
        }

        /// <summary>
        /// Set the pitch
        /// </summary>
        public float pitch
        {
            get
            {
                if (valid_)
                {
                    pitch_ = nativeObj_.getPitch();
                }

                return pitch_;
            }
            set
            {
                pitch_ = Mathf.Clamp(value, 0.001f, 4.0f);
                if (valid_)
                {
                    nativeObj_.setPitch(pitch_);
                }
            }
        }

        /// <summary>
        /// Toggle looping. Use this for sample accurate looping rather than manually
        /// seeking the file to 0 when it finishes playing.
        /// </summary>
        public bool loop
        {
            get { return loop_; }
            set
            {
                if (loop_ != value)
                {
                    loop_ = value;
                    if (valid_)
                    {
                        nativeObj_.enableLooping(loop_);
                    }
                }
            }
        }

        /// <summary>
        /// Minimum distance: the distance after which the attenuation effect kicks in
        /// </summary>
        public float minDistance
        {
            get { return minDistance_; }
            set
            {
                if (value != minDistance_)
                {
                    minDistance_ = Mathf.Max(0, value);
                    attenuationPropsChanged_ = true;
                }
            }
        }

        /// <summary>
        /// Maximum distance: the distance after which the attenuation stops
        /// </summary>
        public float maxDistance
        {
            get { return maxDistance_; }
            set
            {
                if (value != maxDistance_)
                {
                    maxDistance_ = Mathf.Max(0, value);
                    attenuationPropsChanged_ = true;
                }
            }
        }

        /// <summary>
        /// Mute when maximum distance is reached (applicable when attenMode is set AttenuationMode.LOGARITHMIC)
        /// </summary>
        public bool maxDistanceMute
        {
            get { return maxDistanceMute_; }
            set
            {
                if (maxDistanceMute_ != value)
                {
                    maxDistanceMute_ = value;
                    attenuationPropsChanged_ = true;
                }
            }
        }

        /// <summary>
        /// The attenuation curve factor. 1 = 6dB drop with doubling of distance. > 1 steeper curve.
        /// < 1 less steep curve
        /// </summary>
        public float attenFactor
        {
            get { return attenFactor_; }
            set
            {
                if (attenFactor_ != value)
                {
                    attenFactor_ = Mathf.Max(Mathf.Epsilon, value);
                    attenuationPropsChanged_ = true;
                }
            }
        }

        /// <summary>
        /// The attenuation mode: LOGARITHMIC, LINEAR, DISABLE
        /// </summary>
        public AttenuationMode attenMode
        {
            get
            {
                if (valid_)
                {
                    attenMode_ = nativeObj_.getAttenuationMode();
                }

                return attenMode_;
            }
            set
            {
                if (attenMode_ != value)
                {
                    attenMode_ = value;
                    if (valid_)
                    {
                        nativeObj_.setAttenuationMode(attenMode_);
                    }
                }
            }
        }

        /// <summary>
        /// Toggle directional filtering
        /// </summary>
        public bool directionality
        {
            get { return directionality_; }
            set
            {
                if (value != directionality_)
                {
                    directionality_ = value;
                    if (valid_)
                    {
                        nativeObj_.setDirectionalityEnabled(directionality_);
                    }
                }
            }
        }

        /// <summary>
        /// A multiplier for the directionl filter, between 0 and 1,
        /// which changes how subtle or exaggerated the effect is.
        /// </summary>
        public float directionalityLevel
        {
            get { return directionalityLevel_; }
            set
            {
                if (value != directionalityLevel_)
                {
                    directionalityLevel_ = Mathf.Clamp01(value);
                    directionalityPropsChanged_ = true;
                }
            }
        }


        /// <summary>
        /// The directional cone area (in angles, between 0 and 359) where
        /// the sound is not modified. The area outside this will be filtered.
        /// </summary>
        public float directionalityConeArea
        {
            get { return directionalityConeArea_; }
            set
            {
                directionalityConeArea_ = Mathf.Clamp(value, 0, 359);
                directionalityPropsChanged_ = true;
            }
        }

        /// <summary>
        /// If the sound must be spatialised or not
        /// </summary>
        public bool spatialise
        {
            get { return spatialise_; }
            set
            {
                if (value != spatialise_)
                {
                    spatialise_ = value;
                    if (valid_)
                    {
                        nativeObj_.shouldSpatialise(spatialise_);
                    }
                }
            }
        }

        /// <summary>
        /// Setting this to a non-null value will cause the component to use this
        /// Transform, rather that the GameObject it is attached to.
        /// </summary>
        /// <value>The custom transform.</value>
        public Transform customTransform
        {
            get { return customTransform_; }
            set { customTransform_ = value; }
        }

        /// <summary>
        /// Returns the native object interface to access advanced features
        /// </summary>
        /// <value>The native object.</value>
        public NativeAudioObject nativeObject
        {
            get { return nativeObj_; }
        }

        public static TBE.AudioObject createAndPlayOnObject(GameObject ga, string file)
        {
            var obj = ga.AddComponent<AudioObject>();
            if (!obj.open(file))
            {
                Utils.logError("Failed to open " + file, null);
                Destroy(obj);
                return null;
            }

            obj.play();
            return obj;
        }

        public static TBE.AudioObject createOnObject(GameObject ga, string file)
        {
            var obj = ga.AddComponent<AudioObject>();
            if (!obj.open(file))
            {
                Utils.logError("Failed to open " + file, null);
                Destroy(obj);
                return null;
            }

            return obj;
        }
    }
}
