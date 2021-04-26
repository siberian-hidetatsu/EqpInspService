﻿using System;
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
#if true
		// GET eqpapi/<controller>/
		// eqptype  = DEB_T6577
		// eqpid    = e20200403
		// stdate   = 20200403
		// interval = D
		// eddate   = 20200403
		// rvnum    = 1

		public IEnumerable<EqpInpsItem> Get(string eqptype, string eqpid, string stdate, string eddate, string interval)
		{
			string m2mConnString = ConfigurationManager.ConnectionStrings["m2mconn"].ConnectionString;

			EqpInspListDataSet eqpInspListDataSet = new EqpInspListDataSet();

			EqpInpsItem[] eqpInspItems = new EqpInpsItem[0];

			try
			{
				string streportdate = stdate.Substring(0, 4) + "/" + stdate.Substring(4, 2) + "/" + stdate.Substring(6, 2);
				string enreportdate = eddate.Substring(0, 4) + "/" + eddate.Substring(4, 2) + "/" + eddate.Substring(6, 2);

				if (string.IsNullOrEmpty(interval))
				{
					interval = "D";
				}

				string revisionnum = "1";

				using (OracleConnection m2mConn = new OracleConnection(m2mConnString))
				{
					m2mConn.Open();

					List<string[]> values = null;

					string sql;

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

					if (eqpInspListDataSet.EQPINSPITEM.Rows.Count == 0)
						return eqpInspItems;
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
						" and itemcode in (" + itemcode_con + ")\r\n" +
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
						" and " + itemcode_seqnum_con + "\r\n" +
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

						eqpInpsItem.EqpType = eqptype;
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
								catch ( Exception exp )
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

						Array.Resize(ref eqpInspItems, eqpInspItems.Length + 1);
						eqpInspItems[eqpInspItems.Length - 1] = eqpInpsItem;
					}
					#endregion
				}
			}
			catch (Exception exp)
			{
				System.Diagnostics.Debug.WriteLine(exp.Message);
			}

			return eqpInspItems;
		}
	}
#else
		// GET eqpapi/<controller>/
		// eqptype  = DEB_T6577
		// eqpid    = e20200403
		// stdate   = 20200403
		// interval = D
		// eddate   = 20200403
		// rvnum    = 1

		public IEnumerable<EqpInpsSubList> Get(string eqptype, string eqpid, string stdate, string eddate, string interval)
		{
			string m2mConnString = ConfigurationManager.ConnectionStrings["m2mconn"].ConnectionString;

			EqpInspListDataSet eqpInspListDataSet = new EqpInspListDataSet();

			EqpInpsSubList[] eqpInspSubLists = new EqpInpsSubList[0];

			try
			{
				string streportdate = stdate.Substring(0, 4) + "/" + stdate.Substring(4, 2) + "/" + stdate.Substring(6, 2);
				string enreportdate = eddate.Substring(0, 4) + "/" + eddate.Substring(4, 2) + "/" + eddate.Substring(6, 2);

				if (string.IsNullOrEmpty(interval))
				{
					interval = "D";
				}

				string revisionnum = "1";

				using (OracleConnection m2mConn = new OracleConnection(m2mConnString))
				{
					m2mConn.Open();

					List<string[]> values = null;

					string sql;

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
						" and enreportdate = '" + enreportdate + "'\r\n" +
						"order by\r\n" +
						" itemcode,\r\n" +
						" seqnum\r\n";

					//logger.Info(StringForLog(MethodBase.GetCurrentMethod().Name, sql));

					using (OracleDataAdapter da = new OracleDataAdapter(sql, m2mConn))
					{
						// 装置点検項目
						da.Fill(eqpInspListDataSet.EQPINSPITEM);
					}

					if (eqpInspListDataSet.EQPINSPITEM.Rows.Count == 0)
						return eqpInspSubLists;
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
						" and enreportdate = '" + enreportdate + "'\r\n" +
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
						" and itemcode in (" + itemcode_con + ")\r\n" +
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
						" and " + itemcode_seqnum_con + "\r\n" +
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

#if false
						p = new HtmlGenericControl("p");
						PanelEQPINSPITEM.Controls.Add(p);

						#region 点検項目ごとの <div> とタイトルの <span>
						HtmlGenericControl divInspItem = new HtmlGenericControl("div");
						divInspItem.Attributes.Add("class", "inspitem");
						PanelEQPINSPITEM.Controls.Add(divInspItem);

						HtmlGenericControl span = new HtmlGenericControl("span");
						span.ID = "ItemLabel_" + itemcode;
						span.InnerText = itemnm + (subitem1.Length == 0 ? "" : "　" + subitem1) + (subitem2.Length == 0 ? "" : "　" + subitem2);
						span.Attributes.Add("class", "box-title");
						divInspItem.Controls.Add(span);
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
																																				//string judgvalu1 = DBNull.Value.Equals(eqpitemsubmst_row["JUDGVALU1"]) ? "" : eqpitemsubmst_row.JUDGVALU1;  // 判定値1
																																				//string judgvalu2 = DBNull.Value.Equals(eqpitemsubmst_row["JUDGVALU2"]) ? "" : eqpitemsubmst_row.JUDGVALU2;  // 判定値2
																																				//string rangectg = DBNull.Value.Equals(eqpitemsubmst_row["RANGECTG"]) ? "" : eqpitemsubmst_row.RANGECTG; // 範囲区分
																																				//string approval = DBNull.Value.Equals(eqpitemsubmst_row["APPROVAL"]) ? "" : eqpitemsubmst_row.APPROVAL; // 有効化フラグ

							string itemcode_seqnum = itemcode + "_" + seqnum;

							// シーケンスごとの <div>
							HtmlGenericControl divInspSubITem = new HtmlGenericControl("div");
							divInspSubITem.Attributes.Add("class", "inspsubitem");
							divInspItem.Controls.Add(divInspSubITem);

							// 測定項目情報 <dl>
							HtmlGenericControl dl = new HtmlGenericControl("dl");
							divInspSubITem.Controls.Add(dl);

							// 空の <dt></dt>
							HtmlGenericControl dt = new HtmlGenericControl("dt");
							dl.Controls.Add(dt);

						#region サブタイトル用の <dd> ==== ココから ====
							HtmlGenericControl dd = new HtmlGenericControl("dd");
							dd.ID = "SubItemLabel_" + itemcode_seqnum;  // "SubItemLabel_": サブミット時のコントロール検索で使用されている
							dd.Attributes.Add("style", "font-weight:bold;font-size:large");
							dd.Attributes.Add("tabindex", "-1");
							dl.Controls.Add(dd);

							HtmlGenericControl font = new HtmlGenericControl("font");

							dd.InnerText = subitemname;

							if (judgmentcriteria.Length != 0)
							{
								dd = new HtmlGenericControl("dd");
								dd.InnerText = "(" + judgmentcriteria + ")";
								dl.Controls.Add(dd);
							}

							if (inspectionpoint.Length != 0)
							{
								dd = new HtmlGenericControl("dd");
								dl.Controls.Add(dd);

								font = new HtmlGenericControl("font");
								font.Attributes.Add("color", "blue");
								dd.Controls.Add(font);

								font.InnerText = inspectionpoint;
							}

							if (comments.Length != 0)
							{
								dd = new HtmlGenericControl("dd");
								dd.InnerText = comments;
								dl.Controls.Add(dd);
							}
							// サブタイトル用の <dd> ==== ココまで ====
						#endregion

							// 空の <dt></dt>
							dt = new HtmlGenericControl("dt");
							dl.Controls.Add(dt);

						#region サブ点検画像の <dd><img> ==== ココから ====
							if (!string.IsNullOrEmpty(subitemimg))
							{
								string _eqptype = eqptype.Replace('+', '_');
								string subitemimgFileName = MapPath("~/Subitemimg Files") + "\\" + _eqptype + "\\" + subitemimg;
								if (System.IO.File.Exists(subitemimgFileName))
								{
									dd = new HtmlGenericControl("dd");
									dl.Controls.Add(dd);

									System.Web.UI.WebControls.Image img = new System.Web.UI.WebControls.Image();
									img.ImageUrl = "~/Subitemimg Files" + "/" + _eqptype + "/" + subitemimg;
									dd.Controls.Add(img);

									dt = new HtmlGenericControl("dt");
									dl.Controls.Add(dt);
								}
							}
						#endregion <dd><img> ==== ココまで ====

						#region 処置前の親要素の <dd> ==== ココから ====
							// ここに <div> で囲んだコントロールを追加していく
							HtmlGenericControl ddBEFORE = new HtmlGenericControl("dd");
							ddBEFORE.Attributes.Add("class", "inspsubitem_dd");
							dl.Controls.Add(ddBEFORE);
						#endregion 処置前の親要素の <dd> ==== ココまで ====

						#region 処置後の親要素の <dd> ==== ココから ====
							// ここに <div> で囲んだコントロールを追加していく
							HtmlGenericControl ddAFTER = new HtmlGenericControl("dd");
							ddAFTER.Attributes.Add("class", "inspsubitem_dd");
							dl.Controls.Add(ddAFTER);
						#endregion 処置後の親要素の <dd> ==== ココまで ====

						#region 処置前項目のタイトルの <div> ==== ココから ====
							HtmlGenericControl _div = new HtmlGenericControl("div");
							_div.InnerText = beftytle;
							ddBEFORE.Controls.Add(_div);
							// 処置前項目のタイトルの <dd> ==== ココまで ====
						#endregion

						#region 処置後項目のタイトルの <div> ==== ココから ====
							_div = new HtmlGenericControl("div");
							_div.InnerText = afttytle;
							ddAFTER.Controls.Add(_div);
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

							var eqpitemsubexpmst_rows = eqpitemsubmst_row.GetChildRows("EQPITEMSUBMST_EQPITEMSUBEXPMST");
							if (eqpitemsubexpmst_rows.Any())  // 何らかの点検項目の入力があった？
							{
								//StringBuilder valuesParam = new StringBuilder();

						#region 処置前の <table>
								Table tableBEFORE = new Table();
								tableBEFORE.CssClass = "inspsubitem_table";
								ddBEFORE.Controls.Add(tableBEFORE);
						#endregion

						#region 処置後の <table
								Table tableAFTER = new Table();
								tableAFTER.CssClass = "inspsubitem_table";
								ddAFTER.Controls.Add(tableAFTER);
						#endregion

								// EXPSEQNUM(拡張シーケンス) 分繰り返す
								foreach (EqpInspListDataSet.EQPITEMSUBEXPMST_HRow eqpitemsubexpmst_row in eqpitemsubexpmst_rows)
								{
									string expseqnum = DBNull.Value.Equals(eqpitemsubexpmst_row["EXPSEQNUM"]) ? "" : eqpitemsubexpmst_row.EXPSEQNUM;    // 拡張シーケンス
									string itemlabel = DBNull.Value.Equals(eqpitemsubexpmst_row["ITEMLABEL"]) ? "" : eqpitemsubexpmst_row.ITEMLABEL;    // 項目ラベル
																																						//string judgvalu1 = DBNull.Value.Equals(eqpitemsubexpmst_row["JUDGVALU1"]) ? "" : eqpitemsubexpmst_row.JUDGVALU1;  // 判定値1
																																						//string judgvalu2 = DBNull.Value.Equals(eqpitemsubexpmst_row["JUDGVALU2"]) ? "" : eqpitemsubexpmst_row.JUDGVALU2;  // 判定値2
																																						//string rangectg = DBNull.Value.Equals(eqpitemsubexpmst_row["RANGECTG"]) ? "" : eqpitemsubexpmst_row.RANGECTG; // 範囲区分

									string itemcode_seqnum_expseqnum = itemcode_seqnum + "_" + expseqnum;

									string befvalue = "", aftvalue = "";
									var eqpinspitemexp_rows = eqpitemsubexpmst_row.GetChildRows("EQPITEMSUBEXPMST_EQPINSPITEMEXP");
									if ( eqpinspitemexp_rows.Any() )
									{
										EqpInspListDataSet.EQPINSPITEMEXPRow eqpinspitemexp_row = (EqpInspListDataSet.EQPINSPITEMEXPRow)eqpinspitemexp_rows[0];
										befvalue = eqpinspitemexp_row.BEFVALUE;
										aftvalue = eqpinspitemexp_row.AFTVALUE;
									}

						#region 処置前の <tr> ==== ココから ====
									TableRow tableRow = new TableRow();

									TableCell tableCell = new TableCell();
									tableCell.Text = itemlabel + (itemlabel.Length == 0 ? "" : "：");
									tableCell.CssClass = "inspsubitem_itemlabel";
									tableRow.Cells.Add(tableCell);

									// 処置前の入力値
									tableCell = new TableCell();
									TextBox textBoxBEFVALUE = new TextBox();
									textBoxBEFVALUE.ID = "TextBoxBEFVALUE_" + itemcode_seqnum_expseqnum;  // "TextBoxBEFVALUE_": サブミット時のコントロール検索で使用されている
									textBoxBEFVALUE.Text = befvalue;
									tableCell.Controls.Add(textBoxBEFVALUE);
									tableRow.Cells.Add(tableCell);

									tableBEFORE.Rows.Add(tableRow);
									// 処置前の <tr> ==== ココまで ====
						#endregion

						#region 処置後の <tr> ==== ココから ====
									tableRow = new TableRow();

									tableCell = new TableCell();    // <td>
									tableCell.Text = itemlabel + (itemlabel.Length == 0 ? "" : "：");
									tableCell.CssClass = "inspsubitem_itemlabel";
									tableRow.Cells.Add(tableCell);

									// 処置後の入力値
									tableCell = new TableCell();    // <td>
									TextBox textBoxAFTVALUE = new TextBox();
									textBoxAFTVALUE.ID = "TextBoxAFTVALUE_" + itemcode_seqnum_expseqnum;
									textBoxAFTVALUE.Text = aftvalue;
									tableCell.Controls.Add(textBoxAFTVALUE);
									tableRow.Cells.Add(tableCell);

									tableAFTER.Rows.Add(tableRow);
									// 処置後の <tr> ==== ココまで ====
						#endregion

									//valuesParam.Append(valuesParam.Length == 0 ? "" : ",");
									//valuesParam.Append("{" + "bvID:" + textBoxBEFVALUE.ID + "," + "avID:" + textBoxAFTVALUE.ID + "," + "rc:" + rangectg + "," + "jv1:" + judgvalu1 + "," + "jv2:" + judgvalu2 + "}");
								}

						#region 処置前の判定の <div> ==== ココから ====
								_div = new HtmlGenericControl("div");
								ddBEFORE.Controls.Add(_div);

								// 処置前の判定
								DropDownList dropDownListBEFRESULT = new DropDownList();
								dropDownListBEFRESULT.ID = "DropDownListBEFRESULT_" + itemcode_seqnum;
								SetDropDownListResult(dropDownListBEFRESULT, befresult);
								dropDownListBEFRESULT.CssClass = "classic";

								//string okNgSelectedScript = "OkNgSelected(" + dropDownListBEFRESULT.ID + ");";
								//dropDownListBEFRESULT.Attributes["onchange"] = okNgSelectedScript;

								_div.Controls.Add(dropDownListBEFRESULT);
								// 処置前の判定の <div> ==== ココまで ====
						#endregion

						#region 処置後の判定の <div> ==== ココから ====
								_div = new HtmlGenericControl("div");
								ddAFTER.Controls.Add(_div);

								// 処置後の判定
								DropDownList dropDownListAFTRESULT = new DropDownList();
								dropDownListAFTRESULT.ID = "DropDownListAFTRESULT_" + itemcode_seqnum;
								SetDropDownListResult(dropDownListAFTRESULT, aftresult);
								dropDownListAFTRESULT.CssClass = "classic";

								//okNgSelectedScript = "OkNgSelected(" + dropDownListAFTRESULT.ID + ");";
								//dropDownListAFTRESULT.Attributes["onchange"] = okNgSelectedScript;

								_div.Controls.Add(dropDownListAFTRESULT);
								// 処置後の判定の <div> ==== ココまで ====
						#endregion
							}
							else
							{
						#region 処置前の判定の <div> ==== ココから ====
								_div = new HtmlGenericControl("div");
								ddBEFORE.Controls.Add(_div);

								// 処置前の判定
								DropDownList dropDownListBEFRESULT = new DropDownList();
								dropDownListBEFRESULT.ID = "DropDownListBEFRESULT_" + itemcode_seqnum;
								SetDropDownListResult(dropDownListBEFRESULT, befresult);
								dropDownListBEFRESULT.CssClass = "classic";

								//string okNgSelectedScript = "OkNgSelected(" + dropDownListBEFRESULT.ID + ");";
								//dropDownListBEFRESULT.Attributes["onchange"] = okNgSelectedScript;

								_div.Controls.Add(dropDownListBEFRESULT);
								// 処置前の判定の <div> ==== ココまで ====
						#endregion

						#region 処置後の判定の <div> ==== ココから ====
								_div = new HtmlGenericControl("div");
								ddAFTER.Controls.Add(_div);

								// 処置後の判定
								DropDownList dropDownListAFTRESULT = new DropDownList();
								dropDownListAFTRESULT.ID = "DropDownListAFTRESULT_" + itemcode_seqnum;
								SetDropDownListResult(dropDownListAFTRESULT, aftresult);
								dropDownListAFTRESULT.CssClass = "classic";

								//okNgSelectedScript = "OkNgSelected(" + dropDownListAFTRESULT.ID + ");";
								//dropDownListAFTRESULT.Attributes["onchange"] = okNgSelectedScript;

								_div.Controls.Add(dropDownListAFTRESULT);
								// 処置後の判定の <div> ==== ココまで ====
						#endregion
							}

						#region bef|afttytle に -hidden- が設定されていた場合の処理
							if (beftytle == "-hidden-")
							{
								ddBEFORE.Controls.Clear();
								/*
								HtmlGenericControl _margin = new HtmlGenericControl("label");
								_margin.Attributes.Add("class", "inspsubitem_dd_margin");
								ddBEFORE.Controls.Add(_margin);*/
							}

							if (afttytle == "-hidden-")
							{
								ddAFTER.Controls.Clear();
								/*
								HtmlGenericControl _margin = new HtmlGenericControl("label");
								_margin.Attributes.Add("class", "inspsubitem_dd_margin");
								ddAFTER.Controls.Add(_margin);*/
							}
						#endregion

							if (eqpitemsubmst_row != eqpitemsubmst_rows.Last())
							{
								p = new HtmlGenericControl("p/");
								divInspSubITem.Controls.Add(p);
							}
#else
						#region 点検項目ごとの <div> とタイトルの <span>
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

									// 戻り値
									EqpInpsSubList eqpinspsublist = new EqpInpsSubList();

									eqpinspsublist.EqpType = eqptype;
									eqpinspsublist.ItemCode = itemcode;
									eqpinspsublist.ItemName = itemnm;
									eqpinspsublist.SeqNum = seqnum;
									eqpinspsublist.SubItemName = subitemname;
									eqpinspsublist.JudgementCriteria = judgmentcriteria;
									eqpinspsublist.InspectionPoint = inspectionpoint;
									eqpinspsublist.BefTitle = beftytle;
									eqpinspsublist.AftTitle = afttytle;

									eqpinspsublist.ExpSeqNum = expseqnum;
									eqpinspsublist.ItemLabel = itemlabel;
									eqpinspsublist.BefValue = befvalue;
									eqpinspsublist.AftValue = aftvalue;

									eqpinspsublist.BefResult = befresult;
									eqpinspsublist.AftResult = aftresult;

									Array.Resize(ref eqpInspSubLists, eqpInspSubLists.Length + 1);
									eqpInspSubLists[eqpInspSubLists.Length - 1] = eqpinspsublist;
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

								// 戻り値
								EqpInpsSubList eqpinspsublist = new EqpInpsSubList();

								eqpinspsublist.EqpType = eqptype;
								eqpinspsublist.ItemCode = itemcode;
								eqpinspsublist.ItemName = itemnm;
								eqpinspsublist.SeqNum = seqnum;
								eqpinspsublist.SubItemName = subitemname;
								eqpinspsublist.JudgementCriteria = judgmentcriteria;
								eqpinspsublist.InspectionPoint = inspectionpoint;
								eqpinspsublist.BefTitle = beftytle;
								eqpinspsublist.AftTitle = afttytle;

								eqpinspsublist.ExpSeqNum = "";
								eqpinspsublist.ItemLabel = "";
								eqpinspsublist.BefValue = "";
								eqpinspsublist.AftValue = "";

								eqpinspsublist.BefResult = befresult;
								eqpinspsublist.AftResult = aftresult;

								Array.Resize(ref eqpInspSubLists, eqpInspSubLists.Length + 1);
								eqpInspSubLists[eqpInspSubLists.Length - 1] = eqpinspsublist;
							}

							#region bef|afttytle に -hidden- が設定されていた場合の処理
							#endregion
#endif
						}
					}
					#endregion
				}
			}
			catch (Exception exp)
			{
				System.Diagnostics.Debug.WriteLine(exp.Message);
			}

			return eqpInspSubLists;
		}
	}
#endif
}