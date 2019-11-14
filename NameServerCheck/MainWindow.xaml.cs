using Bdev.Net.Dns;
using Bdev.Net.Dns.Records;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Net.NetworkInformation;
using System.Globalization;

namespace NameServerCheck
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                string resourceName = new AssemblyName(args.Name).Name + ".dll";
                string resource = Array.Find(this.GetType().Assembly.GetManifestResourceNames(), element => element.EndsWith(resourceName));

                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource))
                {
                    Byte[] assemblyData = new Byte[stream.Length];
                    stream.Read(assemblyData, 0, assemblyData.Length);
                    return Assembly.Load(assemblyData);
                }
            };

            InitializeComponent();
        }

        #region Eventhandler Buttons

        /// <summary>
        /// Lookup
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Button_Click(object sender, RoutedEventArgs e)
        {
            if(domainTextBox.Text != string.Empty)
            {
                WhoIsLookup(ReturnConvertedDomain(domainTextBox.Text));

                nameserverTextBox.Text = GetDnsRecord(GetDomain(), DnsQType.SOA);
                nameserverTextBox.Text += GetDnsRecord(GetDomain(), DnsQType.NS);
                nameserverTextBox.Text += GetDnsRecord(GetDomain(), DnsQType.MX);

                List<string> aAndCNameRecords = new List<string>();
                aAndCNameRecords.Add(GetDnsRecord(GetDomain(), DnsQType.A));
                aAndCNameRecords.Add(GetDnsRecord(GetDomain("www"), DnsQType.A));
                aAndCNameRecords.Add(GetDnsRecord(GetDomain("ftp"), DnsQType.A));
                aAndCNameRecords.Add(GetDnsRecord(GetDomain("mail"), DnsQType.A));
                aAndCNameRecords.Add(GetDnsRecord(GetDomain("pop"), DnsQType.A));
                aAndCNameRecords.Add(GetDnsRecord(GetDomain("pop3"), DnsQType.A));
                aAndCNameRecords.Add(GetDnsRecord(GetDomain("imap"), DnsQType.A));
                aAndCNameRecords.Add(GetDnsRecord(GetDomain("smtp"), DnsQType.A));
                aAndCNameRecords.Add(GetDnsRecord(GetDomain("webmail"), DnsQType.A));
                aAndCNameRecords.Add(GetDnsRecord(GetDomain("autodiscover"), DnsQType.A));
                aCnameRecords.Text = string.Join(Environment.NewLine, aAndCNameRecords.FindAll(f => !string.IsNullOrWhiteSpace(f.Trim())));

                txtRecords.Text = GetDnsRecord(GetDomain(), DnsQType.TXT);
                srvRecords.Text = GetDnsRecord(GetDomain(), DnsQType.SRV);
            }
        }

        #endregion

        /// <summary>
        /// Converts Domain special characters
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public string ReturnConvertedDomain(string uri)
        {
            string[] domain = { uri };
            IdnMapping idn = new IdnMapping();

            foreach (var name in domain)
            {
                try
                {
                    string punyCode = idn.GetAscii(name);
                    string name2 = idn.GetUnicode(punyCode);
                    return punyCode;
                }
                catch (ArgumentException)
                {
                    MessageBox.Show("{0} is not a valid domain name.", name);
                }
            }
            return domainTextBox.Text;
        }

        public void WhoIsLookup(string uri)
        {
            whoIsTextBox.Text = string.Empty;
            try
            {
                using (TcpClient whoisClient = new TcpClient())
                {
                    whoisClient.Connect(DomainLookup(ReturnEndingFromDomain(uri)), 43);

                    string strDomain = uri.ToString() + Environment.NewLine;
                    byte[] arrDomain = Encoding.ASCII.GetBytes(strDomain.ToCharArray());

                    Stream objStream = whoisClient.GetStream();
                    objStream.Write(arrDomain, 0, strDomain.Length);

                    StreamReader objSr = new StreamReader(whoisClient.GetStream(), Encoding.ASCII);

                    string strServerResponse = string.Empty;

                    List<string> whoisData = new List<string>();
                    while (null != (strServerResponse = objSr.ReadLine()))
                    {
                        whoIsTextBox.Text += strServerResponse + Environment.NewLine;
                    }
                }
            }
            catch (Exception ex)
            {
                whoIsTextBox.Text = ex.ToString();
            }
        }

        /// <summary>
        /// Gets DNSType
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public string GetDnsRecord(string uri, DnsQType type)
        {
            string result = string.Empty;
            Request req = new Request();
            Question q = new Question(uri, type, DnsClass.IN);
            req.AddQuestion(q);
            try
            {
                Response r = Resolver.Lookup(req, GetDnsServer());
                foreach (Answer a in r.Answers)
                {
                    switch (a.Type)
                    {
                        case DnsType.A:
                            result += string.Format("{0} has IPv4 address {1} (ttl: {2}){3}",
                                a.Domain, a.Record, a.Ttl, Environment.NewLine);
                            break;
                        case DnsType.NS:
                            result += string.Format("{0} nameserver {1}; ttl: {2}{3}",
                                a.Domain, a.Record, a.Ttl, Environment.NewLine);
                            break;
                        case DnsType.CNAME:
                            result += string.Format("{0} alias of {1} (ttl: {2}){3}",
                                a.Domain, a.Record, a.Ttl, Environment.NewLine);
                            break;
                        case DnsType.SOA:
                            result += string.Format("typeid:\t\t{0}\nttl:\t\t{1}\ndata:\t\t{2}\ndomain:\t\t{3}\nserial:\t\t{4}\nrefresh:\t\t{5}\nretry:" +
                                "\t\t{6}\nexpiry:\t\t{7}\nminttl:\t\t{8}\nresponsible:\t{9}\n\n", a.Type, a.Ttl,
                                ((Bdev.Net.Dns.Records.SoaRecord)a.Record).PrimaryNameServer, uri,
                                ((Bdev.Net.Dns.Records.SoaRecord)a.Record).Serial, ((Bdev.Net.Dns.Records.SoaRecord)a.Record).Refresh,
                                ((Bdev.Net.Dns.Records.SoaRecord)a.Record).Retry, ((Bdev.Net.Dns.Records.SoaRecord)a.Record).Expire,
                                ((Bdev.Net.Dns.Records.SoaRecord)a.Record).DefaultTtl, ((Bdev.Net.Dns.Records.SoaRecord)a.Record).ResponsibleMailAddress);
                            break;
                        case DnsType.MX:
                            result += string.Format("\n{0} mailserver {1} (pri={2}); ttl: {3}", a.Domain,
                                ((Bdev.Net.Dns.Records.MXRecord)a.Record).DomainName, ((Bdev.Net.Dns.Records.MXRecord)a.Record).Preference, a.Ttl);
                            break;
                        case DnsType.TXT:
                            result += string.Format("{0}:\t{1} (ttl: {2}){3}",
                                a.Domain, a.Record, a.Ttl, Environment.NewLine);
                            break;
                        case DnsType.AAAA:
                            result += string.Format("{0} has IPv6 address {1} (ttl: {2}){3}",
                                a.Domain, a.Record, a.Ttl, Environment.NewLine);
                            break;
                        case DnsType.SRV:
                            result += string.Format("{0}:\t{1} (ttl: {2}){3}",
                                a.Domain, a.Record, a.Ttl, Environment.NewLine);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                if (type == DnsQType.A)
                    return GetDnsRecord(uri, DnsQType.CNAME);

                return uri + ": " + ex.Message + Environment.NewLine;
            }
            return result;
        }

        private IPAddress GetDnsServer()
        {
            IPAddress dnsServer;
            if (IPAddress.TryParse(nameServerComboBox.Text, out dnsServer) == false)
            {
                MessageBox.Show("Invalid DNS Server Address", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return dnsServer;
        }

        private IPAddress GetLocalDnsServer()
        {
            try
            {
                NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
                foreach (NetworkInterface ni in nics)
                {
                    if (ni.OperationalStatus == OperationalStatus.Up)
                    {
                        IPAddressCollection ips = ni.GetIPProperties().DnsAddresses;
                        foreach (System.Net.IPAddress ip in ips)
                        {
                            return ip;
                        }
                    }
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Local DNS not found", "System",MessageBoxButton.OK,MessageBoxImage.Error,MessageBoxResult.OK);
            }
            return null;
        }

        private string GetDomain(string host = null)
        {
            string result = string.Empty;
            if (!string.IsNullOrEmpty(host))
                result += host + ".";

            result += ReturnConvertedDomain(domainTextBox.Text);

            return result;
        }

        private string ReturnEndingFromDomain(string value)
        {
            string domainFromText = domainTextBox.Text;
            domainFromText = domainFromText.Substring(domainFromText.LastIndexOf('.'));
            domainFromText = domainFromText.Substring(1);
            return domainFromText;
        }

        private string DomainLookup(string key)
        {
            string[] data;
            Dictionary<string, string> ServerList = new Dictionary<string, string>();
            string file = Properties.Resources.whois_servers;
            foreach (var line in file.Split(new string[] { System.Environment.NewLine }, StringSplitOptions.None))
            {
                if (!line.StartsWith(";"))
                {
                    data = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (!ServerList.ContainsKey(data[0]))
                        ServerList.Add(data[0], data[1]);
                }
            }

            if (ServerList.ContainsKey(key))
                return ServerList[key];

            return "whois.nic.ch";
        }

        /// <summary>
        /// Start Lookup with Enter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DomainTextBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
                Button_Click(sender, null);
        }

        /// <summary>
        /// Loads Window Parameters
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            FormHelper.LoadFormSettings(this);
            CurrentDNS.Content = GetLocalDnsServer();
        }
        
        /// <summary>
        /// Saves Window Parameters
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            FormHelper.SaveFormSettings(this);
        }        
    }
}
