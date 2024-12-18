using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.ClientFramework
{
    public interface IClientFrameworkMemory : IVersionedMemory
    {
        ClientFramework GetClientFramework();
    }

    class ClientFrameworkMemoryManager : IClientFrameworkMemory
    {
        private readonly TinyIoCContainer container;
        private readonly FFXIVRepository repository;
        private IClientFrameworkMemory memory = null;

        public ClientFrameworkMemoryManager(TinyIoCContainer container)
        {
            this.container = container;
            container.Register<IClientFrameworkMemory70, ClientFrameworkMemory70>();
            repository = container.Resolve<FFXIVRepository>();

            var ffxivMemory = container.Resolve<FFXIVMemory>();
            ffxivMemory.RegisterOnProcessChangeHandler(FindMemory);
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
            List<IClientFrameworkMemory> candidates = new List<IClientFrameworkMemory>();
            candidates.Add(container.Resolve<IClientFrameworkMemory70>());
            memory = FFXIVMemory.FindCandidate(candidates, repository.GetMachinaRegion());
        }

        public bool IsValid()
        {
            if (memory == null || !memory.IsValid())
            {
                return false;
            }
            return true;
        }

        public Version GetVersion()
        {
            if (!IsValid())
                return null;
            return memory.GetVersion();
        }

        public ClientFramework GetClientFramework()
        {
            if (!IsValid())
                return null;
            return memory.GetClientFramework();
        }
    }
}
