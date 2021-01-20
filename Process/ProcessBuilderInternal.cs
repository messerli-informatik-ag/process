using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using Funcky.Extensions;
using Funcky.Monads;
using static Funcky.Functional;

namespace Messerli.Process
{
    public sealed class ProcessBuilderInternal : IProcessBuilder
    {
        private readonly string _program;
        private readonly IImmutableList<string> _arguments = ImmutableList<string>.Empty;
        private readonly Option<string> _workingDirectory;
        private readonly Option<bool> _redirectStandardOutput;
        private readonly Option<bool> _redirectStandardError;
        private readonly Option<IOutputForwarder> _outputForwarder;

        public ProcessBuilderInternal(string program)
        {
            _program = program;
        }

        private ProcessBuilderInternal(
            string program,
            IImmutableList<string> arguments,
            Option<string> workingDirectory,
            Option<bool> redirectStandardOutput,
            Option<bool> redirectStandardError,
            Option<IOutputForwarder> outputForwarder)
        {
            _program = program;
            _arguments = arguments;
            _workingDirectory = workingDirectory;
            _redirectStandardOutput = redirectStandardOutput;
            _redirectStandardError = redirectStandardError;
            _outputForwarder = outputForwarder;
        }

        [Pure]
        public IProcessBuilder AddArgument(string argument)
            => ShallowClone(arguments: Option.Some(_arguments.Add(argument)));

        [Pure]
        public IProcessBuilder AddArguments(params string[] arguments)
            => ShallowClone(arguments: Option.Some(_arguments.AddRange(arguments)));

        [Pure]
        public IProcessBuilder AddArguments(IEnumerable<string> arguments)
            => ShallowClone(arguments: Option.Some(_arguments.AddRange(arguments)));

        [Pure]
        public IProcessBuilder WorkingDirectory(string workingDirectory)
            => ShallowClone(workingDirectory: Option.Some(workingDirectory));

        public IProcessBuilder RedirectStandardOutput(bool redirect = true)
            => ShallowClone(redirectStandardOutput: Option.Some(redirect));

        [Pure]
        public IProcessBuilder RedirectStandardError(bool redirect = true)
            => ShallowClone(redirectStandardError: Option.Some(redirect));

        [Pure]
        public IProcessBuilder RedirectOutputs(bool redirect = true)
            => RedirectStandardError(redirect).RedirectStandardOutput(redirect);

        public IProcessBuilder OutputForwarder(IOutputForwarder outputForwarder)
            => ShallowClone(outputForwarder: Option.Some(outputForwarder));

        public System.Diagnostics.Process Run()
            => System.Diagnostics.Process.Start(CreateProcessStartInfo())!;

        public void RunAndWait(Option<IOutputForwarder> forwarder = default)
            => RunAndWait(forwarder, NoOperation);

        public void RunAndWaitForSuccess(Option<IOutputForwarder> forwarder = default)
            => RunAndWait(forwarder, ValidateExitCode);

        public string RunAndReturnOutput()
        {
            using var process = RedirectStandardOutput().Run();
            process.WaitForExit();
            ValidateExitCode(process);
            return process.StandardOutput.ReadToEnd();
        }

        public override string ToString()
            => $"{_program} {string.Join(" ", _arguments)}";

        private void RunAndWait(Option<IOutputForwarder> forwarder, Action<System.Diagnostics.Process> onExited)
        {
            using var process = forwarder.OrElse(_outputForwarder).Match(none: false, some: True)
                ? RedirectOutputs().Run()
                : Run();

            forwarder.AndThen(f => ForwardOutput(process, f));
            process.WaitForExit();
            onExited(process);
        }

        private ProcessStartInfo CreateProcessStartInfo()
        {
            var startInfo = new ProcessStartInfo(_program);
            _arguments.ForEach(startInfo.ArgumentList.Add);

            _workingDirectory.AndThen(directory => startInfo.WorkingDirectory = directory);

            startInfo.RedirectStandardOutput = _redirectStandardOutput.GetOrElse(false);
            startInfo.RedirectStandardError = _redirectStandardError.GetOrElse(false);

            // The default is true on .NET Framework apps and false on .NET Core apps.
            startInfo.UseShellExecute = false;

            return startInfo;
        }

        private static void ForwardOutput(System.Diagnostics.Process process, IOutputForwarder forwarder)
        {
            while (!process.StandardOutput.EndOfStream || !process.StandardError.EndOfStream)
            {
                if (process.StandardOutput.ReadLine() is { } standardOutputLine)
                {
                    forwarder.WriteOutputLine(standardOutputLine);
                }

                if (process.StandardError.ReadLine() is { } standardErrorLine)
                {
                    forwarder.WriteErrorLine(standardErrorLine);
                }
            }
        }

        private void ValidateExitCode(System.Diagnostics.Process process)
        {
            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"Process '{ToString()}' exited with non-zero exit code: {process.ExitCode}");
            }
        }

        private ProcessBuilderInternal ShallowClone(
            Option<IImmutableList<string>> arguments = default,
            Option<string> workingDirectory = default,
            Option<bool> redirectStandardOutput = default,
            Option<bool> redirectStandardError = default,
            Option<IOutputForwarder> outputForwarder = default)
            => new ProcessBuilderInternal(
                program: _program,
                arguments: arguments.GetOrElse(_arguments),
                workingDirectory: workingDirectory.OrElse(_workingDirectory),
                redirectStandardOutput: redirectStandardOutput.OrElse(_redirectStandardOutput),
                redirectStandardError: redirectStandardError.OrElse(_redirectStandardError),
                outputForwarder: outputForwarder.OrElse(_outputForwarder));
    }
}
