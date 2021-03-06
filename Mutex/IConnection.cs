namespace LockingCenter.Mutex
{
    public interface IConnection
    {
        void Lock(string key);
        void Unlock(string key);
        void Wait(string key);
        void ResetByKey(string key);
        void ResetBySource(string sourceAddr = null);
    }
}