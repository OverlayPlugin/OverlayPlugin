using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.ClientFramework
{
    public enum ClientLang
    {
        // global client
        Japanese = 0,
        English = 1,
        German = 2,
        French = 3,
        // cn client
        Chinese = 4,
        // ko client
        Korean = 6,
        // We don't know what these are yet; treat as unknown for now.
        Unknown5 = 5,
        Unknown7 = 7,
        Unknown8 = 8,
        Unknown9 = 9,
        Unknown10 = 10,
        Unknown11 = 11,
        Unknown12 = 12,
        Unknown13 = 13,
        Unknown14 = 14,
        Unknown15 = 15
    }

    public class ClientFramework
    {
        public bool foundLanguage = false;
        public ClientLang clientLanguage;
    }

    public abstract class ClientFrameworkMemory
    {
        protected FFXIVMemory memory;
        protected ILogger logger;

        protected IntPtr clientFrameworkInstanceAddress = IntPtr.Zero;
        protected IntPtr clientFrameworkAddress = IntPtr.Zero;

        private readonly string clientFrameworkSignature;
        private readonly int clientFrameworkSignatureOffset;

        public ClientFrameworkMemory(TinyIoCContainer container, string clientFrameworkSignature, int clientFrameworkSignatureOffset)
        {
            this.clientFrameworkSignature = clientFrameworkSignature;
            this.clientFrameworkSignatureOffset = clientFrameworkSignatureOffset;
            logger = container.Resolve<ILogger>();
            memory = container.Resolve<FFXIVMemory>();
        }

        private void ResetPointers()
        {
            clientFrameworkInstanceAddress = IntPtr.Zero;
            clientFrameworkAddress = IntPtr.Zero;
        }

        private bool HasValidPointers()
        {
            if (clientFrameworkInstanceAddress == IntPtr.Zero)
                return false;
            if (clientFrameworkAddress == IntPtr.Zero)
                return false;
            return true;
        }

        public bool IsValid()
        {
            if (!memory.IsValid())
                return false;

            if (!HasValidPointers())
                return false;
            return true;
        }

        public virtual void ScanPointers()
        {
            ResetPointers();
            if (!memory.IsValid())
                return;

            List<string> fail = new List<string>();

            List<IntPtr> list = memory.SigScan(clientFrameworkSignature, clientFrameworkSignatureOffset, true);

            if (list != null && list.Count == 1)
            {
                clientFrameworkInstanceAddress = list[0];
            }
            else
            {
                clientFrameworkInstanceAddress = IntPtr.Zero;
                fail.Add(nameof(clientFrameworkInstanceAddress));
            }
            logger.Log(LogLevel.Debug, "clientFrameworkInstanceAddress: 0x{0:X}", clientFrameworkInstanceAddress.ToInt64());

            if (clientFrameworkInstanceAddress != IntPtr.Zero)
            {
                clientFrameworkAddress = memory.ReadIntPtr(clientFrameworkInstanceAddress);
            }
            else
            {
                clientFrameworkAddress = IntPtr.Zero;
                fail.Add(nameof(clientFrameworkAddress));
            }
            logger.Log(LogLevel.Debug, "clientFrameworkAddress: 0x{0:X}", clientFrameworkAddress.ToInt64());

            if (fail.Count == 0)
            {
                logger.Log(LogLevel.Info, $"Found client framework memory via {GetType().Name}.");
                return;
            }

            logger.Log(LogLevel.Error, $"Failed to find client framework memory via {GetType().Name}: {string.Join(", ", fail)}.");
            return;
        }

        public abstract Version GetVersion();
    }
}
