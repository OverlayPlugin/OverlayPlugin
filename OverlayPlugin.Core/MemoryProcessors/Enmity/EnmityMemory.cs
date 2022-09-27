﻿using RainbowMage.OverlayPlugin.MemoryProcessors.Combatant;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.Enmity
{
    public abstract class EnmityMemory : IEnmityMemory {
        private FFXIVMemory memory;
        private ILogger logger;
        private uint loggedScanErrors = 0;
        private CombatantMemoryManager combatantMemory;

        private IntPtr enmityAddress = IntPtr.Zero;

        private string enmitySignature;
        private int enmitySignatureOffset;

        public EnmityMemory(TinyIoCContainer container, string enmitySignature, int enmitySignatureOffset)
        {
            this.enmitySignature = enmitySignature;
            this.enmitySignatureOffset = enmitySignatureOffset;
            memory = new FFXIVMemory(container);
            memory.OnProcessChange += ResetPointers;
            logger = container.Resolve<ILogger>();
            combatantMemory = container.Resolve<MemoryProcessors.Combatant.CombatantMemoryManager>();
            GetPointerAddress();
        }

        private void ResetPointers(object sender, EventArgs _)
        {
            enmityAddress = IntPtr.Zero;
        }

        private bool HasValidPointers()
        {
            if (enmityAddress == IntPtr.Zero)
                return false;
            return true;
        }

        public bool IsValid()
        {
            if (!memory.IsValid())
                return false;

            if (!HasValidPointers())
                GetPointerAddress();

            if (!HasValidPointers())
                return false;

            return true;
        }

        private bool GetPointerAddress()
        {
            if (!memory.IsValid())
                return false;

            bool success = true;

            List<string> fail = new List<string>();

            List<IntPtr> list = memory.SigScan(enmitySignature, 0, true);
            if (list != null && list.Count > 0)
            {
                enmityAddress = IntPtr.Add(list[0], enmitySignatureOffset);
            }
            else
            {
                enmityAddress = IntPtr.Zero;
                fail.Add(nameof(enmityAddress));
                success = false;
            }

            logger.Log(LogLevel.Debug, "enmityAddress: 0x{0:X}", enmityAddress.ToInt64());

            if (!success)
            {
                if (loggedScanErrors < 10)
                {
                    logger.Log(LogLevel.Error, $"Failed to find enmity memory via {GetType().Name}: {string.Join(", ", fail)}.");
                    loggedScanErrors++;

                    if (loggedScanErrors == 10)
                    {
                        logger.Log(LogLevel.Error, "Further enmity memory errors won't be logged.");
                    }
                }
            }
            else
            {
                logger.Log(LogLevel.Info, $"Found enmity memory via {GetType().Name}.");
                loggedScanErrors = 0;
            }

            return success;
        }

        [StructLayout(LayoutKind.Explicit, Size = Size)]
        struct MemoryEnmityListEntry
        {
            public const int Size = 8;

            [FieldOffset(0x00)]
            public uint ID;

            [FieldOffset(0x04)]
            public uint Enmity;
        }

        [StructLayout(LayoutKind.Explicit)]
        unsafe struct MemoryEnmityList
        {
            public const int MaxEntries = 32;
            public static int Size => Marshal.SizeOf(typeof(MemoryEnmityList));

            [FieldOffset(0x00)]
            public fixed byte EntryBuffer[MaxEntries];

            [FieldOffset(0x100)]
            public short Count;

            public MemoryEnmityListEntry this[int index]
            {
                get
                {
                    if (index >= Count)
                    {
                        return new MemoryEnmityListEntry();
                    }
                    fixed (byte* p = EntryBuffer)
                    {
                        return *(MemoryEnmityListEntry*)&p[index * MemoryEnmityListEntry.Size];
                    }
                }
            }
        }

        private unsafe MemoryEnmityList ReadEnmityList()
        {
            byte[] source = memory.GetByteArray(enmityAddress, MemoryEnmityList.Size);
            fixed (byte* p = source)
            {
                return *(MemoryEnmityList*)&p[0];
            }
        }

        public List<EnmityEntry> GetEnmityEntryList(List<Combatant.Combatant> combatantList)
        {
            if (!IsValid() || !combatantMemory.IsValid())
            {
                return new List<EnmityEntry>();
            }

            var mychar = combatantMemory.GetSelfCombatant();

            uint topEnmity = 0;
            var result = new List<EnmityEntry>();

            MemoryEnmityList list = ReadEnmityList();
            for (int i = 0; i < list.Count; i++)
            {
                MemoryEnmityListEntry e = list[i];
                topEnmity = Math.Max(topEnmity, e.Enmity);

                Combatant.Combatant c = null;
                if (e.ID > 0)
                {
                    c = combatantList.Find(x => x.ID == e.ID);
                }

                var entry = new EnmityEntry()
                {
                    ID = e.ID,
                    Enmity = e.Enmity,
                    isMe = e.ID == mychar.ID,
                    Name = c == null ? "Unknown" : c.Name,
                    OwnerID = c == null ? 0 : c.OwnerID,
                    HateRate = (int)(((double)e.Enmity / (double)topEnmity) * 100),
                    Job = c == null ? (byte)0 : c.Job,
                };

                result.Add(entry);
            }
            return result;
        }

    }
}
