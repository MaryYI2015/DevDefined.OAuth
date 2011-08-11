using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;
using System.Xml.Linq;
using DevDefined.OAuth.Framework;

namespace DevDefined.OAuth.Consumer
{
    public interface IConsumerRequest
    {
        IOAuthConsumerContext ConsumerContext { get; }
        IOAuthContext Context { get; }
        
        IConsumerRequest SignWithoutToken();
        IConsumerRequest SignWithToken();
        IConsumerRequest SignWithToken(IToken token);

        Uri ProxyServerUri { get; set; }
        Action<string> ResponseBodyAction { get; set; }
        string AcceptsType { get; set; }
        string RequestBody { get; set; }
        Stream RequestStream { get; set; }
        string ContentType { get; set; }

        // To Response Methods
        XDocument ToDocument();
        HttpWebResponse ToWebResponse();
        NameValueCollection ToBodyParameters();

        [Obsolete]
        byte[] ToBytes();
        byte[] ToBytes(Encoding encoding);
    }
}