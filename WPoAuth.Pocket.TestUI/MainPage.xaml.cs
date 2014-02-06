using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using WPoAuth.Pocket.TestUI.Resources;

namespace WPoAuth.Pocket.TestUI
{
    public partial class MainPage : PhoneApplicationPage
    {
        WPoAuth.Pocket.Auth AUTH;

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            // Sample code to localize the ApplicationBar
            //BuildLocalizedApplicationBar();

            // on start change to the config screen
            MainPivot.SelectedIndex = 1;
            StartRequest.Click += StartRequest_Click;
        }

        /// <summary>
        /// This runs when the user clicks the start request button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartRequest_Click(object sender, RoutedEventArgs e)
        {
            // create and start an oauth request flow
            AUTH = new Auth(RequestEndpoint.Text, AuthorizationEndpoint.Text, ConsumerKey.Text, RedirectURI.Text);
            AUTH.RequestAuthorization += AUTH_RequestAuthorization;
            AUTH.StartRequest();
        }

        /// <summary>
        /// Handle the user authorization request.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void AUTH_RequestAuthorization(object sender, AuthorizationArgs e)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                // show the browser
                MainPivot.SelectedIndex = 0;

                // change UI here
                WebBrowser.Visibility = System.Windows.Visibility.Visible;
                WebBrowser.Navigated += WebBrowser_Navigated;
                WebBrowser.Navigate(new Uri(AuthorizationWebUrl.Text + String.Format("?request_token={0}&redirect_uri={1}", e.token, this.RedirectURI.Text)));
            });
        }

        /// <summary>
        /// The user needs to peform the auth.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WebBrowser_Navigated(object sender, NavigationEventArgs e)
        {
            if (WPoAuth.Pocket.Util.StrCmp(e.Uri.AbsoluteUri, RedirectURI.Text) == true)
            {
                WebBrowser.Visibility = System.Windows.Visibility.Collapsed;
                AUTH.CompleteRequest();
                AUTH.CompleteAuthorization += AUTH_CompleteAuthorization;
            }

        }

        /// <summary>
        /// When complete we will have the token
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void AUTH_CompleteAuthorization(object sender, AuthorizedArgs e)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                AccessToken.Text += e.AccessToken;
            });
        }
    }
}