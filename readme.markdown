# Usage

See https://gist.github.com/972895 for an example of _layout.cshtmlpage

In any view (Layout, Page, Partial, or EditorTemplate), use

	@Script.AddScript("~/script.js", RazorScriptManager.ScriptType.JavaScript)

or

	@Script.AddScript("~/style.css", RazorScriptManager.ScriptType.StyleSheet)

to add a js or css file to the output. In your template, use 

	@Script.OutputScript(ScriptType.JavaScript)

or

	@Script.OutputScript(ScriptType.Stylesheet)

to output all the scripts and styles to your page.

# Configuration

Two AppSettings configuration keys are used.

UseCDNScripts will tell the script manager to use CDN-hosted scripts if provided. To provide a CDN script, pass it as the second parameter in AddScript:

	@Script.AddScript("~/Scripts/jquery-1.6.js", "https://ajax.googleapis.com/ajax/libs/jquery/1.6.0/jquery.min.js", ScriptType.JavaScript)

CompressScripts will tell the script manager to compress the combined output of all local scripts. If UseCDNScripts is set to true, this will only affect local scripts, not CDN-hosted scripts.