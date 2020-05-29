using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ADOCLI
{
	internal class ChangeBlameController
	{
		internal abstract class ChangeBlameQuery
		{
			public abstract string GetFieldName();

			public abstract string GetOperator();

			public abstract string GetValue();

			public string GetQuery()
			{
				return "[" + GetFieldName() + "] " + GetOperator() + " '" + GetValue() + "'";
			}

			public abstract bool Matches(string strValue);
		}

		private class ChangeBlameTagQuery : ChangeBlameQuery
		{
			public string Tag
			{
				get;
				set;
			}

			public override string GetFieldName()
			{
				return "System.Tags";
			}

			public override string ToString()
			{
				return "Tags: " + Tag;
			}

			public override string GetOperator()
			{
				return "CONTAINS";
			}

			public override bool Matches(string strValue)
			{
				return strValue.Contains(Tag, StringComparison.OrdinalIgnoreCase);
			}

			public override string GetValue()
			{
				return Tag;
			}
		}

		private class ChangeBlameFieldQuery : ChangeBlameQuery
		{
			public string Field
			{
				get;
				set;
			}

			public string Value
			{
				get;
				set;
			}

			public override string ToString()
			{
				return Field + "=" + Value;
			}

			public override string GetFieldName()
			{
				if (Field.Contains(' '))
				{
					return "System." + Field.Replace(" ", "");
				}
				return Field;
			}

			public override string GetOperator()
			{
				return "=";
			}

			public override bool Matches(string strValue)
			{
				return string.Equals(strValue, Value, StringComparison.OrdinalIgnoreCase);
			}

			public override string GetValue()
			{
				return Value;
			}
		}

		internal class ChangeInfo
		{
			public string Author
			{
				get;
				set;
			}

			public DateTime ChangedDate
			{
				get;
				set;
			}
		}

		private ChangeBlameQuery query;

		private int id = -1;

		public ChangeBlameController(string queryString)
		{
			Match i = Regex.Match(queryString, "(?<key>[^=]+)=(?<value>.*)");
			if (!i.Success)
			{
				query = new ChangeBlameTagQuery
				{
					Tag = queryString
				};
			}
			else if (i.Groups["key"].Value.Equals("tags", StringComparison.OrdinalIgnoreCase))
			{
				query = new ChangeBlameTagQuery
				{
					Tag = i.Groups["value"].Value
				};
			}
			else
			{
				query = new ChangeBlameFieldQuery
				{
					Field = i.Groups["key"].Value,
					Value = i.Groups["value"].Value
				};
			}
		}

		private string GetValueString(object value)
		{
			if (value is IdentityRef)
			{
				return (value as IdentityRef).DisplayName;
			}
			return value.ToString();
		}

		private async Task<Dictionary<int, ChangeInfo>> GetItems()
		{
			string filter = (id != -1) ? $"[System.Id] = '{id}'" : query.GetQuery();
			Wiql wiql = new Wiql
			{
				Query = "SELECT [System.Id], [System.WorkItemType], [System.Title], [System.AssignedTo], [System.State], [System.Tags] FROM workitems WHERE [System.WorkItemType] = 'Bug' AND " + filter
			};
			using (WorkItemTrackingHttpClient workItemTrackingHttpClient = AdoConfiguration.GetClient<WorkItemTrackingHttpClient>())
			{
				_ = 2;
				try
				{
					WorkItemQueryResult workItemQueryResult = await workItemTrackingHttpClient.QueryByWiqlAsync(wiql);
					Dictionary<int, ChangeInfo> result = new Dictionary<int, ChangeInfo>();
					if (workItemQueryResult.WorkItems.Count() != 0)
					{
						string[] fields = new string[2]
						{
							"System.Id",
							"System.Title"
						};
						Console.WriteLine($"Found {workItemQueryResult.WorkItems.Count()} items");
						for (int i = 0; i < workItemQueryResult.WorkItems.Count(); i += 50)
						{
							IEnumerable<WorkItemReference> setOfIds = workItemQueryResult.WorkItems.Skip(i).Take(50);
							foreach (WorkItem workItem in await workItemTrackingHttpClient.GetWorkItemsAsync(setOfIds.Select((WorkItemReference x) => x.Id), fields, workItemQueryResult.AsOf))
							{
								List<WorkItemUpdate> obj = await workItemTrackingHttpClient.GetUpdatesAsync(workItem.Id.Value);
								string lastUserToUpdate = null;
								DateTime lastChange = DateTime.Now;
								bool hasValue = false;
								foreach (WorkItemUpdate update in obj)
								{
									if (update.Fields != null && update.Fields.ContainsKey(query.GetFieldName()))
									{
										object value = update.Fields[query.GetFieldName()].NewValue;
										string strValue = GetValueString(value);
										if (!hasValue && query.Matches(strValue))
										{
											hasValue = true;
											lastUserToUpdate = update.RevisedBy.DisplayName;
											lastChange = update.RevisedDate;
											Console.WriteLine($"Bug {workItem.Id.Value}: {query.ToString()} set by {lastUserToUpdate} on {lastChange.ToLocalTime()}");
										}
										else if (hasValue && !query.Matches(strValue))
										{
											Console.WriteLine($"Bug {workItem.Id.Value}: {query.ToString()} reset by {update.RevisedBy.DisplayName} on {update.RevisedDate.ToLocalTime().ToString()} - new Value = {strValue}");
											lastChange = DateTime.Now;
											hasValue = false;
										}
									}
								}
								if (lastUserToUpdate != null)
								{
									result[workItem.Id.Value] = new ChangeInfo
									{
										Author = lastUserToUpdate,
										ChangedDate = lastChange
									};
								}
								else
								{
									Console.WriteLine($"Field has never been set to the required value for ID={workItem.Id.Value}");
								}
							}
						}
					}
					return result;
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.ToString());
					throw;
				}
			}
		}

		public void Process()
		{
			Console.WriteLine("Fetching blame for query " + query.ToString());
			Dictionary<int, ChangeInfo> items = GetItems().Result;
			Dictionary<string, int> summary = new Dictionary<string, int>();
			foreach (int item in items.Keys)
			{
				Console.WriteLine($"{item}\t{items[item].Author}\t{items[item].ChangedDate.ToLocalTime()}");
				if (summary.ContainsKey(items[item].Author))
				{
					summary[items[item].Author]++;
				}
				else
				{
					summary[items[item].Author] = 1;
				}
			}
			if (items.Count > 1)
			{
				Console.WriteLine();
				Console.WriteLine("Summary:");
				foreach (string author in summary.Keys)
				{
					Console.WriteLine($"{author} : {summary[author]}");
				}
			}
		}

		internal void SetBugId(int id)
		{
			this.id = id;
		}
	}
}
