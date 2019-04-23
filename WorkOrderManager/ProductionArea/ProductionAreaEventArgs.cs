using System;
using System.Collections.Generic;
using BOMOrderManager;

namespace ProductionArea
{
	public enum ProductionAction
	{
		Error = -1,
		None = 0,
		Prod = 1,
		Done = 2,
		Ready = 3,
		Stop = 4
	}
	internal class ProductionAreaEventArgs : EventArgs
	{
		

		public ProductionAction action = ProductionAction.None; 
		public string idWorkOrder = "";
		public string typeProd = "";
		public List<string> items = new List<string>();
		public object anythingElse; //This should be used with parcimony

		public ProductionAreaEventArgs(){}

		public ProductionAreaEventArgs(ProductionAction action)
		{
			this.action = action;
		}

		public ProductionAreaEventArgs(ProductionAction action, string idWorkOrder, string typeProd, IEnumerable<string> items)
		{
			this.action = action;
			this.idWorkOrder = idWorkOrder;
			this.typeProd = typeProd;
			foreach(var i in items)
			{
				this.items.Add(i);
			}
		}

		public ProductionAreaEventArgs(ProductionAction action, string idWorkOrder, IEnumerable<string> items)
		{
			this.action = action;
			this.idWorkOrder = idWorkOrder;
			foreach(var i in items)
			{
				this.items.Add(i);
			}
		}


	}
}
