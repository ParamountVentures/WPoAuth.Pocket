using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace WPoAuth.Pocket
{
    /// <summary>
    /// A library for performing oAuth 2.0 requests - can be integrated into Windows Phone apps.
    /// Based on code at http://developer.nokia.com/community/wiki/OAuth_on_Windows_Phone.
    /// </summary>
    public class Auth
    {        
        private string ACCESS_TOKEN;
        private int AUTH = 0;
        private int TOKEN = 1;
        private int state;
        private string code;

        private string requestEndpoint = String.Empty;
        private string authorizationEndpoint = String.Empty;
        private string consumerKey = String.Empty;
        private string redirectUri = String.Empty;

        /// <summary>
        /// Constructor to build a fully configured oAuth client instance.
        /// </summary>
        /// <param name="requestEndpoint"></param>
        /// <param name="authorizationEndpoint"></param>
        /// <param name="consumerKey"></param>
        /// <param name="consumerSecret"></param>
        /// <param name="redirectUri"></param>
        public Auth(string requestEndpoint, string authorizationEndpoint, string consumerKey, string redirectUri)
        {
            this.state = AUTH;
            this.requestEndpoint = requestEndpoint;
            this.authorizationEndpoint = authorizationEndpoint;
            this.consumerKey = consumerKey;
            this.redirectUri = redirectUri;
        }

        /// <summary>
        /// Starts an oAuth 2 request.
        /// </summary>
        public void StartRequest()
        {
            // pocket style oauth request
            System.Uri myUri = new System.Uri(this.requestEndpoint);
            HttpWebRequest myRequest = (HttpWebRequest)HttpWebRequest.Create(myUri);
            myRequest.Method = "POST";
            myRequest.ContentType = "application/x-www-form-urlencoded";
            myRequest.BeginGetRequestStream(new AsyncCallback(GetRequestStreamCallback), myRequest);            
        }


        /// <summary>
        /// The last stage in the oAuth request.
        /// </summary>
        public void CompleteRequest()
        {
            HttpWebRequest myRequest = (HttpWebRequest)HttpWebRequest.Create(this.authorizationEndpoint);
            myRequest.Method = "POST";
            myRequest.ContentType = "application/x-www-form-urlencoded";
            myRequest.BeginGetRequestStream(new AsyncCallback(GetRequestStreamCallback), myRequest);
            state = TOKEN;
        }

        /// <summary>
        /// Async method for managing the request before it is made.
        /// </summary>
        /// <param name="callbackResult"></param>
        void GetRequestStreamCallback(IAsyncResult callbackResult)
        {
            if (state == AUTH)
            {
                HttpWebRequest myRequest = (HttpWebRequest)callbackResult.AsyncState;
                
                // End the stream request operation
                using (Stream postStream = myRequest.EndGetRequestStream(callbackResult))
                {
                    // Create the post data
                    string postData = "consumer_key=" + this.consumerKey + "&redirect_uri=" + this.redirectUri;
                    byte[] byteArray = Encoding.UTF8.GetBytes(postData);

                    // Add the post data to the web request
                    postStream.Write(byteArray, 0, byteArray.Length);
                    postStream.Flush();
                }

                // Start the web request
                myRequest.BeginGetResponse(new AsyncCallback(GetResponsetStreamCallback), myRequest);
                
            }
            else if (state == TOKEN)
            {
                HttpWebRequest myRequest = (HttpWebRequest)callbackResult.AsyncState;

                // End the stream request operation
                using (Stream postStream = myRequest.EndGetRequestStream(callbackResult))
                {

                    // Create the post data
                    string postData = "consumer_key=" + this.consumerKey + "&code=" + code;
                    byte[] byteArray = Encoding.UTF8.GetBytes(postData);

                    // Add the post data to the web request
                    postStream.Write(byteArray, 0, byteArray.Length);
                    postStream.Flush();
                }

                // Start the web request
                myRequest.BeginGetResponse(new AsyncCallback(GetResponsetStreamCallback), myRequest);
            }
        }

        /// <summary>
        /// Async method for managing the response that is returned from a request.
        /// </summary>
        /// <param name="callbackResult"></param>
        void GetResponsetStreamCallback(IAsyncResult callbackResult)
        {
            if (state == AUTH)
            {
                HttpWebRequest request = (HttpWebRequest)callbackResult.AsyncState;
                HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(callbackResult);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    Debug.WriteLine("Ok");
                }
                else
                {
                    Debug.WriteLine(response.StatusCode);
                }
                using (StreamReader httpWebStreamReader = new StreamReader(response.GetResponseStream()))
                {
                    string result = httpWebStreamReader.ReadToEnd();
                    //For debug: show results
                    Debug.WriteLine(result);
                    string[] data = result.Split('=');
                    code = data[1];

                    // get the UI to redirect to perform the user auth step
                    AuthorizationArgs e = new AuthorizationArgs(code);
                    OnRequestAuthorization(e);
                }
            }
            else if (state == TOKEN)
            {
                HttpWebRequest request = (HttpWebRequest)callbackResult.AsyncState;
                HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(callbackResult);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    Debug.WriteLine("HTTP Auth success");
                }
                else
                {
                    Debug.WriteLine(response.StatusCode);
                }
                using (StreamReader httpWebStreamReader = new StreamReader(response.GetResponseStream()))
                {
                    string result = httpWebStreamReader.ReadToEnd();
                    //For debug: show results
                    Debug.WriteLine(result);

                    string[] data = result.Split('&');
                    string[] dataQ = data[0].Split('=');
                    ACCESS_TOKEN = dataQ[1];

                    // we should now have the access token
                    AuthorizedArgs e = new AuthorizedArgs(dataQ[1]);
                    OnCompleteAuthorization(e);
                }
            }
        }

        /// <summary>
        /// Handle authorization events.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnRequestAuthorization(AuthorizationArgs e)
        {
            RequestAuthorizationEventHandler handler = RequestAuthorization;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        /// Handle authorization complete events.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnCompleteAuthorization(AuthorizedArgs e)
        {
            CompleteAuthorizationEventHandler handler = CompleteAuthorization;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        // Delegates
        public delegate void RequestAuthorizationEventHandler(object sender, AuthorizationArgs e);
        public delegate void CompleteAuthorizationEventHandler(object sender, AuthorizedArgs e);

        // Events
        public event RequestAuthorizationEventHandler RequestAuthorization;
        public event CompleteAuthorizationEventHandler CompleteAuthorization;
    }


    /// <summary>
    /// Arguments for the authorization process.
    /// </summary>
    public class AuthorizationArgs : EventArgs
    {
        public string token = String.Empty;
        public string clientId = String.Empty;

        public AuthorizationArgs(string token)
            : base()
        {
            this.token = token;
        }
    }


    /// <summary>
    /// Arguments for the authorized stage of the process.
    /// </summary>
    public class AuthorizedArgs : EventArgs
    {
        public string AccessToken = String.Empty;

        public AuthorizedArgs(string token)
            : base()
        {
            this.AccessToken = token;
        }
    }
}
