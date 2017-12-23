﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.IO.Compression;

namespace MusicXMLParser
{
    /// <summary>
    /// https://github.com/shvelo/musicxml_to_arduino
    /// </summary>
    public partial class FormMain : Form
    {
        Pitch pitch = new Pitch();
        
        public FormMain()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }

        public void LoadMXL(string xml)
        {
            
            textBoxDataHolder.Text = "";
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml);

            //// Get elements
            XmlNodeList nodes = xmlDoc.DocumentElement.SelectNodes("/score-partwise/part");
            List<Note> notes = new List<Note>();

            double divisions =  Convert.ToDouble(xmlDoc.DocumentElement.SelectSingleNode("/score-partwise/part/measure/attributes/divisions").InnerText.Trim(' '));

            double tempo;
            if (xmlDoc.DocumentElement.SelectSingleNode("/score-partwise/part/measure/direction/sound").Attributes["tempo"] != null)
            {
                tempo = Math.Round(Convert.ToDouble(xmlDoc.DocumentElement.SelectSingleNode("/score-partwise/part/measure/direction/sound").Attributes["tempo"].Value.Trim(' ')));
            }
            else
            {
                tempo = 30;
            }
                        
            int oneDuration = (int)Math.Round(60.0 / tempo / divisions * 1000.0 / divisions);

            //loop through each part in the score
            foreach (XmlNode node in nodes)
            {
                XmlNodeList subnodes = node.SelectNodes("measure/note");
                
                foreach (XmlNode snode in subnodes)
                {
                    Note n = new Note();
                    //try
                    {
                        n.voice = snode.SelectSingleNode("voice").InnerText;
                        //snode.ChildNodes.                                               
                        if (snode.SelectSingleNode("rest") != null)
                        {
                            n.noteString = "0";
                            n.frequency = 0;                            
                            n.duration = oneDuration * Convert.ToInt32(snode.SelectSingleNode("duration").InnerText);
                            notes.Add(n);
                        }
                        else if (snode.SelectSingleNode("pitch").SelectSingleNode("alter") != null)
                        {
                            string step = snode.SelectSingleNode("pitch").SelectSingleNode("step").InnerText;
                            string octave = snode.SelectSingleNode("pitch").SelectSingleNode("octave").InnerText;
                            n.noteString = "NOTE_" + step + octave;
                            n.frequency = pitch.pitches[n.noteString];
                            if (snode.SelectSingleNode("pitch").SelectSingleNode("alter").InnerText == "1")
                            {
                                n.frequency = (int)Math.Round(n.frequency * 1.05946);
                            }
                            else
                            {
                                n.frequency = (int)Math.Round(n.frequency / 1.05946);
                            }
                            n.noteString = n.frequency.ToString();
                            n.duration = oneDuration * Convert.ToInt32(snode.SelectSingleNode("duration").InnerText);
                            notes.Add(n);
                        }
                        else
                        {
                            string step = snode.SelectSingleNode("pitch").SelectSingleNode("step").InnerText;
                            string octave = snode.SelectSingleNode("pitch").SelectSingleNode("octave").InnerText;
                            n.noteString = "NOTE_" + step + octave;
                            n.frequency = pitch.pitches[n.noteString];
                            n.duration = oneDuration * Convert.ToInt32(snode.SelectSingleNode("duration").InnerText);
                            notes.Add(n);
                        }
                    }
                   // catch (Exception ex)
                    {
                        //MessageBox.Show("Error " + snode.InnerXml + ex.Message);
                    }
                }
            }

            string voice = ""; //track when change
            string output = "";

            string headerString = "";
            string notesString = "";
            string durationString = "";
            string voiceString = "";

            foreach (Note no in notes.OrderBy(n => n.voice).ToList<Note>())
            {
                if (voice == "")
                {
                    voice = no.voice;
                }
                
                if (voice != no.voice)
                {
                    headerString = Environment.NewLine + Environment.NewLine + Environment.NewLine + "Voice:" + voiceString + Environment.NewLine + Environment.NewLine;
                    notesString = notesString.Substring(0, notesString.Length - 2);
                    durationString = durationString.Substring(0, durationString.Length - 2);
                    output += headerString + notesString + Environment.NewLine + Environment.NewLine + durationString + Environment.NewLine;

                    durationString = "";
                    notesString = "";
                    voice = no.voice;
                }
                voiceString = no.voice;
                notesString += no.noteString + ", ";
                durationString += no.duration + ", ";
            }

            //Only one voice in file
            if (output.Length < 5)
            {
                headerString = Environment.NewLine + Environment.NewLine + Environment.NewLine + "Voice:" + voiceString + Environment.NewLine + Environment.NewLine;
                notesString = notesString.Substring(0, notesString.Length - 1);
                durationString = durationString.Substring(0, durationString.Length - 1);
                output += headerString + notesString + Environment.NewLine + Environment.NewLine + durationString + Environment.NewLine;
            }

            textBoxNotes.Text = output;
            textBoxDataHolder.Text = output;
        }


        private void buttonOpenFile_Click(object sender, EventArgs e)
        {
            //numericUpDownLimiter.Value = 400;
            //openFileDialogMXL.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            openFileDialogMXL.InitialDirectory = Environment.CurrentDirectory;
            openFileDialogMXL.Title = "Open MXL File";
            //openFileDialogMXL.DefaultExt = "mxl";
            openFileDialogMXL.Filter = "MXL/XML Files (*.xml; *.mxl)|*.xml; *.mxl|All files (*.*)|*.*";
            openFileDialogMXL.CheckFileExists = true;
            openFileDialogMXL.CheckPathExists = true;
            openFileDialogMXL.Multiselect = false;

            if (openFileDialogMXL.ShowDialog() == DialogResult.OK)
            {
                labelOpenedFile.Text = "Opened File: " + openFileDialogMXL.FileName;
                if (openFileDialogMXL.FileName.Length > 1)
                {
                    if (openFileDialogMXL.FileName.Contains(".xml"))
                    {
                        LoadMXL(File.ReadAllText(openFileDialogMXL.FileName));
                    }
                    else if (openFileDialogMXL.FileName.Contains(".mxl"))
                    {
                        unZipMXL(openFileDialogMXL.FileName);
                    }
                    else
                    {
                        MessageBox.Show("Please select either a '.mxl' or '.xml' file type.", "File Type Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                    /*
                    try
                    {
                        unZipMXL(openFileDialogMXL.FileName);
                    }
                    catch (Exception)
                    {
                        LoadMXL(File.ReadAllText(openFileDialogMXL.FileName));
                    }
                    */
                }
            }
        }

        public void unZipMXL(string file)
        {
            FileInfo f = new FileInfo(file);
            FileStream originalFileStream = f.OpenRead();

            ZipArchive z = new ZipArchive(originalFileStream, ZipArchiveMode.Read);
            foreach (ZipArchiveEntry e in z.Entries)
            {
                if (!e.Name.Contains("container.xml"))
                {
                    e.ExtractToFile(Path.Combine(System.IO.Path.GetTempPath(), "temp.mxl"), true);
                    LoadMXL(File.ReadAllText(Path.Combine(System.IO.Path.GetTempPath(), "temp.mxl")));
                    break;
                }
            }
        }

        private void numericUpDownLimiter_ValueChanged(object sender, EventArgs e)
        {
            textBoxNotes.Text = "";
            List<string> ls = new List<string>();
            string outputline = "";
            foreach (string line in textBoxDataHolder.Lines)
            {
                if (line.Contains(','))
                {
                    ls = line.Split(',').ToList<string>();
                    foreach (string item in ls)
                    {
                        outputline += item + ", ";
                        if (outputline.Split(',').Count() >= numericUpDownLimiter.Value)
                        {
                            break;
                        }
                    }
                    outputline = outputline.Replace("  ", " ");
                    outputline = outputline.Substring(0, outputline.Length - 2);
                }
                else
                {
                    outputline = line;
                }
                textBoxNotes.Text += outputline + Environment.NewLine;
                outputline = "";
            }
        }
    }
}
