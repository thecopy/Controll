using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Controll.Common.Authentication
{
    internal static class HttpHelper
    {
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are flowed back to the caller.")]
        public static Task<HttpWebResponse> GetHttpResponseAsync(this HttpWebRequest request)
        {
            try
            {
                return Task.Factory.FromAsync<HttpWebResponse>(request.BeginGetResponse, ar => (HttpWebResponse)request.EndGetResponse(ar), null);
            }
            catch (Exception ex)
            {
                return TaskAsyncHelper.FromError<HttpWebResponse>(ex);
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are flowed back to the caller.")]
        public static Task<Stream> GetHttpRequestStreamAsync(this HttpWebRequest request)
        {
            try
            {
                return Task.Factory.FromAsync<Stream>(request.BeginGetRequestStream, request.EndGetRequestStream, null);
            }
            catch (Exception ex)
            {
                return TaskAsyncHelper.FromError<Stream>(ex);
            }
        }

        public static Task<HttpWebResponse> GetAsync(string url, Action<HttpWebRequest> requestPreparer)
        {
            HttpWebRequest request = CreateWebRequest(url);
            if (requestPreparer != null)
            {
                requestPreparer(request);
            }
            return request.GetHttpResponseAsync();
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Callers check for null return.")]
        public static string ReadAsString(this HttpWebResponse response)
        {
            try
            {
                using (response)
                {
                    using (Stream stream = response.GetResponseStream())
                    {
                        var reader = new StreamReader(stream);

                        return reader.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
#if NET35
                Debug.WriteLine(String.Format(System.Globalization.CultureInfo.InvariantCulture, "Failed to read response: {0}", ex));
#else
                Debug.WriteLine("Failed to read response: {0}", ex);
#endif
                // Swallow exceptions when reading the response stream and just try again.
                return null;
            }
        }

        private static HttpWebRequest CreateWebRequest(string url)
        {
            HttpWebRequest request = null;
#if WINDOWS_PHONE
            request = (HttpWebRequest)WebRequest.Create(url);
            request.AllowReadStreamBuffering = false;
#elif SILVERLIGHT
            request = (HttpWebRequest)System.Net.Browser.WebRequestCreator.ClientHttp.Create(new Uri(url));
            request.AllowReadStreamBuffering = false;
#else
            request = (HttpWebRequest)WebRequest.Create(url);
#endif
            return request;
        }
    }
}
