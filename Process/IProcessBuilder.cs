using System;
using System.Collections.Generic;
using Funcky.Monads;

namespace Messerli.Process
{
    public interface IProcessBuilder
    {
        IProcessBuilder AddArgument(string argument);

        IProcessBuilder AddArguments(params string[] arguments);

        IProcessBuilder AddArguments(IEnumerable<string> arguments);

        IProcessBuilder WorkingDirectory(string workingDirectory);

        IProcessBuilder RedirectStandardOutput(bool redirect = true);

        IProcessBuilder RedirectStandardError(bool redirect = true);

        IProcessBuilder RedirectOutputs(bool redirect = true);

        /// <summary>
        /// Starts the process.
        /// </summary>
        /// <returns></returns>
        System.Diagnostics.Process Run();

        /// <summary>
        /// Runs and waits for the process to exit.
        /// </summary>
        void RunAndWait(Option<IOutputForwarder> forwarder = default);

        /// <summary>
        /// Runs and waits for the process to exit.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the process exits with a non-zero exit code.</exception>
        void RunAndWaitForSuccess(Option<IOutputForwarder> forwarder = default);

        /// <summary>
        /// Runs the process and returns its standard output.
        /// </summary>
        string RunAndReturnOutput();
    }
}
