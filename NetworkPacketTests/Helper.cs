using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Moq;
using RainbowMage.OverlayPlugin.NetworkProcessors.PacketHelper;

namespace RainbowMage.OverlayPlugin.NetworkPacketTests
{
    public static class Helper
    {
        private static FieldInfo FindField(Type t, String field)
        {
            foreach (FieldInfo fi in t.GetRuntimeFields())
            {
                if (fi.Name == field)
                {
                    return fi;
                }
            }

            var bt = t.BaseType;

            if (bt != null)
            {
                return FindField(bt, field);
            }

            return null;
        }

        public static Mock<FFXIVRepository> MockFFXIVRepository<T>()
        {
            FieldInfo fi = FindField(typeof(T), "ffxiv");
            var mock = new Mock<FFXIVRepository>();

            var ffxiv = mock.Object;

            fi.SetValue(null, ffxiv);

            return mock;
        }

        public static byte[] ReadRawPacketLine(String line, out DateTime dt, bool keepHeader = false)
        {
            // Example line:
            // 252|2024-08-02T20:32:48.9430000+02:00|00000458|10001234|10001234|00000000|03470014|00000000|66AD2650|00000000|0000000A|7673725F|3539315F|2D5F3930|5F335F31|5F305F30|44324553|30423543|45455F34|35434432|00343042|00000000|00000000|697489C3|6C65636E|0000656C|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000|00000000||dd23c6dd43cd1847
            String[] parts = line.Split('|');
            if (parts[0] != "252")
            {
                throw new ArgumentException("Line must be a raw packet line (starts with `252|`)");
            }

            dt = DateTime.Parse(parts[1]);

            List<byte> retList = new List<byte>();

            foreach (String segment in parts.Skip(2))
            {
                // Skip the hash if it's present
                if (segment.Length > 8)
                {
                    continue;
                }
                // Error on invalid segment lengths
                if (segment.Length % 2 != 0)
                {
                    throw new ArgumentException("Invalid segment length in raw network log line");
                }

                byte[] bytes = new byte[segment.Length / 2];
                for (int idx = 0; idx < segment.Length; idx += 2)
                {
                    bytes[idx / 2] = Convert.ToByte(segment.Substring(idx, 2), 16);
                }

                //reverse byte order to match actual data format
                retList.AddRange(bytes.Reverse());
            }

            if (keepHeader)
            {
                return retList.ToArray();
            }
            else
            {
                return retList.Skip(0x20).ToArray();
            }
        }
    }
}
