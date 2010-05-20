using System;
using System.Diagnostics;
using System.Net;
using System.Xml.Linq;
using System.Xml.XPath;
using DevDefined.OAuth.Consumer;
using DevDefined.OAuth.Framework;
using XeroScreencast.Helpers;

namespace XeroScreencast
{
    class PublicApps
    {
        public static void Run()
        {
            // Create the consumer session
            OAuthConsumerContext consumerContext = new OAuthConsumerContext
            {
                ConsumerKey = "ZGIXM2M1Y2RIZJGYNGY1Y2EWZGYZMW",
                ConsumerSecret = "RZRCMBRPK57EAG6GRO4GPLYDH9REPX",
                SignatureMethod = SignatureMethod.HmacSha1,
                UseHeaderForOAuthParameters = true,
                UserAgent = string.Format("Xero.API.ScreenCast v1.0 (Public App Testing)")
            };

            OAuthSession consumerSession = new OAuthSession(
                consumerContext, 
                Settings.Default.RequestTokenURI, 
                Settings.Default.AuthoriseURI,
                Settings.Default.AccessTokenURI);

            // 1. Get a request token
            IToken requestToken = consumerSession.GetRequestToken();
            
            Console.WriteLine("Request Token Key: {0}", requestToken.Token);
            Console.WriteLine("Request Token Secret: {0}", requestToken.TokenSecret);


            // 2. Get the user to log into Xero using the request token in the querystring
            //string authorisationUrl = Settings.Default.AuthoriseURI.AbsoluteUri + "?oauth_token=" + HttpUtility.UrlEncode(requestTokenKey);
            string authorisationUrl = consumerSession.GetUserAuthorizationUrlForToken(requestToken);
            Process.Start(authorisationUrl);

            // 3. Get the use to enter the authorisation code from Xero (4-7 digit number)
            Console.WriteLine("Please input the code you were given in Xero:");
            var verificationCode = Console.ReadLine();

            if (string.IsNullOrEmpty(verificationCode))
            {
                Console.WriteLine("You didn't type a verification code!");
                return;
            }

            verificationCode = verificationCode.Trim();


            // 4. Use the request token and verification code to get an access token
            IToken accessToken;

            try
            {
                accessToken = consumerSession.ExchangeRequestTokenForAccessToken(requestToken, verificationCode);
            }
            catch (OAuthException ex)
            {
                Console.WriteLine("An OAuthException was caught:");
                Console.WriteLine(ex.Report);
                return;
            }

            Console.WriteLine("Access Token Key: {0}", accessToken.Token);
            Console.WriteLine("Access Token Secret: {0}", accessToken.TokenSecret);


            // 5. Make a call to api.xero.com to check that we can use the access token.
            IConsumerRequest getOrganisationRequest = consumerSession
                .Request()
                .ForMethod("GET")
                .ForUri(new Uri("https://api.xero.com/api.xro/2.0/Organisation"))
                .SignWithToken(accessToken);

            string getOrganisationResponse = getOrganisationRequest.ToString();

            if (getOrganisationResponse != string.Empty)
            {
                var organisationXml = XElement.Parse(getOrganisationResponse);
                string organisationName = organisationXml.XPathSelectElement("//Organisation/Name").Value;
                Console.WriteLine(string.Format("You have been authorised against organisation: {0}", organisationName));
            }


            // 6. Make a PUT call to the API - add a dummy contact
            Console.WriteLine("Please enter the name of a new contact to add to Xero");
            string contactName = Console.ReadLine();

            if (string.IsNullOrEmpty(contactName))
            {
                return;
            }

            string putContactRequestBody = string.Format("<Contacts><Contact><Name>{0}</Name></Contact></Contacts>", contactName);
            string putContactResponse;

            IConsumerRequest putContactRequest = consumerSession
                .Request()
                .ForMethod("PUT")
                .ForUri(new Uri("https://api.xero.com/api.xro/2.0/Contacts"))
                .WithFormParameters(new { xml = putContactRequestBody })
                .SignWithToken(accessToken);

            try
            {
                putContactResponse = putContactRequest.ToString();
            }
            catch (OAuthException ex)
            {
                Console.WriteLine("An OAuthException was caught:");
                Console.WriteLine(ex.Report);
                return;
            }
            catch (WebException ex)
            {
                putContactResponse = ex.Response.ResponseBody();

                Console.WriteLine("A WebException was caught:");
                Console.WriteLine(putContactResponse);
                return;
            }

            if (putContactResponse != string.Empty)
            {
                var responseElement = XElement.Parse(putContactResponse);
                string statusCode = responseElement.XPathSelectElement("/Status").Value;

                if (statusCode == "OK")
                {
                    string contactId = responseElement.XPathSelectElement("/Contacts/Contact[1]/ContactID").Value;
                    Console.WriteLine(string.Format("The contact '{0}' was created with id: {1}", contactName, contactId));
                }
            }


            // 7. Try to update the contact that's just been created, but this time use a POST method
            string postContactRequestBody = string.Format("<Contacts><Contact><Name>{0}</Name><EmailAddress>{1}@nowhere.com</EmailAddress></Contact></Contacts>", contactName, contactName.ToLower().Replace(" ", "."));
            string postContactResponse;

            IConsumerRequest postContactRequest = consumerSession
                .Request()
                .ForMethod("POST")
                .ForUri(new Uri("https://api.xero.com/api.xro/2.0/Contacts"))
                .WithFormParameters(new {xml = postContactRequestBody})
                .SignWithToken(accessToken);

            try
            {
                postContactResponse = postContactRequest.ToString();
            }
            catch (OAuthException ex)
            {
                Console.WriteLine("An OAuthException was caught:");
                Console.WriteLine(ex.Report);
                return;
            }
            catch (WebException ex)
            {
                putContactResponse = ex.Response.ResponseBody();

                Console.WriteLine("A WebException was caught:");
                Console.WriteLine(putContactResponse);
                return;
            }
            
            if (postContactResponse != string.Empty)
            {
                var responseElement = XElement.Parse(postContactResponse);
                string statusCode = responseElement.XPathSelectElement("/Status").Value;

                if (statusCode == "OK")
                {
                    string emailAddress = responseElement.XPathSelectElement("/Contacts/Contact[1]/EmailAddress").Value;
                    Console.WriteLine(string.Format("The contact '{0}' was updated with email address: {1}", contactName, emailAddress));
                }
            }
        }
        
    }
    
}
