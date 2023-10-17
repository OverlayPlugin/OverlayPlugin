using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;

namespace RainbowMage.OverlayPlugin.NetworkProcessors
{
    public class LineActorCastExtra
    {
        public const uint LogFileLineID = 263;
        private ILogger logger;
        private readonly int packetSize;
        private readonly int packetOpcode;
        private readonly int offsetMessageType;
        private readonly FFXIVRepository ffxiv;
        private readonly Type actorCastType;
        private readonly FieldInfo fieldX;
        private readonly FieldInfo fieldY;
        private readonly FieldInfo fieldZ;
        private readonly FieldInfo fieldR;

        private Func<string, DateTime, bool> logWriter;

        public LineActorCastExtra(TinyIoCContainer container)
        {
            logger = container.Resolve<ILogger>();
            ffxiv = container.Resolve<FFXIVRepository>();
            var netHelper = container.Resolve<NetworkParser>();
            if (!ffxiv.IsFFXIVPluginPresent())
                return;
            try
            {
                var mach = Assembly.Load("Machina.FFXIV");
                var msgHeaderType = mach.GetType("Machina.FFXIV.Headers.Server_MessageHeader");
                actorCastType = mach.GetType("Machina.FFXIV.Headers.Server_ActorCast");
                fieldX = actorCastType.GetField("PosX");
                fieldY = actorCastType.GetField("PosY");
                fieldZ = actorCastType.GetField("PosZ");
                fieldR = actorCastType.GetField("Rotation");
                packetOpcode = netHelper.GetOpcode("ActorCast");
                packetSize = Marshal.SizeOf(actorCastType);
                offsetMessageType = netHelper.GetOffset(msgHeaderType, "MessageType");
                ffxiv.RegisterNetworkParser(MessageReceived);
            }
            catch (System.IO.FileNotFoundException)
            {
                logger.Log(LogLevel.Error, Resources.NetworkParserNoFfxiv);
            }
            catch (Exception e)
            {
                logger.Log(LogLevel.Error, Resources.NetworkParserInitException, e);
            }

            var customLogLines = container.Resolve<FFXIVCustomLogLines>();
            this.logWriter = customLogLines.RegisterCustomLogLine(new LogLineRegistryEntry()
            {
                Name = "ActorCastExtra",
                Source = "OverlayPlugin",
                ID = LogFileLineID,
                Version = 1,
            });
        }

        private unsafe void MessageReceived(string id, long epoch, byte[] message)
        {
            if (message.Length < packetSize)
                return;

            fixed (byte* buffer = message)
            {
                if (*(ushort*)&buffer[offsetMessageType] == packetOpcode)
                {
                    object packet = Marshal.PtrToStructure(new IntPtr(buffer), actorCastType);
                    DateTime serverTime = ffxiv.EpochToDateTime(epoch);
                    // see https://github.com/ravahn/FFXIV_ACT_Plugin/issues/298
                    // for x/y/x, subtract 7FFF then divide by (2^15 - 1) / 100
                    float x = ((UInt16)fieldX.GetValue(packet) - 0x7FFF) / 32.767f;
                    // In-game uses Y as elevation and Z as north-south, but ACT convention is to use
                    // Z as elevation and Y as north-south.
                    float y = ((UInt16)fieldZ.GetValue(packet) - 0x7FFF) / 32.767f;
                    float z = ((UInt16)fieldY.GetValue(packet) - 0x7FFF) / 32.767f;
                    // for rotation, the packet uses '0' as north, and each increment is 1/65536 of a CCW turn, while
                    // in-game uses 0=south, pi/2=west, +/-pi=north
                    // Machina thinks this is a float but that appears to be incorrect, so we have to reinterpret as
                    // a UInt16
                    double h = BitConverter.ToUInt16(BitConverter.GetBytes(
                                   (float)fieldR.GetValue(packet)), 0)
                               // Normalize to turns
                               / 65536.0
                               // Normalize to radians
                               * 2 * Math.PI
                               // Flip from 0=north to 0=south like the game uses
                               - Math.PI;

                    string line = string.Format(CultureInfo.InvariantCulture,
                        "{0:F3}|{1:F3}|{2:F3}|{3:F3}",
                        x, y, z, h);

                    logWriter(line, serverTime);
                }
            }
        }
    }
}