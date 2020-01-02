// Copyright (c) 2017 Leacme (http://leac.me). View LICENSE.md for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using DNS.Client;
using DNS.Protocol;

namespace Leacme.Lib.DnsLibrarian {

	public class Library {

		/// <summary>
		/// Queries local network interfaces and returns the DNS IPs of the ones that are up.
		/// /// </summary>
		/// <returns>List of IP addresses.</returns>
		public async Task<List<IPAddress>> GetLocalDNSAdresses() {
			List<IPAddress> redAddrs = new List<IPAddress>();
			foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces()) {
				if (ni.OperationalStatus.Equals(OperationalStatus.Up)) {
					foreach (var dns in ni.GetIPProperties().DnsAddresses) {
						redAddrs.Add(dns);
					}
				}
			}

			redAddrs = redAddrs.GroupBy(z => z.ToString()).Select(zz => zz.First()).Where(
				z => z.AddressFamily.Equals(AddressFamily.InterNetwork) //|| z.AddressFamily.Equals(AddressFamily.InterNetworkV6)
				).ToList();

			var retIps = await Task.Run(() => redAddrs.Where(z => {
				PingReply repl = null;
				try {
					repl = new Ping().Send(z);
				} catch {
					Console.Error.WriteLine("Swallowed ping exception.");
				}
				return repl?.Status.Equals(IPStatus.Success) == true;
			}).ToList());

			if (!retIps.Any()) {
				throw new InvalidOperationException("Unable to find local IPv4 DNS, cannot perform lookups.");
			}
			return retIps;
		}

		/// <summary>
		///	Gets the DNS records of a domain.
		/// /// </summary>
		/// <param name="domain">Domain to query for DNS records</param>
		/// <param name="localDNSIPs">List of local DNS Resolver IPs to do the lookup</param>
		/// <returns></returns>
		public async Task<CollatedRecords> GetRecordsOfDomain(Uri domain, List<IPAddress> localDNSIPs) {
			IList<IPAddress> respIPs = new List<IPAddress>();
			IResponse resp = null;

			foreach (var dnsIP in localDNSIPs) {
				DnsClient cli = new DnsClient(dnsIP);
				ClientRequest req = cli.Create();
				req.RecursionDesired = true;
				req.Questions.Add(new Question(Domain.FromString(domain.Host), RecordType.ANY));
				try {
					resp = await req.Resolve();
					respIPs = await cli.Lookup(domain.Host);
				} catch {
					Console.Error.WriteLine("Swallowed lookup exception.");
				}

				if (resp != null && resp.ResponseCode.Equals(ResponseCode.NoError)) {
					break;
				}
			}
			if (resp == null) {
				throw new InvalidOperationException("Unable to get a valid response from the domain, check input.");
			}
			return new CollatedRecords(resp, respIPs);
		}

	}
}