using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EqpInspService.Models
{
	public class EqpInsp
	{
		public string EqpType { get; set; }
		public string ItemCode { get; set; }
		public string ItemName { get; set; }
		public string SeqNum { get; set; }
		public string SubItemName { get; set; }
		public string JudgementCriteria { get; set; }
		public string InspectionPoint { get; set; }
	}
}