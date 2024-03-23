// Copyright (c) 2018-present, Facebook, Inc. 


namespace Facebook.Audio
{
    /// <summary>
    /// A single producer, single consumer, bounded, lock free queue.
    /// </summary>
    /// <typeparam name="T">The data type contained in the queue</typeparam>
    public class SPSCQ<T>
    {
        public delegate void Writer(ref T item);
        public delegate void Reader(T item);
        
        public readonly uint maxSize;

        private T[] data_;
        private uint read_ = 0;
        private uint write_ = 0;
        private uint size_ = 0;
        
        public SPSCQ(uint maxSize)
        {
            this.maxSize = maxSize;
            data_ = new T[maxSize];
        }

        /// <summary>
        /// The number of items in the queue
        /// </summary>
        public uint Count
        {
            get { return size_; }
        }

        /// <summary>
        /// Add an item to the queue
        /// </summary>
        /// <param name="writer">The write function to update the item</param>
        /// <returns>Whether there was room in the queue</returns>
        public bool Push(Writer writer)
        {
            if (size_ == maxSize)
            {
                return false;
            }

            writer(ref data_[write_]);
            write_ = (write_ + 1) % maxSize;
            ++size_;
            return true;
        }

        /// <summary>
        /// Remove an item from the queue
        /// </summary>
        /// <param name="reader">The read function to get the item info</param>
        /// <returns>Whether there were any items left in the queue</returns>
        public bool Pop(Reader reader)
        {
            if (size_ == 0)
            {
                return false;
            }

            reader(data_[read_]);
            read_ = (read_ + 1) % maxSize;
            --size_;
            return true;
        }
    }
}
