using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace RazorScriptManager {
	public class RazorScriptManager : IHttpHandler {
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
					context.Application["__rsm__" + scriptType.ToString()] = null;
					context.Response.Write(result);
					return;
				}
			}

			//not cached
			var scripts = context.Application["__rsm__" + scriptType.ToString()] as IEnumerable<ScriptInfo>;
			context.Application["__rsm__" + scriptType.ToString()] = null;
			if (scripts == null) return;
			var scriptbody = new StringBuilder();

			scripts = scripts.Distinct(new ScriptInfoComparer());

			//add sitewide scripts FIRST, so they're accessible to local scripts
			var siteScripts = scripts.Where(s => s.SiteWide);
			var localScripts = scripts.Where(s => !s.SiteWide).Except(siteScripts, new ScriptInfoComparer());
			var scriptPaths = siteScripts.Concat(localScripts).Select(s => s.LocalPath);
			var minify = bool.Parse(ConfigurationManager.AppSettings["CompressScripts"]);

			foreach (var script in scriptPaths) {
				if (!String.IsNullOrWhiteSpace(script)) {
					using (var file = new System.IO.StreamReader(script)) {
						var fileContent = file.ReadToEnd();
						if (scriptType == ScriptType.Stylesheet) {
							var fromUri = new Uri(context.Server.MapPath("~/"));
							var toUri = new Uri(new FileInfo(script).DirectoryName);
							fileContent = fileContent.Replace("url(", "url(/" + fromUri.MakeRelativeUri(toUri).ToString() + "/");
						}
						if (!minify) scriptbody.AppendLine(String.Format("/* {0} */", script));
						scriptbody.AppendLine(fileContent);
					}
				}
			}

			var hash = GetHash(scripts);
			string scriptOutput = scriptbody.ToString();
			if (minify) {
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

		public static string GetHash(IEnumerable<ScriptInfo> scripts) {
			var input = string.Join(string.Empty, scripts.Select(s => s.LocalPath).Distinct());
			var hash = System.Security.Cryptography.MD5.Create().ComputeHash(Encoding.ASCII.GetBytes(input));
			var sb = new StringBuilder();
			for (int i = 0; i < hash.Length; i++)
				sb.Append(hash[i].ToString("X2"));
			return sb.ToString();
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
		public bool SiteWide { get; set; }

		public ScriptInfo(string localPath, string cdnPath, ScriptType scriptType, bool siteWide = false) {
			LocalPath = localPath;
			CDNPath = cdnPath;
			ScriptType = scriptType;
			SiteWide = siteWide;
		}
	}

	public class ScriptInfoComparer : IEqualityComparer<ScriptInfo> {

		public bool Equals(ScriptInfo x, ScriptInfo y) {
			return x.GetHashCode() == y.GetHashCode();
		}

		public int GetHashCode(ScriptInfo obj) {
			return (obj.LocalPath + obj.CDNPath + obj.ScriptType.ToString()).GetHashCode();
		}

	}

}