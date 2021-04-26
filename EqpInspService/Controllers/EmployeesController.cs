using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using EqpInspService.Models;

namespace EqpInspService.Controllers
{
	public class EmployeesController : ApiController
	{
		// GET api/<controller>
		/*public IEnumerable<string> Get()
		{
			return new string[] { "value1", "value2" };
		}*/
		public IEnumerable<Employee> Get()
		{
			return new[]
			{
				new Employee{Id = 1, Name = "山田 太郎", BirthDay = DateTime.Parse("1970/01/01")},
				new Employee{Id = 2, Name = "佐藤 花子", BirthDay = DateTime.Parse("2000/10/11")}
			};
		}

		// GET api/<controller>/5
		public string Get(int id)
		{
			return "value";
		}

		// POST api/<controller>
		public void Post([FromBody] string value)
		{
		}

		// PUT api/<controller>/5
		public void Put(int id, [FromBody] string value)
		{
		}

		// DELETE api/<controller>/5
		public void Delete(int id)
		{
		}
	}
}