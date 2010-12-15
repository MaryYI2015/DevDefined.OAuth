using System;
using System.Diagnostics;
using System.Web.Mvc;
using DevDefined.OAuth.Consumer;
using DevDefined.OAuth.Framework;
using Xero.ScreencastWeb.Models;
using Xero.ScreencastWeb.Services;

namespace Xero.ScreencastWeb.Controllers
{
    public class ConnectController : ControllerBase
    {

        //
        // GET: /Connect/Index - Main OAuth Connection Endpoint
        
        public ActionResult Index()
        {
            Debug.Write("Processing: /Connect/Index");

            ApiRepository apiRepository = new ApiRepository();
            
            // Do we already have a session token in sessionstate? - is it still usable?
            if (Session.GetAccessToken() != null)
            {
                if (apiRepository.TestConnectionToXeroApi(Session))
                {
                    return View("Done");
                }

                // The current session token+secret doesn't work - probably due to it expiring in 30mins.
                Session.SetAccessToken(null);
            }
            

            // Call api.xero.com/oauth/AccessToken
            IOAuthSession oauthSession = apiRepository.GetOAuthSession();
            IToken requestToken = oauthSession.GetRequestToken();

            Session.SetRequestToken(requestToken);

            Debug.WriteLine("OAuth Request Token: " + requestToken.Token);
            Debug.WriteLine("OAuth Request Secret: " + requestToken.TokenSecret);

            string authorisationUrl = oauthSession.GetUserAuthorizationUrlForToken(requestToken);

            Debug.WriteLine("Redirecting browser to user authorisation uri:" + authorisationUrl);

            return new RedirectResult(authorisationUrl);
        }

        //
        // GET: /Connect/Callback - Callback url from the api.xero.com authorisation endpoint

        public ActionResult Callback()
        {
            Debug.Write("Processing: /Connect/Callback");

            ApiRepository apiRepository = new ApiRepository();

            string verificationCode = Request.Params["oauth_verifier"];

            // Call api.xero.com/oauth/AccessToken
            IOAuthSession oauthSession = apiRepository.GetOAuthSession();

            var requestToken = Session.GetRequestToken();

            if (requestToken == null)
            {
                throw new ApplicationException("The request token could not be retrived from the current http session. Is session state and cookies enabled?");
            }

            IToken accessToken = oauthSession.ExchangeRequestTokenForAccessToken(requestToken, verificationCode);

            Session.SetAccessToken(accessToken);

            // Get the organisation name from the api
            Response organisationResponse = apiRepository.GetItemByIdOrCode<Organisation>(Session, null);
            if (organisationResponse.Organisations != null && organisationResponse.Organisations.Count > 0)
            {
                Session["xero_organisation_name"] = organisationResponse.Organisations[0].Name;
                Session["xero_connection_time"] = DateTime.Now;
            }

            return View("Done");
        }

    }
}
