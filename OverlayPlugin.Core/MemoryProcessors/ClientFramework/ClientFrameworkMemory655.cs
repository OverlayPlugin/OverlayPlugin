using System;
using System.Runtime.InteropServices;
using RainbowMage.OverlayPlugin.MemoryProcessors.FFXIVClientStructs;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.ClientFramework
{
    interface IClientFrameworkMemory655 : IClientFrameworkMemory { }

    // Note: The struct size/offsets and `clientFrameworkSignature` are based on
    // older versions of FFXIVClientStructs. These can't be validated against
    // the current global client, but they also appear to have been stable for
    // multiple releases prior to 7.0.  Including them here so there is at least
    // some support for ko/cn clients until they are on 7.0.
    partial class ClientFrameworkMemory655 : ClientFrameworkMemory, IClientFrameworkMemory655
    {
        private const string clientFrameworkSignature = "440FB6C0488B0D????????";
        private const int clientFrameworkSignatureOffset = -4;

        public ClientFrameworkMemory655(TinyIoCContainer container) : base(container, clientFrameworkSignature, clientFrameworkSignatureOffset) { }

        public override Version GetVersion()
        {
            return new Version(6, 5, 5);
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
