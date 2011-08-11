#region License

// The MIT License
//
// Copyright (c) 2006-2008 DevDefined Limited.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

#endregion

using System;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Web;
using System.Xml.Linq;

using DevDefined.OAuth.Framework;
using DevDefined.OAuth.Utility;

namespace DevDefined.OAuth.Consumer
{
  public class ConsumerRequest : IConsumerRequest
  {
    readonly IOAuthConsumerContext _consumerContext;
    readonly IOAuthContext _context;
    readonly IToken _token;
    readonly ICertificateFactory _clientSslCertificateFactory;

    public ConsumerRequest(IOAuthContext context, IOAuthConsumerContext consumerContext, IToken token)
    {
      _context = context;
      _consumerContext = consumerContext;
      _token = token;
      _clientSslCertificateFactory = null;
    }

    public ConsumerRequest(IOAuthContext context, IOAuthConsumerContext consumerContext, IToken token, ICertificateFactory clientSslCertificateFactory)
    {
        _context = context;
        _consumerContext = consumerContext;
        _token = token;
        _clientSslCertificateFactory = clientSslCertificateFactory;
    }

    public IOAuthConsumerContext ConsumerContext
    {
      get { return _consumerContext; }
    }

    public IOAuthContext Context
    {
      get { return _context; }
    }

    public XDocument ToDocument()
    {
      return XDocument.Parse(ToString());
    }

    public byte[] ToBytes()
    {
      return Convert.FromBase64String(ToString());
    }

    public virtual HttpWebRequest ToWebRequest()
    {
      RequestDescription description = GetRequestDescription();

      var request = (HttpWebRequest) WebRequest.Create(description.Url);
      request.Method = description.Method;
      request.UserAgent = _consumerContext.UserAgent;
      request.ContentType = description.ContentType;

      if (!string.IsNullOrEmpty(AcceptsType))
      {
          request.Accept = AcceptsType;
      }

      try
      {
          if (Context.Headers["If-Modified-Since"] != null)
          {
              string modifiedDateString = Context.Headers["If-Modified-Since"];
              request.IfModifiedSince = DateTime.Parse(modifiedDateString, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
          }
      }
      catch (Exception ex)
      {
          throw new ApplicationException("If-Modified-Since header could not be parsed as a datetime", ex);
      }

      if (ProxyServerUri != null)
      {
          request.Proxy = new WebProxy(ProxyServerUri, false);
      }

      if (description.ContentType == Parameters.HttpFormEncoded)
      {
        using (var writer = new StreamWriter(request.GetRequestStream()))
        {
          writer.Write(description.Body);
        }
      }
      else if (!string.IsNullOrEmpty(description.Body))
      {
          using (var writer = new StreamWriter(request.GetRequestStream()))
          {
              writer.Write(description.Body);
          }
      }
      else if (description.RequestStream != null)
      {
          using (var requestStream = request.GetRequestStream())
          {
              description.RequestStream.CopyTo(requestStream);
          }
      }

      if (description.Headers.Count > 0)
      {
        foreach (string key in description.Headers.AllKeys)
        {
          request.Headers[key] = description.Headers[key];
        }
      }

      // Attach a client ssl certificate to the HttpWebRequest
      if (_clientSslCertificateFactory != null)
      {
          X509Certificate2 certificate = _clientSslCertificateFactory.CreateCertificate();

          if (certificate != null)
          {
              request.ClientCertificates.Add(certificate);
          }
      }

      return request;
    }

    private RequestDescription GetRequestDescription()
    {
      if (string.IsNullOrEmpty(_context.Signature))
      {
        if (_token != null)
        {
          _consumerContext.SignContextWithToken(_context, _token);
        }
        else
        {
          _consumerContext.SignContext(_context);
        }
      }

      RequestDescription description = new RequestDescription
      {
         Url = _context.GenerateUri(),
         Method = _context.RequestMethod
      };

      if ((_context.FormEncodedParameters != null) && (_context.FormEncodedParameters.Count > 0))
      {
        description.ContentType = Parameters.HttpFormEncoded;
        description.Body = UriUtility.FormatQueryString(_context.FormEncodedParameters.ToQueryParametersExcludingTokenSecret());
      }
      else if (!string.IsNullOrEmpty(RequestBody))
      {
          description.Body = UriUtility.UrlEncode(RequestBody);
          description.ContentType = ContentType;
      }
      else if (RequestStream != null)
      {
          description.RequestStream = RequestStream;
          description.ContentType = ContentType;
      }

      if (_consumerContext.UseHeaderForOAuthParameters)
      {
        description.Headers[Parameters.OAuth_Authorization_Header] = _context.GenerateOAuthParametersForHeader();
      }

      return description;
    }

    public HttpWebResponse ToWebResponse()
    {
      try
      {
        HttpWebRequest request = ToWebRequest();
        return (HttpWebResponse)request.GetResponse();      
      }
      catch (WebException webEx)
      {
        OAuthException authException;

        if (WebExceptionHelper.TryWrapException(Context, webEx, out authException, ResponseBodyAction))
        {
          throw authException;
        }

        throw;
      }
    }

    public NameValueCollection ToBodyParameters()
    {
      try
      {
        string encodedFormParameters = ToString();

        if (ResponseBodyAction != null)
        {
            ResponseBodyAction(encodedFormParameters);
        }

        try
        {
          return HttpUtility.ParseQueryString(encodedFormParameters);
        }
        catch (ArgumentNullException)
        {
          throw Error.FailedToParseResponse(encodedFormParameters);
        }
      }
      catch (WebException webEx)
      {
        throw Error.RequestFailed(webEx);
      }
    }

    public IConsumerRequest SignWithoutToken()
    {
      EnsureRequestHasNotBeenSignedYet();
      _consumerContext.SignContext(_context);
      return this;
    }

    public IConsumerRequest SignWithToken()
    {
      return SignWithToken(_token);
    }

    public IConsumerRequest SignWithToken(IToken token)
    {
      EnsureRequestHasNotBeenSignedYet();
      _consumerContext.SignContextWithToken(_context, token);
      return this;
    }

    public Uri ProxyServerUri { get; set; }

    public Action<string> ResponseBodyAction { get; set; }

    public string AcceptsType { get; set; }

    public string RequestBody { get; set; }

    public Stream RequestStream { get; set; }

    private string ResponseBody { get; set; }

    public string ContentType { get; set; }

    public override string ToString()
    {
      if (string.IsNullOrEmpty(ResponseBody))
      {
        ResponseBody = ToWebResponse().ReadToEnd();
      }
            
      return ResponseBody;
    }

    public byte[] ToBytes(Encoding encoding)
    {
        if (string.IsNullOrEmpty(ResponseBody))
        {
            ResponseBody = ToWebResponse().ReadToEnd();
        }

        return encoding.GetBytes(ResponseBody);
    }

    void EnsureRequestHasNotBeenSignedYet()
    {
      if (!string.IsNullOrEmpty(_context.Signature))
      {
        throw Error.ThisConsumerRequestHasAlreadyBeenSigned();
      }
    }

    private class RequestDescription
    {
        public RequestDescription()
        {
            Headers = new NameValueCollection();
        }

        public Uri Url { get; set; }
        public string Method { get; set; }
        public string ContentType { get; set; }
        public string Body { get; set; }
        public Stream RequestStream { get; set; }
        public NameValueCollection Headers { get; private set; }
    }
  }
}