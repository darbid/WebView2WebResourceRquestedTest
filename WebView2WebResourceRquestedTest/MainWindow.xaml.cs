using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WebView2WebResourceRquestedTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;

            this.Browser.CoreWebView2InitializationCompleted += Browser_CoreWebView2InitializationCompleted;
        }

 

        private void Browser_CoreWebView2InitializationCompleted(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs e)
        {
            this.chk_CopyToMemoryStream.IsChecked = true;
            this.Browser.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
            this.Browser.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;
            this.Browser.CoreWebView2.DownloadStarting += CoreWebView2_DownloadStarting;
        }

        private void CoreWebView2_DownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs e)
        {
            System.Diagnostics.Debug.Print("##############################   CoreWebView2_DownloadStarting   ###############################################");
            System.Diagnostics.Debug.Print("TotalBytesToReceive: " + e.DownloadOperation.TotalBytesToReceive.ToString());
            System.Diagnostics.Debug.Print("##############################   CoreWebView2_DownloadStarting   ###############################################");
           
            
            if (IOStream != null)
            {
                IOStream.Dispose();
            }
        }

        private async void CoreWebView2_WebResourceRequested(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            if (e.Request.Uri != @"https://demo.borland.com/testsite/downloads/3rdPartyLicenseTexts.pdf") return;

            CoreWebView2Deferral deferral = e.GetDeferral();

            System.Diagnostics.Debug.Print("##############################   CoreWebView2_WebResourceRequested   ###############################################");
            System.Diagnostics.Debug.Print("ResourceContext: " + e.ResourceContext.ToString());
            System.Diagnostics.Debug.Print("Request.Uri: " + e.Request.Uri);
            System.Diagnostics.Debug.Print("ResultFilePath: " + e.Request.Method);
            foreach (var header in e.Request.Headers)
            {
                System.Diagnostics.Debug.WriteLine($"  {header}");
            }
            System.Diagnostics.Debug.Print("##############################   CoreWebView2_WebResourceRequested   ###############################################");

            using HttpRequestMessage httpreq = ConvertRequest(e.Request);
            using var client = new HttpClient();
            using var response = await client.SendAsync(httpreq);

            System.Net.HttpStatusCode sc = response.StatusCode;
            var totalBytes = response.Content.Headers.ContentLength;
            System.Diagnostics.Debug.Print("##############################   HTTPResponse STATUS and SIZE   ###############################################");
            System.Diagnostics.Debug.Print("HttpStatusCode: " + sc.ToString());
            System.Diagnostics.Debug.Print("Content.Headers.ContentLength: " + totalBytes.ToString());
            System.Diagnostics.Debug.Print("##############################   HTTPResponse STATUS and SIZE   ###############################################");

            response.EnsureSuccessStatusCode();

            System.Diagnostics.Debug.Print("************************************   HTTP RECIEVED RESPONSE HEADERS   *******************************************");

            foreach (var header2 in response.Headers)
            {
                string headerContent = string.Join(",", header2.Value.ToArray()); ;
                System.Diagnostics.Debug.WriteLine(String.Concat("Key: ", header2.Key, "  Value: ", headerContent));
            }
            System.Diagnostics.Debug.Print("************************************   HTTP RECIEVED RESPONSE HEADERS   *******************************************");

            e.Response = await ConvertResponseAsync(response);

            deferral.Complete();


        }

        
        private Stream IOStream;

        private async Task<CoreWebView2WebResourceResponse> ConvertResponseAsync(HttpResponseMessage aResponse)
        {
            CoreWebView2WebResourceResponse cwv2Response;

            var statCode = (int)aResponse.StatusCode;

            if (this.chk_HoldStream.IsChecked ?? true)
            {   //Set the response content to a class variable and hold it till Downloadstart
                IOStream = await aResponse.Content.ReadAsStreamAsync();
                cwv2Response = this.Browser.CoreWebView2.Environment.CreateWebResourceResponse(IOStream, statCode, aResponse.ReasonPhrase, "");
            }
            else if (this.chk_CopyToMemoryStream.IsChecked ?? true)
            {   //Copy the stream to a Memory Stream and then hold it as a class variable.
                IOStream = await aResponse.Content.ReadAsStreamAsync();
                MemoryStream MEMStream2 = new();
                IOStream.CopyTo(MEMStream2);
                IOStream = await aResponse.Content.ReadAsStreamAsync();
                cwv2Response = this.Browser.CoreWebView2.Environment.CreateWebResourceResponse(MEMStream2, statCode, aResponse.ReasonPhrase, "");
            }
            else
            { //Default is what I would normally expect.
                var stream = await aResponse.Content.ReadAsStreamAsync();
                cwv2Response = this.Browser.CoreWebView2.Environment.CreateWebResourceResponse(stream, statCode, aResponse.ReasonPhrase, "");
            }

            CoreWebView2HttpResponseHeaders heads = cwv2Response.Headers;

            foreach (var header in aResponse.Headers)
            {
                string headerContent = string.Join(",", header.Value.ToArray()); ;
                heads.AppendHeader(header.Key, headerContent);
            }

            heads.AppendHeader(@"Content-Disposition", @"attachment");


            System.Diagnostics.Debug.Print("************************************   NEW RESPONSE HEADERS   *******************************************");

            foreach (var header2 in cwv2Response.Headers)
            {
                System.Diagnostics.Debug.WriteLine(String.Concat("Key: ", header2.Key, "  Value: ", header2.Value));
            }
            System.Diagnostics.Debug.Print("************************************   NEW RESPONSE HEADERS   *******************************************");

            return cwv2Response;
        }

        private HttpRequestMessage ConvertRequest(CoreWebView2WebResourceRequest request)
        {
            HttpRequestMessage req = new((HttpMethod.Get), request.Uri);

            foreach (var header in request.Headers)
            {
                req.Headers.Add(header.Key, header.Value);
            }
            return req;
        }

        private void chk_Normal_Click(object sender, RoutedEventArgs e)
        {
            this.chk_CopyToMemoryStream.IsChecked = false;
            this.chk_HoldStream.IsChecked = false;
         
        }

        private void chk_HoldStream_Click(object sender, RoutedEventArgs e)
        {
            this.chk_CopyToMemoryStream.IsChecked = false;
            this.chk_Normal.IsChecked = false;
        }

        private void chk_CopyToMemoryStream_Click(object sender, RoutedEventArgs e)
        {
            this.chk_HoldStream.IsChecked = false;
            this.chk_Normal.IsChecked = false;
        }
    }
}
