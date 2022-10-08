using System.Collections.Generic;
using System.Diagnostics;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.EnmityHud
{
    public interface IEnmityHudMemory
    {
        List<EnmityHudEntry> GetEnmityHudEntries();

        void ScanPointers();
        bool IsValid();
    }

    public class EnmityHudMemoryManager : IEnmityHudMemory
    {
        private readonly TinyIoCContainer container;
        private readonly FFXIVRepository repository;
        private IEnmityHudMemory memory = null;

        public EnmityHudMemoryManager(TinyIoCContainer container)
        {
            this.container = container;
            container.Register<IEnmityHudMemory60, EnmityHudMemory60>();
            container.Register<IEnmityHudMemory62, EnmityHudMemory62>();
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
            List<IEnmityHudMemory> candidates = new List<IEnmityHudMemory>();
            // For CN/KR, try the lang-specific candidate first, then fall back to intl
            if (
                repository.GetMachinaRegion() == GameRegion.Chinese ||
                repository.GetMachinaRegion() == GameRegion.Korean)
            {
                candidates.Add(container.Resolve<IEnmityHudMemory60>());
            }
            candidates.Add(container.Resolve<IEnmityHudMemory62>());

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
            if (memory == null || !memory.IsValid())
            {
                return false;
            }
            return true;
        }

        public List<EnmityHudEntry> GetEnmityHudEntries()
        {
            if (!IsValid())
            {
                return null;
            }
            return memory.GetEnmityHudEntries();
        }
    }
}
