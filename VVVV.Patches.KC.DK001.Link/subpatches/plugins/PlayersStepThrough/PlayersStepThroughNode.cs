#region usings
using System;
using System.ComponentModel.Composition;
using System.Collections.Generic;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Nodes
{
	#region PluginInfo
	[PluginInfo(Name = "StepThrough", Category = "Players", Help = "Basic template with one value in/out", Tags = "")]
	#endregion PluginInfo
	public class PlayersStepThroughNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Quad ID")]
		public IDiffSpread<int> FInID;

		[Input("Path")]
		public IDiffSpread<string> FInPath;
		
		[Output("Quad ID")]
		public ISpread<int> FOutID;

		[Output("Path")]
		public ISpread<string> FOutPath;
		
		[Import()]
		public ILogger FLogger;
		#endregion fields & pins

		class Quad
		{
			public class ContentItem
			{
				public bool MarkForClear = false;
				public DateTime LastPlay = DateTime.Now;
			}
			
			public bool MarkForClear = false;
			public Dictionary<string, ContentItem> Content = new Dictionary<string, ContentItem>();
			
			public string GetFirstContent()
			{
				if (Content.Count == 0)
				{
					return "";
				}
				else 
				{
					var content = Content.GetEnumerator();
					content.MoveNext();
					return content.Current.Key;
				}
			}
		}
		Dictionary<int, Quad> FQuads = new Dictionary<int, Quad>();
		
		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if (FInID.IsChanged || FInPath.IsChanged) {
				SyncChanges(SpreadMax);
			}
			
			FOutID.SliceCount = FQuads.Count;
			FOutPath.SliceCount = FQuads.Count;
			
			int index = 0;
			foreach(var quad in FQuads)
			{
				FOutID[index] = quad.Key;
				FOutPath[index] = quad.Value.GetFirstContent();
				index++;	
			}
		}
		
		void SyncChanges(int SpreadMax)
		{
			//mark everything for clear
			foreach(var quad in FQuads)
			{
				quad.Value.MarkForClear = true;
				foreach(var content in quad.Value.Content)
				{
					content.Value.MarkForClear = true;
				}
			}
			
			//check and add
			for(int i = 0; i<SpreadMax; i++)
			{
				int id = FInID[i];
				string path = FInPath[i];
				
				Quad quad;
				if (FQuads.ContainsKey(id))
				{
					quad = FQuads[id];
					quad.MarkForClear = false;
				} else {
					quad = new Quad();
					FQuads.Add(id, quad);
				}
				
				Quad.ContentItem content;
				if (quad.Content.ContainsKey(path)) {
					content = quad.Content[path];
					content.MarkForClear = false;
				} else {
					content = new Quad.ContentItem();
					quad.Content.Add(path, content);
				}
			}
			
			//clear anything marked for clear
			var toRemove = new List<int>();
			foreach(var quad in FQuads)
			{
				if (quad.Value.MarkForClear)
				{
					toRemove.Add(quad.Key);
				}
			}
			foreach(var quad in toRemove)
			{
				FQuads.Remove(quad);
			}
			foreach(var quad in FQuads)
			{
				var contentToRemove = new List<string>();
				foreach(var content in quad.Value.Content)
				{
					if (content.Value.MarkForClear)
					{
						contentToRemove.Add(content.Key);
					}
				}
				foreach(var content in contentToRemove)
				{
					quad.Value.Content.Remove(content);
				}
			}
		}
	}
}
