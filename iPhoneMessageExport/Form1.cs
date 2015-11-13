using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SQLite;
using System.Text.RegularExpressions;
using System.Security.Cryptography;


namespace iPhoneMessageExport
{

    public partial class Form1 : Form
    {
        string HTMLHEADERFILE = "../../headers.html";

        public static int getTimestamp(DateTime datetime)
        {
            DateTime sTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            return (int)(datetime - sTime).TotalSeconds;
        }

        public static DateTime timestampToDateTime(int timestamp)
        {
            DateTime sTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return sTime.AddSeconds(timestamp);
        }

        private string addHTMLHeaders ( string html )
        {
            string htmlHeaders = File.ReadAllText(HTMLHEADERFILE);
            return html + htmlHeaders;
        }

        /* GLOBAL variables */
        DataTable dtMessageFiles;
        string dbFile = null;
        string dbFileDate = null;
        string dbFileDir = null;

        public Form1()
        {
            InitializeComponent();

            string backupPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Apple Computer\MobileSync\Backup";
            DirectoryInfo dirBackup = new DirectoryInfo(backupPath);
            if (!dirBackup.Exists)
            {
                MessageBox.Show("Error: Cannot find iPhone backup folder.");
                return; // the path does not exist
            }

            dtMessageFiles = new DataTable();
            dtMessageFiles.Columns.Add("Timestamp", typeof(int));
            dtMessageFiles.Columns.Add("Path", typeof(string));
            dtMessageFiles.Columns.Add("FullDate", typeof(string));
            dtMessageFiles.DefaultView.Sort = "Timestamp DESC";

            DirectoryInfo[] dirBackups = dirBackup.GetDirectories("*.", SearchOption.TopDirectoryOnly);
            FileInfo[] files = null;
            foreach (DirectoryInfo dir in dirBackups)
            {
                // check that it contains the messages file (3d0d7e5fb2ce288813306e4d4636395e047a3d28)
                files = dir.GetFiles("3d0d7e5fb2ce288813306e4d4636395e047a3d28", SearchOption.TopDirectoryOnly);
                if (files != null)
                {
                    foreach (System.IO.FileInfo fi in files)
                    {
                        // Unix Timestamp will overflow in the year 2038
                        dtMessageFiles.Rows.Add(getTimestamp(fi.CreationTime), fi.FullName, fi.CreationTime.ToString("f"));
                    }
                }

            }

            DataView dvMessageFiles = dtMessageFiles.DefaultView;
            comboBackups.DataSource = new BindingSource(dvMessageFiles, null);
            comboBackups.DisplayMember = "FullDate";
            comboBackups.ValueMember = "Path";

            // enable comboBackups
            comboBackups.Enabled = true;

        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            // save selected dbFile for later use
            dbFile = comboBackups.SelectedValue.ToString();
            dbFileDate = comboBackups.GetItemText(comboBackups.SelectedItem);
            dbFileDir = dbFile.Substring(0,dbFile.LastIndexOf(@"\"));

            // open SQLite data file
            SQLiteConnection m_dbConnection;
            m_dbConnection = new SQLiteConnection("Data Source="+dbFile+";Version=3;Read Only=True;FailIfMissing=True;");
            m_dbConnection.Open();

            // add error handling

            // ### select the data
            string sql = "SELECT DISTINCT (SELECT GROUP_CONCAT(h.id) FROM chat_handle_join ch INNER JOIN handle h on h.ROWID = ch.handle_id " + 
                "WHERE ch.chat_id = cm.chat_id GROUP BY ch.chat_id) as chatgroup FROM chat_message_join cm " + 
                "INNER JOIN message m ON cm.message_id = m.ROWID INNER JOIN handle h ON m.handle_id = h.ROWID ORDER BY chatgroup;";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            SQLiteDataReader row = command.ExecuteReader();
            lbMessageGroup.Items.Clear();
            while (row.Read())
            {
                //Console.WriteLine("Chatgroup: " + row["chatgroup"]);
                if (row["chatgroup"].ToString().Trim()!="")
                    lbMessageGroup.Items.Add(row["chatgroup"]);
            }
            //Console.ReadKey();
            row.Close();
            m_dbConnection.Close(); // should only do this once?
        }

        private void lbMessageGroup_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnExport.Enabled = true;
        }

        public static string HexStringFromBytes(byte[] bytes)
        {
            var sb = new StringBuilder();
            foreach (byte b in bytes)
            {
                var hex = b.ToString("x2");
                sb.Append(hex);
            }
            return sb.ToString();
        }

        private static string getSHAHash(string s)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(s);
            using (var sha1 = SHA1.Create())
            {
                byte[] hashBytes = sha1.ComputeHash(bytes);
                return HexStringFromBytes(hashBytes);
            }
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            string chatGroup = lbMessageGroup.GetItemText(lbMessageGroup.SelectedItem);
            bool isGroupMessage = (chatGroup.Contains(",")) ? true : false;
            string htmlOutput = "";

            // FEATURE: HTML PREVIEW

            // show dialog for where to save html file
            SaveFileDialog htmlFileDialog = new SaveFileDialog();
            htmlFileDialog.Filter = "HTML File|*.htm,*.html";
            htmlFileDialog.Title = "Save an HTML File";
            htmlFileDialog.ShowDialog();

            // If the file name is not an empty string open it for saving.
            if (htmlFileDialog.FileName == "")
            {
                //MessageBox.Show("Filename cannot be empty!");
                return;
            }

            //MessageBox.Show(htmlFileDialog.FileName);

            // query database
            if (dbFile!=null)
            {
                // open SQLite data file
                SQLiteConnection m_dbConnection;
                m_dbConnection = new SQLiteConnection("Data Source=" + dbFile + ";Version=3;Read Only=True;FailIfMissing=True;");
                m_dbConnection.Open();

                // add error handling

                // ### select the data
                string sql = "SELECT cm.chat_id, (SELECT GROUP_CONCAT(h.id) FROM chat_handle_join ch INNER JOIN handle h on h.ROWID = ch.handle_id " +
                    "WHERE ch.chat_id = cm.chat_id GROUP BY ch.chat_id) as chatgroup, datetime(m.date+978307200,\"unixepoch\",\"localtime\") as date, " +
                    "m.service, CASE m.is_from_me WHEN 1 THEN \"SENT\" WHEN 0 THEN \"RCVD\" END as direction, h.id, " + 
                    "CASE m.type WHEN 1 THEN \"GROUP\" WHEN 0 THEN \"1 - ON - 1\" END as type, replace(m.text,cast(X'EFBFBC' as text),\"[MEDIA]\") as text, " +
                    "(SELECT GROUP_CONCAT(\"MediaDomain-\"||substr(a.filename,3)) FROM message_attachment_join ma " +
                    "JOIN attachment a ON ma.attachment_id = a.ROWID WHERE ma.message_id = m.ROWID GROUP BY ma.message_id) as filereflist " +
                    "FROM chat_message_join cm INNER JOIN message m ON cm.message_id = m.ROWID INNER JOIN handle h ON m.handle_id = h.ROWID " +
                    "WHERE chatgroup = \""+chatGroup+"\" ORDER BY date";
                //string sql = "SELECT m.ROWID, cm.chat_id, (SELECT GROUP_CONCAT(h.id) FROM chat_handle_join ch INNER JOIN handle h on h.ROWID = ch.handle_id WHERE ch.chat_id = cm.chat_id GROUP BY ch.chat_id) as chatgroup, datetime(m.date+978307200,\"unixepoch\",\"localtime\") as date, m.service, CASE m.is_from_me WHEN 1 THEN \"SENT\" WHEN 0 THEN \"RCVD\" END as direction, h.id, CASE m.type WHEN 1 THEN \"GROUP\" WHEN 0 THEN \"1-ON-1\" END as type, replace(m.text,cast(X'EFBFBC' as text),\"[IMG]\") as text, (SELECT GROUP_CONCAT(\"MediaDomain-\"||substr(a.filename,3)) FROM message_attachment_join ma JOIN attachment a ON ma.attachment_id = a.ROWID WHERE ma.message_id = m.ROWID GROUP BY ma.message_id) as filereflist FROM chat_message_join cm INNER JOIN message m ON cm.message_id = m.ROWID INNER JOIN handle h ON m.handle_id = h.ROWID WHERE m.ROWID = \"16460\" ORDER BY chatgroup, date LIMIT 1;";
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                SQLiteDataReader row = command.ExecuteReader();
                htmlOutput = addHTMLHeaders(htmlOutput);
                htmlOutput += "<BODY>\n";
                htmlOutput += "<H1>Messages from " + chatGroup + "</H1>\n";
                htmlOutput += "<H2>as of " + dbFileDate + "</H2>\n";
                htmlOutput += "<DIV id=\"messages\">\n";
                // date, service, direction, id, text, filereflist
                //htmlOutput += "<TR><TH>DATE</TH><TH>SERVICE</TH><TH>DIRECTION</TH><TH>ID</TH><TH>TEXT</TH><TH>FILEREFLIST</TH></TR>\n";
                while (row.Read())
                {
                    string content = (string)row["text"];
                    htmlOutput += "<DIV class=\"message message-" + row["direction"] + "-" + row["service"] + "\">";
                    htmlOutput += "<DIV class=\"timestamp-placeholder\"></DIV><DIV class=\"timestamp\">" + row["date"] + "</DIV>";
                    if (isGroupMessage && row["direction"].ToString() == "RCVD" ) htmlOutput += "<DIV class=\"sender\">" + row["id"] + "</DIV>";
                    // replace image placeholders (ï¿¼) with image files 
                    if (row["filereflist"].ToString().Length > 0)
                    {
                        List<string> mediaFileList = row["filereflist"].ToString().Split(',').ToList();
                        foreach (string mediaFile in mediaFileList) {
                            // get extension of mediaFile
                            // do switch statement for replacement string
                            Regex rgx = new Regex(@"\[MEDIA\]");
                            content = rgx.Replace(content, "<img src=\"" + dbFileDir + @"\" + getSHAHash(mediaFile) + "\"><!-- "+ mediaFile + " //-->", 1);
                        }
                    }
                    htmlOutput += "<DIV class=\"content\">" + content + "</DIV>\n";
                    htmlOutput += "</DIV>\n";
                }
                htmlOutput += "</DIV>\n";
                htmlOutput += "</BODY>\n";
                htmlOutput += "</HTML>\n";

                row.Close();
                m_dbConnection.Close(); // should only do this once?
            }

            // regex replacements
            // REGEX: change phone number format (+12257490000 => (225)749-0000)
            htmlOutput = Regex.Replace(htmlOutput, @"\+\d(\d{3})(\d{3})(\d{4})", "($1)$2-$3");
            // REGEX: change date format (2015-01-01 00:00 => 01/01/2015 12:00am)
            htmlOutput = Regex.Replace(htmlOutput, @"(\d{4})-(\d{2})-(\d{2})\s(\d{2}):(\d{2}):(\d{2})", delegate (Match match)
            {
                int hour = int.Parse(match.ToString().Substring(11, 2));
                string suffix = (hour < 12) ? "am" : "pm";
                if (hour == 0) hour += 12;
                else if (hour > 12) hour -= 12;
                string replace = "$2/$3/$1 " + hour + ":$5" + suffix;
                return match.Result(replace);
            });


            //htmlOutput = datetimeRegex.Replace(htmlOutput, @"$2/$3/$1 $4:$5 am",datetimeEvaluator);


            // do this in separate thread so it won't lock up program

            // output html data
            File.WriteAllText(htmlFileDialog.FileName,htmlOutput);
            //System.IO.FileStream fs = (System.IO.FileStream)htmlFileDialog.OpenFile();
            //fs.Close();

            MessageBox.Show("HTML Export completed!");
            

        }
    }
}
