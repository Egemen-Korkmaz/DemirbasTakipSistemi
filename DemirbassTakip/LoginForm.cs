using System;
using System.Data.SQLite;
using System.Drawing;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace DemirbasTakipProjesi
{
    public class LoginForm : Form
    {
        private readonly string baglantiAdresi;
        private TextBox txtKullaniciAdi;
        private TextBox txtSifre;
        private Label lblHata;

        public string GirisYapanKullanici { get; private set; }
        public string GirisYapanRol { get; private set; }

        public LoginForm(string baglantiAdresi)
        {
            this.baglantiAdresi = baglantiAdresi;
            TasarimiOlustur();
        }

        private void TasarimiOlustur()
        {
            this.Text = "Giriş Yap";
            this.Size = new Size(420, 420);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            try { this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            this.BackColor = Color.FromArgb(30, 32, 40);
            this.Font = new Font("Segoe UI", 10F);

            Label lblBaslik = new Label() { Text = "🔒 Envanter Sistemi Girişi", ForeColor = Color.White, Font = new Font("Segoe UI", 15F, FontStyle.Bold), Top = 35, Left = 40, AutoSize = true };

            Label lblKullanici = new Label() { Text = "Kullanıcı Adı:", ForeColor = Color.LightGray, Top = 105, Left = 40, AutoSize = true };
            txtKullaniciAdi = new TextBox() { Top = 130, Left = 40, Width = 320, Font = new Font("Segoe UI", 11F) };

            Label lblSifre = new Label() { Text = "Şifre:", ForeColor = Color.LightGray, Top = 175, Left = 40, AutoSize = true };
            txtSifre = new TextBox() { Top = 200, Left = 40, Width = 320, Font = new Font("Segoe UI", 11F), PasswordChar = '●' };
            txtSifre.KeyDown += TxtSifre_KeyDown;

            lblHata = new Label() { Text = "", ForeColor = Color.FromArgb(240, 100, 100), Top = 240, Left = 40, Width = 320, Height = 40, Font = new Font("Segoe UI", 9F) };

            Button btnGiris = new Button() { Text = "Giriş Yap", Top = 290, Left = 40, Width = 320, Height = 42, BackColor = Color.FromArgb(0, 122, 204), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 11F, FontStyle.Bold) };
            btnGiris.FlatAppearance.BorderSize = 0;
            btnGiris.Click += BtnGiris_Click;

            this.Controls.AddRange(new Control[] { lblBaslik, lblKullanici, txtKullaniciAdi, lblSifre, txtSifre, lblHata, btnGiris });
            this.AcceptButton = btnGiris;
        }

        private void TxtSifre_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                BtnGiris_Click(sender, e);
            }
        }

        private void BtnGiris_Click(object sender, EventArgs e)
        {
            string girilenAd = txtKullaniciAdi.Text.Trim();
            string girilenSifre = txtSifre.Text;

            if (string.IsNullOrWhiteSpace(girilenAd) || string.IsNullOrWhiteSpace(girilenSifre))
            {
                lblHata.Text = "Kullanıcı adı ve şifre boş bırakılamaz.";
                return;
            }

            try
            {
                using (SQLiteConnection baglanti = new SQLiteConnection(baglantiAdresi))
                {
                    string sorgu = "SELECT sifre_hash, rol FROM Kullanicilar WHERE kullanici_adi = @ad AND aktif = 1";
                    SQLiteCommand komut = new SQLiteCommand(sorgu, baglanti);
                    komut.Parameters.AddWithValue("@ad", girilenAd);

                    baglanti.Open();
                    using (SQLiteDataReader okuyucu = komut.ExecuteReader())
                    {
                        if (okuyucu.Read())
                        {
                            string kayitliHash = okuyucu["sifre_hash"].ToString();
                            string rol = okuyucu["rol"].ToString();
                            string girilenHash = SifreyiHashle(girilenSifre);

                            if (string.Equals(girilenHash, kayitliHash, StringComparison.OrdinalIgnoreCase))
                            {
                                GirisYapanKullanici = girilenAd;
                                GirisYapanRol = rol;
                                this.DialogResult = DialogResult.OK;
                                this.Close();
                                return;
                            }
                        }
                    }
                }

                lblHata.Text = "Kullanıcı adı veya şifre hatalı.";
                txtSifre.Clear();
                txtSifre.Focus();
            }
            catch (Exception ex)
            {
                lblHata.Text = "Bağlantı hatası: " + ex.Message;
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