using JsonSupport;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TorrentMiner
{
    /// <summary>
    /// Configration.xaml 的交互逻辑
    /// </summary>
    public partial class Configration : Window
    {
        public Configration()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            StreamReader sr = new StreamReader("Config.json", Encoding.UTF8);
            string json = sr.ReadToEnd();
            sr.Close();
            JsonObject jo = new JsonObject(json);
            string host = "";jo.TryGetString("host", out host);
            long? port = 0; jo.TryGetAsLong("port", out port);
            string db = ""; jo.TryGetString("db", out db);
            string user = ""; jo.TryGetString("user", out user);
            string pwd = ""; jo.TryGetString("pwd", out pwd);
            long? threadnum = 0; jo.TryGetAsLong("threadnum", out threadnum);
            string server = ""; jo.TryGetString("server", out server);
            string target = ""; jo.TryGetString("target", out target);
            string tor_server = ""; jo.TryGetString("tor_server", out tor_server);
            string save_type = ""; jo.TryGetString("save_type", out save_type);
            string path = ""; jo.TryGetString("path", out path);
            string list_identify = ""; jo.TryGetString("list_identify", out list_identify);
            string list_type = ""; jo.TryGetString("list_type", out list_type);
            string list_identify_text = ""; jo.TryGetString("list_identify_text", out list_identify_text);
            string list_element = ""; jo.TryGetString("list_element", out list_element);
            string content_identify = ""; jo.TryGetString("content_identify", out content_identify);
            string content_type = ""; jo.TryGetString("content_type", out content_type);
            string content_identify_text = ""; jo.TryGetString("content_identify_text", out content_identify_text);
            textBoxHost.Text = host;
            textBoxPort.Text = port.Value.ToString();
            textBoxDBName.Text = db;
            textBoxUser.Text = user;
            textBoxPassword.Text = pwd;
            comboBox.SelectedIndex = (int)(threadnum.Value / 4 - 1) ;
            textBoxServer.Text = server;
            textBoxTarget.Text = target;
            textBoxTorServer.Text = tor_server;
            textBoxSaveType.Text = save_type;
            textBoxPath.Text = path;
            textBoxListIdentify.Text = list_identify;
            textBoxListType.Text = list_type;
            textBoxListIdentifyText.Text = list_identify_text;
            textBoxListElement.Text = list_element;
            textBoxContentIdentify.Text = content_identify;
            textBoxContentType.Text = content_type;
            textBoxContentIdentifyText.Text = content_identify_text;
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            JsonObject jo = new JsonObject();
            try
            {
                jo.AddString("host", textBoxHost.Text != "" ? textBoxHost.Text : "127.0.0.1");
                jo.AddLong("port", textBoxPort.Text != "" ? long.Parse(textBoxPort.Text) : 27017);
                jo.AddString("db", textBoxDBName.Text != "" ? textBoxDBName.Text : "torrents");
                jo.AddString("user", textBoxUser.Text != "" ? textBoxUser.Text : "");
                jo.AddString("pwd", textBoxPassword.Text != "" ? textBoxPassword.Text : "");
                jo.AddLong("threadnum", (comboBox.SelectedIndex + 1) * 4);
                jo.AddString("server", textBoxServer.Text != "" ? textBoxServer.Text : "http://www.ac168.info/bt/");
                jo.AddString("target", textBoxTarget.Text != "" ? textBoxTarget.Text : "http://www.ac168.info/bt/thread.php?fid=4&page={0}");
                jo.AddString("tor_server", textBoxTorServer.Text != "" ? textBoxTorServer.Text : "http://www.jandown.com/fetch.php");
                jo.AddString("save_type", textBoxSaveType.Text != "" ? textBoxSaveType.Text : "db");
                jo.AddString("path", textBoxPath.Text != "" ? textBoxPath.Text : "G:\\迅雷下载\\Torrents\\");
                jo.AddString("list_identify", textBoxListIdentify.Text != "" ? textBoxListIdentify.Text : "class");
                jo.AddString("list_type", textBoxListType.Text != "" ? textBoxListType.Text : "tr");
                jo.AddString("list_identify_text", textBoxListIdentifyText.Text != "" ? textBoxListIdentifyText.Text : "tr3 t_one");
                jo.AddString("list_element", textBoxListElement.Text != "" ? textBoxListElement.Text : "dd");
                jo.AddString("content_identify", textBoxContentIdentify.Text != "" ? textBoxContentIdentify.Text : "class");
                jo.AddString("content_type", textBoxContentType.Text != "" ? textBoxContentType.Text : "div");
                jo.AddString("content_identify_text", textBoxContentIdentifyText.Text != "" ? textBoxContentIdentifyText.Text : "tpc_content");
                StreamWriter sw = new StreamWriter("Config.json", false);
                sw.Write(jo.ToJson());
                sw.Flush();
                sw.Close();
            }
            catch
            {

            }

            this.Close();

        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
