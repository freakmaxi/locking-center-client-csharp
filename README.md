# Locking-Center C# Client

The C# Connector of Locking-Center that is a mutex point to synchronize access between different services. You can limit the 
execution between services and create queueing for the operation.

- [Locking-Center Server](https://github.com/freakmaxi/locking-center)

#### Installation (NuGet)

Please visit the NuGet page for installation options [NuGet](https://www.nuget.org/packages/LockingCenterClient/)

#### Usage

```c#
var mutex = 
    new LockingCenter.Mutex.Connection("localhost:22119");

mutex.Lock("locking-key");
try
{
    ...
}
finally
{
    mutex.Unlock("locking-key")
}
```