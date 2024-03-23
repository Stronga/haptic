// Copyright (c) 2018-present, Facebook, Inc. 


using System.Threading;

namespace Facebook.Audio
{
    public abstract class Job
    {
        volatile bool isFinished = false;
        Thread thread = null;

        public bool IsFinished
        {
            get
            {
                return isFinished;
            }
        }

        public void Start()
        {
            thread = new Thread(Run);
            thread.Start();
        }

        public void Abort()
        {
            thread.Abort();
        }

        protected abstract void ThreadFunction();

        void Run()
        {
            ThreadFunction();
            isFinished = true;
        }
    }
}
