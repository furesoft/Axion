﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Axion.Core;
using Axion.Core.Hierarchy;
using Axion.Core.Processing.Emitting;
using Axion.Core.Specification;
using CodeConsole;
using CodeConsole.ScriptBench;
using CommandLine;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Scripting;
using NLog;
using Module = Axion.Core.Hierarchy.Module;

namespace Axion {
    public static class Program {
        private static readonly Logger logger =
            LogManager.GetCurrentClassLogger();

        public static void Main(string[] arguments) {
            var cliParser = new Parser(
                settings => {
                    settings.EnableDashDash = true;
                    settings.CaseSensitive  = false;
                    settings.AutoHelp       = false;
                    settings.HelpWriter     = null;
                }
            );
            Directory.CreateDirectory(Compiler.OutDir);

            Console.InputEncoding   = Console.OutputEncoding = Encoding.UTF8;
            Console.ForegroundColor = ConsoleColor.White;
            PrintIntro();

            // main processing loop
            while (true) {
                if (arguments.Length > 0) {
                    cliParser.ParseArguments<CommandLineArguments>(arguments)
                             .MapResult(
                                 args => {
                                     if (args.Exit) {
                                         Environment.Exit(0);
                                     }

                                     if (args.Cls) {
                                         Console.Clear();
                                         PrintIntro();
                                         return 0;
                                     }

                                     if (args.Version) {
                                         ConsoleUtils.WriteLine(
                                             Compiler.Version
                                         );
                                         return 0;
                                     }

                                     if (args.Help) {
                                         ConsoleUtils.WriteLine(
                                             CommandLineArguments.HelpText
                                         );
                                         return 0;
                                     }

                                     if (args.EditorHelp) {
                                         ScriptBench.DrawHelpBox();
                                         return 0;
                                     }

                                     if (args.Interactive) {
                                         EnterInteractiveMode();
                                         return 0;
                                     }

                                     ProcessSources(args);
                                     return 0;
                                 },
                                 errors => {
                                     foreach (Error e in errors) {
                                         logger.Error(e.ToString());
                                     }

                                     return 0;
                                 }
                             );
                }

                // wait for next command
                string command;
                do {
                    ConsoleUtils.ClearLine();
                    command = ConsoleUtils.ReadSimple(">>> ");
                } while (string.IsNullOrWhiteSpace(command));

                arguments = Utilities.SplitLaunchArguments(command).ToArray();
            }

            // This loop breaks only by 'exit' command.
            // ReSharper disable once FunctionNeverReturns
        }

        private static void PrintIntro() {
            const string header = "Axion programming language compiler toolset";
            Console.Title = header;
            ConsoleUtils.WriteLine(
                (header + " v. ", ConsoleColor.White),
                (Compiler.Version, ConsoleColor.DarkYellow)
            );
            ConsoleUtils.WriteLine(
                ("Working in ", ConsoleColor.White),
                (Compiler.WorkDir, ConsoleColor.DarkYellow)
            );
            ConsoleUtils.WriteLine(
                "Type '-h', or '--help' to get documentation about launch arguments.\n"
            );
        }

        private static void EnterInteractiveMode() {
            logger.Info(
                "Axion code editor & interpreter mode.\n"
              + "Type 'exit' or 'quit' to exit mode, 'cls' to clear screen."
            );
            while (true) {
                // code editor header
                string rawInput = ConsoleUtils.Read("i>> ");
                string input    = rawInput.Trim().ToUpper();

                switch (input) {
                case "":
                    // skip empty commands
                    continue;
                case "EXIT":
                case "QUIT":
                    // exit from interpreter to main loop
                    logger.Info("\nInteractive interpreter closed.");
                    return;
                default:
                    // Disable logging while editing
                    LogManager.Configuration.Variables["consoleLogLevel"] =
                        "Fatal";
                    LogManager.Configuration.Variables["fileLogLevel"] =
                        "Fatal";
                    LogManager.ReconfigExistingLoggers();

                    // initialize editor
                    var editor = new ScriptBench(
                        firstCodeLine: rawInput,
                        highlighter: new AxionSyntaxHighlighter()
                    );
                    string[] codeLines = editor.Run();

                    // Re-enable logging
                    LogManager.Configuration.Variables["consoleLogLevel"] =
                        "Info";
                    LogManager.Configuration.Variables["fileLogLevel"] = "Info";
                    LogManager.ReconfigExistingLoggers();

                    if (string.IsNullOrWhiteSpace(string.Join("", codeLines))) {
                        continue;
                    }

                    // interpret as source code and output result
                    var module = Module.RawFrom(
                        new FileInfo(Compiler.GetTempSourceFilePath()).Directory!
                    );
                    module.Bind(Unit.FromLines(codeLines));
                    object? result = Compiler.Process(
                        module,
                        new ProcessingOptions(Language.CSharp)
                    );
                    if (module.HasErrors || !(result is CodeWriter codeWriter)) {
                        break;
                    }

                    try {
                        logger.Info("Interpretation:");
                        ExecuteCSharp(codeWriter.ToString());
                    }
                    catch (CompilationErrorException e) {
                        logger.Error(
                            string.Join(Environment.NewLine, e.Diagnostics)
                        );
                    }

                    break;
                }
            }
        }

        private static void ProcessSources(CommandLineArguments args) {
            ProcessingOptions? pOptions;
            if (string.IsNullOrWhiteSpace(args.Mode)) {
                pOptions = new ProcessingOptions(Mode.Reduction);
            }
            else if (Enum.TryParse(args.Mode, true, out Mode mode)) {
                pOptions = new ProcessingOptions(mode);
            }
            else {
                pOptions = new ProcessingOptions(targetLanguage: args.Mode);
            }

            Module module;
            if (args.Files.Any()) {
                int filesCount = args.Files.Count();
                if (filesCount > 1) {
                    logger.Error(
                        "Compiler doesn't support multiple files processing yet."
                    );
                    return;
                }

                var inputFiles = new FileInfo[filesCount];
                for (var i = 0; i < filesCount; i++) {
                    inputFiles[i] = new FileInfo(
                        Utilities.TrimMatchingChars(
                            args.Files.ElementAt(i),
                            '"'
                        )
                    );
                }

                try {
                    module = Module.RawFrom(inputFiles[0].Directory);
                    module.Bind(Unit.FromFile(inputFiles[0]));
                }
                catch (Exception e) {
                    logger.Error(e);
                    return;
                }
            }
            else if (!string.IsNullOrWhiteSpace(args.Code)) {
                var tempDir = new FileInfo(Compiler.GetTempSourceFilePath())
                    .Directory!;
                var unit = Unit.FromCode(
                    Utilities.TrimMatchingChars(args.Code, '"')
                );
                module = Module.RawFrom(tempDir);
                module.Bind(unit);
            }
            else {
                logger.Error(
                    "Neither code nor path to source file not specified."
                );
                return;
            }

            Compiler.Process(module, pOptions);
        }

        private static void ExecuteCSharp(string csCode) {
            if (string.IsNullOrWhiteSpace(csCode)) {
                return;
            }

            var refs = new List<MetadataReference>(
                Spec.CSharp.DefaultImports.Select(
                    asm => MetadataReference.CreateFromFile(asm.Location)
                )
            );

            // Location of the .NET assemblies
            string assemblyPath =
                Path.GetDirectoryName(typeof(object).Assembly.Location)!;

            // Adding some necessary .NET assemblies
            // These assemblies couldn't be loaded correctly via the same construction as above.
            refs.Add(
                MetadataReference.CreateFromFile(
                    Path.Combine(assemblyPath, "mscorlib.dll")
                )
            );
            refs.Add(
                MetadataReference.CreateFromFile(
                    Path.Combine(assemblyPath, "System.dll")
                )
            );
            refs.Add(
                MetadataReference.CreateFromFile(
                    Path.Combine(assemblyPath, "System.Core.dll")
                )
            );
            refs.Add(
                MetadataReference.CreateFromFile(
                    Path.Combine(assemblyPath, "System.Runtime.dll")
                )
            );
            refs.Add(
                MetadataReference.CreateFromFile(
                    typeof(Console).Assembly.Location
                )
            );
            refs.Add(
                MetadataReference.CreateFromFile(
                    Path.Combine(assemblyPath, "System.Private.CoreLib.dll")
                )
            );

            string     assemblyName = Path.GetRandomFileName();
            SyntaxTree syntaxTree   = CSharpSyntaxTree.ParseText(csCode);

            var compilation = CSharpCompilation.Create(
                assemblyName,
                new[] {
                    syntaxTree
                },
                refs,
                new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary
                )
            );

            using var  ms     = new MemoryStream();
            EmitResult result = compilation.Emit(ms);

            if (result.Success) {
                ms.Seek(0, SeekOrigin.Begin);
                Assembly assembly = Assembly.Load(ms.ToArray());

                var type = assembly.GetType("__RootModule__.__RootClass__");
                var main = type!.GetMethod("Main");

                // Let's assume that compiler anyway 'd create Main method for us :)
                main!.Invoke(
                    null,
                    new object[] {
                        new string[0]
                    }
                );
            }
            else {
                IEnumerable<Diagnostic> failures = result.Diagnostics.Where(
                    diagnostic => diagnostic.IsWarningAsError
                               || diagnostic.Severity
                               == DiagnosticSeverity.Error
                );

                foreach (Diagnostic diagnostic in failures) {
                    logger.Error($"{diagnostic.Id}: {diagnostic.GetMessage()}");
                }
            }
        }
    }
}
