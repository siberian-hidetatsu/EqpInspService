﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EqpInspService.Models
{
#if true
	public class EqpInspSubExpItem
	{
		public string ExpSeqNum { get; set; }
		public string ItemLabel { get; set; }
		public string BefValue { get; set; }
		public string AftValue { get; set; }
	}

	public class EqpInspSubItem
	{
		public string SeqNum { get; set; }
		public string SubItemName { get; set; }
		public string SubItemImg { get; set; }
		public string JudgementCriteria { get; set; }
		public string InspectionPoint { get; set; }
		public string BefTitle { get; set; }
		public string AftTitle { get; set; }
		public string BefResult { get; set; }
		public string AftResult { get; set; }

		public EqpInspSubExpItem[] EqpInspSubExpItems;

	}

	public class EqpInpsItem
	{
		public string EqpType { get; set; }
		public string ItemCode { get; set; }
		public string ItemName { get; set; }

		public EqpInspSubItem[] EqpInspSubItems;
	}

#else
	public class EqpInpsSubList
	{
		public string EqpType { get; set; }
		public string ItemCode { get; set; }
		public string ItemName { get; set; }
		public string SeqNum { get; set; }
		public string SubItemName { get; set; }
		public string JudgementCriteria { get; set; }
		public string InspectionPoint { get; set; }
		public string BefTitle { get; set; }
		public string AftTitle { get; set; }
		public string ExpSeqNum { get; set; }
		public string ItemLabel { get; set; }
		public string BefValue { get; set; }
		public string AftValue { get; set; }
		public string BefResult { get; set; }
		public string AftResult { get; set; }
	}
#endif
}