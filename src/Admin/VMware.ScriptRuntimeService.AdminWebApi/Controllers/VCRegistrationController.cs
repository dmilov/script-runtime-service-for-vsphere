// **************************************************************************
//  Copyright 2020-2021 VMware, Inc.
//  SPDX-License-Identifier: Apache-2.0
// **************************************************************************

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IdentityModel.Selectors;
using System.Linq;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using VMware.ScriptRuntimeService.AdminWebApi.DataTypes;
using VMware.ScriptRuntimeService.Ls;
using VMware.ScriptRuntimeService.Setup;
using VMware.ScriptRuntimeService.Setup.ConfigFileWriters;
using VMware.ScriptRuntimeService.Setup.TlsTrustValidators;
using VMware.ScriptRuntimeService.SsoAdmin;

namespace VMware.ScriptRuntimeService.AdminWebApi.Controllers
{
   [ApiController]
   [Route("[controller]")]
   public class VCRegistrationController : ControllerBase
   {
      private readonly ILoggerFactory _loggerFactory;
      private readonly ILogger _logger;
      private IConfiguration _configuration;
      private AdminSettings _adminSettings;

      public VCRegistrationController(IConfiguration Configuration, ILoggerFactory loggerFactory)
      {
         _configuration = Configuration;
         _adminSettings = _configuration.
               GetSection("AdminSettings").
               Get<AdminSettings>();
         _loggerFactory = loggerFactory;
         _logger = _loggerFactory.CreateLogger(typeof(VCRegistrationController));
      }

      private SecureString SecurePassword(string password)
      {
         SecureString result = null;
         if (!string.IsNullOrEmpty(password))
         {
            var securePassword = new SecureString();
            foreach (char c in password)
            {
               securePassword.AppendChar(c);
            }
            result = securePassword;
         }

         return result;
      }

      [HttpPost]
      [ProducesResponseType(typeof(VCRegistrationInfo), StatusCodes.Status200OK)]      
      [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status500InternalServerError)]
      public ActionResult<VCRegistrationInfo> Post([FromBody]VCRegistrationInfo vcRegistrationInfo)
      {
         ActionResult<VCRegistrationInfo> result = null;

         try
         {
            _logger.LogDebug($"User Input VC: {vcRegistrationInfo.VCAddress}");
            _logger.LogDebug($"User Input VC User: {vcRegistrationInfo.UserName}");
            _logger.LogDebug($"User Input VC Thumbprint: {vcRegistrationInfo.VCTlsCertificateThumbprint}");

            var secureVcPassword = SecurePassword(vcRegistrationInfo.Password);

            X509CertificateValidator certificateValidator = new SpecifiedCertificateThumbprintValidator(vcRegistrationInfo.VCTlsCertificateThumbprint);            

            var lookupServiceClient = new LookupServiceClient(
               vcRegistrationInfo.VCAddress,
               certificateValidator);

            var serviceSettings = SetupServiceSettings.NewService(
                  new X509Certificate2(_adminSettings.TlsCertificatePath),
                  new X509Certificate2(_adminSettings.SolutionUserSigningCertificatePath),
                  _adminSettings.ServiceHostname,
                  443);

            _logger.LogDebug($"Service NodeId: {serviceSettings.NodeId}");
            _logger.LogDebug($"Service OwnerId: {serviceSettings.OwnerId}");
            _logger.LogDebug($"Service ServiceId: {serviceSettings.ServiceId}");
            _logger.LogDebug($"Service Endpoint Url: {serviceSettings.EndpointUrl}");

            var ssoSdkUri = lookupServiceClient.GetSsoAdminEndpointUri();
            var stsUri = lookupServiceClient.GetStsEndpointUri();
            _logger.LogDebug($"Resolved SSO SDK Endpoint: {ssoSdkUri}");
            _logger.LogDebug($"Resolved Sts Endpoint: {stsUri}");

            var ssoAdminClient = new SsoAdminClient(ssoSdkUri, stsUri, certificateValidator);


            // --- SSO Solution User Registration ---
            var ssoSolutionRegitration = new SsoSolutionUserRegistration(
               _loggerFactory,
               serviceSettings,
               ssoAdminClient);

            ssoSolutionRegitration.CreateSolutionUser(vcRegistrationInfo.UserName, secureVcPassword);
            // --- SSO Solution User Registration ---

            // --- Lookup Service Registration ---
            var lsRegistration = new LookupServiceRegistration(
               _loggerFactory,
               serviceSettings,
               lookupServiceClient);
            lsRegistration.Register(vcRegistrationInfo.UserName, secureVcPassword);
            // --- Lookup Service Registration ---

            // --- Store VC CA certificates ---
            var trustedCertificatesStore = new TrustedCertificatesStore(
               _loggerFactory,
               ssoAdminClient,
               new K8sConfigWriter(_loggerFactory, null));
            trustedCertificatesStore.SaveVcenterCACertficates();
            // --- Store VC CA certificates ---
         }
         catch (Exception exc)
         {
            result = StatusCode(500, new ErrorDetails(exc));
         }

         return result;
      }
   }
}
