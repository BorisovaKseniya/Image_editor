using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace СЦОИ_лаба_1
{
    public partial class Form1 : Form
    {
        private class Layer
        {
            public Bitmap Image { get; set; }
            public float Opacity { get; set; } = 1.0f;
            public string BlendMode { get; set; } = "Нет";
            public int RedChannel { get; set; } = 255;
            public int GreenChannel { get; set; } = 255;
            public int BlueChannel { get; set; } = 255;
        }

        private List<Layer> layers = new List<Layer>();
        private Bitmap canvas;
        private Graphics g;
        private int h, w;

        public Form1()
        {
            InitializeComponent();
            h = pictureBox1.Height;
            w = pictureBox1.Width;
            canvas = new Bitmap(w, h);
            g = Graphics.FromImage(canvas);
            pictureBox1.Image = canvas;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Выберите изображения";
                openFileDialog.Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp;*.gif";
                openFileDialog.Multiselect = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    foreach (string file in openFileDialog.FileNames)
                    {
                        Bitmap img = new Bitmap(file);
                        var layer = new Layer { Image = img };
                        layers.Add(layer);
                        AddImageLayer(layer);
                    }
                    ApplyLayers();
                }
            }
        }

        private void ApplyLayers()
        {
            g.Clear(Color.Transparent);

            for (int i = layers.Count - 1; i >= 0; i--)
            {
                var layer = layers[i];
                Bitmap processedImage;

                if (layer.BlendMode != "Нет")
                {
                    processedImage = BlendBitmaps(canvas, layer.Image, layer.BlendMode, layer.Opacity, layer.RedChannel, layer.GreenChannel, layer.BlueChannel);
                }
                else
                {
                    // Применяем RGB-изменения вручную к изображению
                    processedImage = ApplyRGBAdjustments(layer.Image, layer.RedChannel, layer.GreenChannel, layer.BlueChannel);
                }

                using (ImageAttributes attr = new ImageAttributes())
                {
                    ColorMatrix matrix = new ColorMatrix { Matrix33 = layer.Opacity };
                    attr.SetColorMatrix(matrix);

                    Rectangle destRect = new Rectangle(0, 0, w, h);
                    g.DrawImage(processedImage, destRect, 0, 0, w, h, GraphicsUnit.Pixel, attr);
                }
            }

            pictureBox1.Image = canvas;
            pictureBox1.Refresh();
        }
        private Bitmap ApplyRGBAdjustments(Bitmap original, int redChannel, int greenChannel, int blueChannel)
        {
            Bitmap adjusted = new Bitmap(original.Width, original.Height);

            for (int y = 0; y < original.Height; y++)
            {
                for (int x = 0; x < original.Width; x++)
                {
                    Color pixel = original.GetPixel(x, y);

                    int r = Math.Min(Math.Max((pixel.R * redChannel / 255), 0), 255);
                    int g = Math.Min(Math.Max((pixel.G * greenChannel / 255), 0), 255);
                    int b = Math.Min(Math.Max((pixel.B * blueChannel / 255), 0), 255);

                    adjusted.SetPixel(x, y, Color.FromArgb(pixel.A, r, g, b));
                }
            }

            return adjusted;
        }

        private Bitmap BlendBitmaps(Bitmap baseImg, Bitmap overlayImg, string mode, float opacity, int redChannel, int greenChannel, int blueChannel)
        {
            int width = Math.Min(baseImg.Width, overlayImg.Width);
            int height = Math.Min(baseImg.Height, overlayImg.Height);

            Bitmap result = new Bitmap(baseImg);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color baseColor = baseImg.GetPixel(x, y);
                    Color overlayColor = overlayImg.GetPixel(x, y);


                    // Применяем изменения на основе ползунков для каждого канала
                    int r = (int)(overlayColor.R * redChannel / 255.0f);
                    int g = (int)(overlayColor.G * greenChannel / 255.0f);
                    int b = (int)(overlayColor.B * blueChannel / 255.0f);

                    switch (mode)
                    {
                        case "Сумма":
                            r = Math.Min(baseColor.R + r, 255);
                            g = Math.Min(baseColor.G + g, 255);
                            b = Math.Min(baseColor.B + b, 255);
                            break;

                        case "Разность":
                            r = Math.Abs(baseColor.R - r);
                            g = Math.Abs(baseColor.G - g);
                            b = Math.Abs(baseColor.B - b);
                            break;

                        case "Умножение":
                            r = (baseColor.R * r) / 255;
                            g = (baseColor.G * g) / 255;
                            b = (baseColor.B * b) / 255;
                            break;

                        case "Среднее":
                            r = (baseColor.R + r) / 2;
                            g = (baseColor.G + g) / 2;
                            b = (baseColor.B + b) / 2;
                            break;

                        default:
                            r = baseColor.R;
                            g = baseColor.G;
                            b = baseColor.B;
                            break;
                    }

                    Color blendedColor = Color.FromArgb((int)(opacity * 255), r, g, b);
                    result.SetPixel(x, y, blendedColor);
                }
            }

            return result;
        }

        private void AddImageLayer(Layer layer)
        {
            // Создание панели для слоя
            Panel layerPanel = new Panel
            {
                Width = 500,
                Height = 1000, // Увеличиваем высоту панели, чтобы разместить все элементы, включая кнопки
                BorderStyle = BorderStyle.Fixed3D
            };

            PictureBox mini = new PictureBox
            {
                Image = layer.Image,
                SizeMode = PictureBoxSizeMode.Zoom,
                Width = 400,
                Height = 400,
            };

            ComboBox blendMode = new ComboBox
            {
                Width = 300
            };
            blendMode.Items.AddRange(new string[] { "Нет", "Сумма", "Разность", "Умножение", "Среднее" });
            blendMode.SelectedIndex = 0;
            blendMode.SelectedIndexChanged += (s, ev) =>
            {
                layer.BlendMode = blendMode.SelectedItem.ToString();
                ApplyLayers();
            };
            Label opacityLabel = new Label
            {
                Text = "Op:",
                AutoSize = true,
                Top = blendMode.Bottom + 425,
                Left = 10
            };

            TrackBar opacityTrack = new TrackBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = (int)(layer.Opacity * 100),
                TickFrequency = 10,
                Width = 200
            };
            opacityTrack.Scroll += (s, ev) =>
            {
                layer.Opacity = opacityTrack.Value / 100f;
                ApplyLayers();
            };
            Label redLabel = new Label { Text = "R:", 
                AutoSize = true,
                Top = blendMode.Bottom + 525,
                Left = 10
            };

            // Ползунки для управления каналами RGB
            TrackBar redTrackBar = new TrackBar
            {
                Minimum = 0,
                Maximum = 255,
                Value = layer.RedChannel,
                TickFrequency = 10,
                Width = 200
            };
            redTrackBar.Scroll += (s, ev) =>
            {
                layer.RedChannel = redTrackBar.Value;
                ApplyLayers();
            };
            Label greenLabel = new Label { Text = "G:",
                AutoSize = true,
                Top = blendMode.Bottom + 625,
                Left = 10
            };
            TrackBar greenTrackBar = new TrackBar
            {
                Minimum = 0,
                Maximum = 255,
                Value = layer.GreenChannel,
                TickFrequency = 10,
                Width = 200
            };
            greenTrackBar.Scroll += (s, ev) =>
            {
                layer.GreenChannel = greenTrackBar.Value;
                ApplyLayers();
            };
            Label blueLabel = new Label { Text = "B:", AutoSize = true,
                Top = blendMode.Bottom + 725,
                Left = 10
            };
            TrackBar blueTrackBar = new TrackBar
            {
                Minimum = 0,
                Maximum = 255,
                Value = layer.BlueChannel,
                TickFrequency = 10,
                Width = 200
            };
            blueTrackBar.Scroll += (s, ev) =>
            {
                layer.BlueChannel = blueTrackBar.Value;
                ApplyLayers();
            };

            // Кнопки перемещения слоёв
            Button moveUpButton = new Button
            {
                Text = "↑",
                Width = 70,
                Height = 70
            };
            moveUpButton.Click += (s, ev) =>
            {
                MoveLayerUp(layer);
                ApplyLayers();
            };

            Button moveDownButton = new Button
            {
                Text = "↓",
                Width = 70,
                Height = 70
            };
            moveDownButton.Click += (s, ev) =>
            {
                MoveLayerDown(layer);
                ApplyLayers();
            };

            // Добавляем элементы в панель
            layerPanel.Controls.Add(mini);
            layerPanel.Controls.Add(blendMode);
            layerPanel.Controls.Add(opacityLabel);
            layerPanel.Controls.Add(opacityTrack);
            layerPanel.Controls.Add(redLabel);
            layerPanel.Controls.Add(redTrackBar);
            layerPanel.Controls.Add(greenLabel);
            layerPanel.Controls.Add(greenTrackBar);
            layerPanel.Controls.Add(blueLabel);
            layerPanel.Controls.Add(blueTrackBar);
            layerPanel.Controls.Add(moveUpButton);
            layerPanel.Controls.Add(moveDownButton);

            // Располагаем элементы внутри панели
            mini.Top = 10;
            mini.Left = (layerPanel.Width - mini.Width) / 2;

            blendMode.Top = mini.Bottom + 10;
            blendMode.Left = (layerPanel.Width - blendMode.Width) / 2;

            opacityTrack.Top = blendMode.Bottom + 10;
            opacityTrack.Left = (layerPanel.Width - opacityTrack.Width) / 2;

            // Располагаем ползунки RGB рядом
            redTrackBar.Top = opacityTrack.Bottom + 10;
            redTrackBar.Left = (layerPanel.Width - redTrackBar.Width) / 2;

            greenTrackBar.Top = redTrackBar.Bottom + 10;
            greenTrackBar.Left = (layerPanel.Width - greenTrackBar.Width) / 2;

            blueTrackBar.Top = greenTrackBar.Bottom + 10;
            blueTrackBar.Left = (layerPanel.Width - blueTrackBar.Width) / 2;

            // Располагаем кнопки рядом (горизонтально)
            moveUpButton.Top = blueTrackBar.Bottom + 10;
            moveUpButton.Left = (layerPanel.Width / 2) - moveUpButton.Width - 5; // Смещаем влево

            moveDownButton.Top = blueTrackBar.Bottom + 10;
            moveDownButton.Left = (layerPanel.Width / 2) + 5; // Смещаем вправо

            // Добавляем панель в FlowLayoutPanel
            flowLayoutPanel1.Controls.Add(layerPanel);
        }

        private void MoveLayerUp(Layer layer)
        {
            int index = layers.IndexOf(layer);
            if (index > 0) // Проверяем, что слой не первый
            {
                // Перемещаем слой вверх в списке
                layers.RemoveAt(index);
                layers.Insert(index - 1, layer);
                flowLayoutPanel1.Controls.Clear();
                foreach (var l in layers)
                {
                    AddImageLayer(l); // Перерисовываем все слои
                }
            }
        }

        private void MoveLayerDown(Layer layer)
        {
            int index = layers.IndexOf(layer);
            if (index < layers.Count - 1) // Проверяем, что слой не последний
            {
                // Перемещаем слой вниз в списке
                layers.RemoveAt(index);
                layers.Insert(index + 1, layer);
                flowLayoutPanel1.Controls.Clear();
                foreach (var l in layers)
                {
                    AddImageLayer(l); // Перерисовываем все слои
                }
            }
        }
    }
}
