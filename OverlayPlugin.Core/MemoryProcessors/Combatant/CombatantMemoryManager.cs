using System;
using System.Collections.Generic;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.Combatant
{
    public interface ICombatantMemory
    {
        bool IsValid();
        Combatant GetSelfCombatant();
        Combatant GetCombatantFromAddress(IntPtr address, uint selfCharID);
        List<Combatant> GetCombatantList();
    }

    public class CombatantMemoryManager : ICombatantMemory
    {
        private readonly TinyIoCContainer container;
        private readonly FFXIVRepository repository;
        private ICombatantMemory memory = null;

        public CombatantMemoryManager(TinyIoCContainer container)
        {
            this.container = container;
            container.Register<ICombatantMemory61, CombatantMemory61>();
            container.Register<ICombatantMemory62, CombatantMemory62>();
            repository = container.Resolve<FFXIVRepository>();
        }

        private void FindMemory()
        {
            List<ICombatantMemory> candidates = new List<ICombatantMemory>();
            // For CN/KR, try the lang-specific candidate first, then fall back to intl
            if (
                repository.GetMachinaRegion() == GameRegion.Chinese ||
                repository.GetMachinaRegion() == GameRegion.Korean)
            {
                candidates.Add(container.Resolve<ICombatantMemory61>());
            }
            candidates.Add(container.Resolve<ICombatantMemory62>());

            foreach (var c in candidates)
            {
                if (c.IsValid())
                {
                    memory = c;
                    break;
                }
            }
        }

        public bool IsValid()
        {
            if (memory == null)
            {
                FindMemory();
            }
            if (memory == null || !memory.IsValid())
            {
                return false;
            }
            return true;
        }

        public Combatant GetCombatantFromAddress(IntPtr address, uint selfCharID)
        {
            if (!IsValid())
            {
                return null;
            }
            return memory.GetCombatantFromAddress(address, selfCharID);
        }

        public List<Combatant> GetCombatantList()
        {
            if (!IsValid())
            {
                return new List<Combatant>();
            }
            return memory.GetCombatantList();
        }

        public Combatant GetSelfCombatant()
        {
            if (!IsValid())
            {
                return null;
            }
            return memory.GetSelfCombatant();
        }
    }
}
