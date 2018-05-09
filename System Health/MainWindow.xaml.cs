using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace OuterBanks.software.SystemHealth
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private BitmapImage ok;
        private BitmapImage error;

        private WatchedItems items;

        public MainWindow()
        {
            InitializeComponent();

            ok = new BitmapImage();
            ok.BeginInit();
            ok.UriSource = new Uri(@"pack://application:,,,/ok.png");
            ok.DecodePixelWidth = 24;
            ok.EndInit();

            error = new BitmapImage();
            error.BeginInit();
            error.UriSource = new Uri(@"pack://application:,,,/error.png");
            error.DecodePixelWidth = 24;
            error.EndInit();

            items = new WatchedItems();

            foreach(WatchedItem wi in items.Items)
            {
                IconAndLabel child = new IconAndLabel();
                child.label.Content = wi.DisplayName;

                ItemList.Children.Add(child);
            }

            dispatcherTimer_Tick(null, null);

            DispatcherTimer dispatchTimer = new DispatcherTimer();
            dispatchTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatchTimer.Interval = new TimeSpan(0,0,10);
            dispatchTimer.Start();
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            foreach(IconAndLabel il in ItemList.Children) {
                WatchedItem wi = items.Items.ElementAt(ItemList.Children.IndexOf(il));

                Boolean result = false;

                switch(wi.Type)
                {
                    case "process":
                        result = CheckForProcess(wi.Identifier);
                        break;
                    case "device":
                        result = CheckForDevice(wi.Identifier);
                        break;
                    case "network":
                        result = CheckForNetwork(wi.Identifier);
                        break;
                    case "url":
                        result = CheckForUrl(wi.Identifier);
                        break;
                }

                il.icon.Source = (result) ? ok : error;
            }
        }

        private Boolean CheckForProcess(String identifier)
        {
            Process[] processList = Process.GetProcessesByName(identifier);

            if (processList.Length == 0) return false;

            return true;
        }

        private Boolean CheckForDevice(String identifier)
        {
            ManagementObjectCollection list;
            SelectQuery q = new SelectQuery("Win32_PnpEntity", "DeviceID = '" + identifier.Replace(@"\", @"\\") + "'");
            using (var searcher = new ManagementObjectSearcher(q))
                list = searcher.Get();

                if (list.Count == 0) return false;

            return true;
        }

        private Boolean CheckForNetwork(String identifier)
        {
            if(identifier.Equals("gateway"))
            {
                return CheckDefaultGateway();
            }

            try
            {
                PingReply reply = new Ping().Send(identifier);

                if (reply.Status == IPStatus.Success)
                {
                    return true;
                }

            }
            catch (Exception e)
            {
                //TODO: Log exceptions
            }

            return false;
        }

        private Boolean CheckForUrl(String identifier)
        {
            Boolean result = false;
            WebRequest request;
            WebResponse response = null;
            try
            {
                request = WebRequest.Create(identifier);
                response = request.GetResponse();

                if(response.GetType() == typeof(HttpWebResponse))
                {
                    HttpStatusCode status = ((HttpWebResponse)response).StatusCode;
                    if ( (int)status >= 200 && (int)status <= 299)
                    {
                        return true;
                    }
                } else if(response.GetType() == typeof(FtpWebResponse))
                {
                    FtpStatusCode status = ((FtpWebResponse)response).StatusCode;
                    if ((int)status <= 399)
                    {
                        return true;
                    }

                } else
                {
                    // No exception, assume good.
                    return true;
                }

            }
            catch(Exception e)
            {
                //TODO: log exceptions
            }
            finally
            {
                if(response != null)
                    try
                    {
                        response.Close();
                    }
                    catch (NotSupportedException e)
                    {
                        // TODO: Log or ignore?
                    }
            }

            return result;
        }

        public static Boolean CheckDefaultGateway()
        {
            try
            {
                IPAddress gateway = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(n => n.OperationalStatus == OperationalStatus.Up)
                    .Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    .SelectMany(n => n.GetIPProperties()?.GatewayAddresses)
                    .Select(g => g?.Address)
                    .Where(a => a != null)
                    .FirstOrDefault();

                PingReply reply = new Ping().Send(gateway);

                if(reply.Status == IPStatus.Success)
                {
                    return true;
                }

            } catch (Exception e)
            {
                //TODO: Log exceptions
            }

            return false;
        }
    }
}
