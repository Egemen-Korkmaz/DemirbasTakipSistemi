using System.Drawing;
using System.Windows.Forms;

namespace DemirbasTakipProjesi
{
    public class SplashForm : Form
    {
        public SplashForm()
        {
            TasarimiOlustur();
        }

        private void TasarimiOlustur()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new Size(480, 300);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(30, 32, 40);
            this.ShowInTaskbar = false;
            this.Paint += (s, e) =>
            {
                using (Pen kenarKalemi = new Pen(Color.FromArgb(0, 122, 204), 2))
                {
                    e.Graphics.DrawRectangle(kenarKalemi, 0, 0, this.Width - 1, this.Height - 1);
                }
            };

            Label lblIkon = new Label()
            {
                Text = "🖥️",
                Font = new Font("Segoe UI Emoji", 40F),
                AutoSize = true,
                Top = 45
            };
            lblIkon.Left = (this.Width - lblIkon.PreferredWidth) / 2;

            Label lblBaslik = new Label()
            {
                Text = "Demirbaş Takip Sistemi",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                AutoSize = true,
                Top = 135
            };
            lblBaslik.Left = (this.Width - TextRenderer.MeasureText(lblBaslik.Text, lblBaslik.Font).Width) / 2;

            Label lblAltBaslik = new Label()
            {
                Text = "Bilgi İşlem Müdürlüğü",
                ForeColor = Color.LightGray,
                Font = new Font("Segoe UI", 10F),
                AutoSize = true,
                Top = 172
            };
            lblAltBaslik.Left = (this.Width - TextRenderer.MeasureText(lblAltBaslik.Text, lblAltBaslik.Font).Width) / 2;

            ProgressBar ilerlemeCubugu = new ProgressBar()
            {
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 25,
                Width = 300,
                Height = 8,
                Top = 220
            };
            ilerlemeCubugu.Left = (this.Width - ilerlemeCubugu.Width) / 2;

            Label lblYukleniyor = new Label()
            {
                Text = "Yükleniyor...",
                ForeColor = Color.Gainsboro,
                Font = new Font("Segoe UI", 9F),
                AutoSize = true,
                Top = 240
            };
            lblYukleniyor.Left = (this.Width - TextRenderer.MeasureText(lblYukleniyor.Text, lblYukleniyor.Font).Width) / 2;

            this.Controls.AddRange(new Control[] { lblIkon, lblBaslik, lblAltBaslik, ilerlemeCubugu, lblYukleniyor });
        }
    }
}