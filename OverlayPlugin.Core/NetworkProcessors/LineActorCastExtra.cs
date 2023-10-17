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
        private OverlayPluginLogLineConfig opcodeConfig;
        private readonly int packetSize;
        private readonly int packetOpcode;
        private readonly int offsetMessageType;
        private readonly FFXIVRepository ffxiv;
        private readonly Type actorCastType;

        private Func<string, DateTime, bool> logWriter;

        public LineActorCastExtra(TinyIoCContainer container)
        {
            logger = container.Resolve<ILogger>();
            ffxiv = container.Resolve<FFXIVRepository>();
            var netHelper = container.Resolve<NetworkParser>();
            if (!ffxiv.IsFFXIVPluginPresent())
                return;
            opcodeConfig = container.Resolve<OverlayPluginLogLineConfig>();
            try
            {
                var mach = Assembly.Load("Machina.FFXIV");
                var msgHeaderType = mach.GetType("Machina.FFXIV.Headers.Server_MessageHeader");
                actorCastType = mach.GetType("Machina.FFXIV.Headers.Server_ActorCast");
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
                    float x = ((UInt16)actorCastType.GetField("PosX").GetValue(packet) - 0x7FFF) / 32.767f;
                    // In-game uses Y as elevation and Z as north-south, but ACT convention is to use
                    // Z as elevation and Y as north-south.
                    float y = ((UInt16)actorCastType.GetField("PosZ").GetValue(packet) - 0x7FFF) / 32.767f;
                    float z = ((UInt16)actorCastType.GetField("PosY").GetValue(packet) - 0x7FFF) / 32.767f;
                    // for rotation, the packet uses '0' as north, and each increment is 1/65536 of a CCW turn, while
                    // in-game uses 0=south, pi/2=west, +/-pi=north
                    // Machina thinks this is a float but that appears to be incorrect, so we have to reinterpret as
                    // a UInt16
                    double h = BitConverter.ToUInt16(BitConverter.GetBytes(
                                   (float)actorCastType.GetField("Rotation").GetValue(packet)), 0)
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