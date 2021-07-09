// **************************************************************************
//  Copyright 2021 VMware, Inc.
//  SPDX-License-Identifier: Apache-2.0
// **************************************************************************

using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using VMware.ScriptRuntimeService.Setup.ConfigFileWriters;
using VMware.ScriptRuntimeService.Setup.K8sClient;
using VMware.ScriptRuntimeService.Setup.SelfSignedCertificates;

namespace VMware.ScriptRuntimeService.Setup.SetupFlows
{
   public class InitialSetupFlow : ISetupFlow
   {
      private ILoggerFactory _loggerFactory;
      private ILogger _logger;
      public InitialSetupFlow(ILoggerFactory loggerFactory)
      {
         if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));

         _loggerFactory = loggerFactory;
         _logger = loggerFactory.CreateLogger(typeof(InitialSetupFlow));
      }

      public int Run(UserInput userInput)
      {
         try
         {
            var certificatesCommonName = userInput.ServiceHostname;

            userInput.EnsureIsValid(SetupFlowType.InitialSetup);

            // --- InMemory Certificates ---
            X509Certificate2 signCertificate = null;
            X509Certificate2 tlsCertificate = null;
            // --- InMemory Certificates ---


            K8sSettings k8sSettings = null;
            if (userInput.K8sSettings != null && File.Exists(userInput.K8sSettings))
            {
               k8sSettings = JsonConvert.DeserializeObject<K8sSettings>(File.ReadAllText(userInput.K8sSettings));
            }

            var configWriter = new K8sConfigWriter(_loggerFactory, k8sSettings);

            // --- Signing Certificate ---
            if (!string.IsNullOrEmpty(userInput.SigningCertificatePath) &&
               File.Exists(userInput.SigningCertificatePath))
            {
               _logger.LogInformation($"Load signing certificate from path {userInput.SigningCertificatePath}");
               signCertificate = new X509Certificate2(userInput.SigningCertificatePath);
            }
            else
            {
               _logger.LogInformation("Generate signing self-signed certificate");
               var signingCertGen = new SigningCertificateGenerator(
                  _loggerFactory,
                  certificatesCommonName,
                  configWriter);

               signCertificate = signingCertGen.Generate(Constants.SignCertificateSecretName);
               if (signCertificate == null)
               {
                  _logger.LogError("Generate signing self-signed certificate failed.");
                  return 3;
               }
            }
            // --- Signing Certificate ---

            // --- TLS Certificate ---
            if (!string.IsNullOrEmpty(userInput.TlsCertificatePath) &&
               File.Exists(userInput.TlsCertificatePath))
            {
               _logger.LogInformation($"Load tls certificate from path {userInput.TlsCertificatePath}");
               tlsCertificate = new X509Certificate2(userInput.TlsCertificatePath);
            }
            else
            {
               _logger.LogInformation("Generate tls self-signed certificate");
               var tlsCertGen = new TlsCertificateGenerator(
                  _loggerFactory,
                  certificatesCommonName,
                  configWriter);

               tlsCertificate = tlsCertGen.Generate(Constants.TlsCertificateSecretName);
               if (tlsCertificate == null)
               {
                  _logger.LogError("Generate tls self-signed certificate failed.");
                  return 4;
               }
            }
            // --- TLS Certificate ---

            // --- Write Admin Settings ---
            var adminWebApiSettings = new AdminWebApiSettings
            {
               AdminSettings = new AdminSettings
               {
                  ServiceHostname = userInput.ServiceHostname,
                  TlsCertificatePath = $"/app/service/settings/certs/tls/{Constants.TlsCertificateSecretName}.crt",
                  SolutionUserSigningCertificatePath = $"/app/service/settings/certs/{Constants.SignCertificateSecretName}.p12",
                  VCRegistrationConfigMap = "vcregistration-settings"
               }
            };
            configWriter.WriteSettings("admin-settings", adminWebApiSettings);
            // --- Write Admin Settings ---

         }
         catch (InvalidUserInputException exc)
         {
            _logger.LogError(exc, exc.Message);
            return 1;
         }
         catch (Exception exc)
         {
            _logger.LogError(exc, exc.Message);
            return 2;
         }

         _logger.LogInformation("Success");
         return 0;
      }
   }
}
