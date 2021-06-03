// **************************************************************************
//  Copyright 2020-2021 VMware, Inc.
//  SPDX-License-Identifier: Apache-2.0
// **************************************************************************

namespace VMware.ScriptRuntimeService.AdminWebApi
{
   public class VCRegistrationSettings
   {
      public string VCenterServer { get; set; }

      public string StsRealm { get; set; }

      public string StsServiceEndpoint { get; set; }

      public string SolutionServiceId { get; set; }

      public string SolutionOwnerId { get; set; }      
   }
}
