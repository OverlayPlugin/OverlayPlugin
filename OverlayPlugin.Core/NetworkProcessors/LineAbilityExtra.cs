using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;

namespace RainbowMage.OverlayPlugin.NetworkProcessors
{
    /**
     * Class for position/angle info from ActionEffect (i.e. line 21/22).
     *
     * This one is implemented different than LineActorCast. ActionEffect has several variants with different
     * sizes to support different maximum numbers of targets. They have a common header, which contains the angle of
     * the cast. However, the x/y/z coordinates are *after* the variable-size portion, so those are accessed by negative
     * indexing from the end of the packet rather than reflectively via Machina definitions.
     *
     * Despite this, we still need to pull out the types from Machina because we need to know the minimum sizes.
     */
    public class LineAbilityExtra
    {
        public const uint LogFileLineID = 264;
        private ILogger logger;
        private int minSize = Int32.MaxValue;
        private readonly int offsetMessageType;
        private readonly FFXIVRepository ffxiv;

        private Type actionEffectHeaderType;
        private Func<string, DateTime, bool> logWriter;
        private Dictionary<int, ActionEffectTypeInfo> opcodeToType = new Dictionary<int, ActionEffectTypeInfo>();
        private FieldInfo fieldR;

        private class ActionEffectTypeInfo
        {
            public int packetSize;
            public Type machinaType;
        }

        public LineAbilityExtra(TinyIoCContainer container)
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
                actionEffectHeaderType = mach.GetType("Machina.FFXIV.Headers.Server_ActionEffectHeader");
                fieldR = actionEffectHeaderType.GetField("rotation");

                int[] sizes = {1, 8, 16, 24, 32};
                foreach (int size in sizes)
                {
                    Type aeType = mach.GetType("Machina.FFXIV.Headers.Server_ActionEffect" + size);
                    var actionEffectTypeInfo = new ActionEffectTypeInfo()
                    {
                        machinaType = aeType,
                        packetSize = Marshal.SizeOf(aeType)
                    };
                    minSize = Math.Min(actionEffectTypeInfo.packetSize, minSize);
                    opcodeToType.Add(netHelper.GetOpcode("Ability" + size), actionEffectTypeInfo);
                }

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
            if (message.Length < minSize)
                return;

            fixed (byte* buffer = message)
            {
                int opCode = *(ushort*)&buffer[offsetMessageType];
                var info = opcodeToType[opCode];
                if (info != null)
                {
                    if (message.Length < info.packetSize)
                    {
                        return;
                    }
                    object packet = Marshal.PtrToStructure(new IntPtr(buffer), actionEffectHeaderType);
                    DateTime serverTime = ffxiv.EpochToDateTime(epoch);
                    float x = (BitConverter.ToUInt16(message, message.Length - 12) - 0x7FFF) / 32.767f;
                    float z = (BitConverter.ToUInt16(message, message.Length - 10) - 0x7FFF) / 32.767f;
                    float y = (BitConverter.ToUInt16(message, message.Length - 8) - 0x7FFF) / 32.767f;
                    double h = ((ushort)fieldR.GetValue(packet))
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