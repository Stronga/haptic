// Copyright (c) 2018-present, Facebook, Inc. 


namespace TBE
{
    public class AudioObjectEventListener
    {
        public delegate void EventDelegate(TBE.Event e, AudioObject obj);
        public EventDelegate newEvent;

        public void onNewEvent(TBE.Event e, AudioObject obj)
        {
            if (newEvent != null)
            {
                newEvent(e, obj);
            }
        }
    }

}
