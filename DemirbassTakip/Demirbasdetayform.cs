using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Windows.Forms;

namespace DemirbasTakipProjesi
{
    public class DemirbasDetayForm : Form
    {
        private readonly string baglantiAdresi;
        private readonly int demirbasId;

        private DataGridView dgvGecmis;
        private Label lblBaslikMarka;
        private Label lblAltBaslikKategori;
        private Label lblDurumRozet;
        private Label lblSeriNo, lblAdet, lblZimmet, lblKayitTarihi;
        private Label lblGecmisSayisi;

        // Aynı renk paleti Form1 ile tutarlı olsun diye buraya da taşındı (sabit, tema yok).
        private static readonly Color RenkArkaplan = Color.FromArgb(18, 19, 26);
        private static readonly Color RenkKart = Color.FromArgb(28, 30, 40);
        private static readonly Color RenkKartAcik = Color.FromArgb(38, 41, 54);
        private static readonly Color RenkKenarlik = Color.FromArgb(52, 56, 72);
        private static readonly Color RenkMetinSoluk = Color.FromArgb(148, 163, 184);
        private static readonly Color RenkVurgu = Color.FromArgb(99, 102, 241);
        private static readonly Color RenkBasari = Color.FromArgb(16, 185, 129);
        private static readonly Color RenkTehlike = Color.FromArgb(239, 68, 68);
        private static readonly Color RenkUyari = Color.FromArgb(245, 158, 11);
        private static readonly Color RenkNotr = Color.FromArgb(71, 85, 105);

        public DemirbasDetayForm(string baglantiAdresi, int demirbasId)
        {
            this.baglantiAdresi = baglantiAdresi;
            this.demirbasId = demirbasId;
            TasarimiOlustur();
            VerileriYukle();
        }

        private void TasarimiOlustur()
        {
            this.Text = "Demirbaş Detayı";
            this.Size = new Size(920, 680);
            this.MinimumSize = new Size(760, 520);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = RenkArkaplan;
            this.Font = new Font("Segoe UI", 10F);

            // --- ÜST BİLGİ KARTI ---
            Panel pnlBilgi = new Panel() { Top = 20, Left = 20, Height = 190, BackColor = RenkKart };
            pnlBilgi.Width = this.ClientSize.Width - 40;
            pnlBilgi.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            Panel pnlBilgiVurgu = new Panel() { Dock = DockStyle.Top, Height = 4, BackColor = RenkVurgu };
            pnlBilgi.Controls.Add(pnlBilgiVurgu);

            lblBaslikMarka = new Label() { Text = "Yükleniyor...", ForeColor = Color.White, Font = new Font("Segoe UI", 16F, FontStyle.Bold), Top = 20, Left = 24, AutoSize = true };
            lblAltBaslikKategori = new Label() { Text = "", ForeColor = RenkMetinSoluk, Font = new Font("Segoe UI", 10.5F), Top = 52, Left = 24, AutoSize = true };

            lblDurumRozet = new Label()
            {
                Text = "",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                BackColor = RenkNotr,
                TextAlign = ContentAlignment.MiddleCenter,
                Top = 20,
                AutoSize = false,
                Height = 28,
                Width = 130,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            Label lblSeriNoBaslik = new Label() { Text = "SERİ NO / BARKOD", ForeColor = RenkMetinSoluk, Font = new Font("Segoe UI", 8F, FontStyle.Bold), Top = 100, Left = 24, AutoSize = true };
            lblSeriNo = new Label() { Text = "-", ForeColor = Color.White, Font = new Font("Segoe UI", 11F, FontStyle.Bold), Top = 120, Left = 24, AutoSize = true };

            Label lblAdetBaslik = new Label() { Text = "ADET", ForeColor = RenkMetinSoluk, Font = new Font("Segoe UI", 8F, FontStyle.Bold), Top = 100, Left = 230, AutoSize = true };
            lblAdet = new Label() { Text = "-", ForeColor = Color.White, Font = new Font("Segoe UI", 11F, FontStyle.Bold), Top = 120, Left = 230, AutoSize = true };

            Label lblZimmetBaslik = new Label() { Text = "ZİMMETLİ KİŞİ / BİRİM", ForeColor = RenkMetinSoluk, Font = new Font("Segoe UI", 8F, FontStyle.Bold), Top = 100, Left = 330, AutoSize = true };
            lblZimmet = new Label() { Text = "-", ForeColor = Color.White, Font = new Font("Segoe UI", 11F, FontStyle.Bold), Top = 120, Left = 330, AutoSize = true };

            Label lblKayitBaslik = new Label() { Text = "KAYIT TARİHİ", ForeColor = RenkMetinSoluk, Font = new Font("Segoe UI", 8F, FontStyle.Bold), Top = 100, Left = 550, AutoSize = true };
            lblKayitTarihi = new Label() { Text = "-", ForeColor = Color.White, Font = new Font("Segoe UI", 11F, FontStyle.Bold), Top = 120, Left = 550, AutoSize = true };

            pnlBilgi.Controls.AddRange(new Control[] {
                lblBaslikMarka, lblAltBaslikKategori, lblDurumRozet,
                lblSeriNoBaslik, lblSeriNo, lblAdetBaslik, lblAdet,
                lblZimmetBaslik, lblZimmet, lblKayitBaslik, lblKayitTarihi
            });

            this.Controls.Add(pnlBilgi);

            // --- GEÇMİŞ BAŞLIĞI ---
            Label lblGecmisBaslik = new Label() { Text = "🕒 Bu Cihazın İşlem Geçmişi", ForeColor = Color.White, Font = new Font("Segoe UI", 12F, FontStyle.Bold), Top = 226, Left = 20, AutoSize = true };
            lblGecmisSayisi = new Label() { Text = "", ForeColor = RenkMetinSoluk, Font = new Font("Segoe UI", 9.5F), Top = 231, Left = 260, AutoSize = true };
            this.Controls.Add(lblGecmisBaslik);
            this.Controls.Add(lblGecmisSayisi);

            // --- GEÇMİŞ TABLOSU ---
            dgvGecmis = new DataGridView();
            dgvGecmis.Top = 260; dgvGecmis.Left = 20;
            dgvGecmis.Width = this.ClientSize.Width - 40;
            dgvGecmis.Height = this.ClientSize.Height - 320;
            dgvGecmis.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            dgvGecmis.ReadOnly = true;
            dgvGecmis.AllowUserToAddRows = false;
            dgvGecmis.AllowUserToResizeRows = false;
            dgvGecmis.RowHeadersVisible = false;
            dgvGecmis.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvGecmis.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvGecmis.MultiSelect = false;
            dgvGecmis.BackgroundColor = RenkArkaplan;
            dgvGecmis.BorderStyle = BorderStyle.None;
            dgvGecmis.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvGecmis.GridColor = RenkKenarlik;
            dgvGecmis.RowTemplate.Height = 34;
            dgvGecmis.Font = new Font("Segoe UI", 9.5F);

            dgvGecmis.DefaultCellStyle.BackColor = RenkKart;
            dgvGecmis.DefaultCellStyle.ForeColor = Color.Gainsboro;
            dgvGecmis.DefaultCellStyle.SelectionBackColor = RenkVurgu;
            dgvGecmis.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvGecmis.DefaultCellStyle.Padding = new Padding(8, 0, 4, 0);
            dgvGecmis.AlternatingRowsDefaultCellStyle.BackColor = RenkKartAcik;

            dgvGecmis.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(12, 13, 18);
            dgvGecmis.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvGecmis.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            dgvGecmis.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 0, 0);
            dgvGecmis.ColumnHeadersHeight = 42;
            dgvGecmis.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgvGecmis.EnableHeadersVisualStyles = false;

            dgvGecmis.CellFormatting += DgvGecmis_CellFormatting;

            this.Controls.Add(dgvGecmis);

            // --- ALT PANEL: KAPAT ---
            Panel pnlAlt = new Panel() { Dock = DockStyle.Bottom, Height = 56, BackColor = Color.FromArgb(22, 24, 32) };
            Button btnKapat = new Button()
            {
                Text = "Kapat",
                Width = 110,
                Height = 34,
                Top = 11,
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
            pnlAlt.Controls.Add(btnKapat);
            pnlAlt.Resize += (s, e) => { btnKapat.Left = pnlAlt.Width - btnKapat.Width - 24; };

            this.Controls.Add(pnlAlt);
            this.AcceptButton = btnKapat;
        }

        private void VerileriYukle()
        {
            try
            {
                using (SQLiteConnection baglanti = new SQLiteConnection(baglantiAdresi))
                {
                    baglanti.Open();

                    // --- Cihaz bilgisi ---
                    string sorguCihaz = @"SELECT D.marka_model, D.seri_no, D.adet, D.durum, D.zimmetli_kisi, 
                                                  D.kayit_tarihi, K.kategori_adi
                                           FROM Demirbaslar D
                                           INNER JOIN Kategoriler K ON D.kategori_ID = K.kategori_ID
                                           WHERE D.demirbas_ID = @id";
                    SQLiteCommand komutCihaz = new SQLiteCommand(sorguCihaz, baglanti);
                    komutCihaz.Parameters.AddWithValue("@id", demirbasId);

                    using (SQLiteDataReader okuyucu = komutCihaz.ExecuteReader())
                    {
                        if (okuyucu.Read())
                        {
                            string marka = okuyucu["marka_model"]?.ToString() ?? "-";
                            string kategori = okuyucu["kategori_adi"]?.ToString() ?? "-";
                            string durum = okuyucu["durum"]?.ToString() ?? "-";

                            lblBaslikMarka.Text = string.IsNullOrWhiteSpace(marka) ? "(Marka/Model belirtilmemiş)" : marka;
                            lblAltBaslikKategori.Text = kategori;
                            lblSeriNo.Text = string.IsNullOrWhiteSpace(okuyucu["seri_no"]?.ToString()) ? "-" : okuyucu["seri_no"].ToString();
                            lblAdet.Text = okuyucu["adet"]?.ToString() ?? "-";
                            lblZimmet.Text = string.IsNullOrWhiteSpace(okuyucu["zimmetli_kisi"]?.ToString()) ? "—" : okuyucu["zimmetli_kisi"].ToString();

                            if (DateTime.TryParse(okuyucu["kayit_tarihi"]?.ToString(), out DateTime kayitTarihi))
                                lblKayitTarihi.Text = kayitTarihi.ToString("dd.MM.yyyy HH:mm");
                            else
                                lblKayitTarihi.Text = okuyucu["kayit_tarihi"]?.ToString() ?? "-";

                            DurumRozetiniAyarla(durum);
                        }
                        else
                        {
                            lblBaslikMarka.Text = "Cihaz bulunamadı (silinmiş olabilir).";
                        }
                    }

                    // --- Bu cihaza ait işlem geçmişi ---
                    string sorguGecmis = @"SELECT islem_tarihi AS 'Tarih', kullanici_adi AS 'Kullanıcı',
                                                   islem_tipi AS 'İşlem', aciklama AS 'Açıklama'
                                            FROM IslemGecmisi
                                            WHERE demirbas_ID = @id
                                            ORDER BY islem_tarihi DESC";
                    SQLiteDataAdapter da = new SQLiteDataAdapter(sorguGecmis, baglanti);
                    da.SelectCommand.Parameters.AddWithValue("@id", demirbasId);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgvGecmis.DataSource = dt;

                    if (dgvGecmis.Columns["Tarih"] != null)
                    {
                        dgvGecmis.Columns["Tarih"].DefaultCellStyle.Format = "dd.MM.yyyy HH:mm:ss";
                        dgvGecmis.Columns["Tarih"].FillWeight = 110;
                    }
                    if (dgvGecmis.Columns["Kullanıcı"] != null) dgvGecmis.Columns["Kullanıcı"].FillWeight = 70;
                    if (dgvGecmis.Columns["İşlem"] != null)
                    {
                        dgvGecmis.Columns["İşlem"].FillWeight = 65;
                        dgvGecmis.Columns["İşlem"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    }
                    if (dgvGecmis.Columns["Açıklama"] != null) dgvGecmis.Columns["Açıklama"].FillWeight = 260;

                    lblGecmisSayisi.Text = dt.Rows.Count == 0
                        ? "(kayıt yok)"
                        : $"({dt.Rows.Count} kayıt)";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Demirbaş detayları getirilirken bir hata oluştu: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DurumRozetiniAyarla(string durum)
        {
            lblDurumRozet.Text = string.IsNullOrWhiteSpace(durum) ? "-" : durum.ToUpper();

            switch (durum)
            {
                case "Aktif":
                case "Zimmetli":
                    lblDurumRozet.BackColor = RenkBasari;
                    break;
                case "Depoda":
                    lblDurumRozet.BackColor = RenkVurgu;
                    break;
                case "Tamirde":
                    lblDurumRozet.BackColor = RenkUyari;
                    break;
                case "Arızalı":
                case "Hurda":
                case "Kayıp":
                    lblDurumRozet.BackColor = RenkTehlike;
                    break;
                default:
                    lblDurumRozet.BackColor = RenkNotr;
                    break;
            }

            // Sağ üstte konumlandır (panel genişliğine göre)
            lblDurumRozet.Left = lblDurumRozet.Parent.Width - lblDurumRozet.Width - 24;
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
                }
                e.CellStyle.Font = new Font(dgvGecmis.Font, FontStyle.Bold);
            }
        }
    }
}