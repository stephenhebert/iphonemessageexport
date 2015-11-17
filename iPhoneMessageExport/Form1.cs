using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using System.Data.SQLite;
using System.Text.RegularExpressions;

namespace iPhoneMessageExport
{

    public partial class Form1 : Form
    {
        /* GLOBAL variables */
        DataTable dtMessageFiles;
        string dbFile = null;
        string dbFileDate = null;
        string dbFileDir = null;
        string messageGroup = null;
        string htmlFile = null;
        string HTMLHEADERFILE = "headers.html";
        string backupPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Apple Computer\MobileSync\Backup";
        string formTitle = null;

        /// <summary>
        /// Returns DataTable of iPhone message backup files given iPhone backup directory.
        /// </summary>
        /// <param name="dirBackup"></param>
        /// <returns></returns>
        private DataTable getBackupFiles(DirectoryInfo dirBackup)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Timestamp", typeof(int));
            dt.Columns.Add("Path", typeof(string));
            dt.Columns.Add("FullDate", typeof(string));
            dt.DefaultView.Sort = "Timestamp DESC";

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
                        dt.Rows.Add(MiscUtil.datetimeToTimestamp(fi.CreationTime), fi.FullName, fi.CreationTime.ToString("f"));
                    }
                }

            }

            return dt;
        }

        /// <summary>
        /// Returns DataTable of MessageGroups from iPhone backup file.
        /// </summary>
        /// <param name="dbFile"></param>
        /// <returns></returns>
        private DataTable getMessageGroupsFromFile(string dbFile)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Value", typeof(string));
            dt.Columns.Add("Display", typeof(string));
            dt.DefaultView.Sort = "Value ASC";

            if (dbFile != null)
            {
                // open SQLite data file
                SQLiteConnection m_dbConnection;
                m_dbConnection = new SQLiteConnection("Data Source=" + dbFile + ";Version=3;Read Only=True;FailIfMissing=True;");
                m_dbConnection.Open();

                // select the data
                string sql = "SELECT DISTINCT (SELECT GROUP_CONCAT(h.id) FROM chat_handle_join ch INNER JOIN handle h on h.ROWID = ch.handle_id " +
                    "WHERE ch.chat_id = cm.chat_id GROUP BY ch.chat_id) as chatgroup FROM chat_message_join cm " +
                    "INNER JOIN message m ON cm.message_id = m.ROWID INNER JOIN handle h ON m.handle_id = h.ROWID ORDER BY chatgroup;";
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                SQLiteDataReader row = command.ExecuteReader();

                while (row.Read())
                {
                    if (row["chatgroup"].ToString().Trim() != "")
                    {
                        // add and prettify US 11-digit phone numbers
                        dt.Rows.Add(row["chatgroup"], Regex.Replace(row["chatgroup"].ToString(), @"\+1(\d{3})(\d{3})(\d{4})\b", "($1)$2-$3"));
                    }
                }
                row.Close();
                m_dbConnection.Close();
            }

            return dt;

        }

        /// <summary>
        /// Inserts HTML headers from header file.
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        private string addHTMLHeaders(string html)
        {
            string htmlHeaders = File.ReadAllText(HTMLHEADERFILE);
            return html + htmlHeaders;
        }

        /// <summary>
        /// Export all messages for MessageGroup in backup file into HTML file. (THREAD)
        /// </summary>
        private void exportHTMLForMessageGroup()
        {

            string htmlOutput = "";
            bool isGroupMessage = (messageGroup.Contains(",")) ? true : false;

            // query database
            if (dbFile != null)
            {
                // open SQLite data file
                SQLiteConnection m_dbConnection;
                m_dbConnection = new SQLiteConnection("Data Source=" + dbFile + ";Version=3;Read Only=True;FailIfMissing=True;");
                m_dbConnection.Open();

                int totalMessages = 1;
                // get count of messages for progress bar
                string sql = "SELECT count(*) as count, (SELECT GROUP_CONCAT(h.id) FROM chat_handle_join ch " +
                    "INNER JOIN handle h on h.ROWID = ch.handle_id WHERE ch.chat_id = cm.chat_id GROUP BY ch.chat_id) as chatgroup " +
                    "FROM chat_message_join cm INNER JOIN message m ON cm.message_id = m.ROWID INNER JOIN handle h ON m.handle_id = h.ROWID " +
                    "WHERE chatgroup = \"" + messageGroup + "\" LIMIT 1";
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                SQLiteDataReader row = command.ExecuteReader();                
                if (row.Read())
                {
                    totalMessages = int.Parse(row["count"].ToString());
                }

                // select the data
                sql = "SELECT cm.chat_id, (SELECT GROUP_CONCAT(h.id) FROM chat_handle_join ch INNER JOIN handle h on h.ROWID = ch.handle_id " +
                    "WHERE ch.chat_id = cm.chat_id GROUP BY ch.chat_id) as chatgroup, datetime(m.date+978307200,\"unixepoch\",\"localtime\") as date, " +
                    "m.service, CASE m.is_from_me WHEN 1 THEN \"SENT\" WHEN 0 THEN \"RCVD\" END as direction, h.id, " +
                    "CASE m.type WHEN 1 THEN \"GROUP\" WHEN 0 THEN \"1 - ON - 1\" END as type, replace(m.text,cast(X'EFBFBC' as text),\"[MEDIA]\") as text, " +
                    "(SELECT GROUP_CONCAT(\"MediaDomain-\"||substr(a.filename,3)) FROM message_attachment_join ma " +
                    "JOIN attachment a ON ma.attachment_id = a.ROWID WHERE ma.message_id = m.ROWID GROUP BY ma.message_id) as filereflist " +
                    "FROM chat_message_join cm INNER JOIN message m ON cm.message_id = m.ROWID INNER JOIN handle h ON m.handle_id = h.ROWID " +
                    "WHERE chatgroup = \"" + messageGroup + "\" ORDER BY date";
                command = new SQLiteCommand(sql, m_dbConnection);
                row = command.ExecuteReader();
                htmlOutput = addHTMLHeaders(htmlOutput);
                htmlOutput += "<BODY>\n";
                htmlOutput += "<H1>Messages from " + messageGroup + "</H1>\n";
                htmlOutput += "<H2>as of " + dbFileDate + "</H2>\n";
                htmlOutput += "<DIV id=\"messages\">\n";
                // fields: date, service, direction, id, text, filereflist
                int i = 0;
                while (row.Read())
                {
                    string content = (string)row["text"];
                    htmlOutput += "<DIV class=\"message message-" + row["direction"] + "-" + row["service"] + "\">";
                    htmlOutput += "<DIV class=\"timestamp-placeholder\"></DIV><DIV class=\"timestamp\">" + row["date"] + "</DIV>";
                    if (isGroupMessage && row["direction"].ToString() == "RCVD") htmlOutput += "<DIV class=\"sender\">" + row["id"] + "</DIV>";
                    // replace image placeholders (ï¿¼) with image files 
                    if (row["filereflist"].ToString().Length > 0)
                    {
                        List<string> mediaFileList = row["filereflist"].ToString().Split(',').ToList();
                        foreach (string mediaFile in mediaFileList)
                        {
                            string replace = null;
                            // get extension of mediaFile
                            switch (mediaFile.Substring(mediaFile.LastIndexOf('.')).ToLower())
                            {
                                // image 
                                case ".jpeg":
                                case ".jpg":
                                case ".png":
                                    replace = "<img src=\"" + dbFileDir + @"\" + MiscUtil.getSHAHash(mediaFile) + "\"><!-- " + mediaFile + " //-->";
                                    break;
                                //case ".mov":
                                //case ".vcf":
                                default:
                                    replace = "";
                                    break;
                            }
                            // do switch statement for replacement string
                            Regex rgx = new Regex(@"\[MEDIA\]");
                            content = rgx.Replace(content, replace, 1);
                        }
                    }
                    htmlOutput += "<DIV class=\"content\">" + content + "</DIV>\n";
                    htmlOutput += "</DIV>\n";

                    i++;
                    backgroundWorker1.ReportProgress(i*100/totalMessages);
                }
                htmlOutput += "</DIV>\n";
                htmlOutput += "</BODY>\n";
                htmlOutput += "</HTML>\n";

                row.Close();
                m_dbConnection.Close();
            }

            // regex replacements
            // REGEX: change phone number format (+12257490000 => (225)749-0000)
            htmlOutput = Regex.Replace(htmlOutput, @"\+1(\d{3})(\d{3})(\d{4})\b", "($1)$2-$3");
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

            // output html data
            File.WriteAllText(htmlFile, htmlOutput);

            return;

        }

        // FORM LOGIC

        /// <summary>
        /// Set tooltip on load button and populate combobox with backup files.
        /// </summary>
        public Form1()
        {
            InitializeComponent();

            // save form title
            formTitle = this.Text;

            // set tooltip on load button
            System.Windows.Forms.ToolTip ToolTip1 = new System.Windows.Forms.ToolTip();
            ToolTip1.SetToolTip(this.btnLoad, "Load Backup File");

            // POPULATE COMBOBOX WITH BACKUP FILES
            DirectoryInfo dirBackup = new DirectoryInfo(backupPath);
            if (!dirBackup.Exists)
            {
                MessageBox.Show("Fatal Error: Cannot find iPhone backup folder.");
                this.Close();
                return; // the path does not exist
            }

            // get backup files
            dtMessageFiles = getBackupFiles(dirBackup);

            DataView dvMessageFiles = dtMessageFiles.DefaultView;
            comboBackups.DataSource = new BindingSource(dvMessageFiles, null);
            comboBackups.DisplayMember = "FullDate";
            comboBackups.ValueMember = "Path";

            // enable comboBackups
            if (comboBackups.Items.Count>0)
                comboBackups.Enabled = true;
            // disable export button until listbox is populated
            btnExport.Enabled = false;

        }


        /// <summary>
        /// Populate ListBox with MessageGroup from dB 
        /// </summary>
        private void btnLoad_Click(object sender, EventArgs e)
        {
            // save selected dbFile for later use
            dbFile = comboBackups.SelectedValue.ToString();
            dbFileDate = comboBackups.GetItemText(comboBackups.SelectedItem);
            dbFileDir = dbFile.Substring(0, dbFile.LastIndexOf(@"\"));

            // get message groups from file
            DataTable dtMessageGroups = getMessageGroupsFromFile(dbFile);

            DataView dvMessageGroups = dtMessageGroups.DefaultView;
            lbMessageGroup.DataSource = new BindingSource(dvMessageGroups, null);
            lbMessageGroup.DisplayMember = "Display";
            lbMessageGroup.ValueMember = "Value";

        }

        /// <summary>
        /// Enable HTML export button
        /// </summary>
        private void lbMessageGroup_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnExport.Enabled = true;
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            messageGroup = lbMessageGroup.SelectedValue.ToString();

            // show dialog for where to save html file
            SaveFileDialog htmlFileDialog = new SaveFileDialog();
            htmlFileDialog.Filter = "HTML File|*.htm,*.html";
            htmlFileDialog.Title = "Save an HTML File";
            htmlFileDialog.ShowDialog();

            // If the file name is not an empty string open it for saving.
            if (htmlFileDialog.FileName == "")
            {
                return;
            }
            htmlFile = htmlFileDialog.FileName;

            if (!File.Exists(HTMLHEADERFILE))
            {
                MessageBox.Show("HTML header file (headers.html) not found in application folder.");
                return;

            }

            // disable interface while exporting
            comboBackups.Enabled = false;
            lbMessageGroup.Enabled = false;
            btnLoad.Enabled = false;
            btnExport.Enabled = false;

            // Export messages from MessageGroup into HTML file
            backgroundWorker1.RunWorkerAsync();
            //Thread exportThread = new Thread(new ThreadStart(exportHTMLForMessageGroup));
            //exportThread.Start();

        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            exportHTMLForMessageGroup();
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            MessageBox.Show("HTML Export completed!");

            // re-enable interface after export completes
            comboBackups.Enabled = true;
            lbMessageGroup.Enabled = true;
            btnLoad.Enabled = true;
            btnExport.Enabled = true;
            this.Text = formTitle;
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            this.Text = formTitle + " - Exporting... ("+ e.ProgressPercentage.ToString() +"%)";
        }
    }
}