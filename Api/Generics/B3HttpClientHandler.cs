using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Api.Generics
{
    public class B3HttpClientHandler : HttpClientHandler
    {
        public B3HttpClientHandler(string p12FileLocation, string certificatePassword)
        {
            ClientCertificateOptions = ClientCertificateOption.Manual;
            SslProtocols = SslProtocols.Tls12;
            ClientCertificates.Add(new X509Certificate2(p12FileLocation, certificatePassword));
            ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) => { return true; };
        }
    }
}
