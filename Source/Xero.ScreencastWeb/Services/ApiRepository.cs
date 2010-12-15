using System;
using System.Configuration;
using System.Diagnostics;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Web;
using System.Web.SessionState;

using DevDefined.OAuth.Consumer;
using DevDefined.OAuth.Framework;
using DevDefined.OAuth.Utility;

using Xero.ScreencastWeb.Models;

namespace Xero.ScreencastWeb.Services
{
    public class ApiRepository
    {

        static ApiRepository()
        {
            ServicePointManager.ServerCertificateValidationCallback += ValidateRemoteCertificate;
            //ServicePointManager.Expect100Continue = true;
            //ServicePointManager.CheckCertificateRevocationList = false;
            //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
        }

        private static bool ValidateRemoteCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslpolicyerrors)
        {
            return true;
        }

        /// <summary>
        /// Gets the OAuth signing certificate from the local certificate store, if specfified in app.config.
        /// </summary>
        /// <returns></returns>
        public AsymmetricAlgorithm GetOAuthSigningCertificate()
        {
            string oauthCertificateFindType = ConfigurationManager.AppSettings["XeroApiOAuthCertificateFindType"];
            string oauthCertificateFindValue = ConfigurationManager.AppSettings["XeroApiOAuthCertificateFindValue"];

            X509FindType x509FindType = (X509FindType)Enum.Parse(typeof (X509FindType), oauthCertificateFindType);

            // Search the LocalMachine certificate store for matching X509 certificates.
            X509Store certStore = new X509Store("My", StoreLocation.LocalMachine);
            certStore.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
            X509Certificate2Collection certificateCollection = certStore.Certificates.Find(x509FindType, oauthCertificateFindValue, false);
            certStore.Close();

            if (certificateCollection.Count == 0)
            {
                throw new ApplicationException("The specified subject does not match a certificate in the local certificate store");
            }

            if (!certificateCollection[0].HasPrivateKey)
            {
                throw new ApplicationException("The specified subject matched a certificate, but there is not private key stored with the certificate");
            }

            return certificateCollection[0].PrivateKey;
        }

        /// <summary>
        /// Return a CertificateFactory that can read the Client SSL certificate from the local machine certificate store
        /// </summary>
        /// <returns></returns>
        public ICertificateFactory GetClientSslCertificateFactory()
        {
            string oauthCertificateFindType = ConfigurationManager.AppSettings["XeroApiSslCertificateFindType"];
            string oauthCertificateFindValue = ConfigurationManager.AppSettings["XeroApiSslCertificateFindValue"];

            if (string.IsNullOrEmpty(oauthCertificateFindType) || string.IsNullOrEmpty(oauthCertificateFindValue))
            {
                return new NullCertificateFactory();
            }

            X509FindType x509FindType = (X509FindType)Enum.Parse(typeof(X509FindType), oauthCertificateFindType);
            ICertificateFactory certificateFactory = new LocalMachineCertificateFactory(oauthCertificateFindValue, x509FindType);

            Debug.Assert(certificateFactory.CreateCertificate() != null);

            return certificateFactory;
        }
        

        public IOAuthConsumerContext GetConsumerContext()
        {
            OAuthConsumerContext consumerContext = new OAuthConsumerContext()
            {
                ConsumerKey = ConfigurationManager.AppSettings["XeroApiConsumerKey"],
                ConsumerSecret = ConfigurationManager.AppSettings["XeroApiConsumerSecret"],
                SignatureMethod = ConfigurationManager.AppSettings["XeroApiSignatureMethod"],
                UseHeaderForOAuthParameters = true,
                UserAgent = string.Format("Xero.API.ScreenCastWeb v1.0"),
                Key = GetOAuthSigningCertificate()
            };

            return consumerContext;
        }


        public IOAuthSession GetOAuthSession(IOAuthConsumerContext consumerContext)
        {
            OAuthSession oAuthSession = new OAuthSession(
                consumerContext,
                ConfigurationManager.AppSettings["XeroApiRequestTokenEndpoint"],
                ConfigurationManager.AppSettings["XeroApiAuthorisationEndpoint"],
                ConfigurationManager.AppSettings["XeroApiAccessTokenEndpoint"],
                ConfigurationManager.AppSettings["XeroApiCallbackUrl"]);

            // Replace the default ConsumerRequestFactory with one that can construct a ConsumerRequest with Client SSL certificates.
            oAuthSession.ConsumerRequestFactory = new ClientCertEnabledConsumerRequestFactory(GetClientSslCertificateFactory());

            return oAuthSession;
        }


        public IOAuthSession GetOAuthSession()
        {
            return GetOAuthSession(GetConsumerContext());
        }
        

        public bool TestConnectionToXeroApi(HttpSessionStateBase session)
        {
            IOAuthSession oauthSession = GetOAuthSession();

            // 5. Make a call to api.xero.com to check that we can use the access token.
            IConsumerRequest getOrganisationRequest = oauthSession
                .Request()
                .ForMethod("GET")
                .ForUri(new Uri(new Uri(ConfigurationManager.AppSettings["XeroApiBaseUrl"]), "/api.xro/2.0/Organisation"))
                .SignWithToken(session.GetAccessToken());

            try
            {
                getOrganisationRequest.ToWebResponse();
                return true;
            }
            catch (Exception ex)
            {
                Trace.Write("Could not 'GET Organisation' from Xero API:" + ex.Message);
                Trace.Write(ex);
                return false;
            }
        }


        public Response GetItemByIdOrCode<TModel>(HttpSessionStateBase session, string resourceId)
            where TModel : ModelBase, new()
        {
            ApiListRequest<TModel> listRequest = new ApiListRequest<TModel> { ResourceId = resourceId };
            return GetItemByIdOrCode(session.GetAccessToken(), listRequest);
        }

        public Response GetItemByIdOrCode<TModel>(HttpSessionState session, string resourceId)
            where TModel : ModelBase, new()
        {
            ApiListRequest<TModel> listRequest = new ApiListRequest<TModel> { ResourceId = resourceId };
            return GetItemByIdOrCode(session.GetAccessToken(), listRequest);
        }

        /// <summary>
        /// Gets the item by id or code.
        /// </summary>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <param name="accessToken">The access token.</param>
        /// <param name="listRequest">The list request.</param>
        /// <returns></returns>
        private Response GetItemByIdOrCode<TModel>(IToken accessToken, ApiListRequest<TModel> listRequest)
            where TModel : ModelBase, new()
        {
            if (accessToken == null)
            {
                return new Response { Status = "NotConnected" };
            }

            IConsumerRequest consumerRequest = GetOAuthSession()
                .Request()
                .ForMethod("GET")
                .ForUri(GetFullEndpointUri(ConfigurationManager.AppSettings["XeroApiBaseUrl"], listRequest))
                .SignWithToken(accessToken);

            // Set the If-Modified-Since http header - if specified
            listRequest.ApplyModifiedSinceDate(consumerRequest);

            return CallXeroApi(consumerRequest);
        }


        /// <summary>
        /// Lists the items.
        /// </summary>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <param name="session">The current http session state containing the access token.</param>
        /// <param name="listRequest">The list request.</param>
        /// <returns></returns>
        public Response ListItems<TModel>(HttpSessionStateBase session, ApiListRequest<TModel> listRequest)
            where TModel : ModelBase, new()
        {
            IToken accessToken = session.GetAccessToken();

            if (accessToken == null)
            {
                return new Response { Status = "NotConnected" };
            }

            IConsumerRequest consumerRequest = GetOAuthSession()
                .Request()
                .ForMethod("GET")
                .ForUri(GetFullEndpointUri(ConfigurationManager.AppSettings["XeroApiBaseUrl"], listRequest))
                .SignWithToken(accessToken);
            
            // Set the If-Modified-Since http header - if specified
            listRequest.ApplyModifiedSinceDate(consumerRequest);

            return CallXeroApi(consumerRequest);
        }


        /// <summary>
        /// Calls the Xero API.
        /// </summary>
        /// <param name="consumerRequest">The consumer request.</param>
        /// <returns></returns>
        private static Response CallXeroApi(IConsumerRequest consumerRequest)
        {
            HttpWebResponse webResponse;

            try
            {
                webResponse = consumerRequest.ToWebResponse();
            }
            catch (WebException ex)
            {
                if (ex.Response is HttpWebResponse)
                {
                    HttpWebResponse httpWebResponse = (HttpWebResponse)ex.Response;

                    if (httpWebResponse.StatusCode == HttpStatusCode.NotFound)
                    {
                        return new Response { Status = "NotFound" };
                    }
                }

                return new Response { Status = "Web Exception: " + ex.Message };
            }
            catch (OAuthException ex)
            {
                return new Response { Status = "OAuth Exception: " + ex.Report };
            }
            catch (Exception ex)
            {
                return new Response { Status = "Exception: " + ex.Message };
            }

            return ModelSerializer.DeSerializer<Response>(webResponse.GetResponseStream().ReadToEnd());
        }


        /// <summary>
        /// Gets the full endpoint URI.
        /// </summary>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <param name="baseApiUri">The base API URI.</param>
        /// <param name="listRequest">The list request.</param>
        /// <returns></returns>
        private static Uri GetFullEndpointUri<TModel>(string baseApiUri, ApiListRequest<TModel> listRequest)
            where TModel : ModelBase, new()
        {
            string endpointUri = baseApiUri;

            if (!endpointUri.EndsWith("/")) { endpointUri += "/"; }

            if (!string.IsNullOrEmpty(listRequest.ResourceName))
            {
                endpointUri += listRequest.ResourceName;
            }

            if (!endpointUri.EndsWith("/")) { endpointUri += "/"; }

            if (!string.IsNullOrEmpty(listRequest.ResourceId))
            {
                endpointUri += listRequest.ResourceId;
            }

            if (listRequest.RequiresQuerystring)
            {
                endpointUri += "?" + listRequest.ToQuerystring();
            }
            
            return new Uri(endpointUri);
        }

    }
}