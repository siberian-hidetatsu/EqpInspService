using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace EqpInspService
{
	public static class WebApiConfig
	{
		public static void Register(HttpConfiguration config)
		{
			// Web API の設定およびサービス

			// Web API ルート
			config.MapHttpAttributeRoutes();

			config.Routes.MapHttpRoute(
				name: "EquipmentApi",
				routeTemplate: "eqpapi/{controller}/{eqptype}"/*,
				defaults: new { eqptype = RouteParameter.Optional }*/
			);

			config.Routes.MapHttpRoute(
				name: "EqpItemSubExpApi",
				routeTemplate: "eqpapi/{controller}/{eqptype}/{itemcode}/{seqnum}"
				);

			config.Routes.MapHttpRoute(
				name: "EqpInspSubListApi",
				routeTemplate: "eqpapi/{controller}/{eqptype}/{eqpid}/{stdate}/{eddate}/{interval}",
				defaults: new { interval = RouteParameter.Optional }
				);

			config.Routes.MapHttpRoute(
				name: "EqpTypeIdListApi",
				routeTemplate: "eqpapi/{controller}/{stdate}/{interval}"
				);

			config.Routes.MapHttpRoute(
				name: "DefaultApi",
				routeTemplate: "api/{controller}/{id}",
				defaults: new { id = RouteParameter.Optional }
			);
		}
	}
}
