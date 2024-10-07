using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RainbowMage.OverlayPlugin.NetworkProcessors.PacketHelper
{
    class RegionalizedPacketHelper<
        HeaderStruct_Global, PacketStruct_Global,
        HeaderStruct_CN, PacketStruct_CN,
        HeaderStruct_KR, PacketStruct_KR>
        where HeaderStruct_Global : struct, IHeaderStruct
        where PacketStruct_Global : struct, IPacketStruct
        where HeaderStruct_CN : struct, IHeaderStruct
        where PacketStruct_CN : struct, IPacketStruct
        where HeaderStruct_KR : struct, IHeaderStruct
        where PacketStruct_KR : struct, IPacketStruct
    {
        public readonly PacketHelper<HeaderStruct_Global, PacketStruct_Global> global;
        public readonly PacketHelper<HeaderStruct_CN, PacketStruct_CN> cn;
        public readonly PacketHelper<HeaderStruct_KR, PacketStruct_KR> kr;

        private RegionalizedPacketHelper(Func<ushort> globalOpcodeFunc, Func<ushort> cnOpcodeFunc, Func<ushort> krOpcodeFunc)
        {
            global = new PacketHelper<HeaderStruct_Global, PacketStruct_Global>(globalOpcodeFunc);
            cn = new PacketHelper<HeaderStruct_CN, PacketStruct_CN>(cnOpcodeFunc);
            kr = new PacketHelper<HeaderStruct_KR, PacketStruct_KR>(krOpcodeFunc);
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

                    default: return global;
                }
            }
        }

        public static RegionalizedPacketHelper<HeaderStruct_Global, PacketStruct_Global, HeaderStruct_CN, PacketStruct_CN, HeaderStruct_KR, PacketStruct_KR>
            CreateFromMachina(string opcodeName)
        {
            Func<ushort> globalOpcodeFunc = () =>
            {
                var opcodes = FFXIVRepository.GetMachinaOpcodes();
                if (opcodes == null)
                {
                    return 0;
                }
                if (!opcodes.TryGetValue(GameRegion.Global, out var globalOpcodes))
                {
                    return 0;
                }
                if (!globalOpcodes.TryGetValue(opcodeName, out var globalOpcode))
                {
                    return 0;
                }
                return globalOpcode;
            };

            Func<ushort> cnOpcodeFunc = () =>
            {
                var opcodes = FFXIVRepository.GetMachinaOpcodes();
                if (opcodes == null)
                {
                    return 0;
                }
                if (!opcodes.TryGetValue(GameRegion.Chinese, out var cnOpcodes))
                {
                    return 0;
                }
                if (!cnOpcodes.TryGetValue(opcodeName, out var cnOpcode))
                {
                    return 0;
                }
                return cnOpcode;
            };

            Func<ushort> krOpcodeFunc = () =>
            {
                var opcodes = FFXIVRepository.GetMachinaOpcodes();
                if (opcodes == null)
                {
                    return 0;
                }
                if (!opcodes.TryGetValue(GameRegion.Korean, out var krOpcodes))
                {
                    return 0;
                }
                if (!krOpcodes.TryGetValue(opcodeName, out var krOpcode))
                {
                    return 0;
                }
                return krOpcode;
            };

            return new RegionalizedPacketHelper<HeaderStruct_Global, PacketStruct_Global, HeaderStruct_CN, PacketStruct_CN, HeaderStruct_KR, PacketStruct_KR>
                (globalOpcodeFunc, cnOpcodeFunc, krOpcodeFunc);
        }

        public static RegionalizedPacketHelper<HeaderStruct_Global, PacketStruct_Global, HeaderStruct_CN, PacketStruct_CN, HeaderStruct_KR, PacketStruct_KR>
            CreateFromOpcodeConfig(OverlayPluginLogLineConfig opcodeConfig, string opcodeName)
        {
            Func<ushort> globalOpcodeFunc = () =>
            {
                var globalOpcodeConfigEntry = opcodeConfig[opcodeName, GameRegion.Global.ToString()];
                return (ushort)(globalOpcodeConfigEntry?.opcode ?? 0);
            };
            Func<ushort> cnOpcodeFunc = () =>
            {
                var cnOpcodeConfigEntry = opcodeConfig[opcodeName, GameRegion.Chinese.ToString()];
                return (ushort)(cnOpcodeConfigEntry?.opcode ?? 0);
            };
            Func<ushort> krOpcodeFunc = () =>
            {
                var krOpcodeConfigEntry = opcodeConfig[opcodeName, GameRegion.Korean.ToString()];
                return (ushort)(krOpcodeConfigEntry?.opcode ?? 0);
            };

            return new RegionalizedPacketHelper<HeaderStruct_Global, PacketStruct_Global, HeaderStruct_CN, PacketStruct_CN, HeaderStruct_KR, PacketStruct_KR>
                (globalOpcodeFunc, cnOpcodeFunc, krOpcodeFunc);
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
        public ushort Opcode { get; private set; }
        public readonly int headerSize;
        public readonly int packetSize;
        private readonly Func<ushort> getOpcodeFunc;

        public PacketHelper(Func<ushort> getOpcodeFunc)
        {
            this.getOpcodeFunc = getOpcodeFunc;
            headerSize = Marshal.SizeOf(typeof(HeaderStruct));
            packetSize = Marshal.SizeOf(typeof(PacketStruct));
        }


        // Tell the compiler to inline this whenever possible for performance reasons
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InitOpcode()
        {
            if (Opcode == 0)
            {
                Opcode = getOpcodeFunc();
            }
        }

        /// <summary>
        /// Construct a string representation of a packet from a byte array
        /// </summary>
        /// <param name="epoch">epoch timestamp from FFXIV_ACT_Plugin's NetworkReceivedDelegate</param>
        /// <param name="message">Message byte array received from FFXIV_ACT_Plugin's NetworkReceivedDelegate</param>
        /// <returns>null for invalid packet, otherwise a constructed packet</returns>
        public string ToString(long epoch, byte[] message)
        {
            InitOpcode();
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
            InitOpcode();
            return packet.ToString(epoch, header.ActorID);
        }

        public unsafe bool ToStructs(byte[] message, out HeaderStruct header, out PacketStruct packet)
        {
            InitOpcode();
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
