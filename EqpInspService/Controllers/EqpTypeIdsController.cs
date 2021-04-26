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
	public class EqpTypeId
	{
		public string EqpType { get; set; }
		public string EqpId { get; set; }
	}

	public class EqpTypeIdsController : ApiController
	{
		public IEnumerable<EqpTypeId> Get()
		{
			EqpTypeId[] eqptypeids = new EqpTypeId[0];

			return eqptypeids;
		}

		// GET eqpapi/<controller>/20200403/D
		public IEnumerable<EqpTypeId> Get(string stdate, string interval)
		{
			string m2mConnString = ConfigurationManager.ConnectionStrings["m2mconn"].ConnectionString;

			EqpTypeId[] eqpTypeIds = new EqpTypeId[0];

			string streportdate = stdate.Substring(0, 4) + "/" + stdate.Substring(4, 2) + "/" + stdate.Substring(6, 2);

			try
			{
				using (OracleConnection m2mConn = new OracleConnection(m2mConnString))
				{
					m2mConn.Open();

					string sql = "select\r\n" +
								 " eqptype,\r\n" +
								 " eqpid\r\n" +
								 "from\r\n" +
								 " eqpinsp\r\n" +
								 "where\r\n" +
								" streportdate = '" + streportdate + "'\r\n" +
								" and interval='" + interval + "'\r\n" +
								 "order by\r\n" +
								 " eqptype,\r\n" +
								 " eqpid\r\n";

					using (OracleCommand oraCmd = new OracleCommand(sql, m2mConn))
					using (OracleDataReader oraReader = oraCmd.ExecuteReader())
					{
						while (oraReader.Read())
						{
							EqpTypeId eqptypeid = new EqpTypeId();

							eqptypeid.EqpType = oraReader["eqptype"].ToString();
							eqptypeid.EqpId = oraReader["eqpid"].ToString();

							Array.Resize(ref eqpTypeIds, eqpTypeIds.Length + 1);
							eqpTypeIds[eqpTypeIds.Length - 1] = eqptypeid;
						}
					}
				}
			}
			catch ( Exception exp )
			{
				System.Diagnostics.Debug.WriteLine(exp.Message);
			}

			return eqpTypeIds;
		}
	}
}