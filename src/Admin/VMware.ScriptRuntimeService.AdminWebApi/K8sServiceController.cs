// **************************************************************************
//  Copyright 2020-2021 VMware, Inc.
//  SPDX-License-Identifier: Apache-2.0
// **************************************************************************

using System;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Logging;
using VMware.ScriptRuntimeService.Setup.K8sClient;

namespace VMware.ScriptRuntimeService.AdminWebApi
{
   /// <summary>
   /// Exposes RestartService operation throguh k8s delete pod
   /// It uses <see cref="JsonConfigurationSource "/> internally.
   /// </summary>
   public class K8sServiceController
   {      
      K8sClient _k8sClient;
      ILogger _logger;
      public K8sServiceController(ILoggerFactory loggerFactory, K8sSettings k8sSettings)
      {
         _k8sClient = new K8sClient(
            loggerFactory,
            k8sSettings?.ClusterEndpoint,
            k8sSettings?.AccessToken,
            k8sSettings?.Namespace);
         _logger = loggerFactory.CreateLogger(typeof(K8sServiceController).FullName);
         _logger.LogDebug("K8sServiceController created");
      }

      public void RestartSrsService()
      {
         try
         {
            var srsApiGatewayPod = _k8sClient.GetPod(label: "app=srs-apigateway");
            if (srsApiGatewayPod != null)
            {
               _k8sClient.DeletePod(srsApiGatewayPod);
            }
         } catch (Exception exc)
         {
            _logger.LogError($"RestartSrsService failed: {exc.ToString()}");
         }         
      }

   }
}
