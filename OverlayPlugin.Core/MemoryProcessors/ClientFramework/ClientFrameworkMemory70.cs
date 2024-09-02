using System;
using System.Runtime.InteropServices;
using RainbowMage.OverlayPlugin.MemoryProcessors.FFXIVClientStructs;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.ClientFramework
{
    interface IClientFrameworkMemory70 : IClientFrameworkMemory { }

    partial class ClientFrameworkMemory70 : ClientFrameworkMemory, IClientFrameworkMemory70
    {
        private const string clientFrameworkSignature = "498BDC48891D????????";
        private const int clientFrameworkSignatureOffset = -4;

        public ClientFrameworkMemory70(TinyIoCContainer container)
            : base(container, clientFrameworkSignature, clientFrameworkSignatureOffset)
        { }

        public override Version GetVersion()
        {
            return new Version(7, 0);
        }

        public unsafe ClientFramework GetClientFramework()
        {
            var ret = new ClientFramework();
            if (!IsValid())
            {
                return ret;
            }

            if (clientFrameworkAddress == IntPtr.Zero)
            {
                return ret;
            }

            try
            {
                var clientFramework = ManagedType<Framework>.GetManagedTypeFromIntPtr(clientFrameworkAddress, memory).ToType();
                ret.clientLanguage = (ClientLang)clientFramework.ClientLanguage;
                ret.foundLanguage = true;
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, "Failed to read client language: {0}", ex);
            }
            return ret;
        }

    }
}
