using System;
using System.Collections.Generic;
using System.Diagnostics;
#if !DEBUG
using Newtonsoft.Json;
#endif

namespace RainbowMage.OverlayPlugin.MemoryProcessors.JobGauge
{
    public enum JobGaugeJob : byte
    {
        None = 0,
        GLA = 1,
        PGL = 2,
        MRD = 3,
        LNC = 4,
        ARC = 5,
        CNJ = 6,
        THM = 7,
        CRP = 8,
        BSM = 9,
        ARM = 10,
        GSM = 11,
        LTW = 12,
        WVR = 13,
        ALC = 14,
        CUL = 15,
        MIN = 16,
        BTN = 17,
        FSH = 18,
        PLD = 19,
        MNK = 20,
        WAR = 21,
        DRG = 22,
        BRD = 23,
        WHM = 24,
        BLM = 25,
        ACN = 26,
        SMN = 27,
        SCH = 28,
        ROG = 29,
        NIN = 30,
        MCH = 31,
        DRK = 32,
        AST = 33,
        SAM = 34,
        RDM = 35,
        BLU = 36,
        GNB = 37,
        DNC = 38,
        RPR = 39,
        SGE = 40,
        VPR = 41,
        PCT = 42,
    }

    public interface IBaseJobGauge
    {
    }

    public interface IJobGauge : IEquatable<IJobGauge>
    {
        JobGaugeJob Job { get; }
        IBaseJobGauge Data { get; }
        int[] RawData { get; }
#if !DEBUG
            [JsonIgnore]
#endif
        object BaseObject { get; }
    }

    public interface IJobGaugeMemory : IVersionedMemory
    {
        IJobGauge GetJobGauge();
    }

    class JobGaugeMemoryManager : IJobGaugeMemory
    {
        private readonly TinyIoCContainer container;
        private readonly FFXIVRepository repository;
        protected readonly ILogger logger;
        private IJobGaugeMemory memory = null;

        public JobGaugeMemoryManager(TinyIoCContainer container)
        {
            this.container = container;
            container.Register<IJobGaugeMemory655, JobGaugeMemory655>();
            container.Register<IJobGaugeMemory74, JobGaugeMemory74>();
            repository = container.Resolve<FFXIVRepository>();
            logger = container.Resolve<ILogger>();

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
            List<IJobGaugeMemory> candidates = new List<IJobGaugeMemory>();
            candidates.Add(container.Resolve<IJobGaugeMemory655>());
            candidates.Add(container.Resolve<IJobGaugeMemory74>());
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

        Version IVersionedMemory.GetVersion()
        {
            if (!IsValid())
                return null;
            return memory.GetVersion();
        }

        public IJobGauge GetJobGauge()
        {
            if (!IsValid())
                return null;
            return memory.GetJobGauge();
        }
    }

}
