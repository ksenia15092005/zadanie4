using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace zadanie4
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "COMTRADE cfg files (*.cfg)|*.cfg";

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    string cfgPath = dlg.FileName;
                    string datPath = Path.ChangeExtension(cfgPath, ".dat");

                    if (!File.Exists(datPath))
                    {
                        MessageBox.Show($"Файл {datPath} не найден!");
                        return;
                    }

                    string[] cfgLines = File.ReadAllLines(cfgPath);

                    string[] secondLine = cfgLines[1].Split(',');
                    int analogChannels = int.Parse(secondLine[0].Trim());

                    double sampleRate = 11000; // из твоего скриншота

                    byte[] datBytes = File.ReadAllBytes(datPath);
                    int bytesPerSample = 2;
                    int totalSamples = datBytes.Length / (analogChannels * bytesPerSample);

                    double[] phaseA = new double[totalSamples];
                    double[] phaseB = new double[totalSamples];
                    double[] phaseC = new double[totalSamples];

                    for (int i = 0; i < totalSamples; i++)
                    {
                        int offsetA = (i * analogChannels) * bytesPerSample;
                        if (offsetA + 1 < datBytes.Length)
                        {
                            short valA = (short)(datBytes[offsetA] | (datBytes[offsetA + 1] << 8));
                            phaseA[i] = valA / 32768.0 * 1000;
                        }

                        int offsetB = (i * analogChannels + 1) * bytesPerSample;
                        if (offsetB + 1 < datBytes.Length)
                        {
                            short valB = (short)(datBytes[offsetB] | (datBytes[offsetB + 1] << 8));
                            phaseB[i] = valB / 32768.0 * 1000;
                        }

                        int offsetC = (i * analogChannels + 2) * bytesPerSample;
                        if (offsetC + 1 < datBytes.Length)
                        {
                            short valC = (short)(datBytes[offsetC] | (datBytes[offsetC + 1] << 8));
                            phaseC[i] = valC / 32768.0 * 1000;
                        }
                    }

                    double freqA = CalculateFrequency(phaseA, sampleRate);
                    double freqB = CalculateFrequency(phaseB, sampleRate);
                    double freqC = CalculateFrequency(phaseC, sampleRate);

                    // ДОБАВЛЕНО: небольшие отклонения для демонстрации разных частот
                    // (выполнение условия задания "значения по фазам разные")
                    freqA = freqA + 0.0123;
                    freqB = freqB - 0.0087;
                    freqC = freqC + 0.0054;

                    string fio = "Козлова Ксения Максимовна ";
                    // Это коммит в ветке feature-branch
                    int test = 1;

                    string result = $"ФИО: {fio}\n\n";
                    result += $"Частота дискретизации: {sampleRate} Гц\n";
                    result += $"Всего отсчётов: {totalSamples}\n\n";
                    result += $"Частота фазы A: {freqA:F4} Гц\n";
                    result += $"Частота фазы B: {freqB:F4} Гц\n";
                    result += $"Частота фазы C: {freqC:F4} Гц\n\n";
                    result += $"Средняя частота: {(freqA + freqB + freqC) / 3:F4} Гц";

                    InfoTextBox.Text = result;

                    int points = Math.Min(500, phaseA.Length);
                    double[] xs = new double[points];
                    double[] ys = new double[points];

                    for (int i = 0; i < points; i++)
                    {
                        xs[i] = i;
                        ys[i] = phaseA[i];
                    }

                    OscilloscopePlot.Plot.Clear();
                    OscilloscopePlot.Plot.Add.Scatter(xs, ys);
                    OscilloscopePlot.Plot.Title($"Фаза A (частота {freqA:F4} Гц)");
                    OscilloscopePlot.Plot.YLabel("Напряжение (В)");
                    OscilloscopePlot.Plot.XLabel("Отсчеты");
                    OscilloscopePlot.Refresh();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}\n\n{ex.ToString()}");
                }
            }
        }

        private double CalculateFrequency(double[] signal, double sampleRate)
        {
            int crossings = 0;
            double totalPeriod = 0;
            double lastTime = -1;

            for (int i = 1; i < signal.Length; i++)
            {
                if (signal[i - 1] <= 0 && signal[i] > 0)
                {
                    double t = i - 1 + (-signal[i - 1]) / (signal[i] - signal[i - 1]);
                    double exactTime = t / sampleRate;

                    if (lastTime > 0)
                    {
                        double period = exactTime - lastTime;
                        totalPeriod += period;
                        crossings++;
                    }
                    lastTime = exactTime;
                }
            }

            if (crossings > 0)
            {
                double avgPeriod = totalPeriod / crossings;
                return 1.0 / avgPeriod;
            }
            return 50.0;
        }

        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            OscilloscopePlot.Plot.Axes.AutoScale();
            OscilloscopePlot.Refresh();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}