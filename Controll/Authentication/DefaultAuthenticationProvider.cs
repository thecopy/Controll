using System;
using System.Net;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Controll.Common.Helpers;
using Controll.Common.Authentication;
using Microsoft.AspNet.SignalR.Client.Hubs;

namespace Controll.Client.Authentication
{
    public class DefaultAuthenticationProvider : IAuthenticationProvider
    {
        private readonly string _url;

        public DefaultAuthenticationProvider(string url)
        {
            _url = url;
        }

        public Task<HubConnection> Connect(string userName, string password, string zombie = null)
        {
            var content = String.Format("username={0}&password={1}", Uri.EscapeUriString(userName), Uri.EscapeUriString(password));
            if (zombie != null)
                content += String.Format("&zombie={0}", Uri.EscapeUriString(zombie));

            var contentBytes = Encoding.ASCII.GetBytes(content);

            var authUri = new UriBuilder(_url)
            {
                Path = "auth"
            };

            var cookieJar = new CookieContainer();
            var request = (HttpWebRequest)WebRequest.Create(authUri.Uri);
            request.CookieContainer = cookieJar;
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = contentBytes.Length;

            return request.GetHttpRequestStreamAsync()
                .Then(stream => StreamExtensions.WriteAsync(stream, contentBytes).Then(() => stream.Dispose()))
                .Then(() => request.GetHttpResponseAsync())
                .Then(response =>
                {
                    var respStatusCode = response.StatusCode;

                    if (respStatusCode < HttpStatusCode.OK || respStatusCode > (HttpStatusCode)299)
                    {
                        throw new WebException(String.Format("Response status code does not indicate success: {0}", respStatusCode));
                    }

                    // Verify the cookie
                    var cookie = cookieJar.GetCookies(new Uri(_url));
                    if (cookie["controll.auth.id"] == null)
                    {
                        throw new SecurityException("Didn't get a cookie from Controll! Ensure your User Name/Password are correct");
                    }

                    // Create a hub connection and give it our cookie jar
                    var connection = new HubConnection(_url)
                    {
                        CookieContainer = cookieJar
                    };

                    return connection;
                });
        }

        public Task RegisterUser(string userName, string password, string email)
        {
            var content = String.Format("username={0}&password={1}&email={2}", 
                Uri.EscapeUriString(userName),
                Uri.EscapeUriString(password),
                Uri.EscapeUriString(email));

            var contentBytes = Encoding.ASCII.GetBytes(content);

            var authUri = new UriBuilder(_url)
            {
                Path = "registeruser"
            };

            var cookieJar = new CookieContainer();

            var request = (HttpWebRequest)WebRequest.Create(authUri.Uri);
            request.CookieContainer = cookieJar;
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = contentBytes.Length;

            return request.GetHttpRequestStreamAsync()
                .Then(stream => stream.WriteAsync(contentBytes).Then(() => stream.Dispose()))
                .Then(() => request.GetHttpResponseAsync())
                .Then(response =>
                {
                    var respStatusCode = response.StatusCode;

                    if (respStatusCode < HttpStatusCode.OK || respStatusCode > (HttpStatusCode)299)
                    {
                        throw new WebException(String.Format("Response status code does not indicate success: {0} {1}", (int)respStatusCode, response.StatusDescription));
                    }
                });
        }
    }
}
