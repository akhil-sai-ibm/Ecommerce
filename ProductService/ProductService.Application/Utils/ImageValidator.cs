using System;
using System.IO;

namespace ProductService.Application.Utils
{
    public static class ImageValidator
    {
        public static bool IsValidImage(Stream stream)
        {
            if (stream == null || stream.Length < 12)
                return false;

            stream.Position = 0;
            var buffer = new byte[12];
            stream.Read(buffer, 0, 12);
            stream.Position = 0; // reset for actual reading later

            // JPG: FF D8 FF
            if (buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF)
                return true;

            // PNG: 89 50 4E 47 0D 0A 1A 0A
            if (buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47 &&
                buffer[4] == 0x0D && buffer[5] == 0x0A && buffer[6] == 0x1A && buffer[7] == 0x0A)
                return true;

            // WebP: RIFF ... WEBP
            if (buffer[0] == 0x52 && buffer[1] == 0x49 && buffer[2] == 0x46 && buffer[3] == 0x46 &&
                buffer[8] == 0x57 && buffer[9] == 0x45 && buffer[10] == 0x42 && buffer[11] == 0x50)
                return true;

            return false;
        }
    }
}

