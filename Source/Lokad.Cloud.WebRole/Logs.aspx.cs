#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using Lokad.Cloud.Diagnostics;

namespace Lokad.Cloud.Web
{
	public partial class Logs : System.Web.UI.Page
	{
		private const int PageSize = 20;

		readonly CloudLogger _logger = (CloudLogger)GlobalSetup.Container.Resolve<ILog>();

		protected void Page_Load(object sender, EventArgs e)
		{
			if(!Page.IsPostBack)
			{
				SetCurrentPageIndex(0);
				LogsView.DataBind();
			}
		}

		protected void DeleteButton_Click(object sender, EventArgs e)
		{
			Page.Validate("del");
			if (!Page.IsValid) return;

			_logger.DeleteOldLogs(DateTime.UtcNow.AddDays(-7*int.Parse(WeeksBox.Text)));

			SetCurrentPageIndex(0);
			LogsView.DataBind();
		}

		protected void LogsView_DataBinding(object sender, EventArgs e)
		{
			LogsView.DataSource = FetchLogs();
		}

		int GetCurrentPageIndex()
		{
			return int.Parse(PageIndex.Value);
		}

		void SetCurrentPageIndex(int index)
		{
			if(index >= 0) PageIndex.Value = index.ToString();
			CurrentPage.Text = (index + 1).ToString();
			PrevPage.Enabled = index > 0;
		}

		protected void PrevPage_Click(object sender, EventArgs e)
		{
			int index = GetCurrentPageIndex();
			
			SetCurrentPageIndex(index - 1);
			LogsView.DataBind();
		}

		protected void NextPage_Click(object sender, EventArgs e)
		{
			int index = GetCurrentPageIndex();

			SetCurrentPageIndex(index + 1);
			LogsView.DataBind();
		}

		protected void OnLevelChanged(object sender, EventArgs e)
		{
			SetCurrentPageIndex(0);
			LogsView.DataBind();
		}

		List<LogEntry> FetchLogs()
		{
			int currentIndex = GetCurrentPageIndex();
			var logs = new List<LogEntry>(_logger.GetLogsOfLevelOrHigher(GetSelectedLevelThreshold(), currentIndex * PageSize).Take(PageSize));
			NextPage.Enabled = logs.Count == PageSize;
			return logs;
		}

		LogLevel GetSelectedLevelThreshold()
		{
			var selectedString = LevelSelector.SelectedValue;
			if(String.IsNullOrEmpty(selectedString))
			{
				return LogLevel.Min;
			}

			return EnumUtil.Parse<LogLevel>(selectedString);
		}
	}
}
