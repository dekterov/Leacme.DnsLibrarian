using System.Collections.Generic;
using System.Net;
using DNS.Protocol;
using DNS.Protocol.ResourceRecords;

namespace Leacme.Lib.DnsLibrarian {
	public class CollatedRecords {
		public IList<IPAddressResourceRecord> IPAddressResourceRecords { get; } = new List<IPAddressResourceRecord>();
		public IList<NameServerResourceRecord> NameServerResourceRecords { get; } = new List<NameServerResourceRecord>();
		public IList<CanonicalNameResourceRecord> CanonicalNameResourceRecords { get; } = new List<CanonicalNameResourceRecord>();
		public IList<StartOfAuthorityResourceRecord> StartOfAuthorityResourceRecords { get; } = new List<StartOfAuthorityResourceRecord>();
		public IList<PointerResourceRecord> PointerResourceRecords { get; } = new List<PointerResourceRecord>();
		public IList<MailExchangeResourceRecord> MailExchangeResourceRecords { get; } = new List<MailExchangeResourceRecord>();
		public IList<TextResourceRecord> TextResourceRecords { get; } = new List<TextResourceRecord>();
		public IList<IPAddress> IPAddresses { get; } = new List<IPAddress>();

		public CollatedRecords(IResponse queriedResponse, IList<IPAddress> returnedIPAddresses) {

			((List<IPAddress>)IPAddresses).AddRange(returnedIPAddresses);

			foreach (var ansRec in queriedResponse.AnswerRecords) {
				switch (ansRec.Type) {
					case RecordType.A:
					case RecordType.AAAA:
						IPAddressResourceRecords.Add((IPAddressResourceRecord)ansRec);
						break;
					case RecordType.NS:
						NameServerResourceRecords.Add((NameServerResourceRecord)ansRec);
						break;
					case RecordType.CNAME:
						CanonicalNameResourceRecords.Add((CanonicalNameResourceRecord)ansRec);
						break;
					case RecordType.SOA:
						StartOfAuthorityResourceRecords.Add((StartOfAuthorityResourceRecord)ansRec);
						break;
					case RecordType.PTR:
						PointerResourceRecords.Add((PointerResourceRecord)ansRec);
						break;
					case RecordType.MX:
						MailExchangeResourceRecords.Add((MailExchangeResourceRecord)ansRec);
						break;
					case RecordType.TXT:
						TextResourceRecords.Add((TextResourceRecord)ansRec);
						break;
					default:
						break;
				}
			}
		}
	}
}