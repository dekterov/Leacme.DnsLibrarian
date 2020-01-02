// Copyright (c) 2017 Leacme (http://leac.me). View LICENSE.md for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Threading;
using DNS.Protocol.ResourceRecords;
using Leacme.Lib.DnsLibrarian;

namespace Leacme.App.DnsLibrarian {

	public class AppUI {

		private StackPanel rootPan = (StackPanel)Application.Current.MainWindow.Content;
		private (StackPanel holder, TextBlock label, TextBox field, Button button) inputPan = App.HorizontalFieldWithButton;
		private List<IPAddress> dnsIPs = new List<IPAddress>();
		private CollatedRecords retrievedRecords;
		private TabControl outputTabs = App.TabControl;

		public AppUI() {
			Library lib = new Library();

			(StackPanel holder, TextBlock label, ComboBox comboBox) dnsSelectorPanel = App.ComboBoxWithLabel;
			var dnsSelector = dnsSelectorPanel.holder.Children.OfType<ComboBox>().First();
			dnsSelectorPanel.holder.Children.OfType<TextBlock>().First().Text = "Using DNS Resolver:";
			dnsSelector.Items = new List<ComboBoxItem>() {
				new ComboBoxItem() { Content = "8.8.8.8" },
				new ComboBoxItem() { Content = "1.1.1.1" },
				new ComboBoxItem() { Content = "64.6.64.6" }
				 };

			((AvaloniaList<object>)outputTabs.Items).Clear();
			outputTabs.Height = 300;
			rootPan.Children.AddRange(new List<IControl> { dnsSelectorPanel.holder, inputPan.holder, outputTabs });

			Dispatcher.UIThread.InvokeAsync(async () => {
				((App)Application.Current).LoadingBar.IsIndeterminate = true;
				(await lib.GetLocalDNSAdresses()).ForEach(z => {
					dnsSelector.Items = Enumerable.Prepend(dnsSelector.Items.Cast<ComboBoxItem>(), new ComboBoxItem() { Content = z.ToString() }).ToList();
				});
				((App)Application.Current).LoadingBar.IsIndeterminate = false;
				dnsSelector.SelectedItem = ((List<ComboBoxItem>)dnsSelector.Items).First();
				((List<ComboBoxItem>)dnsSelector.Items).ForEach(z => { dnsIPs.Add(IPAddress.Parse(z.Content.ToString())); });
			});

			inputPan.holder.Children.OfType<TextBlock>().First().Text = "Domain to query:";
			var domInputBox = inputPan.holder.Children.OfType<TextBox>().First();
			domInputBox.Watermark = "example.com";
			domInputBox.Width = 600;
			var inputBut = inputPan.holder.Children.OfType<Button>().First();
			inputBut.Content = "Lookup Details";

			inputBut.Click += async (z, zz) => {
				if (!string.IsNullOrWhiteSpace(domInputBox.Text)) {
					try {
						if (domInputBox.Text.StartsWith("http://")) {
							domInputBox.Text = domInputBox.Text.Split(new string[] { "http://" }, StringSplitOptions.None).Last();
						} else if (domInputBox.Text.StartsWith("https://")) {
							domInputBox.Text = domInputBox.Text.Split(new string[] { "https://" }, StringSplitOptions.None).Last();
						}
						domInputBox.Text = string.Concat(domInputBox.Text.TakeWhile((c) => c != '/'));

						UriBuilder url = new UriBuilder(domInputBox.Text);
						inputBut.IsEnabled = false;
						((App)Application.Current).LoadingBar.IsIndeterminate = true;
						inputBut.IsEnabled = true;
						retrievedRecords = await lib.GetRecordsOfDomain(url.Uri, dnsIPs);
						PopulateTabs();
						((App)Application.Current).LoadingBar.IsIndeterminate = false;
					} catch (Exception e) {
						if (e is UriFormatException || e is InvalidOperationException) {
							((App)Application.Current).LoadingBar.IsIndeterminate = false;
							var wrongUrlWin = App.NotificationWindow;
							wrongUrlWin.Title = "Error";
							((StackPanel)wrongUrlWin.Content).Children.OfType<TextBlock>().First().Text = e.Message;
							await wrongUrlWin.ShowDialog<Window>(Application.Current.MainWindow);
						}
					}
				}
			};
		}

		private void PopulateTabs() {
			((AvaloniaList<object>)outputTabs.Items).Clear();
			if (retrievedRecords.CanonicalNameResourceRecords.Any()) {
				PopulateTab("CNAME Records", retrievedRecords.CanonicalNameResourceRecords);
			}
			if (retrievedRecords.IPAddressResourceRecords.Any()) {
				PopulateTab("A/AAAA Records", retrievedRecords.IPAddressResourceRecords);
			}
			if (retrievedRecords.MailExchangeResourceRecords.Any()) {
				PopulateTab("MX Records", retrievedRecords.MailExchangeResourceRecords);
			}
			if (retrievedRecords.NameServerResourceRecords.Any()) {
				PopulateTab("NS Records", retrievedRecords.NameServerResourceRecords);
			}
			if (retrievedRecords.PointerResourceRecords.Any()) {
				PopulateTab("PTR Records", retrievedRecords.PointerResourceRecords);
			}
			if (retrievedRecords.StartOfAuthorityResourceRecords.Any()) {
				PopulateTab("SOA Records", retrievedRecords.StartOfAuthorityResourceRecords);
			}
			if (retrievedRecords.TextResourceRecords.Any()) {
				PopulateTab("TXT Records", retrievedRecords.TextResourceRecords);
			}
			if (retrievedRecords.IPAddresses.Any()) {
				PopulateTab("IP Addresses", (IList<BaseResourceRecord>)null, retrievedRecords.IPAddresses);
			}
		}

		private void PopulateTab<T>(string tabHeader, IList<T> records, IList<IPAddress> ips = null) where T : BaseResourceRecord {

			var tab = new TabItem() { Header = tabHeader, Content = App.ScrollViewer };
			var tabPanel = new StackPanel() { Spacing = 7, Margin = new Thickness(0, 7) };
			((ScrollViewer)tab.Content).HorizontalAlignment = HorizontalAlignment.Stretch;
			((ScrollViewer)tab.Content).VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

			((ScrollViewer)tab.Content).Content = tabPanel;
			((AvaloniaList<object>)outputTabs.Items).Add(tab);

			if (records != null) {
				foreach (var record in records) {

					var dataBox = App.TextBox;
					dataBox.Width = 900;
					dataBox.Text = record.ToString().Substring(1, record.ToString().Length - 2);
					tabPanel.Children.Add(dataBox);
				}
			}
			if (ips != null) {
				foreach (var ip in ips) {
					var tb = App.TextBox;
					tb.Width = 900;
					tb.Text = ip.ToString();
					tabPanel.Children.Add(tb);
				}
			}
		}
	}
}