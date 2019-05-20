using System.Runtime.InteropServices;

namespace SharpWorker.Health.Native
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct MemoryStatus
    {
        private uint Length;
        internal uint MemoryLoad;
        internal uint TotalPhysical;
        internal uint AvailablePhysical;
        internal uint TotalPageFile;
        internal uint AvailablePageFile;
        internal uint TotalVirtual;
        internal uint AvailableVirtual;

        public static MemoryStatus Initialize()
        {
            return new MemoryStatus
            {
                Length = checked((uint) Marshal.SizeOf(typeof(MemoryStatus)))
            };
        }
    }
}