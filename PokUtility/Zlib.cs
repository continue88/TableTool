using System.IO;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

public static class Zlib
{
    public static byte[] Compress(byte[] data, int offset, int count, int level)
    {
        MemoryStream memoryStream = new MemoryStream();
        Deflater deflater = new Deflater(level);
        using (DeflaterOutputStream outStream = new DeflaterOutputStream(memoryStream, deflater))
        {
            outStream.IsStreamOwner = false;
            outStream.Write(data, offset, count);
            outStream.Flush();
            outStream.Finish();
        }
        return memoryStream.ToArray();
    }

    public static byte[] Compress(byte[] data)
    {
        return Compress(data, 0, data.Length, Deflater.BEST_COMPRESSION);
    }

    public static byte[] DeCompress(byte[] data)
    {
        MemoryStream memory = new MemoryStream();
        byte[] writeData = new byte[4096];
        using (InflaterInputStream stream = new InflaterInputStream(new MemoryStream(data)))
        {
            while (true)
            {
                int size = stream.Read(writeData, 0, writeData.Length);
                if (size <= 0) break;
                memory.Write(writeData, 0, size);
            }
        }
        return memory.ToArray();
    }
}
