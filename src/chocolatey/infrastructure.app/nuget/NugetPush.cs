﻿namespace chocolatey.infrastructure.app.nuget
{
    using System;
    using System.IO;
    using NuGet;
    using configuration;
    using logging;

    public class NugetPush
    {
        public static void push_package(ChocolateyConfiguration config, string nupkgFilePath)
        {
            var timeout = TimeSpan.FromSeconds(Math.Abs(config.PushCommand.TimeoutInSeconds));
            if (timeout.Seconds <= 0)
            {
                timeout = TimeSpan.FromMinutes(5); // Default to 5 minutes
            }
            const bool disableBuffering = false;

            var packageServer = new PackageServer(config.Source, ApplicationParameters.UserAgent);
            packageServer.SendingRequest += (sender, e) => { if (config.Verbose) "chocolatey".Log().Info(ChocolateyLoggers.Verbose, "{0} {1}".format_with(e.Request.Method, e.Request.RequestUri)); };

            var package = new OptimizedZipPackage(nupkgFilePath);

            try
            {
                packageServer.PushPackage(
                    config.PushCommand.Key,
                    package,
                    new FileInfo(nupkgFilePath).Length,
                    Convert.ToInt32(timeout.TotalMilliseconds),
                    disableBuffering);
            }
            catch (InvalidOperationException ex)
            {
                var message = ex.Message;
                if (!string.IsNullOrWhiteSpace(message) && message.Contains("(500) Internal Server Error"))
                {
                    throw new ApplicationException("There was an internal server error, which might mean the package already exists on a Simple OData Server.",ex);
                }

                throw;
            }


            "chocolatey".Log().Info(ChocolateyLoggers.Important, () => "{0} was pushed successfully to {1}".format_with(package.GetFullName(), config.Source));
        }
    }
}