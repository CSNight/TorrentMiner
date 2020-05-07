using MongoDB.Bson;
using MongoDB.Driver.GridFS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.Windows.Forms;
using System.Threading;
using System.Text.RegularExpressions;
using JsonSupport;
using System.Text;
using System.Windows.Media;

namespace TorrentMiner
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private delegate void pordel(double val);
        pordel por;
        
        
        private int current_page = 1;
        private decimal total_page =0;
        private long total_count = 0;
        MongoDBOP moop =null;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            por= RefreshProgress;
            dataGrid.CanUserAddRows = false;
        }
        private int getPer()
        {
            return int.Parse((comboBox.SelectedItem as ComboBoxItem).Uid);
        }
        private void update_label()
        {
            total_page = Math.Ceiling((decimal)total_count / getPer());
            label.Content = current_page + " / " + total_page + ", Total: " + total_count;
        }

        private void comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Disconnect.IsEnabled)
            {
                update_label();
                update_pagebutton();
                LoadData();

            }
        }

        private void prev_Click(object sender, RoutedEventArgs e)
        {
            if (current_page > 1)
            {
                current_page--;
                update_label();
                update_pagebutton();
                LoadData();
            }
        }

        private void next_Click(object sender, RoutedEventArgs e)
        {
            if (current_page < total_page)
            {
                current_page++;
                update_label();
                update_pagebutton();
                LoadData();
            }
        }

        private void go_Click(object sender, RoutedEventArgs e)
        {
            int gotoP = 0;
            if (textBox.Text != "" && int.TryParse(textBox.Text, out gotoP))
            {
                if (gotoP > 0 && gotoP <= total_page)
                {
                    current_page = gotoP;
                    update_label();
                    update_pagebutton();
                    LoadData();
                }
            }
        }

        private void update_pagebutton()
        {
            if (total_count > 0)
            {
                go.IsEnabled = true;
            }
            else
            {
                go.IsEnabled = false;
            }
            if (current_page == 1)
            {
                prev.IsEnabled = false;
                next.IsEnabled = true;
            }
            else if (current_page > 1&&current_page<total_page)
            {
                prev.IsEnabled = true;
                next.IsEnabled = true;
            }
            else
            {
                prev.IsEnabled = true;
                next.IsEnabled = false;
            }
        }

        private void LoadData()
        {
            Dictionary<string, string> sort = new Dictionary<string, string>();
            sort.Add("timestamp", "ASC");
            int skip = (current_page - 1) * getPer();
            List<BsonDocument> bson_data = moop.FindByKVPair("resources", moop.FilterBuilder(null), sort_list: sort, limit: getPer(), skip: skip);
            List<Resources> bind_data = new List<Resources>();
            bson_data.ForEach(item =>
            {
                Resources row_data = new Resources();
                row_data.Index = skip++;
                row_data.Identify = item.GetValue("identify").ToString();
                row_data.Preview = item.GetValue("preview").ToString();
                row_data.Name= item.GetValue("name").ToString();
                DateTime dtStart = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
                row_data.Time = dtStart.Add(new TimeSpan((long)item.GetValue("timestamp").ToDouble() * 10000000)).ToString("yyyy/MM/dd");
                string tid=item.GetValue("torrent").ToString();
                string pid= item.GetValue("preview").ToString();
                if (pid == "")
                {
                    row_data.IsContainView = false;
                }
                else
                {
                    row_data.IsContainView = true;
                }
                MongoGridFSFileInfo tf = moop.GetFileById("torrents", tid);
                row_data.Size = tf.Length / 1024 * 1.0;
                bind_data.Add(row_data);
            });
            dataGrid.ItemsSource = bind_data;
            System.Windows.Controls.CheckBox s = GetChildObject<System.Windows.Controls.CheckBox>(dataGrid, "checkAll");
            s.IsChecked = false;
        }

        private void ckbSelectedAll_Checked(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < dataGrid.Items.Count; i++)
            {
                FrameworkElement objElement = dataGrid.Columns[0].GetCellContent(dataGrid.Items[i]);
                if (objElement != null)
                {
                    System.Windows.Controls.CheckBox objChk = (System.Windows.Controls.CheckBox)objElement;
                    if (objChk.IsChecked == false)
                    {
                        objChk.IsChecked = true;
                    }
                }
            }
        }

        private void ckbSelectedAll_Unchecked(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < dataGrid.Items.Count; i++)
            {
                FrameworkElement objElement = dataGrid.Columns[0].GetCellContent(dataGrid.Items[i]);
                if (objElement != null)
                {
                    System.Windows.Controls.CheckBox objChk = (System.Windows.Controls.CheckBox)objElement;
                    if (objChk.IsChecked == true)
                    {
                        objChk.IsChecked = false;
                    }
                }
            }
        }

        private void dataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                string pid = (e.AddedItems[0] as Resources).Preview;
                if (pid != "")
                {
                    try
                    {
                        MongoGridFSFileInfo pf = moop.GetFileById("previews", pid);
                        byte[] buf = new byte[pf.OpenRead().Length];
                        pf.OpenRead().Read(buf, 0, buf.Length);
                        BitmapImage img = new BitmapImage();
                        img.BeginInit();
                        img.StreamSource = new MemoryStream(buf);
                        img.EndInit();
                        image.Source = img;

                    }
                    catch
                    {

                    }

                }
            }
        }

        private void dataGrid_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            Resources checkitem = ((e.TargetObject as System.Windows.Controls.CheckBox).DataContext as Resources);
            if (checkitem.IsSelected)
            {
                ListBoxItem o = new ListBoxItem();
                o.Content = checkitem.Name;
                o.Tag = checkitem.Identify;
                listBox.Items.Add(o);
            }
            else
            {
                for (int i = 0; i < listBox.Items.Count; i++)
                {
                    if ((listBox.Items[i] as ListBoxItem).Content.ToString() == checkitem.Name)
                    {
                        listBox.Items.RemoveAt(i);
                    }
                }
            }
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            Configration conf = new Configration();
            if (conf.ShowDialog().Value == true)
            {

            }
        }

        private void ExportAllTor_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog tor_bor = new FolderBrowserDialog();
            tor_bor.Description = "Please select where to save all torrents files";
            if (tor_bor.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (Directory.Exists(tor_bor.SelectedPath))
                {
                    progressbar.Value = 0;
                    long count = 0;
                    Thread th = new Thread(new ThreadStart(delegate
                    {
                        moop.FindByKVPair("resources", moop.FilterBuilder(null)).ForEach(item =>
                        {
                            MongoGridFSFileInfo mof = moop.GetFileById("torrents", item.GetValue("torrent").ToString());
                            string Filepath = string.Join("", item.GetValue("name").ToString().Split());
                            Regex rg = new Regex("[/*:?\"<>|\\\\s]");
                            Filepath = rg.Replace(Filepath, "");
                            using (FileStream fs = new FileStream(tor_bor.SelectedPath + "\\" + Filepath + ".torrent", FileMode.OpenOrCreate))
                            {
                                byte[] buf = new byte[mof.OpenRead().Length];
                                mof.OpenRead().Read(buf, 0, buf.Length);
                                fs.Write(buf, 0, buf.Length);
                                fs.Flush();
                            }
                            double val = Math.Round((count + 1) * 100.0 / total_count, 1);
                            this.Dispatcher.Invoke(por, new object[] { val });
                            count++;
                        });
                        System.Windows.MessageBox.Show("Done");
                        this.Dispatcher.Invoke(por, new object[] { 0 });
                    }));
                    th.Start();
                }
            }
        }

        public void RefreshProgress(double val)
        {
            progressbar.Value = val;
            labelprogress.Content = val + "%";
        }

        private void ExportAllViews_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog tor_bor = new FolderBrowserDialog();
            tor_bor.Description = "Please select where to save all preview files";
            if (tor_bor.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (Directory.Exists(tor_bor.SelectedPath))
                {
                    progressbar.Value = 0;
                    long count = 0;
                    Thread th = new Thread(new ThreadStart(delegate
                    {
                        moop.FindByKVPair("resources", moop.FilterBuilder(null)).ForEach(item =>
                        {
                            if (item.GetValue("preview").ToString() == "")
                            {
                                return;
                            }
                            MongoGridFSFileInfo mof = moop.GetFileById("previews", item.GetValue("preview").ToString());
                            string Filepath = string.Join("", item.GetValue("name").ToString().Split());
                            Regex rg = new Regex("[/*:?\"<>|\\\\s]");
                            Filepath = rg.Replace(Filepath, "");
                            using (FileStream fs = new FileStream(tor_bor.SelectedPath + "\\" + Filepath + ".jpg", FileMode.OpenOrCreate))
                            {
                                byte[] buf = new byte[mof.OpenRead().Length];
                                mof.OpenRead().Read(buf, 0, buf.Length);
                                fs.Write(buf, 0, buf.Length);
                                fs.Flush();
                            }
                            double val = Math.Round((count + 1) * 100.0 / total_count, 1);
                            this.Dispatcher.Invoke(por, new object[] { val });
                            count++;
                        });
                        System.Windows.MessageBox.Show("Done");
                        this.Dispatcher.Invoke(por, new object[] { 0 });
                    }));
                    th.Start();
                }
            }
        }

        private void ExportSelectedViews_Click(object sender, RoutedEventArgs e)
        {
            List<Dictionary<object, object>> items = new List<Dictionary<object, object>>();
            for (int i = 0; i < listBox.Items.Count; i++)
            {
                ListBoxItem lbt = (listBox.Items[i] as ListBoxItem);
                Dictionary<object, object> search = new Dictionary<object, object>();
                Dictionary<object, object> likes = new Dictionary<object, object>();
                likes.Add("$regex", lbt.Tag);
                search.Add("file_name", likes);
                search.Add("content", lbt.Content.ToString());
                items.Add(search);
            }
            if (items.Count == 0)
            {
                return;
            }
            FolderBrowserDialog tor_bor = new FolderBrowserDialog();
            tor_bor.Description = "Please select where to save all preview files";
            if (tor_bor.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (Directory.Exists(tor_bor.SelectedPath))
                {
                    progressbar.Value = 0;
                    long count = 0;
                    Thread th = new Thread(new ThreadStart(delegate
                    {
                        items.ForEach(item =>
                        {
                            string Filepath = string.Join("", item["content"].ToString().Split());
                            Regex rg = new Regex("[/*:?\"<>|\\\\s]");
                            Filepath = rg.Replace(Filepath, "");
                            item.Remove("content");
                            MongoGridFSFileInfo mof = moop.GetFileByInfo("previews", item);
                            using (FileStream fs = new FileStream(tor_bor.SelectedPath + "\\" + Filepath + ".jpg", FileMode.OpenOrCreate))
                            {
                                byte[] buf = new byte[mof.OpenRead().Length];
                                mof.OpenRead().Read(buf, 0, buf.Length);
                                fs.Write(buf, 0, buf.Length);
                                fs.Flush();
                            }
                            double val = Math.Round((count + 1) * 100.0 / items.Count, 1);
                            this.Dispatcher.Invoke(por, new object[] { val });
                            count++;
                        });
                        System.Windows.MessageBox.Show("Job Done", "Message");
                        this.Dispatcher.Invoke(por, new object[] { 0 });
                    }));
                    th.Start();
                }
            }
        }

        private void ExportSelectedTor_Click(object sender, RoutedEventArgs e)
        {
            List<Dictionary<object, object>> items = new List<Dictionary<object, object>>();
            for (int i = 0; i < listBox.Items.Count; i++)
            {
                ListBoxItem lbt = (listBox.Items[i] as ListBoxItem);
                Dictionary<object, object> search = new Dictionary<object, object>();
                Dictionary<object, object> likes = new Dictionary<object, object>();
                likes.Add("$regex", lbt.Tag);
                search.Add("file_name", likes);
                search.Add("content", lbt.Content.ToString());
                items.Add(search);
            }
            if (items.Count == 0)
            {
                return;
            }
            FolderBrowserDialog tor_bor = new FolderBrowserDialog();
            tor_bor.Description = "Please select where to save all torrents files";
            if (tor_bor.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (Directory.Exists(tor_bor.SelectedPath))
                {
                    progressbar.Value = 0;
                    long count = 0;
                    Thread th = new Thread(new ThreadStart(delegate
                    {
                        items.ForEach(item =>
                        {
                            string Filepath = string.Join("", item["content"].ToString().Split());
                            Regex rg = new Regex("[/*:?\"<>|\\\\s]");
                            Filepath = rg.Replace(Filepath, "");
                            item.Remove("content");
                            MongoGridFSFileInfo mof = moop.GetFileByInfo("torrents", item);
                            using (FileStream fs = new FileStream(tor_bor.SelectedPath + "\\" + Filepath + ".torrent", FileMode.OpenOrCreate))
                            {
                                byte[] buf = new byte[mof.OpenRead().Length];
                                mof.OpenRead().Read(buf, 0, buf.Length);
                                fs.Write(buf, 0, buf.Length);
                                fs.Flush();
                            }
                            double val = Math.Round((count + 1) * 100.0 / items.Count, 1);
                            this.Dispatcher.Invoke(por, new object[] { val });
                            count++;
                        });
                        System.Windows.MessageBox.Show("Job Done", "Message");
                        this.Dispatcher.Invoke(por, new object[] { 0 });
                    }));
                    th.Start();
                }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ExportImage_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.AddExtension = true;
            if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                using (FileStream fs = new FileStream(sfd.FileName, FileMode.OpenOrCreate))
                {
                    var encoder = new JpegBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create((image.Source as BitmapImage)));
                    encoder.Save(fs);
                }
            }
        }

        private void ClearImage_Click(object sender, RoutedEventArgs e)
        {
            image.Source = null;
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            StreamReader sr = new StreamReader("Config.json", Encoding.UTF8);
            string json = sr.ReadToEnd();
            sr.Close();
            JsonObject jo = new JsonObject(json);
            string host = "";jo.TryGetString("host", out host);
            string db = ""; jo.TryGetString("db", out db);
            string user = ""; jo.TryGetString("user", out user);
            string pwd = ""; jo.TryGetString("pwd", out pwd);
            long? port = 0;jo.TryGetAsLong("port", out port);
            moop = new MongoDBOP(host, (int)port.Value, db, user, pwd);
            total_count = moop.FindCountByKVPair("resources", moop.FilterBuilder(null));
            compomentEnable(true);
            ConnectToDB.IsEnabled = false;
            Disconnect.IsEnabled = true;
            comboBox.SelectedIndex = 0;
            update_label();
            update_pagebutton();
        }

        private void Disconnect_Click(object sender, RoutedEventArgs e)
        {
            compomentEnable(false);
            ConnectToDB.IsEnabled = true;
            Disconnect.IsEnabled = false;
            dataGrid.ItemsSource = null;
            dataGrid.Items.Clear();
            label.Content = "";
            total_count = 0;
            total_page = 0;
            current_page = 1;
            comboBox.SelectedIndex = -1;
        }

        public void compomentEnable(bool status)
        {
            dataGrid.IsEnabled = status;
            ExportAllTorrents.IsEnabled = status;
            ExportAllViews.IsEnabled = status;
            ExportSelectedTorrents.IsEnabled = status;
            ExportSelectedViews.IsEnabled = status;
            PageClaw.IsEnabled = status;
            IdentifyClaw.IsEnabled = status;
            MissIdentifyClaw.IsEnabled = status;
            MissPreviewsClaw.IsEnabled = status;
            MissTorrentsClaw.IsEnabled = status;
            PreviewsClaw.IsEnabled = status;
            Start.IsEnabled = status;
            comboBox.IsEnabled = status;
            prev.IsEnabled = status;
            next.IsEnabled = status;
            go.IsEnabled = status;
            image.Source = null;
            listBox.Items.Clear();
        }

        private void DelSelect_Click(object sender, RoutedEventArgs e)
        {
            listBox.Items.Remove(listBox.SelectedItem);
        }

        private void ClearList_Click(object sender, RoutedEventArgs e)
        {
            listBox.Items.Clear();
            System.Windows.Controls.CheckBox s = GetChildObject<System.Windows.Controls.CheckBox>(dataGrid,"checkAll");
            s.IsChecked = false;
        }

        public T GetChildObject<T>(DependencyObject obj, string name) where T : FrameworkElement
        {
            DependencyObject child = null;
            T grandChild = null;
            for (int i = 0; i <= VisualTreeHelper.GetChildrenCount(obj) - 1; i++)
            {
                child = VisualTreeHelper.GetChild(obj, i);
                if (child is T && (((T)child).Name == name | string.IsNullOrEmpty(name)))
                {
                    return (T)child;
                }
                else
                {
                    grandChild = GetChildObject<T>(child, name);
                    if (grandChild != null)
                        return grandChild;
                }
            }
            return null;
        }
    }
}
