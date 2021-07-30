using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace VMware.ScriptRuntimeService.AdminWebUI.Controllers
{
   [ApiController]
   [Route("[controller]")]
   public class RegisterVcController : ControllerBase
   {
      private readonly ILogger<RegisterVcController> _logger;

      public RegisterVcController(ILogger<RegisterVcController> logger)
      {
         _logger = logger;
      }

      [HttpGet]
      [ProducesResponseType(typeof(string), 200)]
      public JsonResult Register([FromQuery] string serverAddress, [FromQuery] string username, [FromQuery] string password, [FromQuery] string thumbprint)
      {
         string result = string.Empty;

         try
         {
            var tcpClient = new System.Net.Sockets.TcpClient();
            var iar = tcpClient.BeginConnect(serverAddress, 443, null, null);
            var wait = iar.AsyncWaitHandle.WaitOne(3000, false);

            if (!wait)
            {
               tcpClient.Close();
            }
            else
            {
               tcpClient.EndConnect(iar);

               if (tcpClient.Connected)
               {
                  var tcpStream = tcpClient.GetStream();
                  System.Net.Security.RemoteCertificateValidationCallback p = (a, b, c, d) => { return true; };
                  var sslStream = new System.Net.Security.SslStream(tcpStream, false, p);
                  sslStream.AuthenticateAsClient(serverAddress, null, SslProtocols.Tls12 | SslProtocols.Tls13, false);
                  var certificate = new System.Security.Cryptography.X509Certificates.X509Certificate2(sslStream.RemoteCertificate);

                  result = certificate.Thumbprint;

                  certificate.Dispose();
                  sslStream.Close();
                  sslStream.Dispose();
                  tcpStream.Close();
                  tcpStream.Dispose();
               }

               tcpClient.Close();
            }
         }
         catch
         {
            result = "Error";
         }
        

         return new JsonResult( result);
      }
   }
}
