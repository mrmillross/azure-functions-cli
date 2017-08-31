﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using Azure.Functions.Cli.Common;
using Azure.Functions.Cli.NativeMethods;
using Colors.Net;
using static Colors.Net.StringStaticMethods;

namespace Azure.Functions.Cli.Helpers
{
    internal static class SecurityHelpers
    {
        private static bool CheckCurrentUser(dynamic obj)
        {
            if (obj != null && HasProperty(obj, "User"))
            {
                return $"{Environment.UserDomainName}\\{Environment.UserName}".Equals(obj.User.User.ToString(), StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        private static bool HasProperty(dynamic obj, string propertyName)
        {
            var dictionary = obj as IDictionary<string, object>;
            return dictionary != null && dictionary.ContainsKey(propertyName);
        }

        public static bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static string ReadPassword()
        {
            var password = ConsoleNativeMethods.ReadPassword();
            System.Console.WriteLine();
            return password;
        }

        internal static X509Certificate2 GetOrCreateCertificate(string certPath, string certPassword)
        {
            if (!string.IsNullOrEmpty(certPath) && !string.IsNullOrEmpty(certPassword))
            {
                certPassword = File.Exists(certPassword)
                    ? File.ReadAllText(certPassword).Trim()
                    : certPassword;
                return new X509Certificate2(certPath, certPassword);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ColoredConsole
                .WriteLine("Auto cert generation is currently not working on the .NET Core build.")
                .WriteLine("On Windows you can run:")
                .WriteLine()
                .Write(DarkCyan("PS> "))
                .WriteLine($"$cert = {Yellow("New-SelfSignedCertificate")} -Subject localhost -DnsName localhost -FriendlyName \"Functions Development\" -KeyUsage DigitalSignature -TextExtension @(\"2.5.29.37={{text}}1.3.6.1.5.5.7.3.1\")")
                .Write(DarkCyan("PS> "))
                .WriteLine($"{Yellow("Export-PfxCertificate")} -Cert $cert -FilePath certificate.pfx -Password (ConvertTo-SecureString -String {Red("<password>")} -Force -AsPlainText)")
                .WriteLine()
                .WriteLine("For more checkout https://docs.microsoft.com/en-us/aspnet/core/security/https")
                .WriteLine();
            }
            else
            {
                ColoredConsole
                .WriteLine("Auto cert generation is currently not working on the .NET Core build.")
                .WriteLine("On Unix you can run:")
                .WriteLine()
                .Write(DarkGreen("sh> "))
                .WriteLine("openssl req -new -x509 -newkey rsa:2048 -keyout localhost.key -out localhost.cer -days 365 -subj /CN=localhost")
                .Write(DarkGreen("sh> "))
                .WriteLine("openssl pkcs12 -export -out certificate.pfx -inkey localhost.key -in localhost.cer")
                .WriteLine()
                .WriteLine("For more checkout https://docs.microsoft.com/en-us/aspnet/core/security/https")
                .WriteLine();
            }

            throw new CliException("Auto cert generation is currently not working on the .NET Core build.");
        }
    }
}
