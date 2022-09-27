﻿namespace RainbowMage.OverlayPlugin.MemoryProcessors.Enmity
{
    class EnmityMemory60 : EnmityMemory
    {
        public const string enmitySignature = "83f9ff7412448b048e8bd3488d0d";
        private const int enmitySignatureOffset = -2608;

        public EnmityMemory60(TinyIoCContainer container)
            : base(container, enmitySignature, enmitySignatureOffset)
        { }
    }
}
