using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace DemirbasTakipProjesi
{
    public class KullaniciYonetimiForm : Form
    {
        private readonly string baglantiAdresi;
        private readonly string aktifKullanici;

        private DataGridView dgvKullanicilar;
        private Label lblToplamKayit;

        private TextBox txtKullaniciAdi;
        private TextBox txtSifre;
        private ComboBox cmbRol;
        private CheckBox chkAktif;

        private Button btnEkle, btnSifreSifirla, btnGuncelle, btnSil, btnTemizle;

        private int? seciliKullaniciId = null;

        public KullaniciYonetimiForm(string baglantiAdresi, string aktifKullanici)
        {
            this.baglantiAdresi = baglantiAdresi;
            this.aktifKullanici = aktifKullanici;
            TasarimiOlustur();
            KullanicilariGetir();
        }

        private void TasarimiOlustur()
        {
            this.Text = "Kullanıcı Yönetimi";
            this.Size = new Size(1000, 660);
            this.MinimumSize = new Size(880, 560);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(30, 32, 40);
            this.Font = new Font("Segoe UI", 10F);

            // --- ÜST PANEL ---
            Panel pnlUst = new Panel() { Dock = DockStyle.Top, Height = 66, BackColor = Color.FromArgb(22, 24, 32) };

            Label lblBaslik = new Label()
            {
                Text = "👥 Kullanıcı Yönetimi",
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
            btnYenile.Click += (s, e) => { KullanicilariGetir(); Temizle(); };

            pnlUst.Controls.AddRange(new Control[] { lblBaslik, lblToplamKayit, btnYenile });
            pnlUst.Resize += (s, e) => { btnYenile.Left = pnlUst.Width - btnYenile.Width - 24; };

            // --- SOL PANEL: FORM ALANLARI ---
            Panel pnlSol = new Panel() { Top = 86, Left = 20, Width = 300, Height = 540, BackColor = Color.FromArgb(40, 42, 54) };
            pnlSol.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;

            Label lblFormBaslik = new Label() { Text = "KULLANICI BİLGİLERİ", ForeColor = Color.White, Font = new Font("Segoe UI", 13F, FontStyle.Bold), Top = 15, Left = 20, AutoSize = true };

            Label l1 = new Label() { Text = "Kullanıcı Adı:", ForeColor = Color.LightGray, Top = 60, Left = 20, AutoSize = true };
            txtKullaniciAdi = new TextBox() { Top = 84, Left = 20, Width = 250, Font = new Font("Segoe UI", 11F) };

            Label l2 = new Label() { Text = "Şifre (yeni kullanıcı / sıfırlama):", ForeColor = Color.LightGray, Top = 128, Left = 20, AutoSize = true };
            txtSifre = new TextBox() { Top = 152, Left = 20, Width = 250, Font = new Font("Segoe UI", 11F), PasswordChar = '●' };

            Label l3 = new Label() { Text = "Rol:", ForeColor = Color.LightGray, Top = 196, Left = 20, AutoSize = true };
            cmbRol = new ComboBox() { Top = 220, Left = 20, Width = 250, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 11F) };
            cmbRol.Items.AddRange(new string[] { "Admin", "Personel" });
            cmbRol.SelectedIndex = 1;

            chkAktif = new CheckBox() { Text = "Hesap Aktif", ForeColor = Color.LightGray, Top = 264, Left = 20, AutoSize = true, Checked = true, Font = new Font("Segoe UI", 10F) };

            btnEkle = new Button() { Text = "Yeni Kullanıcı Ekle", Top = 320, Left = 20, Width = 250, Height = 38, BackColor = Color.FromArgb(40, 167, 69), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
            btnSifreSifirla = new Button() { Text = "Şifreyi Sıfırla", Top = 366, Left = 20, Width = 250, Height = 38, BackColor = Color.FromArgb(23, 162, 184), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
            btnGuncelle = new Button() { Text = "Rol / Durum Güncelle", Top = 412, Left = 20, Width = 250, Height = 38, BackColor = Color.FromArgb(0, 122, 204), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
            btnSil = new Button() { Text = "Kullanıcıyı Sil", Top = 458, Left = 20, Width = 250, Height = 35, BackColor = Color.FromArgb(220, 53, 69), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnTemizle = new Button() { Text = "Formu Temizle", Top = 502, Left = 20, Width = 250, Height = 32, BackColor = Color.FromArgb(70, 73, 90), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

            foreach (Button b in new[] { btnEkle, btnSifreSifirla, btnGuncelle, btnSil, btnTemizle })
                b.FlatAppearance.BorderSize = 0;

            btnEkle.Click += BtnEkle_Click;
            btnSifreSifirla.Click += BtnSifreSifirla_Click;
            btnGuncelle.Click += BtnGuncelle_Click;
            btnSil.Click += BtnSil_Click;
            btnTemizle.Click += (s, e) => Temizle();

            pnlSol.Controls.AddRange(new Control[] { lblFormBaslik, l1, txtKullaniciAdi, l2, txtSifre, l3, cmbRol, chkAktif, btnEkle, btnSifreSifirla, btnGuncelle, btnSil, btnTemizle });

            // --- SAĞ PANEL: LİSTE ---
            Panel pnlSagKapsayici = new Panel() { Top = 86, Left = 336, Width = 624, Height = 540, BackColor = Color.FromArgb(30, 32, 40) };
            pnlSagKapsayici.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            dgvKullanicilar = new DataGridView();
            dgvKullanicilar.Dock = DockStyle.Fill;
            dgvKullanicilar.ReadOnly = true;
            dgvKullanicilar.AllowUserToAddRows = false;
            dgvKullanicilar.AllowUserToResizeRows = false;
            dgvKullanicilar.RowHeadersVisible = false;
            dgvKullanicilar.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvKullanicilar.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvKullanicilar.MultiSelect = false;
            dgvKullanicilar.BackgroundColor = Color.FromArgb(30, 32, 40);
            dgvKullanicilar.BorderStyle = BorderStyle.None;
            dgvKullanicilar.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvKullanicilar.GridColor = Color.FromArgb(55, 58, 72);
            dgvKullanicilar.RowTemplate.Height = 36;
            dgvKullanicilar.Font = new Font("Segoe UI", 9.5F);

            dgvKullanicilar.DefaultCellStyle.BackColor = Color.FromArgb(40, 42, 54);
            dgvKullanicilar.DefaultCellStyle.ForeColor = Color.Gainsboro;
            dgvKullanicilar.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 122, 204);
            dgvKullanicilar.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvKullanicilar.DefaultCellStyle.Padding = new Padding(8, 0, 4, 0);
            dgvKullanicilar.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(45, 48, 61);

            dgvKullanicilar.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(20, 22, 30);
            dgvKullanicilar.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvKullanicilar.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            dgvKullanicilar.ColumnHeadersHeight = 44;
            dgvKullanicilar.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgvKullanicilar.EnableHeadersVisualStyles = false;

            dgvKullanicilar.CellFormatting += DgvKullanicilar_CellFormatting;
            dgvKullanicilar.CellClick += DgvKullanicilar_CellClick;

            pnlSagKapsayici.Controls.Add(dgvKullanicilar);

            this.Controls.Add(pnlSagKapsayici);
            this.Controls.Add(pnlSol);
            this.Controls.Add(pnlUst);
        }

        private void KullanicilariGetir()
        {
            try
            {
                using (SQLiteConnection baglanti = new SQLiteConnection(baglantiAdresi))
                {
                    string sorgu = @"SELECT kullanici_ID AS 'ID', kullanici_adi AS 'Kullanıcı Adı',
                                            rol AS 'Rol', aktif AS 'Aktif'
                                     FROM Kullanicilar
                                     ORDER BY kullanici_ID";
                    SQLiteDataAdapter da = new SQLiteDataAdapter(sorgu, baglanti);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgvKullanicilar.DataSource = dt;

                    if (dgvKullanicilar.Columns["ID"] != null)
                        dgvKullanicilar.Columns["ID"].Visible = false;

                    lblToplamKayit.Text = $"({dt.Rows.Count} kullanıcı)";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Kullanıcılar getirilirken bir hata oluştu: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DgvKullanicilar_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvKullanicilar.Columns[e.ColumnIndex].Name == "Aktif" && e.Value != null)
            {
                bool aktifMi = Convert.ToInt32(e.Value) == 1;
                e.Value = aktifMi ? "Evet" : "Hayır";
                e.FormattingApplied = true;
                e.CellStyle.ForeColor = aktifMi ? Color.FromArgb(90, 220, 130) : Color.FromArgb(240, 100, 100);
                e.CellStyle.Font = new Font(dgvKullanicilar.Font, FontStyle.Bold);
            }

            if (dgvKullanicilar.Columns[e.ColumnIndex].Name == "Rol" && e.Value != null)
            {
                e.CellStyle.ForeColor = e.Value.ToString() == "Admin" ? Color.Gold : Color.Gainsboro;
            }
        }

        private void DgvKullanicilar_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (dgvKullanicilar.CurrentRow == null || e.RowIndex < 0) return;

            seciliKullaniciId = Convert.ToInt32(dgvKullanicilar.CurrentRow.Cells["ID"].Value);
            txtKullaniciAdi.Text = dgvKullanicilar.CurrentRow.Cells["Kullanıcı Adı"].Value?.ToString() ?? "";
            cmbRol.Text = dgvKullanicilar.CurrentRow.Cells["Rol"].Value?.ToString() ?? "Personel";
            chkAktif.Checked = Convert.ToInt32(dgvKullanicilar.CurrentRow.Cells["Aktif"].Value) == 1;
            txtSifre.Clear();
        }

        private void BtnEkle_Click(object sender, EventArgs e)
        {
            string ad = txtKullaniciAdi.Text.Trim();
            string sifre = txtSifre.Text;

            if (string.IsNullOrWhiteSpace(ad) || string.IsNullOrWhiteSpace(sifre))
            {
                MessageBox.Show("Kullanıcı adı ve şifre boş bırakılamaz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (sifre.Length < 4)
            {
                MessageBox.Show("Şifre en az 4 karakter olmalıdır.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (SQLiteConnection baglanti = new SQLiteConnection(baglantiAdresi))
                {
                    string sorgu = @"INSERT INTO Kullanicilar (kullanici_adi, sifre_hash, rol, aktif)
                                     VALUES (@ad, @hash, @rol, @aktif)";
                    SQLiteCommand komut = new SQLiteCommand(sorgu, baglanti);
                    komut.Parameters.AddWithValue("@ad", ad);
                    komut.Parameters.AddWithValue("@hash", SifreyiHashle(sifre));
                    komut.Parameters.AddWithValue("@rol", cmbRol.SelectedItem?.ToString() ?? "Personel");
                    komut.Parameters.AddWithValue("@aktif", chkAktif.Checked ? 1 : 0);

                    baglanti.Open();
                    komut.ExecuteNonQuery();
                }

                MessageBox.Show("Kullanıcı başarıyla eklendi!", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                KullanicilariGetir();
                Temizle();
            }
            catch (SQLiteException ex) when (ex.Message.Contains("UNIQUE"))
            {
                MessageBox.Show("Bu kullanıcı adı zaten kayıtlı.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Kullanıcı eklenirken bir hata oluştu: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSifreSifirla_Click(object sender, EventArgs e)
        {
            if (seciliKullaniciId == null)
            {
                MessageBox.Show("Lütfen şifresini sıfırlamak istediğiniz kullanıcıyı listeden seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string yeniSifre = txtSifre.Text;
            if (string.IsNullOrWhiteSpace(yeniSifre) || yeniSifre.Length < 4)
            {
                MessageBox.Show("Lütfen en az 4 karakterli yeni bir şifre girin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (SQLiteConnection baglanti = new SQLiteConnection(baglantiAdresi))
            {
                SQLiteCommand komut = new SQLiteCommand("UPDATE Kullanicilar SET sifre_hash = @hash WHERE kullanici_ID = @id", baglanti);
                komut.Parameters.AddWithValue("@hash", SifreyiHashle(yeniSifre));
                komut.Parameters.AddWithValue("@id", seciliKullaniciId);
                baglanti.Open();
                komut.ExecuteNonQuery();
            }

            MessageBox.Show("Şifre başarıyla sıfırlandı.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            txtSifre.Clear();
        }

        private void BtnGuncelle_Click(object sender, EventArgs e)
        {
            if (seciliKullaniciId == null)
            {
                MessageBox.Show("Lütfen güncellemek istediğiniz kullanıcıyı listeden seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool kendiKaydiMi = string.Equals(txtKullaniciAdi.Text.Trim(), aktifKullanici, StringComparison.OrdinalIgnoreCase);
            if (kendiKaydiMi && (!chkAktif.Checked || cmbRol.Text != "Admin"))
            {
                MessageBox.Show("Kendi hesabınızı pasifleştiremez veya kendi admin yetkinizi kaldıramazsınız.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (SQLiteConnection baglanti = new SQLiteConnection(baglantiAdresi))
            {
                string sorgu = "UPDATE Kullanicilar SET rol = @rol, aktif = @aktif WHERE kullanici_ID = @id";
                SQLiteCommand komut = new SQLiteCommand(sorgu, baglanti);
                komut.Parameters.AddWithValue("@rol", cmbRol.SelectedItem?.ToString() ?? "Personel");
                komut.Parameters.AddWithValue("@aktif", chkAktif.Checked ? 1 : 0);
                komut.Parameters.AddWithValue("@id", seciliKullaniciId);
                baglanti.Open();
                komut.ExecuteNonQuery();
            }

            MessageBox.Show("Kullanıcı bilgileri güncellendi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            KullanicilariGetir();
            Temizle();
        }

        private void BtnSil_Click(object sender, EventArgs e)
        {
            if (seciliKullaniciId == null)
            {
                MessageBox.Show("Lütfen silmek istediğiniz kullanıcıyı listeden seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.Equals(txtKullaniciAdi.Text.Trim(), aktifKullanici, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Oturum açmış olduğunuz kendi hesabınızı silemezsiniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult cevap = MessageBox.Show($"'{txtKullaniciAdi.Text}' kullanıcısı kalıcı olarak silinsin mi?", "Silme Onayı", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (cevap != DialogResult.Yes) return;

            try
            {
                using (SQLiteConnection baglanti = new SQLiteConnection(baglantiAdresi))
                {
                    baglanti.Open();

                    // Son aktif Admin'i silmeyi engelle
                    if (cmbRol.Text == "Admin")
                    {
                        SQLiteCommand sayimKomutu = new SQLiteCommand("SELECT COUNT(*) FROM Kullanicilar WHERE rol = 'Admin' AND aktif = 1", baglanti);
                        long aktifAdminSayisi = (long)sayimKomutu.ExecuteScalar();
                        if (aktifAdminSayisi <= 1)
                        {
                            MessageBox.Show("Sistemdeki son aktif Admin kullanıcısı silinemez.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }

                    SQLiteCommand komut = new SQLiteCommand("DELETE FROM Kullanicilar WHERE kullanici_ID = @id", baglanti);
                    komut.Parameters.AddWithValue("@id", seciliKullaniciId);
                    komut.ExecuteNonQuery();
                }

                MessageBox.Show("Kullanıcı silindi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                KullanicilariGetir();
                Temizle();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Kullanıcı silinirken bir hata oluştu: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Temizle()
        {
            seciliKullaniciId = null;
            txtKullaniciAdi.Clear();
            txtSifre.Clear();
            cmbRol.SelectedIndex = 1;
            chkAktif.Checked = true;
            dgvKullanicilar.ClearSelection();
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