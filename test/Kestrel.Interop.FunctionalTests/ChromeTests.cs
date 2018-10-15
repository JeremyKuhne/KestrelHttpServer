// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using static System.Environment;

namespace Microsoft.AspNetCore.Server.Kestrel.Interop.FunctionalTests
{
    [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Missing SslStream ALPN support: https://github.com/dotnet/corefx/issues/30492")]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win81,
        SkipReason = "Missing Windows ALPN support: https://en.wikipedia.org/wiki/Application-Layer_Protocol_Negotiation#Support")]
    public class ChromeTests : LoggedTest
    {
        private static readonly string _chromeExecutablePath = ResolveChromeExecutablePath();
        private static readonly string _chromeArgs = "--headless --disable-gpu --allow-insecure-localhost --enable-logging --dump-dom --virtual-time-budget=10000";

        [ConditionalTheory]
        [InlineData("", "Interop HTTP/2 GET")]
        [InlineData("?TestMethod=POST", "Interop HTTP/2 POST")]
        public async Task Http2(string requestSuffix, string expectedResponse)
        {
            var deploymentParameters = new DeploymentParameters()
            {
                ApplicationPath = Path.Combine(GetSolutionDir(), "test", "Kestrel.Interop.TestSites"),
                ServerType = ServerType.Kestrel,
                TargetFramework = Tfm.NetCoreApp22,
                RuntimeArchitecture = RuntimeArchitecture.x64
            };

            using (var deployer = new SelfHostDeployer(deploymentParameters, LoggerFactory))
            {
                var deploymentResult = await deployer.DeployAsync();
                var requestUri = $"{deploymentResult.ApplicationBaseUri.Replace("[::]", "127.0.0.1")}";
                var chromeArgs = $"{_chromeArgs} {requestUri}{requestSuffix}";
                var chromeStartInfo = new ProcessStartInfo
                {
                    FileName = _chromeExecutablePath,
                    Arguments = chromeArgs,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                };

                Logger.LogInformation($"Staring chrome: {_chromeExecutablePath} {chromeArgs}");

                var headlessChromeProcess = Process.Start(chromeStartInfo);
                var chromeOutput = await headlessChromeProcess.StandardOutput.ReadToEndAsync();

                headlessChromeProcess.WaitForExit();

                Assert.Contains(expectedResponse, chromeOutput);
            }
        }

        private static string GetSolutionDir()
        {
            for (var dir = new DirectoryInfo(Directory.GetCurrentDirectory()); dir != null; dir = dir.Parent)
            {
                if (dir.EnumerateFiles("*.sln").Any())
                {
                    return dir.FullName;
                }
            }

            throw new InvalidOperationException("Cannot find solution.");
        }

        private static string ResolveChromeExecutablePath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Path.Combine(Environment.GetFolderPath(SpecialFolder.ProgramFilesX86), "Google", "Chrome", "Application", "chrome.exe");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return Path.Combine("/usr", "bin", "google-chrome");
            }

            throw new PlatformNotSupportedException();
        }
    }
}
