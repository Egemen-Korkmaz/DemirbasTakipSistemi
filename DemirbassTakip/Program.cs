using DemirbassTakip;
using System;
using System.Data.SQLite;
using System.Windows.Forms;

namespace DemirbasTakipProjesi
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using (SplashForm splash = new SplashForm())
            {
                splash.Show();
                DateTime splashBaslangic = DateTime.Now;
                while ((DateTime.Now - splashBaslangic).TotalMilliseconds < 1800)
                {
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(10);
                }
                splash.Close();
            }

            string veriDosyasi = System.IO.Path.Combine(Application.StartupPath, "Demirbas.db");
            string baglantiAdresi = $"Data Source={veriDosyasi};Version=3;";

            if (!System.IO.File.Exists(veriDosyasi))
            {
                MessageBox.Show(
                    "Demirbas.db dosyası bulunamadı!\n\n" +
                    "Bu dosyanın, programın (.exe) bulunduğu klasörde olması gerekiyor.",
                    "Veritabanı Dosyası Eksik", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                VeritabaniSemasiniGuncelle(baglantiAdresi);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Veritabanı şeması güncellenirken bir hata oluştu:\n" + ex.Message,
                    "Veritabanı Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (LoginForm girisEkrani = new LoginForm(baglantiAdresi))
            {
                if (girisEkrani.ShowDialog() == DialogResult.OK)
                {
                    Application.Run(new Form1(girisEkrani.GirisYapanKullanici, girisEkrani.GirisYapanRol));
                }
            }
        }

        // Uygulama her açıldığında çalışır. Demirbaslar tablosunda "silindi" ve "silinme_tarihi"
        // sütunları yoksa (eski bir veritabanı dosyasıysa) ekler. Sütunlar zaten varsa hiçbir şey
        // yapmaz — yani bu fonksiyon güvenle her açılışta çağrılabilir (idempotent migration).
        private static void VeritabaniSemasiniGuncelle(string baglantiAdresi)
        {
            using (SQLiteConnection baglanti = new SQLiteConnection(baglantiAdresi))
            {
                baglanti.Open();

                bool silindiSutunuVarMi = false;
                bool silinmeTarihiSutunuVarMi = false;

                SQLiteCommand semaKomutu = new SQLiteCommand("PRAGMA table_info(Demirbaslar)", baglanti);
                using (SQLiteDataReader okuyucu = semaKomutu.ExecuteReader())
                {
                    while (okuyucu.Read())
                    {
                        string sutunAdi = okuyucu["name"].ToString();
                        if (sutunAdi == "silindi") silindiSutunuVarMi = true;
                        if (sutunAdi == "silinme_tarihi") silinmeTarihiSutunuVarMi = true;
                    }
                }

                if (!silindiSutunuVarMi)
                {
                    new SQLiteCommand("ALTER TABLE Demirbaslar ADD COLUMN silindi INTEGER NOT NULL DEFAULT 0", baglanti).ExecuteNonQuery();
                }

                if (!silinmeTarihiSutunuVarMi)
                {
                    new SQLiteCommand("ALTER TABLE Demirbaslar ADD COLUMN silinme_tarihi TEXT", baglanti).ExecuteNonQuery();
                }
            }
        }
    }
}