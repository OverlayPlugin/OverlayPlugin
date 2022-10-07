using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Permissions;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.Aggro
{
    public interface IAggroMemory
    {
        List<AggroEntry> GetAggroList(List<Combatant.Combatant> combatantList);

        void ScanPointers();
        bool IsValid();
    }

    public class AggroMemoryManager : IAggroMemory
    {
        private readonly TinyIoCContainer container;
        private readonly FFXIVRepository repository;
        private IAggroMemory memory = null;

        public AggroMemoryManager(TinyIoCContainer container)
        {
            this.container = container;
            container.Register<IAggroMemory60, AggroMemory60>();
            repository = container.Resolve<FFXIVRepository>();

            var memory = container.Resolve<FFXIVMemory>();
            memory.RegisterOnProcessChangeHandler(FindMemory);
        }

        private void FindMemory(object sender, Process p)
        {
            memory = null;
            if (p == null)
            {
                return;
            }
            ScanPointers();
        }

        public void ScanPointers()
        {
            List<IAggroMemory> candidates = new List<IAggroMemory>();
            candidates.Add(container.Resolve<IAggroMemory60>());

            foreach (var c in candidates)
            {
                c.ScanPointers();
                if (c.IsValid())
                {
                    memory = c;
                    break;
                }
            }
        }

        public bool IsValid()
        {
            return memory != null && memory.IsValid();
        }


        public List<AggroEntry> GetAggroList(List<Combatant.Combatant> combatantList)
        {
            if (!IsValid())
            {
                return null;
            }
            return memory.GetAggroList(combatantList);
        }
    }
}
