using System;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace MovementManager.Service
{

    public interface INotificationService : IDisposable
    {
        void NotifyProgress(int completed, int total);
    }
    public class NotificationService : INotificationService
    {
        private readonly MemoryMappedFile _memoryMappedFile;
        private readonly string _memoryMapName;
        private readonly long _capacity;
        private readonly Process _worker;

        public object WorkerApplication => "MovementManagerWorker.exe";

        public NotificationService( string memoryMapName, long capacity )
        {
            _memoryMapName = memoryMapName;
            _capacity = capacity;
            _memoryMappedFile = GetMemoryMappedFile();

            // TODO include worker service app when building project
            string dir = Environment.CurrentDirectory;
            _worker = CreateWorker( $"{dir}\\WorkerService" );
            _worker.Start();
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

        private Process CreateWorker(string workingDirectory, string prefix = "/k")
        {
            string arguments = $"{WorkerApplication} mapName="+_memoryMapName;
            Process proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = prefix + arguments,
                    WindowStyle = ProcessWindowStyle.Normal,
                    WorkingDirectory = @workingDirectory,
                    UseShellExecute =true
                }
            };

            return proc;
        }

        public void Dispose()
        {
            _memoryMappedFile?.Dispose();
            _worker?.Dispose();
        }
    }
}