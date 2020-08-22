using System;
using System.IO;
using Xproto;
using Google.Protobuf;

namespace PokerTest
{
    public static class ProtoExt
    {
        public static byte[] ToBytes<T>(this T proto) where T: IMessage
        {
            if (proto == null)
                return null;

            using (var ms = new MemoryStream())
            {
                proto.WriteTo(ms);
                return ms.ToArray();
            }
        }

        //public static T ToProto<T>(this Stream stream) where T : IMessage
        //{
        //    if (stream == null) return default(T);

        //    var x = default(T);
        //    x.MergeFrom(stream);

        //    return x;
        //}

        //public static T ToProto<T>(this ByteString stream) where T : IMessage
        //{
        //    if (stream == null) return default(T);

        //    return x.Descriptor.Parser.ParseFrom(stream);
        //}

        //public static T ToProto<T>(this byte[] data) where T : IMessage
        //{
        //    if (data == null || data.Length == 0) return default(T);
        //    try
        //    {
        //        using (var ms = new MemoryStream(data))
        //        {
        //            var x = default(T);
        //            x.MergeFrom(ms);

        //            return x;
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine(e.Message);
        //        return default(T);
        //    }
        //}
    }
}
