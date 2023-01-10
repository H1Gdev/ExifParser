namespace Media.Jpeg
{
    /// <summary>
    /// Marker Code
    /// </summary>
    public enum MarkerCode : byte
    {
        Prefix = 0xff,  // Marker Prefix

        SOF0 = 0xc0,    // Start of Frame 0
        SOF1,           // Start of Frame 1
        SOF2,           // Start of Frame 2
        SOF3,           // Start of Frame 3
        DHT = 0xc4,     // Define Huffman Table
        SOF5,           // Start of Frame 5
        SOF6,           // Start of Frame 6
        SOF7,           // Start of Frame 7
        JPG = 0xc8,     // Reserved for JPEG extensions
        SOF9,           // Start of Frame 9
        SOF10,          // Start of Frame 10
        SOF11,          // Start of Frame 11
        DAC = 0xcc,     // Define Arithmetic Coding Conditioning
        SOF13,          // Start of Frame 13
        SOF14,          // Start of Frame 14
        SOF15,          // Start of Frame 15
        RST0 = 0xd0,    // Restart 0
        RST1,           // Restart 1
        RST2,           // Restart 2
        RST3,           // Restart 3
        RST4,           // Restart 4
        RST5,           // Restart 5
        RST6,           // Restart 6
        RST7,           // Restart 7
        SOI = 0xd8,     // Start of Image
        EOI = 0xd9,     // End of Image
        SOS = 0xda,     // Start of Scan
        DQT = 0xdb,     // Define Quantization Table
        DNL = 0xdc,     // Define Number of Line
        DRI = 0xdd,     // Define Restart Interval
        DHP = 0xde,     // Define Hierarchical Progression
        EXP = 0xdf,     // Expand Reference Component
        APP0 = 0xe0,    // Application Segment 0
        APP1,           // Application Segment 1
        APP2,           // Application Segment 2
        APP3,           // Application Segment 3
        APP4,           // Application Segment 4
        APP5,           // Application Segment 5
        APP6,           // Application Segment 6
        APP7,           // Application Segment 7
        APP8,           // Application Segment 8
        APP9,           // Application Segment 9
        APP10,          // Application Segment 10
        APP11,          // Application Segment 11
        APP12,          // Application Segment 12
        APP13,          // Application Segment 13
        APP14,          // Application Segment 14
        APP15,          // Application Segment 15
        JPG0 = 0xf0,    // JPEG Segment 0
        JPG1,           // JPEG Segment 1
        JPG2,           // JPEG Segment 2
        JPG3,           // JPEG Segment 3
        JPG4,           // JPEG Segment 4
        JPG5,           // JPEG Segment 5
        JPG6,           // JPEG Segment 6
        JPG7,           // JPEG Segment 7
        JPG8,           // JPEG Segment 8
        JPG9,           // JPEG Segment 9
        JPG10,          // JPEG Segment 10
        JPG11,          // JPEG Segment 11
        JPG12,          // JPEG Segment 12
        JPG13,          // JPEG Segment 13
        COM = 0xfe,     // Comment
    }

    public static class MarkerCodeExtension
    {
        public static bool HasData(this MarkerCode markerCode)
        {
            switch (markerCode)
            {
                case MarkerCode.SOI:
                case MarkerCode.EOI:
                case MarkerCode.RST0:
                case MarkerCode.RST1:
                case MarkerCode.RST2:
                case MarkerCode.RST3:
                case MarkerCode.RST4:
                case MarkerCode.RST5:
                case MarkerCode.RST6:
                case MarkerCode.RST7:
                    return false;
                default:
                    return true;
            }
        }
    }
}
