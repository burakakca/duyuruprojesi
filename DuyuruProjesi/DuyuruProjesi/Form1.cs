using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


using System.Net.Sockets;


using Newtonsoft.Json;

namespace DuyuruProjesi
{
    public partial class Form1 : Form
    {
        
        TcpClient clientSocket;
        public Dictionary<string, string> defaultAyarlarDict = new Dictionary<string, string>();
        public Dictionary<string, string> ozelAyarlarDict = new Dictionary<string, string>();
        string kullanici = "Admin";
        public Form1()
        {
            InitializeComponent();
            clientSocket = new TcpClient();
            
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            timer1.Start();
            defaultAyarDictBaslangic();
            ozelAyarDictBaslangic();
            planDateTimePicker.Value = DateTime.Today.AddDays(+7);
            try
            {        
                clientSocket.Connect("127.0.0.1", 9000);
                duyuruYenile();
                durumLabel.Text = "Bağlandı";
                durumLabel.ForeColor = Color.Green;
                Dictionary<string, List<string>>  ayarlar = getAyarlar();
                ayarYap(ayarlar);
            }
            catch(SocketException)
            {
                durumLabel.Text = "Bağlanamadı";
                durumLabel.ForeColor = Color.Red;
                
            }
            
        }
        private void duyuruEkleButton_Click(object sender, EventArgs e)
        {
            if (durumLabel.Text == "Bağlandı")
            {
                string status = statusChecker();
                if (status == "ok\n")
                {
                    if (duyuruTextBox.TextLength > 0)
                    {
                        DialogResult dlg = MessageBox.Show("Eklemek istediğinize emin misiniz?", "", MessageBoxButtons.YesNo);
                        if (dlg == DialogResult.Yes)
                        {
                            duyuruEkle();
                            string bilgi = String.Format("'{0}' başlıklı duyurunuz eklenmiştir.", duyuruBaslikTb.Text);
                            ekleBilgiLabel.Text = bilgi;
                            duyuruYenile();
                            duyuruBaslikTb.Clear();
                            duyuruTextBox.Clear();
                            girisDateTimePicker.ResetText();
                            planDateTimePicker.Value = DateTime.Today.AddDays(+7);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Lütfen duyuru yazdığınıza emin olun.");
                    }

                }
                else
                {
                    durumLabel.Text = "Bağlanamadı";
                    durumLabel.ForeColor = Color.Red;
                    MessageBox.Show("Sunucu çalışmıyor ya da bağlı değilsiniz.");
                }
            }
            else
            {
                MessageBox.Show("Sunucu çalışmıyor ya da bağlı değilsiniz.");
            }
        }
        private void duyuruEkle()
        {
            string duyuru = duyuruTextBox.Text;
            string baslik = duyuruBaslikTb.Text;
            string giris_tarih;
            string duyuru_tur;
            if (aktifRadioButton.Checked == true)
            {
                duyuru_tur = "aktif";
                giris_tarih = DateTime.Now.ToShortDateString();
            }
            else
            {
                duyuru_tur = "plan";
                giris_tarih = girisDateTimePicker.Value.ToShortDateString();
            }
            string cikis_tarih = planDateTimePicker.Value.ToShortDateString();
            Dictionary<string, string> duyuruEkleDict = new Dictionary<string, string>();
            duyuruEkleDict.Add("komut", "ekle");
            duyuruEkleDict.Add("kullanici", kullanici);
            duyuruEkleDict.Add("baslik", baslik);
            duyuruEkleDict.Add("duyuru", duyuruTextBox.Text);
            duyuruEkleDict.Add("giris_tarih", giris_tarih);
            duyuruEkleDict.Add("cikis_tarih", cikis_tarih);
            duyuruEkleDict.Add("duyuru_tur", duyuru_tur);
            string output = JsonConvert.SerializeObject(duyuruEkleDict);
            requestGonderAl(output);
        }

        private void aktifYenileButton_Click(object sender, EventArgs e)
        {
            if (durumLabel.Text == "Bağlandı")
            {
                string status = statusChecker();
               
                if (status == "ok\n")
                { 
                    duyuruYenile();
                }
                else
                {
                    durumLabel.Text = "Bağlanamadı";
                    durumLabel.ForeColor = Color.Red;
                    MessageBox.Show("Sunucu çalışmıyor ya da bağlı değilsiniz.");

                }
            }
            else
            {
                MessageBox.Show("Sunucu çalışmıyor ya da bağlı değilsiniz.");
            }

        }
        private void planDuyuruYenileButton_Click(object sender, EventArgs e)
        {
            if (durumLabel.Text == "Bağlandı")
            {
                string status = statusChecker();

                if (status == "ok\n")
                {
                    duyuruYenile();
                }
                else
                {
                    durumLabel.Text = "Bağlanamadı";
                    durumLabel.ForeColor = Color.Red;
                    MessageBox.Show("Sunucu çalışmıyor ya da bağlı değilsiniz.");

                }
            }
            else
            {
                MessageBox.Show("Sunucu çalışmıyor ya da bağlı değilsiniz.");
            }

        }

        private void duyuruYenile()
        {
            aktifGrid.Rows.Clear();
            planGrid.Rows.Clear();
            List<Dictionary<string, List<string>>> countAndRowIdS = getCountAndRowId();
            Dictionary<string, string> yenileDuyuruDict = new Dictionary<string, string>();
            foreach (string rowId in countAndRowIdS[0]["rowIdS"])
            {
                yenileDuyuruDict.Add("komut", "yenile");
                yenileDuyuruDict.Add("rowid", rowId);
                string giden = JsonConvert.SerializeObject(yenileDuyuruDict);
                string gelen = requestGonderAl(giden);
                List<Dictionary<string, string>> rows = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(gelen);

                if (rows[0]["duyuru_tur"] == "aktif")
                { 
                    aktifGrid.Rows.Add(rows[0]["baslik"], rows[0]["duyuru"], rows[0]["giris_tarih"], rows[0]["cikis_tarih"], rows[0]["rowid"]);
                }
                else
                {
                    planGrid.Rows.Add(rows[0]["baslik"], rows[0]["duyuru"], rows[0]["giris_tarih"], rows[0]["cikis_tarih"], rows[0]["rowid"]);
                }
                yenileDuyuruDict.Clear();
            }
        }
        private void planDuyuruSilButton_Click(object sender, EventArgs e)
        {
            if (durumLabel.Text == "Bağlandı")
            {
                string status = statusChecker();
                if (status == "ok\n")
                {
                    DialogResult dlg = MessageBox.Show("Seçtiğiniz duyuruyu silmek istediğinize emin misiniz?", "", MessageBoxButtons.YesNo);
                    if (dlg == DialogResult.Yes)
                    {
                        try
                        {
                            string rowid = planGrid.SelectedRows[0].Cells[4].Value.ToString();
                            duyuruSil(rowid, "plan");
                            duyuruYenile();
                        }
                        catch (NullReferenceException)
                        {
                            MessageBox.Show("Lütfen duyuru seçtiğinizden emin olun.");
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            MessageBox.Show("Lütfen duyuru seçtiğinizden emin olun.");
                        }
                    }
                }
                else
                {
                    durumLabel.Text = "Bağlanamadı";
                    durumLabel.ForeColor = Color.Red;
                    MessageBox.Show("Sunucu çalışmıyor ya da bağlı değilsiniz.");

                }
            }
            else
            {
                MessageBox.Show("Sunucu çalışmıyor ya da bağlı değilsiniz.");
            }

        }
        private void aktifDuyuruSilButton_Click(object sender, EventArgs e)
        {
            if (durumLabel.Text == "Bağlandı")
            {
                string status = statusChecker();
                if (status == "ok\n")
                {
                    DialogResult dlg = MessageBox.Show("Seçtiğiniz duyuruyu silmek istediğinize emin misiniz?", "", MessageBoxButtons.YesNo);
                    if (dlg == DialogResult.Yes)
                    {
                        try
                        {
                            string rowid = aktifGrid.SelectedRows[0].Cells[4].Value.ToString();
                            duyuruSil(rowid, "aktif");
                            duyuruYenile();
                        }
                        catch (NullReferenceException)
                        {
                            MessageBox.Show("Lütfen duyuru seçtiğinizden emin olun.");
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            MessageBox.Show("Lütfen duyuru seçtiğinizden emin olun.");
                        }
                    }
                }
                else
                {
                    durumLabel.Text = "Bağlanamadı";
                    durumLabel.ForeColor = Color.Red;
                    MessageBox.Show("Sunucu çalışmıyor ya da bağlı değilsiniz.");

                }
            }
            else
            {
                MessageBox.Show("Sunucu çalışmıyor ya da bağlı değilsiniz.");
            }
            
        }
        private void duyuruSil(string rowid, string tur)
        {
            // a gidecek dict...
            Dictionary<string, string> duyuruSilDict = new Dictionary<string, string>();
            duyuruSilDict.Add("komut", "sil");
            duyuruSilDict.Add("tur", tur);
            duyuruSilDict.Add("rowid", rowid);
            //silme işlemi...
            string giden = JsonConvert.SerializeObject(duyuruSilDict);
            string gelen = requestGonderAl(giden);
            //silmeden sonra tekrar update....
        }
        private string requestGonderAl(string strToSend)
        {
            //veri gönderiliyor...


            try
            { 
                NetworkStream serverStream = clientSocket.GetStream();
                byte[] outStream = Encoding.UTF8.GetBytes(strToSend);
                serverStream.Write(outStream, 0, outStream.Length);
                serverStream.Flush();

                //response alınıyor...
                byte[] inStream = new byte[4096];
                int bytesRead = serverStream.Read(inStream, 0, inStream.Length);
                string returnData = Encoding.ASCII.GetString(inStream, 0, bytesRead);
                Console.WriteLine(returnData);
                return returnData;
            }
            catch (System.IO.IOException)
            {
                
                return "bad";
            }
            catch (InvalidOperationException)
            {
                return "bad";
            }
           

        }
        private List<Dictionary<string, List<string>>> getCountAndRowId()
        {
            //update etmden once veritabanindaki verilerin rowidleri cekiliyor.
            Dictionary<string, string> getCountAndRowIdDict = new Dictionary<string, string>();
            getCountAndRowIdDict.Add("komut", "getRowIdS");
            string giden = JsonConvert.SerializeObject(getCountAndRowIdDict);
            string gelen = requestGonderAl(giden);
            List<Dictionary<string, List<string>>> countAndRowIdS = JsonConvert.DeserializeObject<List<Dictionary<string, List<string>>>>(gelen);
            return countAndRowIdS;

        }
        private void aktifGrid_CellContentDoubleClick(object sender, DataGridViewCellEventArgs e)
        {

            aktifRichTb.Clear();
            aktifBaslikTb.Clear();
            aktifBaslikTb.Text = aktifGrid.Rows[e.RowIndex].Cells[0].Value.ToString();
            string richBox =  aktifGrid.Rows[e.RowIndex].Cells[1].Value.ToString();
            aktifRichTb.AppendText(richBox);
        }

        private void planGrid_CellContentDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            planBaslikTb.Clear();
            planRichTb.Clear();
            planBaslikTb.Text = planGrid.Rows[e.RowIndex].Cells[0].Value.ToString();
            string richBox = planGrid.Rows[e.RowIndex].Cells[1].Value.ToString();
            planRichTb.AppendText(richBox);
        }
        public int mousex, mousey, move;

        private void label1_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void label2_Click(object sender, EventArgs e)
        {

            this.Close();
           
        }


        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            move = 1;
            mousex = e.X;
            mousey = e.Y;
        }
        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (aktifRadioButton.Checked == true)
            {
                girisDateTimePicker.Visible = false;
                label12.Visible = true;
                planDateTimePicker.Value = DateTime.Today.AddDays(+7);
            }
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (planRadioButton.Checked == true)
            {

                girisDateTimePicker.Visible = true;
                label12.Visible = false;
                planDateTimePicker.Value = DateTime.Today.AddDays(+7);
            }
        }
        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                clientSocket = new TcpClient();
                string ip = textBox2.Text;
                int port = Convert.ToInt32(textBox3.Text);
                clientSocket.Connect(ip, port);
                duyuruYenile();
                durumLabel.Text = "Bağlandı";
                durumLabel.ForeColor = Color.Green;
            }
            catch (SocketException exp)
            {
                durumLabel.Text = "Bağlanamadı";
                durumLabel.ForeColor = Color.Red;

             
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            tarihSaatLabel.Text = DateTime.Now.ToString();
        }

        private void label10_MouseDoubleClick(object sender, MouseEventArgs e)
        {

        }
        private void panel1_MouseUp(object sender, MouseEventArgs e)
        {
            move = 0;
        }
        private void panel1_MouseMove(object sender, MouseEventArgs e)
        {
            if (move == 1)
            {
                this.SetDesktopLocation(MousePosition.X - mousex, MousePosition.Y - mousey);
            }
        }
        //AYARLAR
        private void ozelGorunumRb_CheckedChanged(object sender, EventArgs e)
        {
            if (ozelGorunumRb.Checked == true)
            {
                arkaPlanDialogButton.Enabled = true;
                duyuruArkaPlanDialogButton.Enabled = true;
                metinRenkDialogButton.Enabled = true;
                metinFontDialogButton.Enabled = true;
      
                ayarUygulaButton.Enabled = true;

                //----------------------------------------------------//
                sayfaArkaRenkTb.BackColor = ColorTranslator.FromHtml(ozelAyarlarDict["sayfaArkaPlan"]);
                duyuruArkaRenkTb.BackColor = ColorTranslator.FromHtml(ozelAyarlarDict["duyuruArkaPlan"]);
                metinRenkTb.BackColor = ColorTranslator.FromHtml(ozelAyarlarDict["metinRengi"]);
                metinFontLbl.Font = new Font(ozelAyarlarDict["metinFontu"], metinFontLbl.Font.Size);
                textBox1.Text = ozelAyarlarDict["donusSure"].Substring(0,1);
               

            }
        }
        private void varsayilanGorunumRb_CheckedChanged(object sender, EventArgs e)
        {
            if (varsayilanGorunumRb.Checked == true)
            {
                arkaPlanDialogButton.Enabled = false;
                duyuruArkaPlanDialogButton.Enabled = false;
                metinRenkDialogButton.Enabled = false;
                metinFontDialogButton.Enabled = false;

                //---------------------------------------------------//
                
                sayfaArkaRenkTb.BackColor = Color.FromArgb(211, 211, 211);
                duyuruArkaRenkTb.BackColor = Color.FromArgb(100, 174, 219);
                metinRenkTb.BackColor = Color.FromArgb(230, 230, 230);
                metinFontLbl.Font = new Font("Sans-Serif", metinFontLbl.Font.Size);
                textBox1.Text = "5";
            }

        }
        private Color getRenk()
        {
            return Color.Blue;
        }
        private void defaultAyarDictBaslangic()
        {
            //form_load da calisiyor.
            defaultAyarlarDict.Clear();
            defaultAyarlarDict.Add("komut", "setAyarlar");
            defaultAyarlarDict.Add("sayfaArkaPlan", "#d3d3d3");
            defaultAyarlarDict.Add("duyuruArkaPlan", "#64aedb");
            defaultAyarlarDict.Add("metinRengi", "#e6e6e6");
            defaultAyarlarDict.Add("metinFontu", "Sans-Serif");
            defaultAyarlarDict.Add("donusSure", "5000");
        }
        private void ozelAyarDictBaslangic()
        {
            //form_load da calisiyor.
            ozelAyarlarDict.Clear();
            ozelAyarlarDict.Add("komut", "setAyarlar");
            ozelAyarlarDict.Add("sayfaArkaPlan", "#d3d3d3");
            ozelAyarlarDict.Add("duyuruArkaPlan", "#64aedb");
            ozelAyarlarDict.Add("metinRengi", "#e6e6e6");
            ozelAyarlarDict.Add("metinFontu", "Sans-Serif");
            ozelAyarlarDict.Add("donusSure", "5000");
        }
        private void ayarUygulaButton_Click(object sender, EventArgs e)
        {
            if (durumLabel.Text == "Bağlandı")
            {
                string status = statusChecker();
                if (status == "ok\n")
                {
                    DialogResult dg = MessageBox.Show("Bu ayarları yapmak istediğinize emin misiniz?", "", MessageBoxButtons.YesNo);
                    if (dg == DialogResult.Yes)
                    {
                        if (varsayilanGorunumRb.Checked == true)
                        {
                            setAyarlar(defaultAyarlarDict);

                        }
                        else if (ozelGorunumRb.Checked == true)
                        {
                            ozelAyarlarDict["donusSure"] = textBox1.Text + "000";
                            setAyarlar(ozelAyarlarDict);
                        }
                        Dictionary<string, List<string>> ayarlar = getAyarlar();
                        ayarYap(ayarlar);
                    }
                       

                }
                else
                {
                    durumLabel.Text = "Bağlanamadı";
                    durumLabel.ForeColor = Color.Red;
                    MessageBox.Show("Sunucu çalışmıyor ya da bağlı değilsiniz.");

                }
            }
            else
            {
                MessageBox.Show("Sunucu çalışmıyor ya da bağlı değilsiniz.");
            }

        }


        private void arkaPlanDialogButton_Click(object sender, EventArgs e)
        {
            DialogResult result = sayfaArkaPlanDialog.ShowDialog();
            // See if user pressed ok.
            if (result == DialogResult.OK)
            {
                Color col = sayfaArkaPlanDialog.Color;
                // Set form background to the selected color.
                sayfaArkaRenkTb.BackColor = col;
                if (col.IsNamedColor)
                {
                    ozelAyarlarDict["sayfaArkaPlan"] = sayfaArkaRenkTb.BackColor.Name.ToString();
                }
                else
                {
                    string clr = sayfaArkaRenkTb.BackColor.Name.ToString();
                    clr = clr.Substring(2);
                    ozelAyarlarDict["sayfaArkaPlan"] = "#" + clr;
                }

                
                
            }
        }

        private void duyuruArkaPlanDialogButton_Click(object sender, EventArgs e)
        {
            DialogResult result = duyuruArkaPlanDialog.ShowDialog();
            // See if user pressed ok.
            if (result == DialogResult.OK)
            {
                Color col = duyuruArkaPlanDialog.Color;
                // Set form background to the selected color.
                duyuruArkaRenkTb.BackColor = col;
                if (col.IsNamedColor)
                {
                    ozelAyarlarDict["duyuruArkaPlan"] = duyuruArkaRenkTb.BackColor.Name.ToString();
                }
                else
                {
                    string clr = duyuruArkaRenkTb.BackColor.Name.ToString();
                    clr = clr.Substring(2);
                    ozelAyarlarDict["duyuruArkaPlan"] = "#" + clr;
                }
            }
        }

        private void metinRenkDialogButton_Click(object sender, EventArgs e)
        {
            DialogResult result = duyuruMetinDialog.ShowDialog();
            // See if user pressed ok.
            if (result == DialogResult.OK)
            {
                Color col = duyuruMetinDialog.Color;
                // Set form background to the selected color.
                metinRenkTb.BackColor = col;
                if (col.IsNamedColor)
                {
                    ozelAyarlarDict["metinRengi"] = metinRenkTb.BackColor.Name.ToString();
                }
                else
                {
                    string clr = metinRenkTb.BackColor.Name.ToString();
                    clr = clr.Substring(2);
                    ozelAyarlarDict["metinRengi"] = "#" + clr;
                }
            }
            
        }
        
        private void metinFontDialogButton_Click(object sender, EventArgs e)
        {
            DialogResult result = metinFontDialog.ShowDialog();
            // See if user pressed ok.
            if (result == DialogResult.OK)
            {
                // Set form background to the selected color.
                Font font = metinFontDialog.Font;
                
                metinFontLbl.Font = new Font(font.FontFamily, metinFontLbl.Font.Size);
                ozelAyarlarDict["metinFontu"] = metinFontLbl.Font.Name.ToString();
                
            }
        }

        private void setAyarlar(Dictionary<string, string> dict)
        {
            string giden = JsonConvert.SerializeObject(dict);
            string gelen = requestGonderAl(giden);
        }

        private string statusChecker()
        {
            Dictionary<string, string> statusDict = new Dictionary<string, string>();
            statusDict.Add("komut", "check");
            string giden = JsonConvert.SerializeObject(statusDict);
            string gelen = requestGonderAl(giden);
            return gelen;


        }

        private Dictionary<string, List<string>> getAyarlar()
        {
            Dictionary<string, string> ayarlarDict = new Dictionary<string, string>();
            ayarlarDict.Add("komut", "getAyarlar");
            string giden = JsonConvert.SerializeObject(ayarlarDict);
            string gelen = requestGonderAl(giden);
            Dictionary<string, List<string>> rows = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(gelen);
            return rows;

        }
        
        
        private void ayarYap(Dictionary<string, List<string>> ayarlarDict)
        {
            ozelAyarlarDict["sayfaArkaPlan"] = ayarlarDict["ayarlar"][0];
            ozelAyarlarDict["duyuruArkaPlan"] = ayarlarDict["ayarlar"][1];
            ozelAyarlarDict["metinRengi"] = ayarlarDict["ayarlar"][2];
            ozelAyarlarDict["metinFontu"] = ayarlarDict["ayarlar"][3];
            ozelAyarlarDict["donusSure"] = ayarlarDict["ayarlar"][4];

        }

    }
}
