namespace LockingCenter.Mutex
{
    public interface IConnection
    {
        void Lock(string key);
        void Unlock(string key);
        void Wait(string key);
        void Reset(string key);
    }
}