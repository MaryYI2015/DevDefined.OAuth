using System.Security.Cryptography.X509Certificates;
using DevDefined.OAuth.Consumer;
using DevDefined.OAuth.Framework;

namespace XeroScreencast
{
    public static class ConsumerSessionFactory
    {
        private const string PrivateConsumerKey = "ZWZMNWNMZME2NJMWNDDINMIXOWY0NT";
        private const string PrivateConsumerSecret = "U4QVSKZF7DC8U9CKBORR77BWC5WR2X";
        private const string PrivateUserAgentString = "Xero.API.ScreenCast v1.0 (Private App Testing)";

        public static IOAuthSession CreatePrivateConsumerSession()
        {
            // Load the private certificate from disk using the password used to create it
            X509Certificate2 privateCertificate = new X509Certificate2(@"..\..\..\Certificates\public_privatekey.pfx", "passw0rd");

            // Create the consumer session
            OAuthConsumerContext consumerContext = new OAuthConsumerContext
            {
                ConsumerKey = PrivateConsumerKey,
                ConsumerSecret = PrivateConsumerSecret,
                SignatureMethod = SignatureMethod.RsaSha1,
                UseHeaderForOAuthParameters = true,
                Key = privateCertificate.PrivateKey,
                UserAgent = string.Format(PrivateUserAgentString)
            };

            return new OAuthSession(
                consumerContext,
                Settings.Default.RequestTokenURI,
                Settings.Default.AuthoriseURI,
                Settings.Default.AccessTokenURI);
        }

        public static IToken CreatePrivateAccessToken()
        {
            // Note: For private applications, the consumer key and secret are also used as the access token and secret
            return new TokenBase
            {
                Token = PrivateConsumerKey,
                TokenSecret = PrivateConsumerSecret
            };
        }

    }
}
