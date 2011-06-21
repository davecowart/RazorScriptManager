# Usage

See https://github.com/davecowart/RazorScriptManager.Demo for an example project

In any view (Layout, Page, Partial, or EditorTemplate), use

	@Script.AddJavaScript("~/Scripts/script.js")

or

	@Script.AddCss("~/Content/style.css")

to add a js or css file to the output. In your template, use 

	@Script.OutputJavaScript()

or

	@Script.OutputCss()

to output all the scripts and styles to your page.

# Configuration

Two AppSettings configuration keys are used.

UseCDNScripts will tell the script manager to use CDN-hosted scripts if provided. To provide a CDN script, pass it as the second parameter in AddScript:

	@Script.AddJavaScript(localPath: "~/Scripts/jquery-1.6.js", cdnPath: "https://ajax.googleapis.com/ajax/libs/jquery/1.6.0/jquery.min.js", siteWide: true)

CompressScripts will tell the script manager to compress the combined output of all local scripts. If UseCDNScripts is set to true, this will only compress local scripts, not CDN-hosted scripts. If CompressScripts is set to false, the full path of each individual file is included as a comment in the script output just before that particular file's content.