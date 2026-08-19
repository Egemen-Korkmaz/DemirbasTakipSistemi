using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Windows.Forms;

namespace DemirbasTakipProjesi
{
    public class GeriDonusumKutusuForm : Form
    {
        private readonly string baglantiAdresi;
        private readonly string aktifKullanici;
        private readonly bool yetkiliMi;

        private DataGridView dgvSilinenler;
        private Label lblToplamKayit;
        private Button btnGeriAl;
        private Button btnKaliciSil;

        // Form1 ile aynı renk paleti (sabit, tek tema)
        private static readonly Color RenkArkaplan = Color.FromArgb(18, 19, 26);
        private static readonly Color RenkKart = Color.FromArgb(28, 30, 40);
        private static readonly Color RenkKartAcik = Color.FromArgb(38, 41, 54);
        private static readonly Color RenkKenarlik = Color.FromArgb(52, 56, 72);
        private static readonly Color RenkMetinSoluk = Color.FromArgb(148, 163, 184);
        private static readonly Color RenkVurgu = Color.FromArgb(99, 102, 241);
        private static readonly Color RenkTehlike = Color.FromArgb(239, 68, 68);
        private static readonly Color RenkNotr = Color.FromArgb(71, 85, 105);

        public GeriDonusumKutusuForm(string baglantiAdresi, string aktifKullanici, string aktifRol)
        {
            this.baglantiAdresi = baglantiAdresi;
            this.aktifKullanici = aktifKullanici;
            this.yetkiliMi = aktifRol == "Admin";
            TasarimiOlustur();
            SilinenleriGetir();
        }

        private void TasarimiOlustur()
        {
            this.Text = "Geri Dönüşüm Kutusu";
            this.Size = new Size(1040, 620);
            this.MinimumSize = new Size(820, 460);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = RenkArkaplan;
            this.Font = new Font("Segoe UI", 10F);

            // --- ÜST PANEL ---
            Panel pnlUst = new Panel() { Dock = DockStyle.Top, Height = 66, BackColor = Color.FromArgb(22, 24, 32) };

            Label lblBaslik = new Label()
            {
                Text = "🗑️ Geri Dönüşüm Kutusu",
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
                BackColor = RenkNotr,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnYenile.FlatAppearance.BorderSize = 0;
            btnYenile.Left = this.ClientSize.Width - btnYenile.Width - 24;
            btnYenile.Click += (s, e) => SilinenleriGetir();

            pnlUst.Controls.AddRange(new Control[] { lblBaslik, lblToplamKayit, btnYenile });
            pnlUst.Resize += (s, e) => { btnYenile.Left = pnlUst.Width - btnYenile.Width - 24; };

            // --- GRİD ---
            Panel pnlGridKapsayici = new Panel() { Dock = DockStyle.Fill, Padding = new Padding(24, 12, 24, 0), BackColor = RenkArkaplan };

            dgvSilinenler = new DataGridView();
            dgvSilinenler.Dock = DockStyle.Fill;
            dgvSilinenler.ReadOnly = true;
            dgvSilinenler.AllowUserToAddRows = false;
            dgvSilinenler.AllowUserToResizeRows = false;
            dgvSilinenler.RowHeadersVisible = false;
            dgvSilinenler.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvSilinenler.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvSilinenler.MultiSelect = false;
            dgvSilinenler.BackgroundColor = RenkArkaplan;
            dgvSilinenler.BorderStyle = BorderStyle.None;
            dgvSilinenler.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvSilinenler.GridColor = RenkKenarlik;
            dgvSilinenler.RowTemplate.Height = 36;
            dgvSilinenler.Font = new Font("Segoe UI", 9.5F);

            dgvSilinenler.DefaultCellStyle.BackColor = RenkKart;
            dgvSilinenler.DefaultCellStyle.ForeColor = Color.Gainsboro;
            dgvSilinenler.DefaultCellStyle.SelectionBackColor = RenkVurgu;
            dgvSilinenler.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvSilinenler.DefaultCellStyle.Padding = new Padding(8, 0, 4, 0);
            dgvSilinenler.AlternatingRowsDefaultCellStyle.BackColor = RenkKartAcik;

            dgvSilinenler.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(12, 13, 18);
            dgvSilinenler.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvSilinenler.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            dgvSilinenler.ColumnHeadersHeight = 44;
            dgvSilinenler.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgvSilinenler.EnableHeadersVisualStyles = false;

            pnlGridKapsayici.Controls.Add(dgvSilinenler);

            // --- ALT PANEL: GERİ AL / KALICI SİL / KAPAT ---
            Panel pnlAlt = new Panel() { Dock = DockStyle.Bottom, Height = 64, BackColor = Color.FromArgb(22, 24, 32) };

            btnGeriAl = new Button()
            {
                Text = "♻️ Seçileni Geri Al",
                Width = 180,
                Height = 38,
                Top = 13,
                Left = 24,
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };
            btnGeriAl.FlatAppearance.BorderSize = 0;
            btnGeriAl.Click += BtnGeriAl_Click;

            btnKaliciSil = new Button()
            {
                Text = "🗑️ Kalıcı Olarak Sil",
                Width = 180,
                Height = 38,
                Top = 13,
                Left = 216,
                BackColor = RenkTehlike,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };
            btnKaliciSil.FlatAppearance.BorderSize = 0;
            btnKaliciSil.Click += BtnKaliciSil_Click;

            if (!yetkiliMi)
            {
                // Kalıcı silme geri alınamaz bir işlem — sadece Admin yapabilir.
                btnKaliciSil.Enabled = false;
                btnKaliciSil.BackColor = RenkNotr;
                ToolTip ipucu = new ToolTip();
                ipucu.SetToolTip(btnKaliciSil, "Bu işlem için yetkiniz yok (Admin gerekli).");
            }

            Button btnKapat = new Button()
            {
                Text = "Kapat",
                Width = 110,
                Height = 34,
                Top = 15,
                BackColor = RenkNotr,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                DialogResult = DialogResult.OK
            };
            btnKapat.FlatAppearance.BorderSize = 0;
            btnKapat.Left = pnlAlt.Width - btnKapat.Width - 24;
            btnKapat.Click += (s, e) => this.Close();
            pnlAlt.Resize += (s, e) => { btnKapat.Left = pnlAlt.Width - btnKapat.Width - 24; };

            pnlAlt.Controls.AddRange(new Control[] { btnGeriAl, btnKaliciSil, btnKapat });

            this.Controls.Add(pnlGridKapsayici);
            this.Controls.Add(pnlUst);
            this.Controls.Add(pnlAlt);

            this.AcceptButton = btnKapat;
        }

        private void SilinenleriGetir()
        {
            try
            {
                using (SQLiteConnection baglanti = new SQLiteConnection(baglantiAdresi))
                {
                    string sorgu = @"SELECT D.demirbas_ID, K.kategori_adi AS 'Kategori', D.marka_model AS 'Marka / Model',
                                            D.seri_no AS 'Seri No', D.adet AS 'Adet', D.durum AS 'Durum',
                                            D.zimmetli_kisi AS 'Zimmetli Kişi', D.silinme_tarihi AS 'Silinme Tarihi'
                                     FROM Demirbaslar D
                                     INNER JOIN Kategoriler K ON D.kategori_ID = K.kategori_ID
                                     WHERE D.silindi = 1
                                     ORDER BY D.silinme_tarihi DESC";
                    SQLiteDataAdapter da = new SQLiteDataAdapter(sorgu, baglanti);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgvSilinenler.DataSource = dt;

                    if (dgvSilinenler.Columns["demirbas_ID"] != null)
                        dgvSilinenler.Columns["demirbas_ID"].Visible = false;

                    if (dgvSilinenler.Columns["Silinme Tarihi"] != null)
                        dgvSilinenler.Columns["Silinme Tarihi"].DefaultCellStyle.Format = "dd.MM.yyyy HH:mm:ss";

                    lblToplamKayit.Text = dt.Rows.Count == 0 ? "(kutu boş)" : $"({dt.Rows.Count} silinmiş kayıt)";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Silinen kayıtlar getirilirken bir hata oluştu: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnGeriAl_Click(object sender, EventArgs e)
        {
            if (dgvSilinenler.CurrentRow == null)
            {
                MessageBox.Show("Lütfen geri almak istediğiniz kaydı listeden seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int id = Convert.ToInt32(dgvSilinenler.CurrentRow.Cells["demirbas_ID"].Value);
            string kategori = dgvSilinenler.CurrentRow.Cells["Kategori"].Value?.ToString() ?? "-";
            string marka = dgvSilinenler.CurrentRow.Cells["Marka / Model"].Value?.ToString() ?? "";

            try
            {
                using (SQLiteConnection baglanti = new SQLiteConnection(baglantiAdresi))
                {
                    SQLiteCommand komut = new SQLiteCommand(
                        "UPDATE Demirbaslar SET silindi = 0, silinme_tarihi = NULL WHERE demirbas_ID = @id",
                        baglanti);
                    komut.Parameters.AddWithValue("@id", id);
                    baglanti.Open();
                    komut.ExecuteNonQuery();

                    SQLiteCommand logKomutu = new SQLiteCommand(
                        "INSERT INTO IslemGecmisi (kullanici_adi, islem_tipi, demirbas_ID, aciklama) VALUES (@ad, @tip, @did, @aciklama)",
                        baglanti);
                    logKomutu.Parameters.AddWithValue("@ad", aktifKullanici);
                    logKomutu.Parameters.AddWithValue("@tip", "Geri Alma");
                    logKomutu.Parameters.AddWithValue("@did", id);
                    logKomutu.Parameters.AddWithValue("@aciklama", $"{kategori} {marka} geri dönüşüm kutusundan geri alındı.");
                    logKomutu.ExecuteNonQuery();
                }

                MessageBox.Show("Kayıt başarıyla geri alındı.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                SilinenleriGetir();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Kayıt geri alınırken bir hata oluştu: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnKaliciSil_Click(object sender, EventArgs e)
        {
            if (!yetkiliMi) return; // Zaten buton devre dışı ama çift güvenlik

            if (dgvSilinenler.CurrentRow == null)
            {
                MessageBox.Show("Lütfen kalıcı olarak silmek istediğiniz kaydı listeden seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int id = Convert.ToInt32(dgvSilinenler.CurrentRow.Cells["demirbas_ID"].Value);
            string kategori = dgvSilinenler.CurrentRow.Cells["Kategori"].Value?.ToString() ?? "-";
            string marka = dgvSilinenler.CurrentRow.Cells["Marka / Model"].Value?.ToString() ?? "";

            DialogResult cevap = MessageBox.Show(
                $"'{kategori} {marka}' KALICI OLARAK silinsin mi?\n\nBu işlem geri alınamaz!",
                "Kalıcı Silme Onayı", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (cevap != DialogResult.Yes) return;

            try
            {
                using (SQLiteConnection baglanti = new SQLiteConnection(baglantiAdresi))
                {
                    SQLiteCommand komut = new SQLiteCommand("DELETE FROM Demirbaslar WHERE demirbas_ID = @id", baglanti);
                    komut.Parameters.AddWithValue("@id", id);
                    baglanti.Open();
                    komut.ExecuteNonQuery();

                    SQLiteCommand logKomutu = new SQLiteCommand(
                        "INSERT INTO IslemGecmisi (kullanici_adi, islem_tipi, demirbas_ID, aciklama) VALUES (@ad, @tip, @did, @aciklama)",
                        baglanti);
                    logKomutu.Parameters.AddWithValue("@ad", aktifKullanici);
                    logKomutu.Parameters.AddWithValue("@tip", "Silme");
                    logKomutu.Parameters.AddWithValue("@did", id);
                    logKomutu.Parameters.AddWithValue("@aciklama", $"{kategori} {marka} kalıcı olarak silindi.");
                    logKomutu.ExecuteNonQuery();
                }

                MessageBox.Show("Kayıt kalıcı olarak silindi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                SilinenleriGetir();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Kayıt kalıcı olarak silinirken bir hata oluştu: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}