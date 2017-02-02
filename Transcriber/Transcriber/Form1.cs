using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Speech;
using System.Speech.AudioFormat;
using System.Speech.Recognition;
using System.Media;
using System.Diagnostics;
using MediaToolkit;
using MediaToolkit.Model;
using NAudio;

namespace Transcriber
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private SpeechRecognitionEngine _speechRecog = new SpeechRecognitionEngine(new System.Globalization.CultureInfo("en-US"));
        private List<string> _videoFormats = new List<string>();
        private string Path;
        private string audioPath = "";
        private int previousSecond = 0;
        private string Chatter;
        private bool transcribeFormat = true;
        private string transcribedText = "";
        private TimeSpan currentMp3Duration;
        private VideoInfo currentSoundInfo;

        private void button1_Click(object sender, EventArgs e)
        {
            string newword = textBox1.Text.Remove(textBox1.Text.Length - 3, 3);
            var inputfile = new MediaFile { Filename = textBox1.Text };
            var outputfile = new MediaFile { Filename = newword + "avi" };

            using (var engine = new Engine())
            {
                engine.Convert(inputfile, outputfile);
            }
            MessageBox.Show("Completed Audio Split!","Split Successful",MessageBoxButtons.OK,MessageBoxIcon.Information);
        }

        private void textBox1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        private void textBox1_DragDrop(object sender, DragEventArgs e)
        {
            string path = ((string[])e.Data.GetData(DataFormats.FileDrop, false))[0];
            //if (path.EndsWith(".avi"))
                textBox1.Text = path;
            //else { MessageBox.Show("Error the current file is not a avi file", "Not Supported File", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string newword = textBox1.Text.Split('.')[0] + ".wav";
            var inputfile = new MediaFile { Filename = textBox1.Text };
            var outputfile = new MediaFile { Filename = newword };
            audioPath = newword;

            using (var engine = new Engine())
            {
                engine.Convert(inputfile, outputfile);
            }

            NAudio.Wave.WaveFileReader reader = new NAudio.Wave.WaveFileReader(audioPath);
            currentMp3Duration = reader.TotalTime;
            reader.Dispose();

            DictationGrammar defaultDictationGrammar = new DictationGrammar();
            defaultDictationGrammar.Name = "default dictation";
            defaultDictationGrammar.Enabled = true;

            DictationGrammar spellingDictationGrammar =
              new DictationGrammar("grammar:dictation#spelling");
            spellingDictationGrammar.Name = "spelling dictation";
            spellingDictationGrammar.Enabled = true;

            DictationGrammar customDictationGrammar =
              new DictationGrammar("grammar:dictation");
            customDictationGrammar.Name = "question dictation";
            customDictationGrammar.Enabled = true;

            _speechRecog.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(_speechRecog_SpeechRecognized);
            _speechRecog.SetInputToWaveFile(newword);
            _speechRecog.UpdateRecognizerSetting("CFGConfidenceRejectionThreshold", 80);
            _speechRecog.UpdateRecognizerSetting("HighConfidenceThreshold", 80);
            _speechRecog.UpdateRecognizerSetting("NormalConfidenceThreshold",80 );
            _speechRecog.UpdateRecognizerSetting("LowConfidenceThreshold", 80);
            _speechRecog.LoadGrammar(defaultDictationGrammar);
            _speechRecog.LoadGrammar(new Grammar(new GrammarBuilder(new Choices(GetLines(Properties.Resources.Oxfords_3000_2)))));
            _speechRecog.LoadGrammar(spellingDictationGrammar);
            _speechRecog.LoadGrammar(customDictationGrammar);
            _speechRecog.RecognizeAsync(RecognizeMode.Multiple);
            _speechRecog.RecognizeCompleted += _speechRecog_RecognizeCompleted;
        }

        private void _speechRecog_RecognizeCompleted(object sender, RecognizeCompletedEventArgs e)
        {
            transcribedText = richTextBox1.Text;
            string[] Sections = Chatter.Split('#');
            currentSoundInfo = new VideoInfo(Sections,currentMp3Duration);

        }

        private string[] GetLines(string text)
        {

            List<string> lines = new List<string>();
            using (MemoryStream ms = new MemoryStream())
            {
                StreamWriter sw = new StreamWriter(ms);
                sw.Write(text);
                sw.Flush();

                ms.Position = 0;

                string line;

                using (StreamReader sr = new StreamReader(ms))
                {
                    while ((line = sr.ReadLine()) != null)
                    {
                        lines.Add(line);
                    }  
                }
                sw.Close();
            }



            return lines.ToArray();
        }

        private void _speechRecog_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (transcribeFormat)
            {
                int counter = 0;
                string sentence = "";
                foreach (RecognizedWordUnit word in e.Result.Words)
                {
                    if (e.Result.Audio.AudioPosition.Seconds == 0 && counter == 0)
                    {
                        richTextBox1.Text += " (" + e.Result.Audio.AudioPosition.Hours + ":" + e.Result.Audio.AudioPosition.Minutes + ":" + e.Result.Audio.AudioPosition.Seconds + ") ";
                        counter += 1;
                    }
                    if (e.Result.Audio.AudioPosition.Seconds == previousSecond && e.Result.Audio.AudioPosition.Seconds + e.Result.Audio.AudioPosition.Minutes != 0)
                    {
                        richTextBox1.Text += word.Text + " ";
                        //if the second did not change do not right the time again
                    }
                    else if (previousSecond == 0 && e.Result.Audio.AudioPosition.Minutes == 0 && previousSecond == e.Result.Audio.AudioPosition.Seconds)
                    {
                        richTextBox1.Text += word.Text + " ";
                    }
                    else
                    {
                        richTextBox1.Text += word.Text + " (" + e.Result.Audio.AudioPosition.Hours + ":" + e.Result.Audio.AudioPosition.Minutes + ":" + e.Result.Audio.AudioPosition.Seconds + ") " + " ";
                        previousSecond = e.Result.Audio.AudioPosition.Seconds;
                    }
                    sentence += word.Text + " ";
                }
                sentence.TrimEnd(' ');
                sentence += "|" + e.Result.Audio.AudioPosition.ToString() + "_"+ e.Result.Audio.Duration.ToString();
                Chatter += sentence + "#";
            }
            else
            {
                string sentence = "";
                foreach (RecognizedWordUnit word in e.Result.Words)
                {
                    richTextBox1.Text += word.Text + " " + e.Result.Audio.AudioPosition.Duration() + "|";
                    sentence += word.Text + " ";
                }
                sentence.TrimEnd(' ');
                sentence += "|" + e.Result.Audio.AudioPosition.ToString() + "_" + e.Result.Audio.Duration.ToString();
                Chatter += sentence + "#";
                richTextBox1.Text = Chatter;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            _videoFormats.Add(".MOV");
            _videoFormats.Add(".MPEG4");
            _videoFormats.Add(".MP4");
            _videoFormats.Add(".AVI");
            _videoFormats.Add(".WMV");
            _videoFormats.Add(".MPEGPS");
            _videoFormats.Add(".FLV");
            _videoFormats.Add(".WebM");
            
            comboBox2.Items.AddRange(_videoFormats.ToArray());
        }

        private void button4_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            string filter = "Video files|";
            foreach (string format in _videoFormats)
            {
                filter += "*" + format.ToLower() + "; ";
            }
            ofd.Filter = filter;
            ofd.ShowDialog();
            textBox2.Text = ofd.FileName;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Path = textBox2.Text.Split('.')[0];
            var inputfile = new MediaFile { Filename = textBox2.Text };
            var outputfile = new MediaFile { Filename = Path + comboBox2.Items[comboBox2.SelectedIndex]};

            using (var engine = new Engine())
            {
                engine.Convert(inputfile, outputfile);
            }
            MessageBox.Show("Completed!", "Conversion Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.ShowDialog();
            Path = sfd.FileName;
            using (StreamWriter sw = new StreamWriter(Path))
            {
                sw.Write(richTextBox1.Text);
            }
        }

    }
}
