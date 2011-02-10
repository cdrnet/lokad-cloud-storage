#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Web.UI;
using System.Web.UI.HtmlControls;

namespace Lokad.Cloud.Web
{
	public partial class NavBar : UserControl
	{
		private string _selected;

		public string Selected
		{
			get { return _selected; }
			set
			{
				if(!string.IsNullOrEmpty(_selected))
				{
				    throw new InvalidOperationException("Property already set.");
				}

				_selected = value;
				((HtmlGenericControl)FindControl(_selected)).Attributes["class"] = "active";
			}
		}
	}
}