// Copyright (c) 2018-present, Facebook, Inc. 


using System.Collections.Generic;
using TBE;

namespace Facebook.Audio
{
    public class TPool<T> where T : class
    {
        public delegate T CreateObject();
        
        public readonly int maxSize;

        private readonly CreateObject createFn_;
        private readonly Queue<T> inactive_;
        private readonly List<T> active_;

        public int NumUsed
        {
            get { return active_.Count; }
        }

        public int NumAvailable
        {
            get { return inactive_.Count; }
        }
        
        public T Get()
        {
            T o = null;
            
            if (inactive_.Count == 0)
            {
                // if we have a max size, don't grow the pool
                if (maxSize > 0)
                {
                    return null;
                }

                // create a new object
                o = createFn_();
            }
            else
            {
                // grab an object from the queue
                o = inactive_.Dequeue();
            }
            
            // add to the active list and return the object
            active_.Add(o);
            return o;
        }

        public bool Return(T objectToReturn)
        {
            int idx = active_.IndexOf(objectToReturn);
            
            if (idx < 0)
            {
                Utils.logWarning(string.Format(
                    "Tried to return an object that doesn't belong to this pool: {0}",
                    objectToReturn), null);
                return false;
            }

            inactive_.Enqueue(active_[idx]);
            active_.RemoveAt(idx);
            return true;
        }
        
        public TPool(int initialSize, CreateObject createFn, int maxSize = 0)
        {
            this.maxSize = maxSize;
            createFn_ = createFn;
            
            // reserve space for the object containers
            inactive_ = new Queue<T>(initialSize);
            active_ = new List<T>(initialSize);

            // fill up the inactive queue with objects
            for (int i = 0; i < initialSize; ++i)
            {
                inactive_.Enqueue(createFn_());
            }
        }
    }
}
