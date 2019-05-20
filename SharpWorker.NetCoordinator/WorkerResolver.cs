using System;
using System.IO;
using System.Reflection;

namespace SharpWorker.NetCoordinator
{
    public class WorkerResolver : DefaultWorkerResolver
    {
        private readonly DirectoryInfo _workersDirectory;

        public WorkerResolver()
        {
            _workersDirectory = new DirectoryInfo(Environment.CurrentDirectory);
        }

        /// <inheritdoc />
        public override Assembly[] GetAssemblies()
        {
            // Load additional worker assemblies
            if (_workersDirectory != null)
            {
                if (!_workersDirectory.Exists)
                {
                    _workersDirectory.Create();
                }
                foreach (var workerDirectory in _workersDirectory.GetDirectories())
                {
                    var workerFile =
                        new FileInfo(Path.Combine(workerDirectory.FullName, workerDirectory.Name + ".dll"));

                    if (workerFile.Exists)
                    {
                        try
                        {
                            Assembly.LoadFile(workerFile.FullName);
                        }
                        catch (Exception)
                        {
                            // ignored
                        }
                    }
                }
            }

            return base.GetAssemblies();
        }
    }
}