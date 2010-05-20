using System;
using System.IO;
using System.Net;

namespace XeroScreencast.Helpers
{
    public static class WebResponseExtensions
    {

        public static string ResponseBody(this WebResponse webResponse)
        {
            if (webResponse == null)
            {
                return string.Empty;
            }

            try
            {
                using (StreamReader streamReader = new StreamReader(webResponse.GetResponseStream()))
                {
                    return streamReader.ReadToEnd();
                }
            }
            catch (ArgumentException)
            {
                return string.Empty;
            }
        }

    }
}
