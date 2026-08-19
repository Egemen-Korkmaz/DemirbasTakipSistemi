using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace DemirbasTakipProjesi
{
    public class IslemGecmisiForm : Form
    {
        private readonly string baglantiAdresi;
        private readonly string aktifKullanici;
        private readonly bool yetkiliMi;
        private DataGridView dgvGecmis;
        private Label lblToplamKayit;

        public IslemGecmisiForm(string baglantiAdresi, string aktifKullanici, string aktifRol)
        {
            this.baglantiAdresi = baglantiAdresi;
            this.aktifKullanici = aktifKullanici;
            this.yetkiliMi = aktifRol == "Admin";
            TasarimiOlustur();
            GecmisiGetir();
        }

        private void TasarimiOlustur()
        {
            this.Text = "İşlem Geçmişi";
            this.Size = new Size(1080, 620);
            this.MinimumSize = new Size(780, 420);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(30, 32, 40);
            this.Font = new Font("Segoe UI", 10F);

            Panel pnlGridKapsayici = new Panel() { Dock = DockStyle.Fill, Padding = new Padding(24, 12, 24, 0), BackColor = Color.FromArgb(30, 32, 40) };

            dgvGecmis = new DataGridView();
            dgvGecmis.Dock = DockStyle.Fill;
            dgvGecmis.ReadOnly = true;
            dgvGecmis.AllowUserToAddRows = false;
            dgvGecmis.AllowUserToResizeRows = false;
            dgvGecmis.RowHeadersVisible = false;
            dgvGecmis.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvGecmis.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvGecmis.MultiSelect = false;
            dgvGecmis.BackgroundColor = Color.FromArgb(30, 32, 40);
            dgvGecmis.BorderStyle = BorderStyle.None;
            dgvGecmis.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvGecmis.GridColor = Color.FromArgb(55, 58, 72);
            dgvGecmis.RowTemplate.Height = 36;
            dgvGecmis.Font = new Font("Segoe UI", 9.5F);

            dgvGecmis.DefaultCellStyle.BackColor = Color.FromArgb(40, 42, 54);
            dgvGecmis.DefaultCellStyle.ForeColor = Color.Gainsboro;
            dgvGecmis.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 122, 204);
            dgvGecmis.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvGecmis.DefaultCellStyle.Padding = new Padding(8, 0, 4, 0);
            dgvGecmis.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(45, 48, 61);

            dgvGecmis.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(20, 22, 30);
            dgvGecmis.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvGecmis.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            dgvGecmis.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 0, 0);
            dgvGecmis.ColumnHeadersHeight = 44;
            dgvGecmis.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgvGecmis.EnableHeadersVisualStyles = false;

            dgvGecmis.CellFormatting += DgvGecmis_CellFormatting;

            pnlGridKapsayici.Controls.Add(dgvGecmis);

            Panel pnlUst = new Panel() { Dock = DockStyle.Top, Height = 66, BackColor = Color.FromArgb(22, 24, 32) };

            Label lblBaslik = new Label()
            {
                Text = "📋 İşlem Geçmişi",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                Top = 16,
                Left = 24,
                AutoSize = true
            };

            int sayacSol = 24 + TextRenderer.MeasureText(lblBaslik.Text, lblBaslik.Font).Width + 14;

            lblToplamKayit = new Label()
            {
                Text = "",
                ForeColor = Color.Gold,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Top = 26,
                Left = sayacSol,
                AutoSize = true
            };

            Button btnYenile = new Button()
            {
                Text = "🔄 Yenile",
                Width = 110,
                Height = 36,
                Top = 15,
                BackColor = Color.FromArgb(60, 63, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnYenile.FlatAppearance.BorderSize = 0;
            btnYenile.Left = this.ClientSize.Width - btnYenile.Width - 24;
            btnYenile.Click += (s, e) => GecmisiGetir();

            pnlUst.Controls.AddRange(new Control[] { lblBaslik, lblToplamKayit, btnYenile });
            pnlUst.Resize += (s, e) => { btnYenile.Left = pnlUst.Width - btnYenile.Width - 24; };

            Panel pnlAlt = new Panel() { Dock = DockStyle.Bottom, Height = 56, BackColor = Color.FromArgb(22, 24, 32) };

            Button btnKapat = new Button()
            {
                Text = "Kapat",
                Width = 110,
                Height = 34,
                Top = 11,
                BackColor = Color.FromArgb(70, 73, 90),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                DialogResult = DialogResult.OK
            };
            btnKapat.FlatAppearance.BorderSize = 0;
            btnKapat.Left = this.ClientSize.Width - btnKapat.Width - 24;
            btnKapat.Click += (s, e) => this.Close();

            pnlAlt.Controls.Add(btnKapat);
            pnlAlt.Resize += (s, e) => { btnKapat.Left = pnlAlt.Width - btnKapat.Width - 24; };

            // Sadece Admin görebilir/kullanabilir — geri dönüşü olmayan bir işlem olduğu için
            // ayrıca tıklandığında şifre tekrar sorulur (bkz. BtnTemizle_Click).
            if (yetkiliMi)
            {
                Button btnTemizle = new Button()
                {
                    Text = "🧹 Geçmişi Temizle",
                    Width = 170,
                    Height = 34,
                    Top = 11,
                    Left = 24,
                    BackColor = Color.FromArgb(239, 68, 68),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
                };
                btnTemizle.FlatAppearance.BorderSize = 0;
                btnTemizle.Click += BtnTemizle_Click;
                pnlAlt.Controls.Add(btnTemizle);
            }

            this.Controls.Add(pnlGridKapsayici);
            this.Controls.Add(pnlUst);
            this.Controls.Add(pnlAlt);

            this.AcceptButton = btnKapat;
        }

        private void GecmisiGetir()
        {
            try
            {
                using (SQLiteConnection baglanti = new SQLiteConnection(baglantiAdresi))
                {
                    string sorgu = @"SELECT islem_tarihi AS 'Tarih', kullanici_adi AS 'Kullanıcı', 
                                            islem_tipi AS 'İşlem', demirbas_ID AS 'Demirbaş ID', 
                                            aciklama AS 'Açıklama'
                                     FROM IslemGecmisi
                                     ORDER BY islem_tarihi DESC";
                    SQLiteDataAdapter da = new SQLiteDataAdapter(sorgu, baglanti);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgvGecmis.DataSource = dt;

                    SutunlariBicimlendir();

                    lblToplamKayit.Text = $"({dt.Rows.Count} kayıt)";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("İşlem geçmişi getirilirken bir hata oluştu: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SutunlariBicimlendir()
        {
            if (dgvGecmis.Columns["Tarih"] != null)
            {
                dgvGecmis.Columns["Tarih"].DefaultCellStyle.Format = "dd.MM.yyyy HH:mm:ss";
                dgvGecmis.Columns["Tarih"].FillWeight = 110;
            }

            if (dgvGecmis.Columns["Kullanıcı"] != null)
                dgvGecmis.Columns["Kullanıcı"].FillWeight = 75;

            if (dgvGecmis.Columns["İşlem"] != null)
            {
                dgvGecmis.Columns["İşlem"].FillWeight = 70;
                dgvGecmis.Columns["İşlem"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            if (dgvGecmis.Columns["Demirbaş ID"] != null)
            {
                dgvGecmis.Columns["Demirbaş ID"].FillWeight = 60;
                dgvGecmis.Columns["Demirbaş ID"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            if (dgvGecmis.Columns["Açıklama"] != null)
                dgvGecmis.Columns["Açıklama"].FillWeight = 240;
        }

        private void DgvGecmis_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvGecmis.Columns[e.ColumnIndex].Name == "İşlem" && e.Value != null)
            {
                switch (e.Value.ToString())
                {
                    case "Ekleme":
                        e.CellStyle.ForeColor = Color.FromArgb(90, 220, 130);
                        break;
                    case "Güncelleme":
                        e.CellStyle.ForeColor = Color.FromArgb(100, 180, 240);
                        break;
                    case "Silme":
                        e.CellStyle.ForeColor = Color.FromArgb(240, 100, 100);
                        break;
                    case "Geri Alma":
                        e.CellStyle.ForeColor = Color.FromArgb(180, 140, 255);
                        break;
                    case "Geçmiş Temizlendi":
                        e.CellStyle.ForeColor = Color.FromArgb(245, 158, 11);
                        break;
                }
                e.CellStyle.Font = new Font(dgvGecmis.Font, FontStyle.Bold);
            }
        }

        // Geçmişi temizlemek geri alınamaz bir işlem olduğu için, buton zaten Admin'e özel
        // olmasına rağmen ek bir güvenlik katmanı olarak mevcut kullanıcının şifresi tekrar
        // sorulur. Bu, birinin oturumu açık bırakılmış bir bilgisayardan geçmişi kötüye
        // kullanarak silmesini zorlaştırır.
        private void BtnTemizle_Click(object sender, EventArgs e)
        {
            using (Form dlg = new Form())
            {
                dlg.Text = "Kimlik Doğrulama Gerekli";
                dlg.Size = new Size(380, 260);
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.BackColor = Color.FromArgb(28, 30, 40);
                dlg.Font = new Font("Segoe UI", 10F);

                Label lblUyari = new Label()
                {
                    Text = "⚠️ İşlem geçmişini temizlemek geri alınamaz.\nDevam etmek için şifreni tekrar gir:",
                    ForeColor = Color.White,
                    Top = 20,
                    Left = 24,
                    Width = 320,
                    Height = 50,
                    Font = new Font("Segoe UI", 9.5F)
                };

                TextBox txtSifre = new TextBox()
                {
                    Top = 80,
                    Left = 24,
                    Width = 300,
                    Font = new Font("Segoe UI", 11F),
                    PasswordChar = '●',
                    BackColor = Color.FromArgb(38, 41, 54),
                    ForeColor = Color.White,
                    BorderStyle = BorderStyle.FixedSingle
                };

                Label lblHata = new Label()
                {
                    Text = "",
                    ForeColor = Color.FromArgb(239, 68, 68),
                    Top = 112,
                    Left = 24,
                    Width = 300,
                    Height = 34,
                    Font = new Font("Segoe UI", 8.5F)
                };

                Button btnOnayla = new Button()
                {
                    Text = "Onayla ve Temizle",
                    Top = 152,
                    Left = 24,
                    Width = 145,
                    Height = 34,
                    BackColor = Color.FromArgb(239, 68, 68),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };
                btnOnayla.FlatAppearance.BorderSize = 0;

                Button btnIptal = new Button()
                {
                    Text = "İptal",
                    Top = 152,
                    Left = 179,
                    Width = 145,
                    Height = 34,
                    BackColor = Color.FromArgb(71, 85, 105),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    DialogResult = DialogResult.Cancel
                };
                btnIptal.FlatAppearance.BorderSize = 0;

                btnOnayla.Click += (s, e2) =>
                {
                    if (string.IsNullOrWhiteSpace(txtSifre.Text))
                    {
                        lblHata.Text = "Lütfen şifreni gir.";
                        return;
                    }

                    try
                    {
                        using (SQLiteConnection baglanti = new SQLiteConnection(baglantiAdresi))
                        {
                            baglanti.Open();

                            SQLiteCommand kontrolKomutu = new SQLiteCommand("SELECT sifre_hash FROM Kullanicilar WHERE kullanici_adi = @ad", baglanti);
                            kontrolKomutu.Parameters.AddWithValue("@ad", aktifKullanici);
                            object kayitliHash = kontrolKomutu.ExecuteScalar();

                            if (kayitliHash == null || !string.Equals(SifreyiHashle(txtSifre.Text), kayitliHash.ToString(), StringComparison.OrdinalIgnoreCase))
                            {
                                lblHata.Text = "Şifre hatalı.";
                                txtSifre.Clear();
                                txtSifre.Focus();
                                return;
                            }

                            // Önce geçmişi tamamen sil...
                            new SQLiteCommand("DELETE FROM IslemGecmisi", baglanti).ExecuteNonQuery();

                            // ...sonra bu temizleme işleminin KENDİSİNİ tek bir kayıt olarak
                            // yeniden ekle. Böylece "geçmiş sessizce silindi" durumu oluşmaz —
                            // kim, ne zaman temizlediği hâlâ görülebilir.
                            SQLiteCommand logKomutu = new SQLiteCommand(
                                "INSERT INTO IslemGecmisi (kullanici_adi, islem_tipi, demirbas_ID, aciklama) VALUES (@ad, @tip, NULL, @aciklama)",
                                baglanti);
                            logKomutu.Parameters.AddWithValue("@ad", aktifKullanici);
                            logKomutu.Parameters.AddWithValue("@tip", "Geçmiş Temizlendi");
                            logKomutu.Parameters.AddWithValue("@aciklama", $"Tüm işlem geçmişi {aktifKullanici} tarafından temizlendi.");
                            logKomutu.ExecuteNonQuery();
                        }

                        MessageBox.Show("İşlem geçmişi temizlendi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        dlg.DialogResult = DialogResult.OK;
                        dlg.Close();
                        GecmisiGetir();
                    }
                    catch (Exception ex)
                    {
                        lblHata.Text = "Hata: " + ex.Message;
                    }
                };

                dlg.Controls.AddRange(new Control[] { lblUyari, txtSifre, lblHata, btnOnayla, btnIptal });
                dlg.AcceptButton = btnOnayla;
                dlg.CancelButton = btnIptal;

                dlg.ShowDialog(this);
            }
        }

        private string SifreyiHashle(string duzMetinSifre)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] veriBaytlari = Encoding.UTF8.GetBytes(duzMetinSifre);
                byte[] hashBaytlari = sha256.ComputeHash(veriBaytlari);
                StringBuilder sb = new StringBuilder();
                foreach (byte b in hashBaytlari)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }
    }
}