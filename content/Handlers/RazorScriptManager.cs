using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.SessionState;
using System.Configuration;
using System.IO;

namespace RazorScriptManager {
	public class RazorScriptManager : IHttpHandler, IReadOnlySessionState {
		#region IHttpHandler Members

		public bool IsReusable {
			get { return false; }
		}

		public void ProcessRequest(HttpContext context) {
			var cache = context.Cache;
			var scriptType = (ScriptType)Enum.Parse(typeof(ScriptType), context.Request.Params["type"]);

			switch (scriptType) {
				case ScriptType.JavaScript:
					context.Response.ContentType = @"application/javascript";
					break;
				case ScriptType.Stylesheet:
					context.Response.ContentType = @"text/css";
					break;
			}

			//cached
			var hashString = context.Request.Params["hash"];
			if (!String.IsNullOrWhiteSpace(hashString)) {
				var result = cache[HttpUtility.UrlDecode(hashString)] as string;
				if (!string.IsNullOrWhiteSpace(result)) {
					context.Response.Write(result);
					return;
				}
			}


			//not cached
			var scripts = context.Session["__rsm__" + scriptType.ToString()] as List<ScriptInfo>;
			context.Session["__rsm__" + scriptType.ToString()] = null;
			if (scripts == null) return;
			var scriptbody = new StringBuilder();
			foreach (var script in scripts.Select(s => s.LocalPath).Distinct()) {
				if (!String.IsNullOrWhiteSpace(script)) {
					using (var file = new System.IO.StreamReader(script)) {
						var fromUri = new Uri(context.Server.MapPath("~/"));
						var toUri = new Uri(new FileInfo(script).DirectoryName);
						var relativeUri = fromUri.MakeRelativeUri(toUri);
						scriptbody.Append(file.ReadToEnd().Replace("url(", "url(/" + relativeUri.ToString() + "/"));
					}
				}
			}

			var hash = GetHash(scripts);
			string scriptOutput = scriptbody.ToString();
			if (bool.Parse(ConfigurationManager.AppSettings["CompressScripts"])) {
				switch (scriptType) {
					case ScriptType.JavaScript:
						var jscompressor = new Yahoo.Yui.Compressor.JavaScriptCompressor(scriptOutput);
						scriptOutput = jscompressor.Compress();
						break;
					case ScriptType.Stylesheet:
						scriptOutput = Yahoo.Yui.Compressor.CssCompressor.Compress(scriptOutput);
						break;
				}
			}
			cache[hash] = scriptOutput;
			context.Response.Write(scriptOutput);
		}

		#endregion

		public static string GetHash(List<ScriptInfo> scripts) {
			var input = string.Join("", scripts.Select(s => s.LocalPath));
			var bytes = Encoding.ASCII.GetBytes(input);
			var hash = Convert.ToBase64String(bytes);
			return hash;
		}
	}

	public enum ScriptType {
		JavaScript,
		Stylesheet
	}

	public class ScriptInfo {
		public string LocalPath { get; set; }
		public string CDNPath { get; set; }
		public ScriptType ScriptType { get; set; }

		public ScriptInfo(string localPath, string cdnPath, ScriptType scriptType) {
			LocalPath = localPath;
			CDNPath = cdnPath;
			ScriptType = scriptType;
		}
	}

}