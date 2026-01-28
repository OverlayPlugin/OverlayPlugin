using System;
using System.Runtime.InteropServices;

namespace RainbowMage.OverlayPlugin.NetworkProcessors.PacketHelper
{
    class RegionalizedPacketHelper<
        HeaderStruct_Global, PacketStruct_Global,
        HeaderStruct_CN, PacketStruct_CN,
        HeaderStruct_KR, PacketStruct_KR,
        HeaderStruct_TC, PacketStruct_TC>
        where HeaderStruct_Global : struct, IHeaderStruct
        where PacketStruct_Global : struct, IPacketStruct
        where HeaderStruct_CN : struct, IHeaderStruct
        where PacketStruct_CN : struct, IPacketStruct
        where HeaderStruct_KR : struct, IHeaderStruct
        where PacketStruct_KR : struct, IPacketStruct
        where HeaderStruct_TC : struct, IHeaderStruct
        where PacketStruct_TC : struct, IPacketStruct
    {
        public readonly PacketHelper<HeaderStruct_Global, PacketStruct_Global> global;
        public readonly PacketHelper<HeaderStruct_CN, PacketStruct_CN> cn;
        public readonly PacketHelper<HeaderStruct_KR, PacketStruct_KR> kr;
        public readonly PacketHelper<HeaderStruct_TC, PacketStruct_TC> tc;

        public RegionalizedPacketHelper(ushort globalOpcode, ushort cnOpcode, ushort krOpcode, ushort tcOpcode)
        {
            global = new PacketHelper<HeaderStruct_Global, PacketStruct_Global>(globalOpcode);
            cn = new PacketHelper<HeaderStruct_CN, PacketStruct_CN>(cnOpcode);
            kr = new PacketHelper<HeaderStruct_KR, PacketStruct_KR>(krOpcode);
            tc = new PacketHelper<HeaderStruct_TC, PacketStruct_TC>(tcOpcode);
        }

        public IPacketHelper this[GameRegion gameRegion]
        {
            get
            {
                switch (gameRegion)
                {
                    case GameRegion.Global: return global;
                    case GameRegion.Chinese: return cn;
                    case GameRegion.Korean: return kr;
                    case GameRegion.TraditionalChinese: return tc;

                    default: return global;
                }
            }
        }

        public static RegionalizedPacketHelper<HeaderStruct_Global, PacketStruct_Global, HeaderStruct_CN, PacketStruct_CN, HeaderStruct_KR, PacketStruct_KR, HeaderStruct_TC, PacketStruct_TC>
            CreateFromMachina(string opcodeName)
        {
            var opcodes = FFXIVRepository.GetMachinaOpcodes();
            if (opcodes == null)
            {
                return null;
            }

            if (!opcodes.TryGetValue(GameRegion.Global, out var globalOpcodes))
            {
                return null;
            }
            if (!opcodes.TryGetValue(GameRegion.Chinese, out var cnOpcodes))
            {
                return null;
            }
            if (!opcodes.TryGetValue(GameRegion.Korean, out var krOpcodes))
            {
                return null;
            }
            if (!opcodes.TryGetValue(GameRegion.TraditionalChinese, out var tcOpcodes))
            {
                // @TODO: Once FFXIV_ACT_Plugin has TC opcodes for global release, remove this default
                tcOpcodes = new System.Collections.Generic.Dictionary<string, ushort>();
                // return null;
            }

            if (!globalOpcodes.TryGetValue(opcodeName, out var globalOpcode))
            {
                globalOpcode = 0;
            }
            if (!cnOpcodes.TryGetValue(opcodeName, out var cnOpcode))
            {
                cnOpcode = 0;
            }
            if (!krOpcodes.TryGetValue(opcodeName, out var krOpcode))
            {
                krOpcode = 0;
            }
            if (!tcOpcodes.TryGetValue(opcodeName, out var tcOpcode))
            {
                tcOpcode = 0;
            }

            return new RegionalizedPacketHelper<HeaderStruct_Global, PacketStruct_Global, HeaderStruct_CN, PacketStruct_CN, HeaderStruct_KR, PacketStruct_KR, HeaderStruct_TC, PacketStruct_TC>
                (globalOpcode, cnOpcode, krOpcode, tcOpcode);
        }

        public static RegionalizedPacketHelper<HeaderStruct_Global, PacketStruct_Global, HeaderStruct_CN, PacketStruct_CN, HeaderStruct_KR, PacketStruct_KR, HeaderStruct_TC, PacketStruct_TC>
            CreateFromOpcodeConfig(OverlayPluginLogLineConfig opcodeConfig, string opcodeName)
        {
            var globalOpcodeConfigEntry = opcodeConfig[opcodeName, GameRegion.Global.ToString()];
            var cnOpcodeConfigEntry = opcodeConfig[opcodeName, GameRegion.Chinese.ToString()];
            var krOpcodeConfigEntry = opcodeConfig[opcodeName, GameRegion.Korean.ToString()];
            var tcOpcodeConfigEntry = opcodeConfig[opcodeName, GameRegion.TraditionalChinese.ToString()];

            ushort globalOpcode = (ushort)(globalOpcodeConfigEntry?.opcode ?? 0);
            ushort cnOpcode = (ushort)(cnOpcodeConfigEntry?.opcode ?? 0);
            ushort krOpcode = (ushort)(krOpcodeConfigEntry?.opcode ?? 0);
            ushort tcOpcode = (ushort)(tcOpcodeConfigEntry?.opcode ?? 0);

            return new RegionalizedPacketHelper<HeaderStruct_Global, PacketStruct_Global, HeaderStruct_CN, PacketStruct_CN, HeaderStruct_KR, PacketStruct_KR, HeaderStruct_TC, PacketStruct_TC>
                (globalOpcode, cnOpcode, krOpcode, tcOpcode);
        }
    }

    interface IPacketHelper
    {
        string ToString(long epoch, byte[] message);
    }

    class PacketHelper<HeaderStruct, PacketStruct> : IPacketHelper
        where HeaderStruct : struct, IHeaderStruct
        where PacketStruct : struct, IPacketStruct
    {
        public readonly ushort Opcode;
        public readonly int headerSize;
        public readonly int packetSize;

        public PacketHelper(ushort opcode)
        {
            Opcode = opcode;
            headerSize = Marshal.SizeOf(typeof(HeaderStruct));
            packetSize = Marshal.SizeOf(typeof(PacketStruct));
        }

        /// <summary>
        /// Construct a string representation of a packet from a byte array
        /// </summary>
        /// <param name="epoch">epoch timestamp from FFXIV_ACT_Plugin's NetworkReceivedDelegate</param>
        /// <param name="message">Message byte array received from FFXIV_ACT_Plugin's NetworkReceivedDelegate</param>
        /// <returns>null for invalid packet, otherwise a constructed packet</returns>
        public string ToString(long epoch, byte[] message)
        {
            if (ToStructs(message, out var header, out var packet) == false)
            {
                return null;
            }

            return ToString(epoch, header, packet);
        }

        /// <summary>
        /// Construct a string representation of a packet from a byte array
        /// </summary>
        /// <param name="epoch">epoch timestamp from FFXIV_ACT_Plugin's NetworkReceivedDelegate</param>
        /// <param name="message">Message byte array received from FFXIV_ACT_Plugin's NetworkReceivedDelegate</param>
        /// <returns>null for invalid packet, otherwise a constructed packet</returns>
        public string ToString(long epoch, HeaderStruct header, PacketStruct packet)
        {
            return packet.ToString(epoch, header.ActorID);
        }

        public unsafe bool ToStructs(byte[] message, out HeaderStruct header, out PacketStruct packet)
        {
            // Message is too short to contain this packet
            if (message.Length < headerSize + packetSize)
            {
                header = default;
                packet = default;

                return false;
            }

            fixed (byte* messagePtr = message)
            {
                var headerPtr = new IntPtr(messagePtr);
                header = Marshal.PtrToStructure<HeaderStruct>(headerPtr);

                if (header.Opcode != Opcode)
                {
                    header = default;
                    packet = default;

                    return false;
                }

                var packetPtr = new IntPtr(messagePtr + headerSize);
                packet = Marshal.PtrToStructure<PacketStruct>(packetPtr);

                return true;
            }
        }
    }

    interface IPacketStruct
    {
        string ToString(long epoch, uint ActorID);
    }

    interface IHeaderStruct
    {
        uint ActorID { get; }
        uint Opcode { get; }
    }
}
