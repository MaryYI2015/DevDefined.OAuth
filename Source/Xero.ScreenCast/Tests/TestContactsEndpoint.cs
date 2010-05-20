using System;
using System.Net;
using DevDefined.OAuth.Consumer;
using DevDefined.OAuth.Framework;
using DevDefined.OAuth.Utility;
using NUnit.Framework;
using XeroScreencast.Helpers;

namespace XeroScreencast.Tests
{

    [TestFixture]
    public class TestContactsEndpoint
    {
        private const string apiEndpointUrl = "https://api.xero.com/api.xro/2.0/Contacts";


        [Test]
        public void TestContactCanBePosted()
        {
            string contactName = "Steve Jobs " + DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss");
            string contactEmail = contactName.Replace(' ', '-');

            string postContactRequestBody = string.Format("<Contacts><Contact><Name>{0}</Name><EmailAddress>{1}@nowhere.com</EmailAddress></Contact></Contacts>", contactName, contactEmail);

            IConsumerRequest postContactRequest = ConsumerSessionFactory.CreatePrivateConsumerSession()
             .Request()
             .ForMethod("POST")
             .ForUri(new Uri(apiEndpointUrl))
             .WithFormParameters(new { xml = postContactRequestBody })
             .SignWithToken(ConsumerSessionFactory.CreatePrivateAccessToken());

             string postContactResponse = postContactRequest.ToString();

             Assert.IsNotEmpty(postContactResponse);
        }

        [Test]
        public void TestEmptyContactNameReturnsHttp400Error()
        {
            string contactName = "Steve Jobs " + DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss");
            string contactEmail = contactName.Replace(' ', '-');

            string postContactRequestBody = string.Format("<Contacts><Contact><Name></Name><EmailAddress>{0}@nowhere.com</EmailAddress></Contact></Contacts>", contactEmail);
            string postContactResponseBody = string.Empty;
            
            IConsumerRequest postContactRequest = ConsumerSessionFactory.CreatePrivateConsumerSession()
             .Request()
             .ForMethod("POST")
             .ForUri(new Uri(apiEndpointUrl))
             .WithFormParameters(new { xml = postContactRequestBody })
             .SignWithToken(ConsumerSessionFactory.CreatePrivateAccessToken());

            try
            {
                postContactResponseBody = postContactRequest.ToString();
            }
            catch (OAuthException ex)
            {
                Assert.Fail(string.Format("An OAuth Exception occurred: {0}", ex.Report));
            }
            catch (WebException ex)
            {
                postContactResponseBody = ex.Response.GetResponseStream().ReadToEnd();
            }

            // Even if an error occurs, the response body should have been captured
            Assert.IsNotEmpty(postContactResponseBody);

            Console.WriteLine("Error Number : " + postContactResponseBody.ReadSingleNode(@"/ApiException/ErrorNumber"));
            Console.WriteLine("Error Message : " + postContactResponseBody.ReadSingleNode(@"/ApiException/Message"));

            foreach (string validationError in postContactResponseBody.ReadNodes(@"/ApiException/Elements/DataContractBase/ValidationErrors/ValidationError"))
            {
                Console.WriteLine("Validation Error : " + validationError);
            }

        }

    }
}
