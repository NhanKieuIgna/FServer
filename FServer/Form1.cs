using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FServer
{
    public partial class Form1 : Form
    {
        // ========================================================
        // BIẾN TOÀN CỤC
        // ========================================================

        // Server dùng để lắng nghe client kết nối
        private TcpListener server;

        // Thread chạy nền để chờ client
        private Thread serverThread;

        // Kiểm tra server có đang chạy hay không
        private bool isRunning = false;

        // Thư mục lưu file
        private string rootFolder = @"D:\MyFTP";

        // ========================================================
        // CÁC CONTROL GIAO DIỆN
        // ========================================================

        private Button btnStart;
        private Button btnStop;
        private Button btnChooseFolder;
        private Button btnClearLog;

        private TextBox txtPort;
        private TextBox txtFolder;
        private TextBox txtLog;

        private Label lblStatus;

        // ========================================================
        // HÀM KHỞI TẠO
        // ========================================================

        public Form1()
        {
            InitializeComponent();
            CreateUI();
        }

        // ========================================================
        // TẠO GIAO DIỆN
        // ========================================================

        private void CreateUI()
        {
            // ----- Form -----
            this.Text = "FTP SERVER";
            this.Width = 650;
            this.Height = 520;

            // ----- Label Port -----
            Label lblPort = new Label();
            lblPort.Text = "Port:";
            lblPort.Left = 20;
            lblPort.Top = 20;
            lblPort.Width = 50;
            this.Controls.Add(lblPort);

            // ----- TextBox Port -----
            txtPort = new TextBox();
            txtPort.Text = "2121";
            txtPort.Left = 80;
            txtPort.Top = 20;
            txtPort.Width = 100;
            this.Controls.Add(txtPort);

            // ----- Label Folder -----
            Label lblFolder = new Label();
            lblFolder.Text = "Folder:";
            lblFolder.Left = 20;
            lblFolder.Top = 60;
            lblFolder.Width = 50;
            this.Controls.Add(lblFolder);

            // ----- TextBox Folder -----
            txtFolder = new TextBox();
            txtFolder.Text = rootFolder;
            txtFolder.Left = 80;
            txtFolder.Top = 60;
            txtFolder.Width = 350;
            this.Controls.Add(txtFolder);

            // ----- Button chọn thư mục -----
            btnChooseFolder = new Button();
            btnChooseFolder.Text = "Chọn";
            btnChooseFolder.Left = 450;
            btnChooseFolder.Top = 58;
            btnChooseFolder.Click += BtnChooseFolder_Click;
            this.Controls.Add(btnChooseFolder);

            // ----- Button Start -----
            btnStart = new Button();
            btnStart.Text = "START";
            btnStart.Left = 80;
            btnStart.Top = 100;
            btnStart.Width = 100;
            btnStart.Click += BtnStart_Click;
            this.Controls.Add(btnStart);

            // ----- Button Stop -----
            btnStop = new Button();
            btnStop.Text = "STOP";
            btnStop.Left = 200;
            btnStop.Top = 100;
            btnStop.Width = 100;
            btnStop.Enabled = false;
            btnStop.Click += BtnStop_Click;
            this.Controls.Add(btnStop);

            // ----- Status -----
            lblStatus = new Label();
            lblStatus.Text = "Server chưa chạy";
            lblStatus.Left = 20;
            lblStatus.Top = 150;
            lblStatus.Width = 500;
            this.Controls.Add(lblStatus);

            // ----- TextBox Log -----
            txtLog = new TextBox();
            txtLog.Multiline = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Left = 20;
            txtLog.Top = 180;
            txtLog.Width = 580;
            txtLog.Height = 250;
            txtLog.ReadOnly = true;
            this.Controls.Add(txtLog);

            // ----- Button Clear Log -----
            btnClearLog = new Button();
            btnClearLog.Text = "Clear Log";
            btnClearLog.Left = 480;
            btnClearLog.Top = 440;
            btnClearLog.Click += (s, e) => txtLog.Clear();
            this.Controls.Add(btnClearLog);
        }

        // ========================================================
        // CHỌN THƯ MỤC
        // ========================================================

        private void BtnChooseFolder_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                txtFolder.Text = dialog.SelectedPath;
                rootFolder = dialog.SelectedPath;
            }
        }

        // ========================================================
        // START SERVER
        // ========================================================

        private void BtnStart_Click(object sender, EventArgs e)
        {
            // Lấy thư mục
            rootFolder = txtFolder.Text.Trim();

            // Nếu chưa có thư mục thì tạo
            if (!Directory.Exists(rootFolder))
            {
                Directory.CreateDirectory(rootFolder);
            }

            // Kiểm tra port
            int port;

            bool checkPort = int.TryParse(txtPort.Text, out port);

            if (checkPort == false)
            {
                MessageBox.Show("Port không hợp lệ");
                return;
            }

            try
            {
                // Tạo server
                server = new TcpListener(IPAddress.Any, port);

                // Bắt đầu lắng nghe
                server.Start();

                isRunning = true;

                // Tạo thread chờ client
                serverThread = new Thread(WaitForClient);
                serverThread.IsBackground = true;
                serverThread.Start();

                Log("SERVER đã khởi động");
                lblStatus.Text = "Server đang chạy";

                btnStart.Enabled = false;
                btnStop.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        // ========================================================
        // STOP SERVER
        // ========================================================

        private void BtnStop_Click(object sender, EventArgs e)
        {
            isRunning = false;

            server.Stop();

            Log("SERVER đã dừng");

            lblStatus.Text = "Server đã dừng";

            btnStart.Enabled = true;
            btnStop.Enabled = false;
        }

        // ========================================================
        // CHỜ CLIENT KẾT NỐI
        // ========================================================

        private void WaitForClient()
        {
            Log("Đang chờ client kết nối...");

            while (isRunning)
            {
                try
                {
                    // Chấp nhận client
                    TcpClient client = server.AcceptTcpClient();

                    // Lấy IP client
                    string clientIP =
                        ((IPEndPoint)client.Client.RemoteEndPoint)
                        .Address
                        .ToString();

                    Log("Client kết nối: " + clientIP);

                    // Mỗi client chạy thread riêng
                    Thread clientThread =
                        new Thread(() => HandleClient(client, clientIP));

                    clientThread.IsBackground = true;
                    clientThread.Start();
                }
                catch
                {
                    if (!isRunning)
                    {
                        break;
                    }
                }
            }
        }

        // ========================================================
        // XỬ LÝ CLIENT
        // ========================================================

        private void HandleClient(TcpClient client, string clientIP)
        {
            try
            {
                // Stream giao tiếp
                NetworkStream ns = client.GetStream();

                StreamReader reader = new StreamReader(ns);

                StreamWriter writer = new StreamWriter(ns);
                writer.AutoFlush = true;

                // Gửi thông báo chào
                writer.WriteLine("220 Welcome FTP Server");

                while (true)
                {
                    // Nhận lệnh từ client
                    string request = reader.ReadLine();

                    if (request == null)
                    {
                        break;
                    }

                    Log(clientIP + " >> " + request);

                    // Tách lệnh
                    string command = "";

                    if (request.Length >= 4)
                    {
                        command =
                            request.Substring(0, 4)
                            .Trim()
                            .ToUpper();
                    }
                    else
                    {
                        command = request.ToUpper();
                    }

                    // =================================================
                    // USER
                    // =================================================

                    if (command == "USER")
                    {
                        writer.WriteLine("331 Nhập password");
                    }

                    // =================================================
                    // PASS
                    // =================================================

                    else if (command == "PASS")
                    {
                        writer.WriteLine("230 Đăng nhập thành công");
                    }

                    // =================================================
                    // LIST
                    // =================================================

                    else if (command == "LIST")
                    {
                        string[] files =
                            Directory.GetFiles(rootFolder);

                        writer.WriteLine("150 Danh sách file:");

                        foreach (string file in files)
                        {
                            FileInfo info = new FileInfo(file);

                            writer.WriteLine(info.Name);
                        }

                        writer.WriteLine("226 Hoàn tất");
                    }

                    // =================================================
                    // MKD - TẠO THƯ MỤC
                    // =================================================

                    else if (command == "MKD")
                    {
                        string folderName =
                            request.Substring(4).Trim();

                        string fullPath =
                            Path.Combine(rootFolder, folderName);

                        Directory.CreateDirectory(fullPath);

                        writer.WriteLine("257 Đã tạo thư mục");

                        Log("Đã tạo folder: " + folderName);
                    }

                    // =================================================
                    // STOR - UPLOAD FILE
                    // =================================================

                    else if (command == "STOR")
                    {
                        string fileName =
                            request.Substring(5).Trim();

                        fileName = Path.GetFileName(fileName);

                        string fullPath =
                            Path.Combine(rootFolder, fileName);

                        writer.WriteLine("150 Ready");

                        BinaryReader br =
                            new BinaryReader(ns);

                        long fileSize = br.ReadInt64();

                        byte[] data =
                            br.ReadBytes((int)fileSize);

                        File.WriteAllBytes(fullPath, data);

                        writer.WriteLine("226 Upload xong");

                        Log("Client upload file: " + fileName);
                    }

                    // =================================================
                    // RETR - DOWNLOAD FILE
                    // =================================================

                    else if (command == "RETR")
                    {
                        string fileName =
                            request.Substring(5).Trim();

                        string fullPath =
                            Path.Combine(rootFolder, fileName);

                        if (File.Exists(fullPath))
                        {
                            byte[] data =
                                File.ReadAllBytes(fullPath);

                            writer.WriteLine("150 Sending file");

                            BinaryWriter bw =
                                new BinaryWriter(ns);

                            bw.Write((long)data.Length);

                            bw.Write(data);

                            bw.Flush();

                            Log("Đã gửi file: " + fileName);
                        }
                        else
                        {
                            writer.WriteLine("550 File không tồn tại");
                        }
                    }

                    // =================================================
                    // QUIT
                    // =================================================

                    else if (command == "QUIT")
                    {
                        writer.WriteLine("221 Goodbye");

                        Log("Client ngắt kết nối");

                        break;
                    }

                    // =================================================
                    // LỆNH KHÔNG HỖ TRỢ
                    // =================================================

                    else
                    {
                        writer.WriteLine("502 Command not supported");
                    }
                }

                // Đóng client
                client.Close();
            }
            catch (Exception ex)
            {
                Log("Lỗi client: " + ex.Message);
            }
        }

        // ========================================================
        // GHI LOG
        // ========================================================

        private void Log(string message)
        {
            string text =
                "[" + DateTime.Now.ToString("HH:mm:ss") + "] "
                + message;

            // Kiểm tra khác thread hay không
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action(() =>
                {
                    txtLog.AppendText(text + Environment.NewLine);
                }));
            }
            else
            {
                txtLog.AppendText(text + Environment.NewLine);
            }
        }
    }
}
