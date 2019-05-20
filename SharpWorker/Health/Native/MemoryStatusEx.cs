using System.Runtime.InteropServices;

namespace SharpWorker.Health.Native
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct MemoryStatusEx
    {
        private uint Length;
        internal uint MemoryLoad;
        internal ulong TotalPhysical;
        internal ulong AvailablePhysical;
        internal ulong TotalPageFile;
        internal ulong AvailablePageFile;
        internal ulong TotalVirtual;
        internal ulong AvailableVirtual;
        internal ulong AvailableExtendedVirtual;

        public static MemoryStatusEx Initialize()
        {
            return new MemoryStatusEx
            {
                Length = checked((uint) Marshal.SizeOf(typeof(MemoryStatusEx)))
            };
        }
    }
}