using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using DevDefined.OAuth.Framework;

namespace DevDefined.OAuth.Consumer
{
    [Obsolete("Use the normal ConsumerRequest instead", true)]
    public class ClientCertEnabledConsumerRequest : ConsumerRequest
    {
        private readonly ICertificateFactory _certificateFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientCertEnabledConsumerRequest"/> class.
        /// </summary>
        /// <param name="certificateFactory">The certificate factory.</param>
        /// <param name="context">The context.</param>
        /// <param name="consumerContext">The consumer context.</param>
        /// <param name="token">The token.</param>
        public ClientCertEnabledConsumerRequest(ICertificateFactory certificateFactory, IOAuthContext context, IOAuthConsumerContext consumerContext, IToken token)
            : base(context, consumerContext, token)
        {
            _certificateFactory = certificateFactory;
        }

        /// <summary>
        /// Converts the current ConsumerRequest to an HttpWebRequest
        /// </summary>
        /// <returns>Return an HttpWebRequest with a client certificate attached.</returns>
        public override HttpWebRequest ToWebRequest()
        {
            var webReqeust = base.ToWebRequest();

            X509Certificate2 certificate = _certificateFactory.CreateCertificate();

            // Attach the certificate to the HttpWebRequest
            if (certificate != null)
            {
                webReqeust.ClientCertificates.Add(certificate);
            }
            
            return webReqeust;
        }

    }
}