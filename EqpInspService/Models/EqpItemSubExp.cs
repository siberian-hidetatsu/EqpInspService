using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EqpInspService.Models
{
	public class EqpItemSubExp
	{
		public string EqpType { get; set; }
		public string ItemCode { get; set; }
		public string SeqNum { get; set; }
		public string SubItemName { get; set; }
		public string JudgementCriteria { get; set; }
		public string InspectionPoint { get; set; }
		public string BefTitle { get; set; }
		public string AftTitle { get; set; }
		public string ExpSeqNum { get; set; }
		public string ItemLabel { get; set; }
	}
}