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
        private readonly Type headerType;
        private readonly Type actorCastType;
        private readonly FieldInfo fieldCastSourceId;
        private readonly FieldInfo fieldX;
        private readonly FieldInfo fieldY;
        private readonly FieldInfo fieldZ;
        private readonly FieldInfo fieldR;

        private readonly Func<string, DateTime, bool> logWriter;

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
                headerType = mach.GetType("Machina.FFXIV.Headers.Server_MessageHeader");
                actorCastType = mach.GetType("Machina.FFXIV.Headers.Server_ActorCast");
                fieldCastSourceId = headerType.GetField("ActorID");
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
                    object header = Marshal.PtrToStructure(new IntPtr(buffer), headerType);
                    UInt32 sourceId = (UInt32)fieldCastSourceId.GetValue(header);

                    object packet = Marshal.PtrToStructure(new IntPtr(buffer), actorCastType);
                    DateTime serverTime = ffxiv.EpochToDateTime(epoch);
                    // for x/y/x, subtract 7FFF then divide by (2^15 - 1) / 100
                    float x = ffxiv.ConvertUInt16Coordinate((UInt16)fieldX.GetValue(packet));
                    // In-game uses Y as elevation and Z as north-south, but ACT convention is to use
                    // Z as elevation and Y as north-south.
                    float y = ffxiv.ConvertUInt16Coordinate((UInt16)fieldZ.GetValue(packet));
                    float z = ffxiv.ConvertUInt16Coordinate((UInt16)fieldY.GetValue(packet));
                    // for rotation, the packet uses '0' as north, and each increment is 1/65536 of a CCW turn, while
                    // in-game uses 0=south, pi/2=west, +/-pi=north
                    // Machina thinks this is a float but that appears to be incorrect, so we have to reinterpret as
                    // a UInt16
                    double h = ffxiv.ConvertHeading(ffxiv.InterpretFloatAsUInt16((float)fieldR.GetValue(packet)));



                    string line = string.Format(CultureInfo.InvariantCulture,
                        "{0:X8}|{1:F3}|{2:F3}|{3:F3}|{4:F3}",
                        sourceId, x, y, z, h);

                    logWriter(line, serverTime);
                }
            }
        }
    }
}