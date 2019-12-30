using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using CefSharp.WinForms;
using CefSharp;
using System.Reflection;
using CefSharp.Callback;
using System.Net;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;


namespace MathEditor
{
    public partial class Form1 : Form
    {
        public Form1() { InitializeComponent(); }

        //==============================================================================================
        // DEFINE VARIABLES
        //==============================================================================================
        public CefSharp.WinForms.ChromiumWebBrowser browser;
        public CefSharp.WinForms.ChromiumWebBrowser svg_render_aux_browser;

        //==============================================================================================
        // INITIALIZE
        //==============================================================================================
        private void Form1_Load(object sender, EventArgs e)
        {
            CefSettings settings = new CefSettings();

            //Use to debbug
            //settings.RemoteDebuggingPort = 8088;

            //Create custom scheme to enable local files on CefSharp
            settings.RegisterScheme(new CefCustomScheme
            {
                SchemeName = CustomProtocolSchemeHandlerFactory.SchemeName,
                SchemeHandlerFactory = new CustomProtocolSchemeHandlerFactory()
            });

            //Initialize browser
            CefSharp.Cef.Initialize(settings);
            browser = new ChromiumWebBrowser("about:blank");
            svg_render_aux_browser = new ChromiumWebBrowser("about:blank");

            // Enable Cross Script Data Transfers for Mathjax and local files for CefSharp
            browser.BrowserSettings.WebSecurity = CefState.Disabled;
            browser.BrowserSettings.FileAccessFromFileUrls = CefState.Enabled;
            browser.BrowserSettings.UniversalAccessFromFileUrls = CefState.Enabled;
            browser.BrowserSettings.LocalStorage = CefState.Enabled;
            svg_render_aux_browser.BrowserSettings.WebSecurity = CefState.Disabled;
            svg_render_aux_browser.BrowserSettings.FileAccessFromFileUrls = CefState.Enabled;
            svg_render_aux_browser.BrowserSettings.UniversalAccessFromFileUrls = CefState.Enabled;
            svg_render_aux_browser.BrowserSettings.LocalStorage = CefState.Enabled;

            //Add browser control to panel
            splitContainer1.Panel1.Controls.Add(browser);
            splitContainer1.Panel1.Controls.Add(svg_render_aux_browser);
            browser.Dock = DockStyle.Fill;
            browser.MenuHandler = new CustomMenuHandler();
            svg_render_aux_browser.Visible = false;
            
        }

        //==============================================================================================
        // BUTTON CLICKS
        //==============================================================================================
        private void toolStripButton1_Click(object sender, EventArgs e) { Render(richTextBox1.Text); }
        
        private void richTextBox1_KeyDown(object sender, KeyEventArgs e) { if (e.KeyCode == Keys.B && e.Control) { Render(richTextBox1.Text); } }

        private void toolStripButton2_Click(object sender, EventArgs e) { System.Diagnostics.Process.Start("https://github.com/frankovacevich/math_editor"); }

        // Copy button click
        private void toolStripButton5_Click(object sender, EventArgs e)
        {
            try {
                Clipboard.SetImage(CreatePNG());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ups! Something went wrong.\n\n" + ex.Message);
            }
        }

        // Save text click
        private void saveTextToFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try {
                SaveFileDialog SFD = new SaveFileDialog();
                SFD.Filter = "All files (*.*)|*.*";
                if (SFD.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(SFD.FileName,richTextBox1.Text);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ups! Something went wrong.\n\n" + ex.Message);
            }
        }

        // Load text click
        private void loadFromFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog OFD = new OpenFileDialog();
                OFD.Filter = "All files (*.*)|*.*";
                if (OFD.ShowDialog() == DialogResult.OK)
                {
                    richTextBox1.Text = File.ReadAllText(OFD.FileName);
                    Render(richTextBox1.Text);
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("Ups! Something went wrong.\n\n" + ex.Message);
            }
        }

        // SVG button click
        private void toolStripButton4_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && e.Clicks == 1)
            {
                try
                {
                    CreateSVG(richTextBox1.Text);
                    string[] filesToDrag = { Application.StartupPath + "/temp.svg" };
                    toolStripButton4.DoDragDrop(new DataObject(DataFormats.FileDrop, filesToDrag), DragDropEffects.Copy | DragDropEffects.Move);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ups! Something went wrong.\n\n" + ex.Message);
                }
            }
            else
            {
                try
                {
                    SaveFileDialog SFD = new SaveFileDialog();
                    SFD.Filter = "(*.svg)|*.svg";
                    if (SFD.ShowDialog() == DialogResult.OK)
                    {
                        if (File.Exists(SFD.FileName)) { File.Delete(SFD.FileName); }
                        File.Copy("temp.svg", SFD.FileName);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ups! Something went wrong.\n\n" + ex.Message);
                }
            }
        }

        // PNG button click
        private void toolStripButton3_MouseDown(object sender, MouseEventArgs e)
        {
            CreatePNG();
            if (e.Button == MouseButtons.Left && e.Clicks == 1)
            {
                try
                {
                    string[] filesToDrag = { Application.StartupPath + "/temp.png" };
                    toolStripButton3.DoDragDrop(new DataObject(DataFormats.FileDrop, filesToDrag), DragDropEffects.Copy | DragDropEffects.Move);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ups! Something went wrong.\n\n" + ex.Message);
                }
            }
            else
            {
                try
                {
                    SaveFileDialog SFD = new SaveFileDialog();
                    SFD.Filter = "(*.png)|*.png";
                    if (SFD.ShowDialog() == DialogResult.OK)
                    {
                        if (File.Exists(SFD.FileName)) { File.Delete(SFD.FileName); }
                        File.Copy("temp.png", SFD.FileName);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ups! Something went wrong.\n\n" + ex.Message);
                }
            }
        }

        //==============================================================================================
        // HELPER FUNCTIONS
        //==============================================================================================
        private Bitmap CreatePNG()
        {
            Point p = splitContainer1.Panel1.PointToScreen(new Point(0, 0));
            int x = p.X;
            int y = p.Y;
            Bitmap bitmap = new Bitmap(splitContainer1.Panel1.ClientSize.Width, splitContainer1.Panel1.ClientSize.Height);
            Graphics g = Graphics.FromImage(bitmap);
            g.CopyFromScreen(x,y,0,0,splitContainer1.Panel1.ClientSize);
            bitmap.Save("temp.png");
            return bitmap;
        }

        private void CreateSVG(string source_text, double scale_factor = 7.5)
        {
            if (source_text == "") return;

            // CREATE HTML STRING
            string source_code;
            source_code = "<html>";
            source_code += "<head>";

            // LOAD MATHJAX
            source_code += LoadMathjaxHtmlCode("tex-svg");

            // SOME JSCRIPT CODE TO DOWNLOAD THE SVG FILE
            source_code += "<script>";
            source_code += "var xmlHeader = '<' + '?xml version=\"1.0\" encoding=\"UTF - 8\" standalone=\"no\" ?' + '>\\n';\n";
            source_code += "var element = MathJax.tex2svg(\"" + source_text.Replace("\\","\\\\") + "\", {em: 16, ex: 8, containerWidth: 2, display: true, scale: 1, lineWidth: 1000000});\n";
            source_code += "var svg = element.firstElementChild;\n";
            source_code += "svg.setAttribute('width', (parseFloat(svg.getAttribute('width').replace('ex',''))*" + scale_factor.ToString() + ").toString() + 'px');\n";
            source_code += "svg.setAttribute('height', (parseFloat(svg.getAttribute('height').replace('ex',''))*" + scale_factor.ToString() + ").toString() + 'px');\n";
            source_code += "svg.removeAttribute('style');\n";
            source_code += "svg.removeAttribute('focusable');\n";
            source_code += "svg.removeAttribute('role');\n";
            source_code += "</script>";
            source_code += "</head>";
            source_code += "<body></body>";
            source_code += "<script>document.body.innerHTML = xmlHeader + svg.outerHTML;\n</script>";
            source_code += "</html>";

            // LOAD SOURCE CODE AND ADD A LISTENER FOR ONCE IT'S FINISHED LOADING
            svg_render_aux_browser.FrameLoadEnd += svg_render_aux_browser_load_ended;
            svg_render_aux_browser.LoadHtml(source_code);

            void svg_render_aux_browser_load_ended(object sender, FrameLoadEndEventArgs e)
            {
                if (e.Frame.IsMain)
                {
                    svg_render_aux_browser.GetSourceAsync().ContinueWith(taskHtml =>
                    {
                        // ONCE IT'S FINISHED LOADING, SAVE FILE
                        int counter = 5;
                        while (counter>0)
                        {
                            try
                            {
                                var html = taskHtml.Result;
                                string svg_code = html.Substring(html.IndexOf("<svg"), html.IndexOf("</svg>") - html.IndexOf("<svg") + 6);
                                StreamWriter svg_temp_file = new StreamWriter("temp.svg");
                                svg_temp_file.Write(svg_code);
                                svg_temp_file.Close();
                            }
                            catch (Exception ex)
                            {
                                counter--;
                            }
                            finally
                            {
                                counter = 0;
                            }
                        }
                    });
                }
            }
        }
 
        private void Render(string source_text)
        {
            // CREATE SOURCE CODE FOR WEB BROWSER TO DISPLAY LATEX
            string source_code;
            source_code = "<html>";
            source_code += "<head>";

            // LOAD MATHJAX
            source_code += LoadMathjaxHtmlCode();

            // ADD THIS STYLE TO PREVENT MATHJAX BLUE OUTLINING AND CENTERING
            source_code += "<style>:focus {outline:0 !important;} .container { position: absolute; top: 50%; left: 50%; transform: translateX(-50%) translateY(-50%);}</style>";

            // CREATE BODY
            source_code += "</head>";
            source_code += "<body><div class='container'>";
            source_code += "$$" + source_text + "$$";
            source_code += "</div></body></html>";

            // LOAD HTML CODE ON BROWSER AND CREATE IMAGE FILES
            browser.LoadHtml(source_code);
            CreateSVG(source_text);
        }

        private string LoadMathjaxHtmlCode(string renderer = "tex-mml-chtml")
        {
            string source_code = "";

            // CONFIGURE MATHJAX
            source_code += "<script>";
            source_code += "MathJax = {";
            source_code += "messageStyle: \"none\",";
            source_code += "options: { renderActions: { addMenu: [0] } },";
            source_code += "extensions: [\"tex2jax.js\",\"TeX / AMSmath.js\"],";
            source_code += "jax: [\"input/TeX\", \"output/HTML-CSS\"]";
            //source_code += "\"HTML-CSS\": { availableFonts: [], preferredFonts: \"TeX\", webFont:\"\", imageFont:\"\", undefinedFamily:\"'Arial Unicode MS',serif\" }";
            source_code += "};";
            source_code += "</script>";

            // WE'RE USING MATHJAX 3
            source_code += "<script type=\"text/javascript\" src=\"local://_/mathjax-3/es5/" + renderer + ".js\"></script>";

            return source_code;
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {

        }
    }



    //==============================================================================================
    // IMPORTANT CLASSES TO MAKE CEFSHARP WORK
    //==============================================================================================
    public class CustomMenuHandler : CefSharp.IContextMenuHandler
    {
        public void OnBeforeContextMenu(IWebBrowser browserControl, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model){ model.Clear(); }
        public bool OnContextMenuCommand(IWebBrowser browserControl, IBrowser browser, IFrame frame, IContextMenuParams parameters, CefMenuCommand commandId, CefEventFlags eventFlags) { return false; }
        public void OnContextMenuDismissed(IWebBrowser browserControl, IBrowser browser, IFrame frame) {}
        public bool RunContextMenu(IWebBrowser browserControl, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model, IRunContextMenuCallback callback) { return false; }
        
    }

    public class CustomProtocolSchemeHandler : ResourceHandler
    {
        // Specifies where you bundled app resides.
        // Basically path to your index.html
        private string frontendFolderPath;

        public CustomProtocolSchemeHandler()
        {
            frontendFolderPath = Application.StartupPath;
        }

        // Process request and craft response.
        public override CefReturnValue ProcessRequestAsync(IRequest request, ICallback callback)
        {
            var uri = new Uri(request.Url);
            var fileName = uri.AbsolutePath;

            var requestedFilePath = frontendFolderPath + fileName;

            var isAccesToFilePermitted = IsRequestedPathInsideFolder(
                new DirectoryInfo(requestedFilePath),
                new DirectoryInfo(frontendFolderPath));

            if (isAccesToFilePermitted && File.Exists(requestedFilePath))
            {
                byte[] bytes = File.ReadAllBytes(requestedFilePath);
                Stream = new MemoryStream(bytes);

                var fileExtension = Path.GetExtension(fileName);
                MimeType = GetMimeType(fileExtension);

                callback.Continue();
                return CefReturnValue.Continue;
            }

            callback.Dispose();
            return CefReturnValue.Cancel;
        }

        // Added for security reasons.
        // In this code it is used to verify that requested file is descendant to your index.html.
        public bool IsRequestedPathInsideFolder(DirectoryInfo path, DirectoryInfo folder)
        {
            if (path.Parent == null)
            {
                return false;
            }

            if (string.Equals(path.Parent.FullName, folder.FullName, StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            return IsRequestedPathInsideFolder(path.Parent, folder);
        }
    }

    public class CustomProtocolSchemeHandlerFactory : ISchemeHandlerFactory
    {
        public const string SchemeName = "local";

        public IResourceHandler Create(IBrowser browser, IFrame frame, string schemeName, IRequest request)
        {
            return new CustomProtocolSchemeHandler();
        }
    }
    
}