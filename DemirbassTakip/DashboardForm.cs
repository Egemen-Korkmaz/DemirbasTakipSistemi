using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace DemirbasTakipProjesi
{
    public class DashboardForm : Form
    {
        private readonly string baglantiAdresi;
        private PastaGrafikPanel pastaPaneli;
        private BarGrafikPanel barPaneli;
        private Label lblToplamKayit;

        private static readonly Color[] Renkler = new Color[]
        {
            Color.FromArgb(0, 173, 181),
            Color.FromArgb(255, 179, 71),
            Color.FromArgb(114, 137, 218),
            Color.FromArgb(240, 100, 100),
            Color.FromArgb(120, 220, 130),
            Color.FromArgb(200, 120, 220),
            Color.FromArgb(255, 205, 86),
            Color.FromArgb(100, 190, 230)
        };

        public DashboardForm(string baglantiAdresi)
        {
            this.baglantiAdresi = baglantiAdresi;
            TasarimiOlustur();
            VerileriYukle();
        }

        private void TasarimiOlustur()
        {
            this.Text = "Envanter Dashboard";
            this.Size = new Size(1080, 640);
            this.MinimumSize = new Size(820, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(30, 32, 40);
            this.Font = new Font("Segoe UI", 10F);

            Panel pnlUst = new Panel() { Dock = DockStyle.Top, Height = 66, BackColor = Color.FromArgb(22, 24, 32) };

            Label lblBaslik = new Label()
            {
                Text = "📊 Envanter Dashboard",
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
            btnYenile.Click += (s, e) => VerileriYukle();

            pnlUst.Controls.AddRange(new Control[] { lblBaslik, lblToplamKayit, btnYenile });
            pnlUst.Resize += (s, e) => { btnYenile.Left = pnlUst.Width - btnYenile.Width - 24; };

            // --- İÇERİK: PASTA VE BAR GRAFİĞİ YAN YANA ---
            TableLayoutPanel pnlIcerik = new TableLayoutPanel()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(20),
                BackColor = Color.FromArgb(30, 32, 40)
            };
            pnlIcerik.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            pnlIcerik.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            GroupBox grpPasta = new GroupBox()
            {
                Text = "Kategoriye Göre Dağılım",
                Dock = DockStyle.Fill,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            pastaPaneli = new PastaGrafikPanel() { Dock = DockStyle.Fill, BackColor = Color.FromArgb(30, 32, 40) };
            grpPasta.Controls.Add(pastaPaneli);

            GroupBox grpBar = new GroupBox()
            {
                Text = "Duruma Göre Dağılım",
                Dock = DockStyle.Fill,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            barPaneli = new BarGrafikPanel() { Dock = DockStyle.Fill, BackColor = Color.FromArgb(30, 32, 40) };
            grpBar.Controls.Add(barPaneli);

            pnlIcerik.Controls.Add(grpPasta, 0, 0);
            pnlIcerik.Controls.Add(grpBar, 1, 0);

            this.Controls.Add(pnlIcerik);
            this.Controls.Add(pnlUst);
        }

        private void VerileriYukle()
        {
            try
            {
                using (SQLiteConnection baglanti = new SQLiteConnection(baglantiAdresi))
                {
                    baglanti.Open();

                    string sorguKategori = @"SELECT K.kategori_adi, SUM(D.adet) AS toplam
                                             FROM Demirbaslar D
                                             INNER JOIN Kategoriler K ON D.kategori_ID = K.kategori_ID
                                             WHERE D.silindi = 0
                                             GROUP BY K.kategori_adi
                                             ORDER BY toplam DESC";
                    List<(string Etiket, int Deger, Color Renk)> kategoriVerisi = new List<(string, int, Color)>();
                    using (SQLiteCommand komut = new SQLiteCommand(sorguKategori, baglanti))
                    using (SQLiteDataReader okuyucu = komut.ExecuteReader())
                    {
                        int i = 0;
                        while (okuyucu.Read())
                        {
                            kategoriVerisi.Add((okuyucu.GetString(0), Convert.ToInt32(okuyucu["toplam"]), Renkler[i % Renkler.Length]));
                            i++;
                        }
                    }
                    pastaPaneli.Veriler = kategoriVerisi;

                    string sorguDurum = @"SELECT durum, SUM(adet) AS toplam
                                          FROM Demirbaslar
                                          WHERE silindi = 0
                                          GROUP BY durum
                                          ORDER BY toplam DESC";
                    List<(string Etiket, int Deger, Color Renk)> durumVerisi = new List<(string, int, Color)>();
                    using (SQLiteCommand komut = new SQLiteCommand(sorguDurum, baglanti))
                    using (SQLiteDataReader okuyucu = komut.ExecuteReader())
                    {
                        int i = 0;
                        while (okuyucu.Read())
                        {
                            durumVerisi.Add((okuyucu.GetString(0), Convert.ToInt32(okuyucu["toplam"]), Renkler[i % Renkler.Length]));
                            i++;
                        }
                    }
                    barPaneli.Veriler = durumVerisi;

                    int toplamCihaz = kategoriVerisi.Sum(v => v.Deger);
                    lblToplamKayit.Text = $"({toplamCihaz} cihaz)";

                    pastaPaneli.Invalidate();
                    barPaneli.Invalidate();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Dashboard verileri getirilirken bir hata oluştu: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    internal class PastaGrafikPanel : Panel
    {
        public List<(string Etiket, int Deger, Color Renk)> Veriler { get; set; } = new List<(string, int, Color)>();

        public PastaGrafikPanel()
        {
            this.DoubleBuffered = true;
            this.ResizeRedraw = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            if (Veriler == null || Veriler.Count == 0)
            {
                using (Font f = new Font("Segoe UI", 10F))
                {
                    e.Graphics.DrawString("Veri bulunamadı.", f, Brushes.Gainsboro, new PointF(20, 20));
                }
                return;
            }

            int toplam = 0;
            foreach (var v in Veriler) toplam += v.Deger;
            if (toplam == 0) return;

            int cap = Math.Min(this.Width - 220, this.Height - 40);
            if (cap < 60) cap = 60;
            int pastaSol = 20;
            int pastaUst = Math.Max(10, (this.Height - cap) / 2);
            Rectangle pastaAlani = new Rectangle(pastaSol, pastaUst, cap, cap);

            float aci = -90f;
            using (Font fEtiket = new Font("Segoe UI", 9F))
            {
                int legendTop = pastaUst;
                int legendLeft = pastaSol + cap + 30;

                foreach (var v in Veriler)
                {
                    float oran = (float)v.Deger / toplam;
                    float sweep = oran * 360f;

                    using (Brush dilimFircasi = new SolidBrush(v.Renk))
                    {
                        e.Graphics.FillPie(dilimFircasi, pastaAlani, aci, sweep);
                    }
                    aci += sweep;

                    using (Brush kareFircasi = new SolidBrush(v.Renk))
                    {
                        e.Graphics.FillRectangle(kareFircasi, legendLeft, legendTop, 14, 14);
                    }
                    string legendMetni = $"{v.Etiket}  ({v.Deger} adet, %{(oran * 100):0})";
                    e.Graphics.DrawString(legendMetni, fEtiket, Brushes.Gainsboro, legendLeft + 20, legendTop - 1);
                    legendTop += 24;
                }
            }

            using (Pen kenarKalemi = new Pen(this.BackColor, 2))
            {
                e.Graphics.DrawEllipse(kenarKalemi, pastaAlani);
            }
        }
    }
    internal class BarGrafikPanel : Panel
    {
        public List<(string Etiket, int Deger, Color Renk)> Veriler { get; set; } = new List<(string, int, Color)>();

        public BarGrafikPanel()
        {
            this.DoubleBuffered = true;
            this.ResizeRedraw = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            if (Veriler == null || Veriler.Count == 0)
            {
                using (Font f = new Font("Segoe UI", 10F))
                {
                    e.Graphics.DrawString("Veri bulunamadı.", f, Brushes.Gainsboro, new PointF(20, 20));
                }
                return;
            }

            int maksDeger = 1;
            foreach (var v in Veriler) if (v.Deger > maksDeger) maksDeger = v.Deger;

            int altPay = 50;
            int ustPay = 30;
            int grafikYuksekligi = Math.Max(20, this.Height - altPay - ustPay);
            int adet = Veriler.Count;
            int araBosluk = 20;
            int cizimGenisligi = Math.Max(40, this.Width - 40);
            int barGenisligi = Math.Max(16, (cizimGenisligi - (adet + 1) * araBosluk) / adet);

            using (Font fEtiket = new Font("Segoe UI", 8.5F))
            using (Font fDeger = new Font("Segoe UI", 9F, FontStyle.Bold))
            {
                int x = 20 + araBosluk;
                foreach (var v in Veriler)
                {
                    int barYuksekligi = (int)((float)v.Deger / maksDeger * grafikYuksekligi);
                    int barUst = ustPay + (grafikYuksekligi - barYuksekligi);

                    using (Brush barFircasi = new SolidBrush(v.Renk))
                    {
                        e.Graphics.FillRectangle(barFircasi, x, barUst, barGenisligi, barYuksekligi);
                    }

                    string degerMetni = v.Deger.ToString();
                    SizeF degerBoyutu = e.Graphics.MeasureString(degerMetni, fDeger);
                    e.Graphics.DrawString(degerMetni, fDeger, Brushes.White, x + (barGenisligi - degerBoyutu.Width) / 2, barUst - 20);

                    SizeF etiketBoyutu = e.Graphics.MeasureString(v.Etiket, fEtiket);
                    float etiketSol = x + (barGenisligi - etiketBoyutu.Width) / 2;
                    e.Graphics.DrawString(v.Etiket, fEtiket, Brushes.Gainsboro, etiketSol, ustPay + grafikYuksekligi + 6);

                    x += barGenisligi + araBosluk;
                }
            }

            using (Pen tabanCizgisi = new Pen(Color.FromArgb(70, 73, 88), 1))
            {
                e.Graphics.DrawLine(tabanCizgisi, 20, ustPay + grafikYuksekligi, this.Width - 20, ustPay + grafikYuksekligi);
            }
        }
    }
}