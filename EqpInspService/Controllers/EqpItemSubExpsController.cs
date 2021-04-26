using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Oracle.ManagedDataAccess.Client;
using EqpInspService.Models;

namespace EqpInspService.Controllers
{
	public class EqpItemSubExpsController : ApiController
	{
		// GET eqpapi/<controller>/DEB_T6577/00102/2
		public IEnumerable<EqpItemSubExp> Get(string eqptype, string itemcode, string seqnum)
		{
			string m2mConnString = ConfigurationManager.ConnectionStrings["m2mconn"].ConnectionString;

			EqpItemSubExp[] eqpItemSubExps = new EqpItemSubExp[0];

			try
			{
				using (OracleConnection m2mConn = new OracleConnection(m2mConnString))
				{
					m2mConn.Open();

					string sql = "select\r\n" +
								 " eqpitemsubmst.eqptype,\r\n" +
								 " eqpitemsubmst.itemcode,\r\n" +
								 " eqpitemsubmst.seqnum,\r\n" +
								 " eqpitemsubmst.subitemname,\r\n" +
								 " eqpitemsubmst.judgmentcriteria,\r\n" +
								 " eqpitemsubmst.inspectionpoint,\r\n" +
								 " eqpitemsubmst.beftytle,\r\n" +
								 " eqpitemsubmst.afttytle,\r\n" +
								 " eqpitemsubexpmst.expseqnum,\r\n" +
								 " eqpitemsubexpmst.itemlabel\r\n" +
								 "from\r\n" +
								 " eqpitemsubmst left outer join eqpitemsubexpmst on (eqpitemsubmst.eqptype = eqpitemsubexpmst.eqptype) and(eqpitemsubmst.itemcode = eqpitemsubexpmst.itemcode) and(eqpitemsubmst.seqnum = eqpitemsubexpmst.seqnum)\r\n" +
								 "where\r\n" +
								 " (eqpitemsubmst.eqptype = '" + eqptype + "') and\r\n" +
								 " (eqpitemsubmst.itemcode = '" + itemcode + "') and\r\n" +
								 " (eqpitemsubmst.seqnum = " + seqnum + ")\r\n" +
								 "order by\r\n" +
								 " eqpitemsubmst.seqnum,\r\n" +
								 " eqpitemsubexpmst.expseqnum\r\n";

					using (OracleCommand oraCmd = new OracleCommand(sql, m2mConn))
					using (OracleDataReader oraReader = oraCmd.ExecuteReader())
					{
						while (oraReader.Read())
						{
							EqpItemSubExp eqpitemsubexp = new EqpItemSubExp();

							eqpitemsubexp.EqpType = oraReader["eqptype"].ToString();
							eqpitemsubexp.ItemCode = oraReader["itemcode"].ToString();
							eqpitemsubexp.SeqNum = oraReader["seqnum"].ToString();
							eqpitemsubexp.SubItemName = oraReader["subitemname"].ToString();
							eqpitemsubexp.JudgementCriteria = oraReader["judgmentcriteria"].ToString();
							eqpitemsubexp.InspectionPoint = oraReader["inspectionpoint"].ToString();
							eqpitemsubexp.BefTitle = oraReader["beftytle"].ToString();
							eqpitemsubexp.AftTitle = oraReader["afttytle"].ToString();
							eqpitemsubexp.ExpSeqNum = oraReader["expseqnum"].ToString();
							eqpitemsubexp.ItemLabel = oraReader["itemlabel"].ToString();

							Array.Resize(ref eqpItemSubExps, eqpItemSubExps.Length + 1);
							eqpItemSubExps[eqpItemSubExps.Length - 1] = eqpitemsubexp;
						}
					}
				}
			}
			catch (Exception exp)
			{
				System.Diagnostics.Debug.WriteLine(exp.Message);
			}

			return eqpItemSubExps;
		}
	}
}