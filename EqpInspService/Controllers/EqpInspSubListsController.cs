#define	UPDATE_20210519
#define	UPDATE_20210520
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.IO;
using System.Web;
using System.Web.Http;
using Oracle.ManagedDataAccess.Client;
using EqpInspService.Models;

namespace EqpInspService.Controllers
{
	public class EqpInspSubListsController : ApiController
	{
		// GET eqpapi/<controller>/
		// eqptype  = DEB_T6577
		// eqpid    = e20200403
		// stdate   = 20200403
		// interval = D
		// eddate   = 20200403
		// rvnum    = 1

#if UPDATE_20210520

		public EquipInspec Get(string eqptype, string eqpid, string stdate, string eddate, string interval)
#else
		public IEnumerable<EqpInpsItem> Get(string eqptype, string eqpid, string stdate, string eddate, string interval)
#endif
		{
			string m2mConnString = ConfigurationManager.ConnectionStrings["m2mconn"].ConnectionString;

			EqpInspListDataSet eqpInspListDataSet = new EqpInspListDataSet();

#if UPDATE_20210520
			EquipInspec equipInspec = new EquipInspec();
#else
			EqpInpsItem[] eqpInspItems = new EqpInpsItem[0];
#endif

			try
			{
				string streportdate = stdate.Substring(0, 4) + "/" + stdate.Substring(4, 2) + "/" + stdate.Substring(6, 2);
				string enreportdate = eddate.Substring(0, 4) + "/" + eddate.Substring(4, 2) + "/" + eddate.Substring(6, 2);

				if (string.IsNullOrEmpty(interval))
				{
					interval = "D";
				}

				string revisionnum = "1";

#if UPDATE_20210519
				bool allmaster = false;     // true: 履歴の有り無しに関わらず、全てのマスタを読み込む false:履歴ありのマスタのみ

				if (interval.Length > 1)
				{
					allmaster = (interval[1] == 'a');   // とりあえず、interval の２バイト目を allmaster フラグとして利用する。
					interval = interval[0].ToString();
				}
#endif

				using (OracleConnection m2mConn = new OracleConnection(m2mConnString))
				{
					m2mConn.Open();

					List<string[]> values = null;

					string sql;

#if UPDATE_20210519
					#region 装置点検履歴
					StringBuilder resultCaseEnd = new StringBuilder();
					string compreRESULT = ConfigurationManager.AppSettings["CompreRESULT"];
					Common.MakeCaseEndSentence(resultCaseEnd, compreRESULT, "result");

					sql =
						"select\r\n" +
						" eqpid,\r\n" +
						" eqpinsp.interval,\r\n" +
						" eqpinsp.eqptype,\r\n" +
						" serialnum,\r\n" +
						" assetnum,\r\n" +
						" to_char(streportdate,'yyyy/mm/dd') as streportdate,\r\n" +
						" to_char(enreportdate,'yyyy/mm/dd') as enreportdate,\r\n" +
						" floor,\r\n" +
						" imploperatorname,\r\n" +
						" respoperatorname,\r\n" +
						" " + resultCaseEnd + ",\r\n" +
						" eqpinsp.revisionnum,\r\n" +
						" em.inspectionname\r\n" +
						"from\r\n" +
						" eqpinsp\r\n" +
						" left outer join eqpmainmst_h em on (eqpinsp.eqptype = em.eqptype and eqpinsp.revisionnum = em.revisionnum)\r\n" +
						"where\r\n" +
						" eqpid='" + eqpid + "'\r\n" +
						" and streportdate = '" + streportdate + "'\r\n" +
						" and eqpinsp.interval='" + interval + "'\r\n"/* +
						" and enreportdate = '" + enreportdate + "'\r\n"*/;

					using (OracleDataAdapter da = new OracleDataAdapter(sql, m2mConn))
					{
						// 装置点検
						da.Fill(eqpInspListDataSet.EQPINSP);
					}
#if UPDATE_20210520
					if (eqpInspListDataSet.EQPINSP.Rows.Count == 0)
						return equipInspec;

					#region EqpInspRoot セット
					equipInspec.EqpType = eqpInspListDataSet.EQPINSP.First().EQPTYPE;
					equipInspec.InspectionName = eqpInspListDataSet.EQPINSP.First().INSPECTIONNAME;
					equipInspec.Result = eqpInspListDataSet.EQPINSP.First().RESULT;

					equipInspec.EqpInspItems = new EqpInpsItem[0];
					#endregion
#endif
					#endregion
#endif

					#region 点検項目履歴
					#region 装置点検項目履歴を読み込む
					sql =
						"select\r\n" +
						" eqpid,\r\n" +
						" interval,\r\n" +
						" to_char(streportdate,'yyyy/mm/dd') as streportdate,\r\n" +
						" to_char(enreportdate,'yyyy/mm/dd') as enreportdate,\r\n" +
						" itemcode,\r\n" +
						" seqnum,\r\n" +
						" nvl(befresult,' ') as befresult,\r\n" +
						" nvl(aftresult,' ') as aftresult\r\n" +
						"from\r\n" +
						" eqpinspitem\r\n" +
						"where\r\n" +
						" eqpid='" + eqpid + "'\r\n" +
						" and streportdate = '" + streportdate + "'\r\n" +
						" and interval='" + interval + "'\r\n" +
						//" and enreportdate = '" + enreportdate + "'\r\n" +
						"order by\r\n" +
						" itemcode,\r\n" +
						" seqnum\r\n";

					//logger.Info(StringForLog(MethodBase.GetCurrentMethod().Name, sql));

					using (OracleDataAdapter da = new OracleDataAdapter(sql, m2mConn))
					{
						// 装置点検項目
						da.Fill(eqpInspListDataSet.EQPINSPITEM);
					}
#if !UPDATE_20210520
					if (eqpInspListDataSet.EQPINSPITEM.Rows.Count == 0)
						return eqpInspItems;
#endif
					#endregion

					#region 装置点検拡張項目履歴を読み込む
					sql =
						"select\r\n" +
						" eqpid,\r\n" +
						" interval,\r\n" +
						" to_char(streportdate,'yyyy/mm/dd') as streportdate,\r\n" +
						" to_char(enreportdate,'yyyy/mm/dd') as enreportdate,\r\n" +
						" itemcode,\r\n" +
						" seqnum,\r\n" +
						" expseqnum,\r\n" +
						" nvl(befvalue,' ') as befvalue,\r\n" +
						" nvl(aftvalue,' ') as aftvalue\r\n" +
						"from\r\n" +
						" eqpinspitemexp\r\n" +
						"where\r\n" +
						" eqpid='" + eqpid + "'\r\n" +
						" and streportdate = '" + streportdate + "'\r\n" +
						" and interval='" + interval + "'\r\n" +
						//" and enreportdate = '" + enreportdate + "'\r\n" +
						"order by\r\n" +
						" itemcode,\r\n" +
						" seqnum,\r\n" +
						" expseqnum\r\n";

					//logger.Info(StringForLog(MethodBase.GetCurrentMethod().Name, sql));

					using (OracleDataAdapter da = new OracleDataAdapter(sql, m2mConn))
					{
						// 装置点検拡張項目
						da.Fill(eqpInspListDataSet.EQPINSPITEMEXP);
					}
					#endregion

					// リビジョンの有り無しによって変化する SQL
					string revision_column = null, revision_table = null, revision_con = null;
					if (string.IsNullOrEmpty(revisionnum))
					{
						revision_column = "null as revisionnum";
						revision_table = "";
						revision_con = "";
					}
					else
					{
						revision_column = "revisionnum";
						revision_table = "_h";
						revision_con = " and revisionnum=" + revisionnum + "\r\n";
					}

					#region 装置点検[拡張]項目(EQPINSPITEM[EXP])の履歴から遡って点検項目マスタを検索する
					var query_item =
						(from
						  n in eqpInspListDataSet.EQPINSPITEM.AsEnumerable()
						 select (string)n["itemcode"])
						.Union
						(from
						  n in eqpInspListDataSet.EQPINSPITEMEXP.AsEnumerable()
						 select (string)n["itemcode"])
						/*.OrderBy(n => n)*/;

					string[] itemcodes = query_item.ToArray();
					StringBuilder itemcode_con = new StringBuilder();
					foreach (var value in itemcodes)
					{
						itemcode_con.Append("'" + value + "',");
					}
					itemcode_con.Length -= 1;   // 1:,

					sql =
						"select\r\n" +
						" eqptype,\r\n" +
						" itemcode,\r\n" +
						" itemname,\r\n" +
						" subitem1,\r\n" +
						" subitem2,\r\n" +
						" subitem3,\r\n" +
						" " + revision_column + "\r\n" +
						"from\r\n" +
						" eqpitemmst" + revision_table + "\r\n" +
						"where\r\n" +
						" eqptype='" + eqptype + "'\r\n" +
#if UPDATE_20210519
						(allmaster ? "" : " and itemcode in (" + itemcode_con + ")\r\n") +
#else
						" and itemcode in (" + itemcode_con + ")\r\n" +
#endif
						revision_con +
						"order by\r\n" +
						" itemcode\r\n";

					//logger.Info(StringForLog(MethodBase.GetCurrentMethod().Name, sql));

					using (OracleDataAdapter da = new OracleDataAdapter(sql, m2mConn))
					{
						// 点検項目マスタ
						da.Fill(eqpInspListDataSet.EQPITEMMST_H);
					}
					#endregion

					#region 装置点検[拡張]項目(EQPINSPITEM[EXP])の履歴から遡って点検項目SUBマスタを検索する
					// 装置点検項目と装置点検拡張項目に含まれるに itemcode と seqnum の組み合わせを抽出する
					// linq distinct and select new query
					// https://stackoverflow.com/questions/9992117/linq-distinct-and-select-new-query
					var query_itemseq =
						(from
						  n in eqpInspListDataSet.EQPINSPITEM.AsEnumerable()
						 select Tuple.Create(n["itemcode"], n["seqnum"]))
						.Union
						(from
						  n in eqpInspListDataSet.EQPINSPITEMEXP.AsEnumerable()
						 select Tuple.Create(n["itemcode"], n["seqnum"]))
						/*.OrderBy(n => n.Item1)
						.ThenBy(n => n.Item2)*/;

					var query_itemseq_result = query_itemseq.Select(n => new string[] { (string)n.Item1, (string)n.Item2 });
					values = query_itemseq_result.ToList();

					StringBuilder itemcode_seqnum_con = new StringBuilder();
					foreach (var value in values)
					{
						itemcode_seqnum_con.Append("(itemcode='" + value[0] + "' and seqnum='" + value[1] + "') or ");
					}
					itemcode_seqnum_con.Length -= 4;    // 4:' or '
					itemcode_seqnum_con.Insert(0, "(");
					itemcode_seqnum_con.Append(")");

					sql =
						"select\r\n" +
						" eqptype,\r\n" +
						" itemcode,\r\n" +
						" seqnum,\r\n" +
						" subitemname,\r\n" +
						" subitemimg,\r\n" +
						" inspectionpoint,\r\n" +
						" judgmentcriteria,\r\n" +
						" beftytle,\r\n" +
						" afttytle,\r\n" +
						" comments,\r\n" +
						" autojudgeflg,\r\n" +
						" " + revision_column + "\r\n" +
						"from\r\n" +
						" eqpitemsubmst" + revision_table + "\r\n" +
						"where\r\n" +
						" eqptype='" + eqptype + "'\r\n" +
#if UPDATE_20210519
						(allmaster ? "" : " and " + itemcode_seqnum_con + "\r\n") +
#else
						" and " + itemcode_seqnum_con + "\r\n" +
#endif
						revision_con +
						"order by\r\n" +
						" itemcode,\r\n" +
						" seqnum\r\n";

					//logger.Info(StringForLog(MethodBase.GetCurrentMethod().Name, sql));

					using (OracleDataAdapter da = new OracleDataAdapter(sql, m2mConn))
					{
						// 点検項目SUBマスタ
						da.Fill(eqpInspListDataSet.EQPITEMSUBMST_H);
					}
					#endregion

					#region 点検項目SUBマスタに紐付いている点検項目SUB拡張マスタを検索する
					sql = "select\r\n" +
						  " eqptype,\r\n" +
						  " itemcode,\r\n" +
						  " seqnum,\r\n" +
						  " expseqnum,\r\n" +
						  " itemlabel,\r\n" +
						  " judgvalu1,\r\n" +
						  " judgvalu2,\r\n" +
						  " rangectg,\r\n" +
						  " " + revision_column + "\r\n" +
						  "from\r\n" +
						  " eqpitemsubexpmst" + revision_table + "\r\n" +
						  "where\r\n" +
						  " eqptype='" + eqptype + "'\r\n" +
						  " and " + itemcode_seqnum_con + "\r\n" +  // 2020/04/03 この条件があった方がいいかも。付け忘れてた？
						  revision_con +
						  "order by\r\n" +
						  " itemcode,\r\n" +
						  " seqnum,\r\n" +
						  " expseqnum\r\n";

					//logger.Info(StringForLog(MethodBase.GetCurrentMethod().Name, sql));

					using (OracleDataAdapter da = new OracleDataAdapter(sql, m2mConn))
					{
						// 点検項目SUB拡張マスタ
						da.Fill(eqpInspListDataSet.EQPITEMSUBEXPMST_H);
					}
					#endregion

					// ITEMCODE(点検項目コード) 分繰り返す
					foreach (EqpInspListDataSet.EQPITEMMST_HRow eqpitemmst_row in eqpInspListDataSet.EQPITEMMST_H.Rows.OfType<DataRow>())
					{
						var eqpitemsubmst_rows = eqpitemmst_row.GetChildRows("EQPITEMMST_EQPITEMSUBMST");
						if (eqpitemsubmst_rows.Length == 0)
							continue;

						string itemcode = eqpitemmst_row.ITEMCODE;
						string itemnm = DBNull.Value.Equals(eqpitemmst_row["ITEMNAME"]) ? "" : eqpitemmst_row.ITEMNAME;
						string subitem1 = DBNull.Value.Equals(eqpitemmst_row["SUBITEM1"]) ? "" : eqpitemmst_row.SUBITEM1;
						string subitem2 = DBNull.Value.Equals(eqpitemmst_row["SUBITEM2"]) ? "" : eqpitemmst_row.SUBITEM2;

						#region 点検項目ごとの <div> とタイトルの <span>
						#endregion

						#region EqpInspItem セット
						EqpInpsItem eqpInpsItem = new EqpInpsItem();

#if !UPDATE_20210520
						eqpInpsItem.EqpType = eqptype;
#endif
						eqpInpsItem.ItemCode = itemcode;
						eqpInpsItem.ItemName = itemnm;

						eqpInpsItem.EqpInspSubItems = new EqpInspSubItem[0];
						#endregion

						// SEQNUM(シーケンス) 分繰り返す
						foreach (EqpInspListDataSet.EQPITEMSUBMST_HRow eqpitemsubmst_row in eqpitemsubmst_rows)
						{
							string seqnum = DBNull.Value.Equals(eqpitemsubmst_row["SEQNUM"]) ? "" : eqpitemsubmst_row.SEQNUM;   // シーケンス
							string subitemname = DBNull.Value.Equals(eqpitemsubmst_row["SUBITEMNAME"]) ? "" : eqpitemsubmst_row.SUBITEMNAME;    // サブ点検項目名
							string subitemimg = DBNull.Value.Equals(eqpitemsubmst_row["SUBITEMIMG"]) ? "" : eqpitemsubmst_row.SUBITEMIMG;    // サブ点検画像
							string inspectionpoint = DBNull.Value.Equals(eqpitemsubmst_row["INSPECTIONPOINT"]) ? "" : eqpitemsubmst_row.INSPECTIONPOINT;    // 点検個所
							string judgmentcriteria = DBNull.Value.Equals(eqpitemsubmst_row["JUDGMENTCRITERIA"]) ? "" : eqpitemsubmst_row.JUDGMENTCRITERIA;    // 判定基準
							string beftytle = DBNull.Value.Equals(eqpitemsubmst_row["BEFTYTLE"]) ? "" : eqpitemsubmst_row.BEFTYTLE;    // 処置前項目のタイトル
							string afttytle = DBNull.Value.Equals(eqpitemsubmst_row["AFTTYTLE"]) ? "" : eqpitemsubmst_row.AFTTYTLE;    // 処置後項目のタイトル
							string comments = DBNull.Value.Equals(eqpitemsubmst_row["COMMENTS"]) ? "" : eqpitemsubmst_row.COMMENTS; // コメント

							string autojudgeflg = DBNull.Value.Equals(eqpitemsubmst_row["AUTOJUDGEFLG"]) ? "" : eqpitemsubmst_row.AUTOJUDGEFLG; // 自動判定フラグ

							string itemcode_seqnum = itemcode + "_" + seqnum;

							// シーケンスごとの <div>

							// 測定項目情報 <dl>

							// 空の <dt></dt>

							#region サブタイトル用の <dd> ==== ココから ====
							// サブタイトル用の <dd> ==== ココまで ====
							#endregion

							// 空の <dt></dt>

							#region サブ点検画像の <dd><img> ==== ココから ====
							string subitemimg_base64 = "";
							if (!string.IsNullOrEmpty(subitemimg.Trim()))
							{
								string _eqptype = eqptype.Replace('+', '_');
#if true
								try
								{
									// 画像を高品質に拡大／縮小するには？
									// https://www.atmarkit.co.jp/ait/articles/0305/02/news003.html

									string url = "http://t530_hyper_v/mietas-m2m" + "/Subitemimg Files" + "/" + _eqptype + "/" + subitemimg;

									System.Net.WebClient wc = new System.Net.WebClient();
									Stream stream = wc.OpenRead(url);
									System.Drawing.Bitmap src = new System.Drawing.Bitmap(stream);
									stream.Close();

									// 画像を縮小する
									int w = src.Width / 3;
									int h = src.Height / 3;

									System.Drawing.Bitmap dest = new System.Drawing.Bitmap(w, h);
									System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(dest);

									/*foreach (System.Drawing.Drawing2D.InterpolationMode im in Enum.GetValues(typeof(System.Drawing.Drawing2D.InterpolationMode)))
									{
										if (im == System.Drawing.Drawing2D.InterpolationMode.Invalid)
											continue;
										g.InterpolationMode = im;
										g.DrawImage(src, 0, 0, w, h);
										dest.Save("C:\\temp\\" + im.ToString() + ".png", System.Drawing.Imaging.ImageFormat.Png);
									}*/
									g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Default;
									g.DrawImage(src, 0, 0, w, h);
#if (DEBUG)
									dest.Save(@"c:\temp\" + _eqptype + "_" + subitemimg + ".png", System.Drawing.Imaging.ImageFormat.Png);
#endif

									// Bitmapからbyte[] 配列に変換する
									MemoryStream ms = new MemoryStream();
									dest.Save(ms, System.Drawing.Imaging.ImageFormat.Png);

									byte[] buff = ms.GetBuffer();

									// Base64エンコードで文字列に変換
									subitemimg_base64 = Convert.ToBase64String(buff);
								}
								catch (Exception exp)
								{
									System.Diagnostics.Debug.WriteLine(exp.Message);
								}
#else
								//string subitemimgFileName = MapPath("~/Subitemimg Files") + "\\" + _eqptype + "\\" + subitemimg;
								string subitemimgFileName = System.Web.Hosting.HostingEnvironment.MapPath("~/Subitemimg Files") + "\\" + _eqptype + "\\" + subitemimg;;
								if (File.Exists(subitemimgFileName))
								{
									/*dd = new HtmlGenericControl("dd");
									dl.Controls.Add(dd);

									System.Web.UI.WebControls.Image img = new System.Web.UI.WebControls.Image();
									img.ImageUrl = "~/Subitemimg Files" + "/" + _eqptype + "/" + subitemimg;
									dd.Controls.Add(img);

									dt = new HtmlGenericControl("dt");
									dl.Controls.Add(dt);*/

									// 画像ファイルを読み込む
									using (FileStream fs = new FileStream(subitemimgFileName, FileMode.Open, FileAccess.Read))
									{
										byte[] buff = new byte[fs.Length];

										// [注意]
										// 大きいファイル int 型の最大値以上はNG（未検証）
										int readBytes = fs.Read(buff, 0, (int)fs.Length);

										// Base64エンコードで文字列に変換
										subitemimg_base64 = Convert.ToBase64String(buff);
									}
								}
#endif
							}
							#endregion <dd><img> ==== ココまで ====

							#region 処置前の親要素の <dd> ==== ココから ====
							// ここに <div> で囲んだコントロールを追加していく
							#endregion 処置前の親要素の <dd> ==== ココまで ====

							#region 処置後の親要素の <dd> ==== ココから ====
							// ここに <div> で囲んだコントロールを追加していく
							#endregion 処置後の親要素の <dd> ==== ココまで ====

							#region 処置前項目のタイトルの <div> ==== ココから ====
							// 処置前項目のタイトルの <dd> ==== ココまで ====
							#endregion

							#region 処置後項目のタイトルの <div> ==== ココから ====
							// 処置後項目のタイトルの <div> ==== ココまで ====
							#endregion

							string befresult = "", aftresult = "";
							var eqpinspitem_rows = eqpitemsubmst_row.GetChildRows("EQPITEMSUBMST_EQPINSPITEM");
							if (eqpinspitem_rows.Any())
							{
								EqpInspListDataSet.EQPINSPITEMRow eqpinspitem_row = (EqpInspListDataSet.EQPINSPITEMRow)eqpinspitem_rows[0];
								befresult = eqpinspitem_row.BEFRESULT.Trim();
								aftresult = eqpinspitem_row.AFTRESULT.Trim();
							}

							#region EqpInspSubItem セット
							EqpInspSubItem eqpInspSubItem = new EqpInspSubItem();
							eqpInspSubItem.SeqNum = seqnum;
							eqpInspSubItem.SubItemName = subitemname;
							eqpInspSubItem.SubItemImg = subitemimg_base64;
							eqpInspSubItem.JudgementCriteria = judgmentcriteria;
							eqpInspSubItem.InspectionPoint = inspectionpoint;
							eqpInspSubItem.BefTitle = beftytle;
							eqpInspSubItem.AftTitle = afttytle;
							eqpInspSubItem.BefResult = befresult;
							eqpInspSubItem.AftResult = aftresult;

							eqpInspSubItem.EqpInspSubExpItems = new EqpInspSubExpItem[0];

							Array.Resize(ref eqpInpsItem.EqpInspSubItems, eqpInpsItem.EqpInspSubItems.Length + 1);
							eqpInpsItem.EqpInspSubItems[eqpInpsItem.EqpInspSubItems.Length - 1] = eqpInspSubItem;
							#endregion

							var eqpitemsubexpmst_rows = eqpitemsubmst_row.GetChildRows("EQPITEMSUBMST_EQPITEMSUBEXPMST");
							if (eqpitemsubexpmst_rows.Any())  // 何らかの点検項目の入力があった？
							{
								#region 処置前の <table>
								#endregion

								#region 処置後の <table>
								#endregion

								// EXPSEQNUM(拡張シーケンス) 分繰り返す
								foreach (EqpInspListDataSet.EQPITEMSUBEXPMST_HRow eqpitemsubexpmst_row in eqpitemsubexpmst_rows)
								{
									string expseqnum = DBNull.Value.Equals(eqpitemsubexpmst_row["EXPSEQNUM"]) ? "" : eqpitemsubexpmst_row.EXPSEQNUM;    // 拡張シーケンス
									string itemlabel = DBNull.Value.Equals(eqpitemsubexpmst_row["ITEMLABEL"]) ? "" : eqpitemsubexpmst_row.ITEMLABEL;    // 項目ラベル

									string itemcode_seqnum_expseqnum = itemcode_seqnum + "_" + expseqnum;

									string befvalue = "", aftvalue = "";
									var eqpinspitemexp_rows = eqpitemsubexpmst_row.GetChildRows("EQPITEMSUBEXPMST_EQPINSPITEMEXP");
									if (eqpinspitemexp_rows.Any())
									{
										EqpInspListDataSet.EQPINSPITEMEXPRow eqpinspitemexp_row = (EqpInspListDataSet.EQPINSPITEMEXPRow)eqpinspitemexp_rows[0];
										befvalue = eqpinspitemexp_row.BEFVALUE;
										aftvalue = eqpinspitemexp_row.AFTVALUE;
									}

									#region 処置前の <tr> ==== ココから ====
									// 処置前の <tr> ==== ココまで ====
									#endregion

									#region 処置後の <tr> ==== ココから ====
									// 処置後の <tr> ==== ココまで ====
									#endregion

									#region EqpInspSubExpItem セット
									EqpInspSubExpItem eqpInspSubExpItem = new EqpInspSubExpItem();
									eqpInspSubExpItem.ExpSeqNum = expseqnum;
									eqpInspSubExpItem.ItemLabel = itemlabel;
									eqpInspSubExpItem.BefValue = befvalue;
									eqpInspSubExpItem.AftValue = aftvalue;

									Array.Resize(ref eqpInspSubItem.EqpInspSubExpItems, eqpInspSubItem.EqpInspSubExpItems.Length + 1);
									eqpInspSubItem.EqpInspSubExpItems[eqpInspSubItem.EqpInspSubExpItems.Length - 1] = eqpInspSubExpItem;
									#endregion
								}

								#region 処置前の判定の <div> ==== ココから ====
								// 処置前の判定の <div> ==== ココまで ====
								#endregion

								#region 処置後の判定の <div> ==== ココから ====
								// 処置後の判定の <div> ==== ココまで ====
								#endregion
							}
							else
							{
								#region 処置前の判定の <div> ==== ココから ====
								// 処置前の判定の <div> ==== ココまで ====
								#endregion

								#region 処置後の判定の <div> ==== ココから ====
								// 処置後の判定の <div> ==== ココまで ====
								#endregion

								#region EqpInspSubExpItem セット
								EqpInspSubExpItem eqpInspSubExpItem = new EqpInspSubExpItem();
								eqpInspSubExpItem.ExpSeqNum = ""/*expseqnum*/;
								eqpInspSubExpItem.ItemLabel = ""/*itemlabel*/;
								eqpInspSubExpItem.BefValue = ""/*befvalue*/;
								eqpInspSubExpItem.AftValue = ""/*aftvalue*/;

								Array.Resize(ref eqpInspSubItem.EqpInspSubExpItems, eqpInspSubItem.EqpInspSubExpItems.Length + 1);
								eqpInspSubItem.EqpInspSubExpItems[eqpInspSubItem.EqpInspSubExpItems.Length - 1] = eqpInspSubExpItem;
								#endregion
							}

							#region bef|afttytle に -hidden- が設定されていた場合の処理
							#endregion
						}

#if UPDATE_20210520
						Array.Resize(ref equipInspec.EqpInspItems, equipInspec.EqpInspItems.Length + 1);
						equipInspec.EqpInspItems[equipInspec.EqpInspItems.Length - 1] = eqpInpsItem;
#else
						Array.Resize(ref eqpInspItems, eqpInspItems.Length + 1);
						eqpInspItems[eqpInspItems.Length - 1] = eqpInpsItem;
#endif
					}
					#endregion
				}
			}
			catch (Exception exp)
			{
				System.Diagnostics.Debug.WriteLine(exp.Message);
			}

#if UPDATE_20210520
			return equipInspec;
#else
			return eqpInspItems;
#endif
		}
	}

	public class Common
	{
		/// <summary>
		/// text$value 文字列を分解する
		/// </summary>
		/// <param name="textValue"></param>
		/// <param name="_text"></param>
		/// <param name="_value"></param>
		public static void GetTextValueFromSettings(string textValue, out string _text, out string _value)
		{
			if (textValue.IndexOf("$") == -1)
			{
				_text = _value = textValue;
			}
			else
			{
				string[] item = textValue.Split('$');
				_text = item[0];
				_value = item[1];
			}
		}

		/// <summary>
		/// text$value,text$value,... の文字列から case ～ end 分を作成する
		/// </summary>
		/// <param name="caseEnd"></param>
		/// <param name="textValues"></param>
		/// <param name="columnName"></param>
		public static void MakeCaseEndSentence(StringBuilder caseEnd, string textValues, string columnName)
		{
			if (textValues != null)
			{
				caseEnd.Append("case " + columnName + " ");

				foreach (var value in textValues.Split(','))
				{
					string _text, _value;
					GetTextValueFromSettings(value, out _text, out _value);
					caseEnd.Append("when '" + _value + "' then '" + _text + "' ");
				}

				caseEnd.Append("else " + columnName + " ");

				if (columnName.IndexOf('.') != -1)
				{
					columnName = columnName.Substring(columnName.IndexOf('.') + 1); // テーブル別名指定があれば削除する
				}
				caseEnd.Append("end " + columnName);
			}
			else
			{
				caseEnd.Append(columnName);
			}
		}
	}

}
