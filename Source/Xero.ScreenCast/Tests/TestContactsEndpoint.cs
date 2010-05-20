using System;
using System.Net;
using DevDefined.OAuth.Consumer;
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
            string postContactResponse = string.Empty;
            string postContactError = string.Empty;
            
            IConsumerRequest postContactRequest = ConsumerSessionFactory.CreatePrivateConsumerSession()
             .Request()
             .ForMethod("POST")
             .ForUri(new Uri(apiEndpointUrl))
             .WithFormParameters(new { xml = postContactRequestBody })
             .SignWithToken(ConsumerSessionFactory.CreatePrivateAccessToken());

            // This will capture the response message, if a WebException is thrown
            postContactRequest.ResponseBodyAction = (response => postContactError = response);

            // The empty contact name should cause the request to throw an http 400 error. 
            Assert.Throws<WebException>(() => postContactResponse = postContactRequest.ToString());

            Console.WriteLine("Error Number : " + postContactError.ReadSingleNode(@"/ApiException/ErrorNumber"));
            Console.WriteLine("Error Message : " + postContactError.ReadSingleNode(@"/ApiException/Message"));

            foreach (string validationError in postContactError.ReadNodes(@"/ApiException/Elements/DataContractBase/ValidationErrors/ValidationError"))
            {
                Console.WriteLine("Validation Error : " + validationError);
            }

        }

    }
}
