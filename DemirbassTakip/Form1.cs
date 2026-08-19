using DemirbassTakip;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Zen.Barcode;

namespace DemirbasTakipProjesi
{
    public partial class Form1 : Form
    {
        string baglantiAdresi = $"Data Source={System.IO.Path.Combine(Application.StartupPath, "Demirbas.db")};Version=3;";
        private readonly string aktifKullanici;
        private readonly string aktifRol;

        DataGridView dgvListe;
        TextBox txtMarka, txtSeriNo, txtZimmet, txtArama, txtBarkodOku;
        ComboBox cmbKategori, cmbDurum;
        ComboBox cmbFiltreKategori, cmbFiltreDurum;
        NumericUpDown numAdet;
        Button btnEkle, btnGuncelle, btnSil;
        PictureBox picBarkod;

        Label lblToplamCihaz, lblZimmetliCihaz, lblArizaliCihaz;

        // Toplu zimmet raporu yazdırma sırasında kullanılan geçici durum bilgisi
        private List<DataRow> topluRaporSatirlari;
        private string topluRaporKisi;
        private int topluRaporIndeks;

        // --- MODERN RENK PALETİ (sabit — tema geçişi kaldırıldı) ---
        private static readonly Color RenkArkaplan = Color.FromArgb(18, 19, 26);
        private static readonly Color RenkKart = Color.FromArgb(28, 30, 40);
        private static readonly Color RenkKartAcik = Color.FromArgb(38, 41, 54);
        private static readonly Color RenkKenarlik = Color.FromArgb(52, 56, 72);
        private static readonly Color RenkTabloBaslikBg = Color.FromArgb(12, 13, 18);
        private static readonly Color RenkMetinAna = Color.White;
        private static readonly Color RenkMetinSoluk = Color.FromArgb(148, 163, 184);
        private static readonly Color RenkVurgu = Color.FromArgb(99, 102, 241);
        private static readonly Color RenkVurguKoyu = Color.FromArgb(79, 70, 229);
        private static readonly Color RenkBasari = Color.FromArgb(16, 185, 129);
        private static readonly Color RenkBasariKoyu = Color.FromArgb(5, 150, 105);
        private static readonly Color RenkTehlike = Color.FromArgb(239, 68, 68);
        private static readonly Color RenkTehlikeKoyu = Color.FromArgb(220, 38, 38);
        private static readonly Color RenkBilgi = Color.FromArgb(14, 165, 233);
        private static readonly Color RenkBilgiKoyu = Color.FromArgb(2, 132, 199);
        private static readonly Color RenkNotr = Color.FromArgb(71, 85, 105);
        private static readonly Color RenkNotrKoyu = Color.FromArgb(51, 65, 85);
        private static readonly Color RenkUyari = Color.FromArgb(245, 158, 11);
        private static readonly Color RenkUyariKoyu = Color.FromArgb(217, 119, 6);
        private static readonly Color RenkMor = Color.FromArgb(139, 92, 246);
        private static readonly Color RenkMorKoyu = Color.FromArgb(124, 58, 237);
        private static readonly Color RenkAltinMetin = Color.FromArgb(251, 191, 36);
        private static readonly Color RenkYesilMetin = Color.FromArgb(52, 211, 153);
        private static readonly Color RenkKirmiziMetin = Color.FromArgb(248, 113, 113);
        private static readonly Color RenkTuruncuMetin = Color.FromArgb(251, 146, 60);
        private static readonly Color RenkVurguMetin = Color.FromArgb(129, 140, 248);


        public Form1(string aktifKullanici, string aktifRol)
        {
            this.aktifKullanici = aktifKullanici;
            this.aktifRol = aktifRol;

            // Pencere boyutu/konumu/simgesi sadece BİR KEZ (ilk açılışta) ayarlanır.
            // TasarimiOlustur() tema değişince tekrar çağrıldığında bunlar tekrar set edilirse
            // kullanıcı pencereyi büyütmüş/taşımış olsa bile pencere varsayılan boyuta sıfırlanır.
            this.Text = "Profesyonel Bilgi İşlem Envanter ve Zimmet Takip Paneli";
            this.Size = new Size(1300, 900);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.WindowState = FormWindowState.Maximized;
            try { this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            TasarimiOlustur();
            KategorileriDoldur();
            VerileriGetir();
        }

        private void VerileriGetir()
        {
            try
            {
                using (SQLiteConnection baglanti = new SQLiteConnection(baglantiAdresi))
                {
                    string sorgu = @"SELECT D.demirbas_ID, K.kategori_adi AS 'Kategori', D.marka_model AS 'Marka / Model',
                                            D.seri_no AS 'Seri No', D.adet AS 'Adet', D.durum AS 'Durum', 
                                            D.zimmetli_kisi AS 'Zimmetli Kişi', D.kayit_tarihi AS 'Kayıt Tarihi'
                                     FROM Demirbaslar D
                                     INNER JOIN Kategoriler K ON D.kategori_ID = K.kategori_ID
                                     WHERE D.silindi = 0";
                    SQLiteDataAdapter da = new SQLiteDataAdapter(sorgu, baglanti);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgvListe.DataSource = dt;

                    if (dgvListe.Columns["demirbas_ID"] != null)
                        dgvListe.Columns["demirbas_ID"].Visible = false;

                    IstatistikleriGuncelle(dt);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Bağlantı Hatası: " + ex.Message);
            }
        }

        private void IstatistikleriGuncelle(DataTable dt)
        {
            int toplam = 0;
            int zimmetli = 0;
            int arizali = 0;

            foreach (DataRow row in dt.Rows)
            {
                int adet = Convert.ToInt32(row["Adet"]);
                string durum = row["Durum"].ToString();

                toplam += adet;
                if (durum == "Zimmetli") zimmetli += adet;
                if (durum == "Arızalı" || durum == "Tamirde") arizali += adet;
            }

            lblToplamCihaz.Text = $"📊 Toplam Envanter: {toplam} Adet";
            lblZimmetliCihaz.Text = $"👤 Zimmettekiler: {zimmetli} Adet";
            lblArizaliCihaz.Text = $"🛠️ Arızalı/Tamirde: {arizali} Adet";

            int istatistikSol = 340;
            lblToplamCihaz.Left = istatistikSol;
            istatistikSol += TextRenderer.MeasureText(lblToplamCihaz.Text, lblToplamCihaz.Font).Width + 25;

            lblZimmetliCihaz.Left = istatistikSol;
            istatistikSol += TextRenderer.MeasureText(lblZimmetliCihaz.Text, lblZimmetliCihaz.Font).Width + 25;

            lblArizaliCihaz.Left = istatistikSol;
        }

        // Arama kutusu + Kategori dropdown + Durum dropdown'ın hepsini tek bir RowFilter
        // ifadesinde birleştirir (AND ile). Üçü de aynı anda uygulanır: "sadece Yazıcı kategorisinde,
        // sadece Arızalı durumda VE içinde 'HP' geçenler" gibi.
        private void FiltreyiUygula()
        {
            if (!(dgvListe.DataSource is DataTable dt)) return;

            try
            {
                List<string> kosullar = new List<string>();

                string aranan = txtArama.Text.Replace("'", "''").Replace("[", "").Replace("]", "");
                if (!string.IsNullOrWhiteSpace(aranan))
                {
                    kosullar.Add(string.Format(
                        "(Kategori LIKE '%{0}%' OR [Marka / Model] LIKE '%{0}%' OR [Zimmetli Kişi] LIKE '%{0}%')",
                        aranan));
                }

                if (cmbFiltreKategori.SelectedIndex > 0)
                {
                    string kategori = cmbFiltreKategori.Text.Replace("'", "''");
                    kosullar.Add($"Kategori = '{kategori}'");
                }

                if (cmbFiltreDurum.SelectedIndex > 0)
                {
                    string durum = cmbFiltreDurum.Text.Replace("'", "''");
                    kosullar.Add($"Durum = '{durum}'");
                }

                // Not: DataView.RowFilter kendi söz dizimine sahiptir; kullanıcı '[', ']' veya '%'
                // gibi özel karakterler yazarsa geçersiz bir filtre ifadesi oluşup istisna fırlatabilir.
                // Böyle bir durumda çökmek yerine mevcut filtreyi korumak (sessizce yoksaymak) en güvenlisi.
                dt.DefaultView.RowFilter = string.Join(" AND ", kosullar);
            }
            catch (EvaluateException)
            {
                // Geçersiz filtre ifadesi — kullanıcı yazmaya devam ettikçe muhtemelen düzelecek, yoksay.
            }
        }

        private void TxtArama_TextChanged(object sender, EventArgs e)
        {
            FiltreyiUygula();
        }

        private void CmbFiltre_SelectedIndexChanged(object sender, EventArgs e)
        {
            FiltreyiUygula();
        }

        private void TxtBarkodOku_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;

                if (string.IsNullOrWhiteSpace(txtBarkodOku.Text)) return;

                bool cihazBulundu = false;
                string arananBarkod = txtBarkodOku.Text.Trim();

                foreach (DataGridViewRow satir in dgvListe.Rows)
                {
                    if (satir.Cells["Seri No"].Value != null && satir.Cells["Seri No"].Value.ToString().Trim() == arananBarkod)
                    {
                        dgvListe.ClearSelection();
                        satir.Selected = true;
                        dgvListe.CurrentCell = satir.Cells[1];

                        cmbKategori.Text = satir.Cells["Kategori"].Value?.ToString() ?? "";
                        txtMarka.Text = satir.Cells["Marka / Model"].Value?.ToString() ?? "";
                        txtSeriNo.Text = satir.Cells["Seri No"].Value?.ToString() ?? "";
                        numAdet.Value = Convert.ToDecimal(satir.Cells["Adet"].Value ?? 1);
                        cmbDurum.Text = satir.Cells["Durum"].Value?.ToString() ?? "Aktif";
                        txtZimmet.Text = satir.Cells["Zimmetli Kişi"].Value?.ToString() ?? "";

                        BarkodGorseliUret(txtSeriNo.Text);

                        cihazBulundu = true;
                        break;
                    }
                }

                if (!cihazBulundu)
                {
                    MessageBox.Show("Bu barkod veya seri numarasına ait bir demirbaş envanterde bulunamadı!", "Kayıt Bulunamadı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                txtBarkodOku.Clear();
                txtBarkodOku.Focus();
            }
        }

        private void BarkodGorseliUret(string barkodMetni)
        {
            if (!string.IsNullOrWhiteSpace(barkodMetni))
            {
                try
                {
                    Code128BarcodeDraw barkodNesnesi = BarcodeDrawFactory.Code128WithChecksum;
                    picBarkod.Image = barkodNesnesi.Draw(barkodMetni, 40);
                }
                catch
                {
                    picBarkod.Image = null;
                }
            }
            else
            {
                picBarkod.Image = null;
            }
        }

        private void KategorileriDoldur()
        {
            using (SQLiteConnection baglanti = new SQLiteConnection(baglantiAdresi))
            {
                SQLiteDataAdapter da = new SQLiteDataAdapter("SELECT kategori_ID, kategori_adi FROM Kategoriler ORDER BY kategori_adi", baglanti);
                DataTable dt = new DataTable();
                da.Fill(dt);
                cmbKategori.DataSource = dt;
                cmbKategori.DisplayMember = "kategori_adi";
                cmbKategori.ValueMember = "kategori_ID";

                // Filtre dropdown'ı basit metin listesi olarak dolduruluyor (ValueMember gerekmiyor,
                // çünkü burada sadece görüntülenen "Kategori" sütunuyla metin karşılaştırması yapılıyor).
                cmbFiltreKategori.Items.Clear();
                cmbFiltreKategori.Items.Add("Tüm Kategoriler");
                foreach (DataRow satir in dt.Rows)
                {
                    cmbFiltreKategori.Items.Add(satir["kategori_adi"].ToString());
                }
                cmbFiltreKategori.SelectedIndex = 0;
            }
        }

        // Ekle ve Güncelle işlemlerinde tekrar eden parametre atama kodu — DRY için tek yerde toplandı.
        private void DemirbasParametreleriEkle(SQLiteCommand komut)
        {
            komut.Parameters.AddWithValue("@kat", cmbKategori.SelectedValue);
            komut.Parameters.AddWithValue("@marka", string.IsNullOrWhiteSpace(txtMarka.Text) ? (object)DBNull.Value : txtMarka.Text);
            komut.Parameters.AddWithValue("@seri", string.IsNullOrWhiteSpace(txtSeriNo.Text) ? (object)DBNull.Value : txtSeriNo.Text);
            komut.Parameters.AddWithValue("@adet", numAdet.Value);
            komut.Parameters.AddWithValue("@durum", cmbDurum.SelectedItem?.ToString() ?? "Aktif");
            komut.Parameters.AddWithValue("@zimmet", string.IsNullOrWhiteSpace(txtZimmet.Text) ? (object)DBNull.Value : txtZimmet.Text);
        }

        // Ekle/Güncelle'den önce zorunlu alanların dolu olup olmadığını kontrol eder.
        // Kategori seçilmeden kayıt denenirse veritabanı hatası yerine anlaşılır bir uyarı gösterir.
        private bool GirdileriDogrula()
        {
            if (cmbKategori.SelectedValue == null)
            {
                MessageBox.Show("Lütfen bir donanım/kategori türü seçin.", "Eksik Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtMarka.Text) && string.IsNullOrWhiteSpace(txtSeriNo.Text))
            {
                MessageBox.Show("Lütfen en azından marka/model veya seri no bilgisi girin.", "Eksik Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private void BtnEkle_Click(object sender, EventArgs e)
        {
            if (!GirdileriDogrula()) return;

            try
            {
                int yeniId;

                using (SQLiteConnection baglanti = new SQLiteConnection(baglantiAdresi))
                {
                    string sorgu = @"INSERT INTO Demirbaslar (kategori_ID, marka_model, seri_no, adet, durum, zimmetli_kisi) 
                                     VALUES (@kat, @marka, @seri, @adet, @durum, @zimmet);
                                     SELECT last_insert_rowid();";
                    SQLiteCommand komut = new SQLiteCommand(sorgu, baglanti);
                    DemirbasParametreleriEkle(komut);

                    baglanti.Open();
                    yeniId = Convert.ToInt32(komut.ExecuteScalar());
                }

                IslemGecmisineKaydet("Ekleme", yeniId, $"{txtMarka.Text} (Seri No: {txtSeriNo.Text}) eklendi.");

                VerileriGetir();
                Temizle();
                MessageBox.Show("Demirbaş başarıyla kaydedildi!", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Demirbaş kaydedilirken bir hata oluştu: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnYazdir_Click(object sender, EventArgs e)
        {
            if (dgvListe.SelectedRows.Count == 0)
            {
                MessageBox.Show("Lütfen PDF raporu oluşturmak için tablodan bir cihaz seçin!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "PDF Dosyası (*.pdf)|*.pdf";
                sfd.Title = "Zimmet Raporunu PDF Olarak Kaydet";

                var seciliSatir = dgvListe.SelectedRows[0];
                string zimmetAlan = seciliSatir.Cells["Zimmetli Kişi"].Value?.ToString() ?? "Depo";

                string temizAd = string.Join("_", zimmetAlan.Split(Path.GetInvalidFileNameChars()));
                sfd.FileName = $"Zimmet_Tutanagi_{temizAd}.pdf";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        PrintDocument pd = new PrintDocument();

                        pd.PrinterSettings.PrinterName = "Microsoft Print to PDF";
                        pd.PrinterSettings.PrintToFile = true;
                        pd.PrinterSettings.PrintFileName = sfd.FileName;

                        foreach (PaperSize ps in pd.PrinterSettings.PaperSizes)
                        {
                            if (ps.Kind == PaperKind.A4)
                            {
                                pd.DefaultPageSettings.PaperSize = ps;
                                break;
                            }
                        }

                        pd.PrintPage += new PrintPageEventHandler(ZimmetRaporuCiz);
                        pd.Print();

                        MessageBox.Show("Zimmet raporu başarıyla PDF olarak indirildi!", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("PDF kaydedilirken bir hata oluştu: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void ZimmetRaporuCiz(object sender, PrintPageEventArgs e)
        {
            var seciliSatir = dgvListe.SelectedRows[0];
            string kategori = seciliSatir.Cells["Kategori"].Value?.ToString() ?? "-";
            string markaModel = seciliSatir.Cells["Marka / Model"].Value?.ToString() ?? "-";
            string esyaAdi = markaModel != "-" ? $"{kategori} - {markaModel}" : kategori;
            string seriNo = seciliSatir.Cells["Seri No"].Value?.ToString() ?? "-";
            string adet = seciliSatir.Cells["Adet"].Value?.ToString() ?? "1";
            string durum = seciliSatir.Cells["Durum"].Value?.ToString() ?? "-";
            string zimmetAlan = seciliSatir.Cells["Zimmetli Kişi"].Value?.ToString() ?? "Depoda / Boşta";

            Font fontBaslik = new Font("Arial", 16, FontStyle.Bold);
            Font fontAltBaslik = new Font("Arial", 12, FontStyle.Bold);
            Font fontMetin = new Font("Arial", 11, FontStyle.Regular);
            Font fontKalinMetin = new Font("Arial", 11, FontStyle.Bold);
            SolidBrush fircaSiyah = new SolidBrush(Color.Black);
            Pen kalemSiyah = new Pen(Color.Black, 1);
            Pen kalinKalem = new Pen(Color.Black, 2);

            int x = 70;
            int y = 50;

            e.Graphics.DrawString("Bilgi İşlem Müdürlüğü", fontAltBaslik, fircaSiyah, new PointF(330, y)); y += 40;

            e.Graphics.DrawString("ZİMMET TESLİM VE TAAHHÜT TUTANAĞI", fontAltBaslik, fircaSiyah, new PointF(260, y)); y += 20;
            e.Graphics.DrawLine(kalinKalem, x, y, 750, y); y += 30;

            e.Graphics.DrawString($"Tarih: {DateTime.Now:dd.MM.yyyy}", fontKalinMetin, fircaSiyah, new PointF(620, y)); y += 40;

            string aciklamaMetni = "Aşağıda detay bilgileri ve seri numarası belirtilen kurum demirbaşı, çalışır ve eksiksiz " +
                                   "vaziyette ilgili personele görevi süresince kullanılmak üzere teslim edilmiştir.";

            RectangleF aciklamaAlani = new RectangleF(x, y, 680, 50);
            e.Graphics.DrawString(aciklamaMetni, fontMetin, fircaSiyah, aciklamaAlani); y += 60;

            string[,] tabloVerileri = {
                { "Demirbaş / Eşya Adı:", esyaAdi },
                { "Marka / Model Bilgisi:", markaModel },
                { "Cihaz Kategorisi:", kategori },
                { "Seri No / Barkod:", seriNo },
                { "Teslim Edilen Adet:", adet + " Adet" },
                { "Cihazın Mevcut Durumu:", durum }
            };

            for (int i = 0; i < tabloVerileri.GetLength(0); i++)
            {
                y += 5;
                e.Graphics.DrawString(tabloVerileri[i, 0], fontKalinMetin, fircaSiyah, new PointF(x + 10, y));
                e.Graphics.DrawString(tabloVerileri[i, 1], fontMetin, fircaSiyah, new PointF(x + 250, y));
                y += 25;
                e.Graphics.DrawLine(kalemSiyah, x, y, 750, y);
            }

            e.Graphics.DrawLine(kalemSiyah, x, y - 180, x, y);
            e.Graphics.DrawLine(kalemSiyah, 750, y - 180, 750, y);
            y += 40;

            e.Graphics.DrawString("TAAHHÜT ŞARTLARI:", fontKalinMetin, fircaSiyah, new PointF(x, y)); y += 25;
            string maddeler = "1. Personel, teslim aldığı cihazı yalnızca kurum işlerinde kullanmakla yükümlüdür.\n" +
                              "2. Cihazda meydana gelecek arıza durumlarında kullanıcı müdahale etmeden Bilgi İşlem'e bildirecektir.\n" +
                              "3. Personel görevden ayrılma durumunda cihazı eksiksiz olarak Bilgi İşlem Müdürlüğü'ne teslim edecektir.";
            e.Graphics.DrawString(maddeler, fontMetin, fircaSiyah, new RectangleF(x, y, 680, 80)); y += 120;

            e.Graphics.DrawLine(kalemSiyah, x, y, 750, y); y += 20;

            e.Graphics.DrawString("TESLİM EDEN (Bilgi İşlem)", fontAltBaslik, fircaSiyah, new PointF(x + 50, y));
            e.Graphics.DrawString("TESLİM ALAN (Personel)", fontAltBaslik, fircaSiyah, new PointF(x + 420, y));

            y += 25;
            e.Graphics.DrawString("Ad Soyad: ............................", fontMetin, fircaSiyah, new PointF(x + 50, y));
            e.Graphics.DrawString($"Ad Soyad: {zimmetAlan}", fontMetin, fircaSiyah, new PointF(x + 420, y));

            y += 30;
            e.Graphics.DrawString("İmza: ", fontMetin, fircaSiyah, new PointF(x + 50, y));
            e.Graphics.DrawString("İmza: ", fontMetin, fircaSiyah, new PointF(x + 420, y));
        }

        private void BtnGuncelle_Click(object sender, EventArgs e)
        {
            if (dgvListe.CurrentRow == null)
            {
                MessageBox.Show("Lütfen güncellemek istediğiniz cihazı tablodan seçin!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!GirdileriDogrula()) return;

            try
            {
                int id = Convert.ToInt32(dgvListe.CurrentRow.Cells["demirbas_ID"].Value);

                using (SQLiteConnection baglanti = new SQLiteConnection(baglantiAdresi))
                {
                    string sorgu = @"UPDATE Demirbaslar 
                                     SET kategori_ID = @kat, marka_model = @marka, seri_no = @seri, 
                                         adet = @adet, durum = @durum, zimmetli_kisi = @zimmet 
                                     WHERE demirbas_ID = @id";

                    SQLiteCommand komut = new SQLiteCommand(sorgu, baglanti);
                    komut.Parameters.AddWithValue("@id", id);
                    DemirbasParametreleriEkle(komut);

                    baglanti.Open();
                    komut.ExecuteNonQuery();
                }

                IslemGecmisineKaydet("Güncelleme", id, $"{txtMarka.Text} (Seri No: {txtSeriNo.Text}) güncellendi.");

                VerileriGetir();
                Temizle();
                MessageBox.Show("Cihaz bilgileri başarıyla güncellendi!", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Cihaz güncellenirken bir hata oluştu: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DgvListe_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (dgvListe.CurrentRow == null || e.RowIndex < 0) return;

            cmbKategori.Text = dgvListe.CurrentRow.Cells["Kategori"].Value?.ToString() ?? "";
            txtMarka.Text = dgvListe.CurrentRow.Cells["Marka / Model"].Value?.ToString() ?? "";
            txtSeriNo.Text = dgvListe.CurrentRow.Cells["Seri No"].Value?.ToString() ?? "";
            numAdet.Value = Convert.ToDecimal(dgvListe.CurrentRow.Cells["Adet"].Value ?? 1);
            cmbDurum.Text = dgvListe.CurrentRow.Cells["Durum"].Value?.ToString() ?? "Aktif";
            txtZimmet.Text = dgvListe.CurrentRow.Cells["Zimmetli Kişi"].Value?.ToString() ?? "";
            BarkodGorseliUret(txtSeriNo.Text);
        }

        // Tabloya çift tıklanınca o cihazın detay + geçmiş penceresini açar.
        private void DgvListe_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return; // Başlık satırına çift tıklanmışsa yoksay

            int id = Convert.ToInt32(dgvListe.Rows[e.RowIndex].Cells["demirbas_ID"].Value);

            using (DemirbasDetayForm detayForm = new DemirbasDetayForm(baglantiAdresi, id))
            {
                detayForm.ShowDialog();
            }
        }

        private void BtnSil_Click(object sender, EventArgs e)
        {
            if (dgvListe.CurrentRow == null) return;

            int id = Convert.ToInt32(dgvListe.CurrentRow.Cells["demirbas_ID"].Value);
            string kategori = dgvListe.CurrentRow.Cells["Kategori"].Value?.ToString() ?? "-";
            string marka = dgvListe.CurrentRow.Cells["Marka / Model"].Value?.ToString() ?? "";

            DialogResult cevap = MessageBox.Show(
                $"'{kategori} {marka}' envanterden silinsin mi?\n\n" +
                "Not: Bu kayıt kalıcı olarak silinmez, Geri Dönüşüm Kutusu'na taşınır ve istenirse geri alınabilir.",
                "Silme Onayı", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (cevap != DialogResult.Yes) return;

            try
            {
                using (SQLiteConnection baglanti = new SQLiteConnection(baglantiAdresi))
                {
                    // Kaydı gerçekten silmek yerine "silindi" olarak işaretliyoruz (soft delete).
                    // Böylece yanlışlıkla silinen bir cihaz Geri Dönüşüm Kutusu'ndan geri alınabilir.
                    SQLiteCommand komut = new SQLiteCommand(
                        "UPDATE Demirbaslar SET silindi = 1, silinme_tarihi = datetime('now','localtime') WHERE demirbas_ID = @id",
                        baglanti);
                    komut.Parameters.AddWithValue("@id", id);
                    baglanti.Open();
                    komut.ExecuteNonQuery();
                }

                IslemGecmisineKaydet("Silme", id, $"{kategori} {marka} envanterden silindi (Geri Dönüşüm Kutusu'na taşındı).");

                VerileriGetir();
                Temizle();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Cihaz silinirken bir hata oluştu: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void IslemGecmisineKaydet(string islemTipi, int demirbasId, string aciklama)
        {
            try
            {
                using (SQLiteConnection baglanti = new SQLiteConnection(baglantiAdresi))
                {
                    string sorgu = @"INSERT INTO IslemGecmisi (kullanici_adi, islem_tipi, demirbas_ID, aciklama) 
                                     VALUES (@kullanici, @tip, @id, @aciklama)";
                    SQLiteCommand komut = new SQLiteCommand(sorgu, baglanti);
                    komut.Parameters.AddWithValue("@kullanici", aktifKullanici);
                    komut.Parameters.AddWithValue("@tip", islemTipi);
                    komut.Parameters.AddWithValue("@id", demirbasId);
                    komut.Parameters.AddWithValue("@aciklama", aciklama);
                    baglanti.Open();
                    komut.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("İşlem geçmişi kaydedilirken bir hata oluştu: " + ex.Message, "Log Hatası", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        private void BtnGecmis_Click(object sender, EventArgs e)
        {
            using (IslemGecmisiForm gecmisForm = new IslemGecmisiForm(baglantiAdresi, aktifKullanici, aktifRol))
            {
                gecmisForm.ShowDialog();
            }
        }

        private void Temizle()
        {
            txtMarka.Clear(); txtSeriNo.Clear(); txtZimmet.Clear(); numAdet.Value = 1;
            cmbDurum.SelectedIndex = 0;
            picBarkod.Image = null;
        }

        // Admin olmayan kullanıcılar için bir butonu devre dışı bırakıp açıklayıcı bir tooltip ekler.
        // Önceden btnSil, btnGecmis ve btnKullaniciYonetimi için aynı 3 satır ayrı ayrı tekrarlanıyordu.
        private void AdminIcinKilitle(Button btn, ToolTip ipucu)
        {
            btn.Enabled = false;
            btn.BackColor = RenkNotr;
            ipucu.SetToolTip(btn, "Bu işlem için yetkiniz yok (Admin gerekli).");
        }

        // Bir kontrolün köşelerini yuvarlatır. Sadece sabit boyutlu (Dock/Anchor ile büyüyüp
        // küçülmeyen) kontrollerde kullanılmalı — aksi halde form yeniden boyutlandığında
        // bölge (Region) eski boyuta göre kalır ve görüntü bozulur.
        private void KenariYuvarlaklastir(Control kontrol, int yaricap)
        {
            if (kontrol.Width <= 0 || kontrol.Height <= 0) return;

            int cap = yaricap * 2;
            GraphicsPath yol = new GraphicsPath();
            yol.AddArc(0, 0, cap, cap, 180, 90);
            yol.AddArc(kontrol.Width - cap, 0, cap, cap, 270, 90);
            yol.AddArc(kontrol.Width - cap, kontrol.Height - cap, cap, cap, 0, 90);
            yol.AddArc(0, kontrol.Height - cap, cap, cap, 90, 90);
            yol.CloseFigure();
            kontrol.Region = new Region(yol);
        }

        // Butona fare üzerine gelince/ayrılınca renk geçişi (hover) efekti ekler.
        private void HoverEfektiEkle(Button buton, Color normalRenk, Color hoverRenk)
        {
            buton.MouseEnter += (s, e) => buton.BackColor = hoverRenk;
            buton.MouseLeave += (s, e) => buton.BackColor = normalRenk;
        }

        private void TasarimiOlustur()
        {
            // Not: Text/Size/StartPosition/Icon burada DEĞİL — constructor'da bir kez ayarlanıyor.
            this.BackColor = RenkArkaplan;
            this.Font = new Font("Segoe UI", 10F);

            // İnce üst vurgu şeridi — modern SaaS panellerinde sık görülen bir detay
            Panel pnlUstVurgu = new Panel() { Dock = DockStyle.Top, Height = 3, BackColor = RenkVurgu };
            this.Controls.Add(pnlUstVurgu);

            Panel pnlSol = new Panel() { Top = 20, Left = 20, Width = 300, Height = 850, BackColor = RenkKart };
            pnlSol.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            this.Controls.Add(pnlSol);

            // Kartın üstünde ince bir vurgu şeridi (Dock kullanıldığı için pnlSol yeniden
            // boyutlansa bile bozulmaz, absolute-pozisyonlu diğer kontrolleri etkilemez)
            Panel pnlSolVurgu = new Panel() { Dock = DockStyle.Top, Height = 4, BackColor = RenkVurgu };
            pnlSol.Controls.Add(pnlSolVurgu);

            Label lblBaslik = new Label() { Text = "SİSTEME EŞYA EKLE", ForeColor = RenkMetinAna, Font = new Font("Segoe UI", 14F, FontStyle.Bold), Top = 18, Left = 20, AutoSize = true };

            Label l1 = new Label() { Text = "Donanım / Kategori Türü:", ForeColor = RenkMetinSoluk, Top = 55, Left = 20, AutoSize = true };
            cmbKategori = new ComboBox() { Top = 80, Left = 20, Width = 250, Height = 30, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 11F), FlatStyle = FlatStyle.Flat, BackColor = RenkKartAcik, ForeColor = RenkMetinAna };

            Label l2 = new Label() { Text = "Marka / Model Bilgisi:", ForeColor = RenkMetinSoluk, Top = 125, Left = 20, AutoSize = true };
            txtMarka = new TextBox() { Top = 150, Left = 20, Width = 250, Height = 28, Font = new Font("Segoe UI", 11F), BackColor = RenkKartAcik, ForeColor = RenkMetinAna, BorderStyle = BorderStyle.FixedSingle };

            Label l3 = new Label() { Text = "Seri No / Barkod:", ForeColor = RenkMetinSoluk, Top = 195, Left = 20, AutoSize = true };
            txtSeriNo = new TextBox() { Top = 220, Left = 20, Width = 250, Height = 28, Font = new Font("Segoe UI", 11F), BackColor = RenkKartAcik, ForeColor = RenkMetinAna, BorderStyle = BorderStyle.FixedSingle };

            Label l4 = new Label() { Text = "Adet:", ForeColor = RenkMetinSoluk, Top = 265, Left = 20, AutoSize = true };
            numAdet = new NumericUpDown() { Top = 290, Left = 20, Width = 250, Height = 28, Minimum = 1, Maximum = 1000, Font = new Font("Segoe UI", 11F), BackColor = RenkKartAcik, ForeColor = RenkMetinAna, BorderStyle = BorderStyle.FixedSingle };

            Label l5 = new Label() { Text = "Cihazın Durumu:", ForeColor = RenkMetinSoluk, Top = 335, Left = 20, AutoSize = true };
            cmbDurum = new ComboBox() { Top = 360, Left = 20, Width = 250, Height = 30, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 11F), FlatStyle = FlatStyle.Flat, BackColor = RenkKartAcik, ForeColor = RenkMetinAna };
            cmbDurum.Items.AddRange(new string[] { "Aktif", "Zimmetli", "Depoda", "Tamirde", "Arızalı", "Hurda", "Kayıp" });
            cmbDurum.SelectedIndex = 0;

            Label l6 = new Label() { Text = "Zimmetlenen Kişi/Birim:", ForeColor = RenkMetinSoluk, Top = 405, Left = 20, AutoSize = true };
            txtZimmet = new TextBox() { Top = 430, Left = 20, Width = 250, Height = 28, Font = new Font("Segoe UI", 11F), BackColor = RenkKartAcik, ForeColor = RenkMetinAna, BorderStyle = BorderStyle.FixedSingle };

            // Canlı Barkod Görsel Alanı Tasarımı
            Label lblBarkodGorsel = new Label() { Text = "Cihaz Barkod Çizgisi:", ForeColor = RenkAltinMetin, Top = 475, Left = 20, AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            picBarkod = new PictureBox() { Top = 495, Left = 20, Width = 250, Height = 55, BackColor = Color.White, SizeMode = PictureBoxSizeMode.CenterImage };
            KenariYuvarlaklastir(picBarkod, 8);

            btnEkle = new Button() { Text = "Eşyayı Sisteme Kaydet", Top = 565, Left = 20, Width = 250, Height = 38, BackColor = RenkBasari, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
            btnGuncelle = new Button() { Text = "Seçili Cihazı Güncelle", Top = 611, Left = 20, Width = 250, Height = 38, BackColor = RenkBilgi, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
            btnSil = new Button() { Text = "Seçili Cihazı Sil", Top = 657, Left = 20, Width = 250, Height = 35, BackColor = RenkTehlike, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

            btnEkle.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            btnGuncelle.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            btnSil.Anchor = AnchorStyles.Top | AnchorStyles.Left;

            Button btnGecmis = new Button() { Text = "🕒 İşlem Geçmişini Görüntüle", Top = 700, Left = 20, Width = 250, Height = 38, BackColor = RenkNotr, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            btnGecmis.Anchor = AnchorStyles.Top | AnchorStyles.Left;

            Button btnDashboard = new Button() { Text = "📊 Dashboard'u Görüntüle", Top = 746, Left = 20, Width = 250, Height = 38, BackColor = RenkVurgu, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            btnDashboard.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            btnDashboard.FlatAppearance.BorderSize = 0;
            btnDashboard.Click += (s, e) =>
            {
                using (DashboardForm dashboard = new DashboardForm(baglantiAdresi))
                {
                    dashboard.ShowDialog();
                }
            };

            btnEkle.FlatAppearance.BorderSize = 0;
            btnGuncelle.FlatAppearance.BorderSize = 0;
            btnSil.FlatAppearance.BorderSize = 0;
            btnGecmis.FlatAppearance.BorderSize = 0;
            // (btnDashboard ve btnKullaniciYonetimi'nin BorderSize'ı kendi tanımlarında ayrıca ayarlanıyor)

            btnEkle.Click += BtnEkle_Click;
            btnGuncelle.Click += BtnGuncelle_Click;
            btnSil.Click += BtnSil_Click;
            btnGecmis.Click += BtnGecmis_Click;
            bool yetkiliMi = aktifRol == "Admin";
            ToolTip yetkiIpucu = new ToolTip();

            Button btnKullaniciYonetimi = new Button() { Text = "👥 Kullanıcı Yönetimi", Top = 792, Left = 20, Width = 250, Height = 38, BackColor = RenkMor, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            btnKullaniciYonetimi.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            btnKullaniciYonetimi.FlatAppearance.BorderSize = 0;
            btnKullaniciYonetimi.Click += (s, e) =>
            {
                using (KullaniciYonetimiForm kullaniciForm = new KullaniciYonetimiForm(baglantiAdresi, aktifKullanici))
                {
                    kullaniciForm.ShowDialog();
                }
            };

            if (!yetkiliMi)
            {
                AdminIcinKilitle(btnKullaniciYonetimi, yetkiIpucu);
            }

            // Sol paneldeki tüm butonlara yuvarlak köşe + hover efekti uygula
            KenariYuvarlaklastir(btnEkle, 8);
            KenariYuvarlaklastir(btnGuncelle, 8);
            KenariYuvarlaklastir(btnSil, 8);
            KenariYuvarlaklastir(btnGecmis, 8);
            KenariYuvarlaklastir(btnDashboard, 8);
            KenariYuvarlaklastir(btnKullaniciYonetimi, 8);

            if (yetkiliMi)
            {
                HoverEfektiEkle(btnKullaniciYonetimi, RenkMor, RenkMorKoyu);
            }
            HoverEfektiEkle(btnEkle, RenkBasari, RenkBasariKoyu);
            HoverEfektiEkle(btnGuncelle, RenkBilgi, RenkBilgiKoyu);
            HoverEfektiEkle(btnDashboard, RenkVurgu, RenkVurguKoyu);
            HoverEfektiEkle(btnGecmis, RenkNotr, RenkNotrKoyu);
            HoverEfektiEkle(btnSil, RenkTehlike, RenkTehlikeKoyu);

            pnlSol.Controls.AddRange(new Control[] { lblBaslik, l1, cmbKategori, l2, txtMarka, l3, txtSeriNo, l4, numAdet, l5, cmbDurum, l6, txtZimmet, lblBarkodGorsel, picBarkod, btnEkle, btnGuncelle, btnSil, btnGecmis, btnDashboard, btnKullaniciYonetimi });

            lblToplamCihaz = new Label() { Text = "📊 Toplam Envanter: 0 Adet", ForeColor = RenkAltinMetin, Font = new Font("Segoe UI", 11F, FontStyle.Bold), Top = 25, Left = 340, AutoSize = true };
            lblZimmetliCihaz = new Label() { Text = "👤 Zimmettekiler: 0 Adet", ForeColor = RenkYesilMetin, Font = new Font("Segoe UI", 11F, FontStyle.Bold), Top = 25, Left = 590, AutoSize = true };
            lblArizaliCihaz = new Label() { Text = "🛠️ Arızalı/Tamirde: 0 Adet", ForeColor = RenkKirmiziMetin, Font = new Font("Segoe UI", 11F, FontStyle.Bold), Top = 25, Left = 820, AutoSize = true };

            this.Controls.AddRange(new Control[] { lblToplamCihaz, lblZimmetliCihaz, lblArizaliCihaz });

            int satirTop = 76;
            int akanSol = 340;
            Font satirFontuKalin = new Font("Segoe UI", 10F, FontStyle.Bold);
            Font satirFontuNormal = new Font("Segoe UI", 10F);

            // --- FİLTRE SATIRI: Serbest metin + Kategori + Durum (üçü birlikte uygulanır) ---
            // Not: Toplam genişlik, sağ üstteki saat/kullanıcı etiketleriyle (Left=1080) çakışmasın
            // diye elemanlar dar tutuldu (yaklaşık 340-950 arası).
            Label lblArama = new Label() { Text = "🔍 Filtrele:", ForeColor = RenkMetinAna, Font = satirFontuKalin, Top = satirTop + 3, Left = akanSol, AutoSize = true };
            akanSol += TextRenderer.MeasureText(lblArama.Text, lblArama.Font).Width + 8;

            txtArama = new TextBox() { Top = satirTop, Left = akanSol, Width = 105, Height = 26, Font = satirFontuNormal, BackColor = RenkKartAcik, ForeColor = RenkMetinAna, BorderStyle = BorderStyle.FixedSingle };
            txtArama.TextChanged += TxtArama_TextChanged;
            akanSol += txtArama.Width + 14;

            Label lblFiltreKategori = new Label() { Text = "Kategori:", ForeColor = RenkMetinSoluk, Font = satirFontuNormal, Top = satirTop + 3, Left = akanSol, AutoSize = true };
            akanSol += TextRenderer.MeasureText(lblFiltreKategori.Text, lblFiltreKategori.Font).Width + 6;

            cmbFiltreKategori = new ComboBox() { Top = satirTop - 2, Left = akanSol, Width = 130, Height = 26, DropDownStyle = ComboBoxStyle.DropDownList, Font = satirFontuNormal, FlatStyle = FlatStyle.Flat, BackColor = RenkKartAcik, ForeColor = RenkMetinAna };
            cmbFiltreKategori.SelectedIndexChanged += CmbFiltre_SelectedIndexChanged;
            akanSol += cmbFiltreKategori.Width + 14;

            Label lblFiltreDurum = new Label() { Text = "Durum:", ForeColor = RenkMetinSoluk, Font = satirFontuNormal, Top = satirTop + 3, Left = akanSol, AutoSize = true };
            akanSol += TextRenderer.MeasureText(lblFiltreDurum.Text, lblFiltreDurum.Font).Width + 6;

            cmbFiltreDurum = new ComboBox() { Top = satirTop - 2, Left = akanSol, Width = 110, Height = 26, DropDownStyle = ComboBoxStyle.DropDownList, Font = satirFontuNormal, FlatStyle = FlatStyle.Flat, BackColor = RenkKartAcik, ForeColor = RenkMetinAna };
            cmbFiltreDurum.Items.Add("Tüm Durumlar");
            cmbFiltreDurum.Items.AddRange(new string[] { "Aktif", "Zimmetli", "Depoda", "Tamirde", "Arızalı", "Hurda", "Kayıp" });
            cmbFiltreDurum.SelectedIndex = 0;
            cmbFiltreDurum.SelectedIndexChanged += CmbFiltre_SelectedIndexChanged;

            this.Controls.AddRange(new Control[] { lblArama, txtArama, lblFiltreKategori, cmbFiltreKategori, lblFiltreDurum, cmbFiltreDurum });

            // --- İKİNCİ SATIR: Hızlı barkod okut + Raporlar menüsü ---
            int satirTop2 = 114;
            int akanSol2 = 340;

            Label lblBarkodBaslik = new Label() { Text = "🛑 Hızlı Barkod Okut:", ForeColor = RenkTuruncuMetin, Font = satirFontuKalin, Top = satirTop2 + 3, Left = akanSol2, AutoSize = true };
            akanSol2 += TextRenderer.MeasureText(lblBarkodBaslik.Text, lblBarkodBaslik.Font).Width + 10;

            txtBarkodOku = new TextBox() { Top = satirTop2, Left = akanSol2, Width = 140, Height = 26, Font = satirFontuNormal, BackColor = RenkKartAcik, ForeColor = RenkMetinAna, BorderStyle = BorderStyle.FixedSingle };
            txtBarkodOku.KeyDown += TxtBarkodOku_KeyDown; // Event bağlandı
            akanSol2 += txtBarkodOku.Width + 30;

            this.Controls.AddRange(new Control[] { lblBarkodBaslik, txtBarkodOku });

            // --- RAPORLAR ▾ (tek düğme, açılır menü) ---
            // Önceden burada 4 ayrı buton 2 satır kaplıyordu; artık hepsi tek bir menüde.
            ContextMenuStrip menuRaporlar = new ContextMenuStrip();
            menuRaporlar.BackColor = RenkKart;
            menuRaporlar.Font = new Font("Segoe UI", 9.5F);
            menuRaporlar.ShowImageMargin = false;

            ToolStripMenuItem miZimmetRaporu = new ToolStripMenuItem("📄  Zimmet Raporu (PDF)") { ForeColor = RenkMetinAna, BackColor = RenkKart };
            miZimmetRaporu.Click += BtnYazdir_Click;

            ToolStripMenuItem miExcel = new ToolStripMenuItem("📊  Excel'e Aktar (CSV)") { ForeColor = RenkMetinAna, BackColor = RenkKart };
            miExcel.Click += BtnExcelAktar_Click;

            ToolStripMenuItem miTopluZimmet = new ToolStripMenuItem("📑  Toplu Zimmet Raporu") { ForeColor = RenkMetinAna, BackColor = RenkKart };
            miTopluZimmet.Click += BtnTopluZimmet_Click;

            ToolStripMenuItem miYedekle = new ToolStripMenuItem("💾  Veritabanını Yedekle") { ForeColor = RenkMetinAna, BackColor = RenkKart };
            miYedekle.Click += BtnYedekle_Click;

            ToolStripMenuItem miGeriDonusum = new ToolStripMenuItem("🗑️  Geri Dönüşüm Kutusu") { ForeColor = RenkMetinAna, BackColor = RenkKart };
            miGeriDonusum.Click += (s, e) =>
            {
                using (GeriDonusumKutusuForm geriDonusumForm = new GeriDonusumKutusuForm(baglantiAdresi, aktifKullanici, aktifRol))
                {
                    geriDonusumForm.ShowDialog();
                }
                VerileriGetir(); // geri alınan bir kayıt varsa ana liste tazelensin
            };

            menuRaporlar.Items.AddRange(new ToolStripItem[] { miZimmetRaporu, miExcel, miTopluZimmet, miYedekle, new ToolStripSeparator(), miGeriDonusum });

            Button btnRaporlar = new Button()
            {
                Text = "📋 Raporlar  ▾",
                Top = satirTop2,
                Left = akanSol2,
                Width = 160,
                Height = 30,
                BackColor = RenkVurgu,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };
            btnRaporlar.FlatAppearance.BorderSize = 0;
            btnRaporlar.Click += (s, e) => menuRaporlar.Show(btnRaporlar, new Point(0, btnRaporlar.Height));
            KenariYuvarlaklastir(btnRaporlar, 6);
            HoverEfektiEkle(btnRaporlar, RenkVurgu, RenkVurguKoyu);

            this.Controls.Add(btnRaporlar);

            dgvListe = new DataGridView();
            dgvListe.Top = 152; dgvListe.Left = 340;
            dgvListe.Width = 910; dgvListe.Height = 688;
            dgvListe.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            dgvListe.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvListe.AllowUserToAddRows = false;
            dgvListe.AllowUserToResizeRows = false;
            dgvListe.ReadOnly = true;
            dgvListe.RowHeadersVisible = false;
            dgvListe.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvListe.MultiSelect = false;
            dgvListe.BackgroundColor = RenkArkaplan;
            dgvListe.BorderStyle = BorderStyle.None;
            dgvListe.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvListe.GridColor = RenkKenarlik;
            dgvListe.RowTemplate.Height = 34;
            dgvListe.Font = new Font("Segoe UI", 9.5F);

            dgvListe.DefaultCellStyle.BackColor = RenkKart;
            dgvListe.DefaultCellStyle.ForeColor = RenkMetinAna;
            dgvListe.DefaultCellStyle.SelectionBackColor = RenkVurgu;
            dgvListe.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvListe.DefaultCellStyle.Padding = new Padding(6, 0, 0, 0);
            dgvListe.AlternatingRowsDefaultCellStyle.BackColor = RenkKartAcik;

            dgvListe.ColumnHeadersDefaultCellStyle.BackColor = RenkTabloBaslikBg;
            dgvListe.ColumnHeadersDefaultCellStyle.ForeColor = RenkMetinAna;
            dgvListe.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            dgvListe.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 0, 0, 0);
            dgvListe.ColumnHeadersHeight = 44;
            dgvListe.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgvListe.EnableHeadersVisualStyles = false;

            dgvListe.CellClick += DgvListe_CellClick;
            dgvListe.CellDoubleClick += DgvListe_CellDoubleClick;

            this.Controls.Add(dgvListe);

            // Saat metnini şimdiden yazıyoruz ki genişliğini ölçüp kullanıcı adını
            // hemen soluna, aynı satıra doğru şekilde yerleştirebilelim.
            Label lblCanliSaat = new Label()
            {
                Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"),
                Top = 25,
                Left = 1080,
                ForeColor = RenkVurguMetin,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            this.Controls.Add(lblCanliSaat);

            string kullaniciMetni = $"👤 {aktifKullanici}";
            Font kullaniciFontu = new Font("Segoe UI", 10F, FontStyle.Bold);
            int kullaniciGenisligi = TextRenderer.MeasureText(kullaniciMetni, kullaniciFontu).Width;

            Label lblAktifKullanici = new Label()
            {
                Text = kullaniciMetni,
                ForeColor = RenkYesilMetin,
                Font = kullaniciFontu,
                Top = 27,
                Left = lblCanliSaat.Left - kullaniciGenisligi - 18,
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            this.Controls.Add(lblAktifKullanici);

            Timer zamanlayici = new Timer() { Interval = 1000 };
            zamanlayici.Tick += (s, e) => { lblCanliSaat.Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"); };
            zamanlayici.Start();
        }

        private void BtnExcelAktar_Click(object sender, EventArgs e)
        {
            if (!(dgvListe.DataSource is DataTable dt) || dt.Rows.Count == 0)
            {
                MessageBox.Show("Dışa aktarılacak veri bulunamadı.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "CSV Dosyası - Excel uyumlu (*.csv)|*.csv";
                sfd.FileName = $"Demirbas_Envanter_{DateTime.Now:yyyyMMdd_HHmm}.csv";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        List<DataGridViewColumn> gorunurSutunlar = new List<DataGridViewColumn>();
                        foreach (DataGridViewColumn sutun in dgvListe.Columns)
                            if (sutun.Visible) gorunurSutunlar.Add(sutun);

                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine(string.Join(";", gorunurSutunlar.Select(s => CsvIcinKacisla(s.HeaderText))));

                        // Aktif filtreyi (txtArama) dikkate alarak görünen satırları yaz
                        foreach (DataRowView satir in dt.DefaultView)
                        {
                            var degerler = gorunurSutunlar.Select(s => CsvIcinKacisla(satir[s.DataPropertyName]?.ToString() ?? ""));
                            sb.AppendLine(string.Join(";", degerler));
                        }

                        File.WriteAllText(sfd.FileName, sb.ToString(), new UTF8Encoding(true));
                        MessageBox.Show("Envanter listesi Excel uyumlu CSV dosyası olarak kaydedildi!", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Dışa aktarma sırasında bir hata oluştu: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private string CsvIcinKacisla(string deger)
        {
            if (deger.Contains(";") || deger.Contains("\"") || deger.Contains("\n"))
                return "\"" + deger.Replace("\"", "\"\"") + "\"";
            return deger;
        }

        private void BtnTopluZimmet_Click(object sender, EventArgs e)
        {
            if (!(dgvListe.DataSource is DataTable dt) || dt.Rows.Count == 0)
            {
                MessageBox.Show("Envanterde herhangi bir kayıt bulunamadı.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            List<string> kisiler = dt.AsEnumerable()
                .Select(r => r["Zimmetli Kişi"]?.ToString())
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .Distinct()
                .OrderBy(k => k)
                .ToList();

            if (kisiler.Count == 0)
            {
                MessageBox.Show("Sistemde zimmetli cihazı bulunan bir kişi/birim yok.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string seciliKisi = KisiSecimDialogGoster(kisiler);
            if (seciliKisi == null) return;

            List<DataRow> cihazlar = dt.AsEnumerable()
                .Where(r => (r["Zimmetli Kişi"]?.ToString() ?? "") == seciliKisi)
                .ToList();

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "PDF Dosyası (*.pdf)|*.pdf";
                string temizAd = string.Join("_", seciliKisi.Split(Path.GetInvalidFileNameChars()));
                sfd.FileName = $"Toplu_Zimmet_Tutanagi_{temizAd}.pdf";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        topluRaporKisi = seciliKisi;
                        topluRaporSatirlari = cihazlar;
                        topluRaporIndeks = 0;

                        PrintDocument pd = new PrintDocument();
                        pd.PrinterSettings.PrinterName = "Microsoft Print to PDF";
                        pd.PrinterSettings.PrintToFile = true;
                        pd.PrinterSettings.PrintFileName = sfd.FileName;

                        foreach (PaperSize ps in pd.PrinterSettings.PaperSizes)
                        {
                            if (ps.Kind == PaperKind.A4)
                            {
                                pd.DefaultPageSettings.PaperSize = ps;
                                break;
                            }
                        }

                        pd.PrintPage += TopluZimmetRaporuCiz;
                        pd.Print();

                        MessageBox.Show("Toplu zimmet raporu başarıyla PDF olarak indirildi!", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("PDF oluşturulurken bir hata oluştu: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private string KisiSecimDialogGoster(List<string> kisiler)
        {
            using (Form dlg = new Form())
            {
                dlg.Text = "Kişi / Birim Seçin";
                dlg.Size = new Size(360, 190);
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.BackColor = RenkKart;
                dlg.Font = new Font("Segoe UI", 10F);

                Label lbl = new Label() { Text = "Toplu zimmet raporu için kişi/birim seçin:", ForeColor = RenkMetinSoluk, Top = 20, Left = 24, Width = 300, AutoSize = true };
                ComboBox cmb = new ComboBox() { Top = 55, Left = 24, Width = 300, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10.5F), FlatStyle = FlatStyle.Flat, BackColor = RenkKartAcik, ForeColor = RenkMetinAna };
                cmb.Items.AddRange(kisiler.ToArray());
                cmb.SelectedIndex = 0;

                Button btnTamam = new Button() { Text = "Rapor Oluştur", Top = 105, Left = 24, Width = 145, Height = 34, BackColor = RenkVurgu, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.OK };
                btnTamam.FlatAppearance.BorderSize = 0;
                Button btnIptal = new Button() { Text = "İptal", Top = 105, Left = 179, Width = 145, Height = 34, BackColor = RenkNotr, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel };
                btnIptal.FlatAppearance.BorderSize = 0;

                dlg.Controls.AddRange(new Control[] { lbl, cmb, btnTamam, btnIptal });
                dlg.AcceptButton = btnTamam;
                dlg.CancelButton = btnIptal;

                return dlg.ShowDialog(this) == DialogResult.OK ? cmb.SelectedItem.ToString() : null;
            }
        }

        private void TopluZimmetRaporuCiz(object sender, PrintPageEventArgs e)
        {
            Font fontAltBaslik = new Font("Arial", 12, FontStyle.Bold);
            Font fontMetin = new Font("Arial", 10, FontStyle.Regular);
            Font fontKalinMetin = new Font("Arial", 10, FontStyle.Bold);
            SolidBrush fircaSiyah = new SolidBrush(Color.Black);
            Pen kalemSiyah = new Pen(Color.Black, 1);
            Pen kalinKalem = new Pen(Color.Black, 2);

            int x = 50;
            float y = 50;
            int sayfaAlt = e.MarginBounds.Bottom;

            string[] basliklar = { "Kategori", "Marka / Model", "Seri No", "Adet", "Durum" };
            int[] sutunGenislik = { 190, 190, 130, 50, 110 };
            int tabloGenislik = 0;
            foreach (int g in sutunGenislik) tabloGenislik += g;
            float minSatirYuksekligi = 26;

            // NOT: Trimming/NoWrap kullanılmıyor — resmi bir tutanakta hiçbir bilgi kesilmemeli.
            // Bunun yerine metin hücre genişliğine göre otomatik alt satıra kayıyor (word-wrap)
            // ve satır yüksekliği en uzun hücreye göre dinamik hesaplanıyor.
            StringFormat hucreFormati = new StringFormat
            {
                Alignment = StringAlignment.Near,
                LineAlignment = StringAlignment.Near
            };

            e.Graphics.DrawString("Bilgi İşlem Müdürlüğü", fontAltBaslik, fircaSiyah, new PointF(320, y)); y += 35;
            e.Graphics.DrawString("TOPLU ZİMMET LİSTESİ", fontAltBaslik, fircaSiyah, new PointF(300, y)); y += 20;
            e.Graphics.DrawLine(kalinKalem, x, y, x + tabloGenislik, y); y += 25;

            e.Graphics.DrawString($"Zimmetli Kişi/Birim: {topluRaporKisi}", fontKalinMetin, fircaSiyah, new PointF(x, y));
            e.Graphics.DrawString($"Tarih: {DateTime.Now:dd.MM.yyyy}", fontKalinMetin, fircaSiyah, new PointF(x + 460, y));
            y += 35;

            float basSatirYuksekligi = SatirYuksekligiHesapla(e.Graphics, basliklar, sutunGenislik, fontKalinMetin, minSatirYuksekligi);
            int basX = x;
            for (int i = 0; i < basliklar.Length; i++)
            {
                RectangleF basAlani = new RectangleF(basX + 4, y + 3, sutunGenislik[i] - 8, basSatirYuksekligi - 4);
                e.Graphics.DrawString(basliklar[i], fontKalinMetin, fircaSiyah, basAlani, hucreFormati);
                basX += sutunGenislik[i];
            }
            y += basSatirYuksekligi;
            e.Graphics.DrawLine(kalemSiyah, x, y, x + tabloGenislik, y);

            while (topluRaporIndeks < topluRaporSatirlari.Count)
            {
                DataRow r = topluRaporSatirlari[topluRaporIndeks];
                string[] degerler =
                {
                    r["Kategori"]?.ToString() ?? "-",
                    r["Marka / Model"]?.ToString() ?? "-",
                    r["Seri No"]?.ToString() ?? "-",
                    r["Adet"]?.ToString() ?? "-",
                    r["Durum"]?.ToString() ?? "-"
                };

                float satirYuksekligi = SatirYuksekligiHesapla(e.Graphics, degerler, sutunGenislik, fontMetin, minSatirYuksekligi);

                if (y + satirYuksekligi > sayfaAlt)
                {
                    e.HasMorePages = true;
                    return;
                }

                int satX = x;
                for (int i = 0; i < degerler.Length; i++)
                {
                    RectangleF hucreAlani = new RectangleF(satX + 4, y + 3, sutunGenislik[i] - 8, satirYuksekligi - 4);
                    e.Graphics.DrawString(degerler[i], fontMetin, fircaSiyah, hucreAlani, hucreFormati);
                    satX += sutunGenislik[i];
                }
                y += satirYuksekligi;
                e.Graphics.DrawLine(kalemSiyah, x, y, x + tabloGenislik, y);

                topluRaporIndeks++;
            }

            e.HasMorePages = false;

            y += 40;
            e.Graphics.DrawString($"Toplam {topluRaporSatirlari.Count} kalem zimmetli demirbaş yukarıda listelenmiştir.", fontMetin, fircaSiyah, new PointF(x, y));
            y += 50;
            e.Graphics.DrawString("Teslim Alan İmza: ....................................", fontMetin, fircaSiyah, new PointF(x, y));
            e.Graphics.DrawString("Teslim Eden (Bilgi İşlem) İmza: ........................", fontMetin, fircaSiyah, new PointF(x + 330, y));
        }

        // Bir satırdaki hücrelerin, sütun genişliğine göre kaç satıra kaydığını hesaplayıp
        // en uzun hücreye yetecek satır yüksekliğini döndürür. Bu sayede hiçbir metin kesilmez.
        private float SatirYuksekligiHesapla(Graphics g, string[] degerler, int[] genislikler, Font font, float minYukseklik)
        {
            float maxYukseklik = minYukseklik;
            for (int i = 0; i < degerler.Length; i++)
            {
                SizeF olculenBoyut = g.MeasureString(degerler[i] ?? "", font, genislikler[i] - 8);
                float gerekliYukseklik = olculenBoyut.Height + 8;
                if (gerekliYukseklik > maxYukseklik) maxYukseklik = gerekliYukseklik;
            }
            return maxYukseklik;
        }

        private void BtnYedekle_Click(object sender, EventArgs e)
        {
            try
            {
                string kaynakDosya = System.IO.Path.Combine(Application.StartupPath, "Demirbas.db");
                if (!File.Exists(kaynakDosya))
                {
                    MessageBox.Show("Yedeklenecek veritabanı dosyası bulunamadı.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Filter = "SQLite Veritabanı (*.db)|*.db";
                    sfd.FileName = $"Demirbas_Yedek_{DateTime.Now:yyyyMMdd_HHmmss}.db";

                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        File.Copy(kaynakDosya, sfd.FileName, true);
                        MessageBox.Show("Veritabanı yedeği başarıyla oluşturuldu!", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Yedekleme sırasında bir hata oluştu: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}