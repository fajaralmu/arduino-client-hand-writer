using System;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace MovementManager.Service
{

    public interface INotificationService
    {
        void NotifyProgress(int completed, int total);
    }
    public class NotificationService : INotificationService
    {
        private readonly MemoryMappedFile _memoryMappedFile;
        private readonly string _memoryMapName;
        private readonly long _capacity;

        public NotificationService( string memoryMapName, long capacity )
        {
            _memoryMapName = memoryMapName;
            _capacity = capacity;
            _memoryMappedFile = GetMemoryMappedFile();
        }

        private MemoryMappedFile GetMemoryMappedFile()
        {
            return MemoryMappedFile.CreateOrOpen( _memoryMapName, _capacity );
        }

        public void NotifyProgress(int completed, int total)
        {
            using (var accessor = _memoryMappedFile.CreateViewAccessor(0, _capacity))
            {
                accessor.WriteArray<int>(0, new int [] { completed, total }, 0, 2 );
                // accessor.Write(2, total);
            }
            
        }
    }
}