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
	public class EqpInspsController : ApiController
	{
		public IEnumerable<EqpInsp> Get()
		{
			EqpInsp[] eqpInsps = new EqpInsp[0];

			return eqpInsps;
		}

		// GET eqpapi/<controller>/DEB_T6577
		public IEnumerable<EqpInsp> Get(string eqptype)
		{
			string m2mConnString = ConfigurationManager.ConnectionStrings["m2mconn"].ConnectionString;

			EqpInsp[] eqpInsps = new EqpInsp[0];

			try
			{
				using (OracleConnection m2mConn = new OracleConnection(m2mConnString))
				{
					m2mConn.Open();

					string sql = "select\r\n" +
								 " eqpitemmst.eqptype,\r\n" +
								 " eqpitemmst.itemcode,\r\n" +
								 " eqpitemmst.itemname,\r\n" +
								 " eqpitemsubmst.seqnum,\r\n" +
								 " eqpitemsubmst.subitemname,\r\n" +
								 " eqpitemsubmst.judgmentcriteria,\r\n" +
								 " eqpitemsubmst.inspectionpoint\r\n" +
								 "from\r\n" +
								 " eqpitemmst inner join eqpmainmst on(eqpitemmst.eqptype = eqpmainmst.eqptype)\r\n" +
								 " inner join eqpitemsubmst on(eqpitemmst.eqptype = eqpitemsubmst.eqptype) and(eqpitemmst.itemcode = eqpitemsubmst.itemcode)\r\n" +
								 "where\r\n" +
								 " (eqpitemmst.eqptype = '" + eqptype + "')\r\n" +
								 "order by\r\n" +
								 " eqpitemmst.eqptype,\r\n" +
								 " eqpitemmst.itemcode,\r\n" +
								 " eqpitemsubmst.seqnum\r\n";

					using (OracleCommand oraCmd = new OracleCommand(sql, m2mConn))
					using (OracleDataReader oraReader = oraCmd.ExecuteReader())
					{
						while (oraReader.Read())
						{
							EqpInsp eqpinsp = new EqpInsp();

							eqpinsp.EqpType = oraReader["eqptype"].ToString();
							eqpinsp.ItemCode = oraReader["itemcode"].ToString();
							eqpinsp.ItemName = oraReader["itemname"].ToString();
							eqpinsp.SeqNum = oraReader["seqnum"].ToString();
							eqpinsp.SubItemName = oraReader["subitemname"].ToString();
							eqpinsp.JudgementCriteria = oraReader["judgmentcriteria"].ToString();
							eqpinsp.InspectionPoint = oraReader["inspectionpoint"].ToString();

							Array.Resize(ref eqpInsps, eqpInsps.Length + 1);
							eqpInsps[eqpInsps.Length - 1]  = eqpinsp;
						}
					}
				}
			}
			catch ( Exception exp )
			{
				System.Diagnostics.Debug.WriteLine(exp.Message);
			}

			return eqpInsps;
		}
	}
}