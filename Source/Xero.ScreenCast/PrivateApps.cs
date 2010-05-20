using System;
using System.IO;
using System.Net;
using System.Xml.Linq;
using System.Xml.XPath;
using DevDefined.OAuth.Consumer;
using DevDefined.OAuth.Framework;
using DevDefined.OAuth.Utility;
using XeroScreencast.Helpers;

namespace XeroScreencast
{
    class PrivateApps
    {

        public static void Run()
        {
            IToken accessToken = ConsumerSessionFactory.CreatePrivateAccessToken();
            IOAuthSession consumerSession = ConsumerSessionFactory.CreatePrivateConsumerSession();
            

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
                putContactResponse = ex.Response.GetResponseStream().ReadToEnd();

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
                .WithFormParameters(new { xml = postContactRequestBody })
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
                putContactResponse = ex.Response.GetResponseStream().ReadToEnd();

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
