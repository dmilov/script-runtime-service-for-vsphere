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
using VMware.ScriptRuntimeService.Setup.K8sClient;
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
      private K8sSettings _k8sSettings;

      public VCRegistrationController(IConfiguration Configuration, ILoggerFactory loggerFactory)
      {
         _configuration = Configuration;
         _adminSettings = _configuration.
               GetSection("AdminSettings").
               Get<AdminSettings>();
         _k8sSettings = _configuration.
               GetSection("K8sSettings").
               Get<K8sSettings>();
         if (_k8sSettings.ClusterEndpoint == null)
         {
            _k8sSettings = null;
         }
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
      public ActionResult<VCRegistrationInfo> Post([FromBody] VCRegistrationInfo vcRegistrationInfo)
      {
         ActionResult<VCRegistrationInfo> result = null;

         try
         {
            _logger.LogDebug($"User Input VC: {vcRegistrationInfo.VCAddress}");
            _logger.LogDebug($"User Input VC User: {vcRegistrationInfo.UserName}");
            _logger.LogDebug($"User Input VC Thumbprint: {vcRegistrationInfo.VCTlsCertificateThumbprint}");

            var secureVcPassword = SecurePassword(vcRegistrationInfo.Password);

            var vcRegistrationSettings = new VCRegistrationSettings();
            vcRegistrationSettings.VCenterServer = vcRegistrationInfo.VCAddress;

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

            vcRegistrationSettings.SolutionOwnerId = serviceSettings.OwnerId;
            vcRegistrationSettings.SolutionServiceId = serviceSettings.ServiceId;            

            var ssoSdkUri = lookupServiceClient.GetSsoAdminEndpointUri();
            var stsUri = lookupServiceClient.GetStsEndpointUri();
            _logger.LogDebug($"Resolved SSO SDK Endpoint: {ssoSdkUri}");
            _logger.LogDebug($"Resolved Sts Endpoint: {stsUri}");

            vcRegistrationSettings.StsServiceEndpoint = stsUri.ToString();

            var ssoAdminClient = new SsoAdminClient(ssoSdkUri, stsUri, certificateValidator);

            // --- SSO Solution User Registration ---
            var ssoSolutionRegitration = new SsoSolutionUserRegistration(
               _loggerFactory,
               serviceSettings,
               ssoAdminClient);

            ssoSolutionRegitration.CreateSolutionUser(vcRegistrationInfo.UserName, secureVcPassword);

            vcRegistrationSettings.StsRealm = ssoSolutionRegitration.GetTrustedCertificate(
              vcRegistrationInfo.UserName,
              secureVcPassword)?.Thumbprint;
            // --- SSO Solution User Registration ---

            // --- Lookup Service Registration ---
            var lsRegistration = new LookupServiceRegistration(
               _loggerFactory,
               serviceSettings,
               lookupServiceClient);
            lsRegistration.Register(vcRegistrationInfo.UserName, secureVcPassword);
            // --- Lookup Service Registration ---

            var configWriter = new K8sConfigWriter(_loggerFactory, _k8sSettings);
            // --- Store VC CA certificates ---
            var trustedCertificatesStore = new TrustedCertificatesStore(
               _loggerFactory,
               ssoAdminClient,
               configWriter);
            trustedCertificatesStore.SaveVcenterCACertficates();
            // --- Store VC CA certificates ---

            // --- Save VC Registration Settings ---
            configWriter.WriteSettings(_adminSettings.VCRegistrationConfigMap, vcRegistrationSettings);
            // --- Save VC Registration Settings ---
            result = Ok();
         }
         catch (Exception exc)
         {
            result = StatusCode(500, new ErrorDetails(exc));
         }

         return result;
      }

      [HttpDelete]
      [ProducesResponseType(typeof(VCRegistrationInfo), StatusCodes.Status200OK)]
      [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
      [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status500InternalServerError)]
      public ActionResult Delete([FromBody] VCRegistrationInfo vcRegistrationInfo)
      {
         ActionResult result = Ok();
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

            var ssoSdkUri = lookupServiceClient.GetSsoAdminEndpointUri();
            var stsUri = lookupServiceClient.GetStsEndpointUri();
            _logger.LogDebug($"Resolved SSO SDK Endpoint: {ssoSdkUri}");
            _logger.LogDebug($"Resolved Sts Endpoint: {stsUri}");

            var ssoAdminClient = new SsoAdminClient(ssoSdkUri, stsUri, certificateValidator);

            var configWriter = new K8sConfigWriter(_loggerFactory, _k8sSettings);
            var vcRegistrationSettings = configWriter.ReadSettings<VCRegistrationSettings>(_adminSettings.VCRegistrationConfigMap);
            if (vcRegistrationSettings != null &&
                vcRegistrationSettings.VCenterServer == vcRegistrationInfo.VCAddress)
            {
               _logger.LogDebug($"Delete SRS Solution User: {vcRegistrationSettings.SolutionOwnerId}");               
               ssoAdminClient.DeleteLocalPrincipal(vcRegistrationInfo.UserName, secureVcPassword, vcRegistrationSettings.SolutionOwnerId);

               _logger.LogDebug("Remove Lookup Service registration");
               var lsRegistration = new LookupServiceRegistration(
                  _loggerFactory,
                  new SetupServiceSettings(vcRegistrationSettings.SolutionServiceId),
                  lookupServiceClient);
               lsRegistration.Deregister(vcRegistrationInfo.UserName, secureVcPassword);

               configWriter.DeleteSettings(_adminSettings.VCRegistrationConfigMap);

            } else
            {
               result = StatusCode(StatusCodes.Status404NotFound, new ErrorDetails(new Exception($"No SRS registration found for vCenter Server: {vcRegistrationInfo.VCAddress}")));
            }
         }
         catch (Exception exc)
         {
            result = StatusCode(500, new ErrorDetails(exc));
         }
         return result;
      }
   }
}
