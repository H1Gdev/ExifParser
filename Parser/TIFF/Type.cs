using System;
using System.Collections.ObjectModel;

namespace Media.Tiff
{
    /// <summary>
    /// Type
    /// </summary>
    public enum Type : ushort
    {
        BYTE = 1,
        ASCII = 2,
        SHORT = 3,
        LONG = 4,
        RATIONAL = 5,

        SBYTE = 6,
        UNDEFINED = 7,
        SSHORT = 8,
        SLONG = 9,
        SRATIONAL = 10,
        FLOAT = 11,
        DOUBLE = 12,
    }

    public static class TypeExtension
    {
        private static readonly ReadOnlyCollection<uint> bytesPerType = Array.AsReadOnly(new uint[] { 0, 1, 1, 2, 4, 8, 1, 1, 2, 4, 8, 4, 8 });

        public static uint GetBytesPerType(this Type type)
        {
            return ((int)type < bytesPerType.Count) ? bytesPerType[(int)type] : 0;
        }
    }
}
