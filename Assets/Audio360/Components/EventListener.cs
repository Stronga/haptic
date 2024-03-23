// Copyright (c) 2018-present, Facebook, Inc. 


namespace TBE
{   
    public class EventListener 
    {
        public delegate void EventDelegate(TBE.Event e);
        public EventDelegate newEvent;

        public void onNewEvent(TBE.Event e)
        {
            if (newEvent != null)
            {
                newEvent(e);
            }
        }
    }

}
