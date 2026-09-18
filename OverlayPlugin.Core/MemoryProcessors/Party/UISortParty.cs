using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using FFXIVClientStructs.Global.FFXIV.Client.UI;
using Newtonsoft.Json.Linq;
using RainbowMage.OverlayPlugin.MemoryProcessors.ContentFinderSettings;
using RainbowMage.OverlayPlugin.MemoryProcessors.Party;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.UISortParty
{


    public class UISortParty

    {

        [StructLayout(LayoutKind.Explicit, Size = 0xA8)]
        public unsafe struct PartyRoleListStruct
        {

            [FieldOffset(0x48)] public fixed short tankOrder[4];
            [FieldOffset(0x68)] public fixed short healerOrder[4];
            [FieldOffset(0x88)] public fixed short dpsOrder[14];
        };
        private IntPtr PartyRoleListModule = IntPtr.Zero;
        private const string GetUIModulePtrSignature = "E8????????807B1D01";
        private const string FrameworkPSignature = "498BDC48891D????????";
        TinyIoCContainer tinyIoC;
        private FFXIVMemory memory;
        private ILogger logger;
        public PartyRoleListStruct party;
        public unsafe UISortParty(TinyIoCContainer container)
        {
            tinyIoC = container;
            memory = container.Resolve<FFXIVMemory>();
            logger = container.Resolve<ILogger>();
            memory.RegisterOnProcessChangeHandler(FindMemory);

        }

        private void FindMemory(object sender, Process e)
        {
            memory = null;
            if (e == null)
            {
                return;
            }
            ScanPointers();

        }

        public unsafe SortParty[] GetUISortedPartyList()
        {
            var sort = new SortParty[31];
            for (int i = 0; i < sort.Length; i++)
            {
                sort[i] = new SortParty();
            }


            for (int i = 0; i < 4; i++)
            {
                sort[i].order = i;
                sort[i].classJob = party.tankOrder[i];
                if (sort[i].classJob == 1)
                {
                    //骑士
                    sort[22].order = i;
                    sort[22].classJob = 19;
                }
                if (sort[i].classJob == 3)
                {
                    //战士
                    sort[23].order = i;
                    sort[23].classJob = 21;
                }

            }
            for (int i = 4; i < 8; i++)
            {

                sort[i].order = i;
                sort[i].classJob = party.healerOrder[i - 4];
                if (sort[i].classJob == 6)
                {
                    //白魔
                    sort[24].order = i;
                    sort[24].classJob = 24;
                }
            }
            for (int i = 8; i < 22; i++)
            {
                sort[i].order = i;
                sort[i].classJob = party.healerOrder[i - 8];
                if (sort[i].classJob == 2)
                {
                    //武僧
                    sort[25].order = i;
                    sort[25].classJob = 20;
                }
                if (sort[i].classJob == 4)
                {
                    //龙骑
                    sort[26].order = i;
                    sort[26].classJob = 22;
                }
                if (sort[i].classJob == 29)
                {
                    //忍者
                    sort[27].order = i;
                    sort[27].classJob = 30;
                }
                if (sort[i].classJob == 5)
                {
                    //诗人
                    sort[28].order = i;
                    sort[28].classJob = 23;
                }
                if (sort[i].classJob == 7)
                {
                    //黑魔
                    sort[29].order = i;
                    sort[29].classJob = 25;
                }
                if (sort[i].classJob == 26)
                {
                    //召唤
                    sort[30].order = i;
                    sort[30].classJob = 27;
                }
            }
            return sort;
        }
        private unsafe PartyRoleListStruct GetPartyRoleList()
        {
            var rawData = memory.GetByteArray(PartyRoleListModule, sizeof(PartyRoleListStruct));
            fixed (byte* buffer = rawData)
            {
                return (PartyRoleListStruct)Marshal.PtrToStructure(new IntPtr(buffer), typeof(PartyRoleListStruct));
            }
        }


        public void ScanPointers()
        {
            memory = tinyIoC.Resolve<FFXIVMemory>();
            List<string> fail = new List<string>();
            var list = memory.SigScan(FrameworkPSignature, -4, true, 0);
            IntPtr FrameworkPtr = IntPtr.Zero;
            list = memory.SigScan(FrameworkPSignature, -4, true, 0);

            if (list != null && list.Count > 0)
            {
                FrameworkPtr = list[0];
                var GetUIModuleOffsetPtr = memory.SigScan(GetUIModulePtrSignature, -8, true, 11)[0];
                var GetUIModuleOffset = memory.Read32(GetUIModuleOffsetPtr, 1)[0];
                var uiMoudle = memory.ReadIntPtr(memory.ReadIntPtr(FrameworkPtr) + GetUIModuleOffset);
                //FFXIVClientStructs.FFXIV.Client.UI.Misc.PartyRoleListModule* GetPartyRoleListModule()
                var GetPartyRoleListModule = memory.ReadIntPtr(memory.ReadIntPtr(uiMoudle) + 8 * 69) + 3;
                var GetPartyRoleListModuleOffset = memory.Read32(GetPartyRoleListModule, 1)[0];
                var agent = IntPtr.Add(uiMoudle, GetPartyRoleListModuleOffset);
                PartyRoleListModule = agent;
                party = GetPartyRoleList();
            }
            else
            {
                FrameworkPtr = IntPtr.Zero;
                fail.Add(nameof(FrameworkPtr));
            }

            logger.Log(LogLevel.Debug, "FrameworkPtr: 0x{0:X}", FrameworkPtr.ToInt64()); ;
        }
        private bool HasValidPointers()
        {
            if (PartyRoleListModule == IntPtr.Zero)
                return false;
            return true;
        }
        public bool IsValid()
        {
            if (!memory.IsValid())
                return false;

            if (!HasValidPointers())
                return false;

            return true; ;
        }
    }
}
