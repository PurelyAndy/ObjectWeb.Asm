using System.Runtime.InteropServices;

namespace ObjectWeb.Asm.Util
{
    [StructLayout(LayoutKind.Explicit)]
    public struct Int32AndSingleConverter
    {
        [FieldOffset(0)] private int Int32;
        [FieldOffset(0)] private float Single;
        public static float Convert(int value)
        {
            return new Int32AndSingleConverter { Int32 = value }.Single;
        }
        public static int Convert(float value)
        {
            return new Int32AndSingleConverter { Single = value }.Int32;
        }
    }
}