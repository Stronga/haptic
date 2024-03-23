// Copyright (c) 2018-present, Facebook, Inc. 


namespace TBE
{
    public interface IObject 
    {
        void onInit();
        void onTerminate();

        bool mustNotDestroyOnLoad();
    }
}
