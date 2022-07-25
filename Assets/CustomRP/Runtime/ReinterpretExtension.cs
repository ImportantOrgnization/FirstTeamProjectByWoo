using System.Runtime.InteropServices;

public static class ReinterpretExtension 
{
    [StructLayout(LayoutKind.Explicit)]
    struct IntFloat
    {
        [FieldOffset(0)]
        public int intValue;
        [FieldOffset(0)]
        public float floatValue;
    }
    public static float ReinterpretAsFloat(this int value)
    {
        IntFloat convert = default;
        convert.intValue = value;
        return convert.floatValue;
    }
}
