// **************************************************************************
//  Copyright 2020-2021 VMware, Inc.
//  SPDX-License-Identifier: Apache-2.0
// **************************************************************************


using System.Runtime.Serialization;

namespace VMware.ScriptRuntimeService.AdminWebApi.DataTypes
{
   [DataContract(Name = "vc_registration_info")]
   public class VCRegistrationInfo
   {
      [DataMember(Name = "vc_address")]
      public string VCAddress { get; set; }
      [DataMember(Name = "vc_tls_certificate_thumbprint")]
      public string VCTlsCertificateThumbprint { get; set; }
      [DataMember(Name = "username")]
      public string UserName { get; set; }
      [DataMember(Name = "password")]
      public string Password { get; set; }
   }
}
