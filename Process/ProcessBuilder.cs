using System;
using System.Runtime.InteropServices;

namespace Messerli.Process
{
    public static class ProcessBuilder
    {
        private static readonly string FallbackDotnetExecutable = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dotnet.exe" : "dotnet";

        /// <summary>
        /// A <see cref="IProcessBuilder"/> for the <c>dotnet</c> executable.
        /// </summary>
        /// <remarks>Respects the <c>DOTNET_HOST_PATH</c> environment variable.</remarks>
        public static IProcessBuilder Dotnet => new ProcessBuilderInternal(DotnetExecutable);

        private static string DotnetExecutable => Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? FallbackDotnetExecutable;

        /// <summary>
        /// Creates a new <see cref="IProcessBuilder"/> for the provided program.
        /// </summary>
        public static IProcessBuilder Create(string program) => new ProcessBuilderInternal(program);
    }
}
