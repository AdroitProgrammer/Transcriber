using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using NHyphenator;


namespace Transcriber
{
    class VideoInfo
    {
        public string[] sentences;
        public Image[] Frames;
        private int Fps;
        private double amountOfSlots;
        private TimeSpan Duration;
        private Hyphenator hypenator = new Hyphenator(HyphenatePatternsLanguage.EnglishUs,"-");
        private double timePerFrame;


        public VideoInfo(string[] wordsNtimes, TimeSpan duration, int fps = 30)
        {
            Fps = fps;
            timePerFrame = 1000 / fps;
            Duration = duration;
            wordsNtimes = wordsNtimes.Where(x => !string.IsNullOrEmpty(x)).ToArray();
            amountOfSlots = duration.TotalMilliseconds / timePerFrame;
            sentences = wordsNtimes;
            Frames = new Image[(int)amountOfSlots];

            setSyllableTime();
        }

        private long convertToMilliseconds(string input)
        {
            if (!input.Contains("."))
                input += ".0";

            int hours = int.Parse(input.Split(':')[0]);
            int minutes = int.Parse(input.Split(':')[1]);
            int seconds = int.Parse(input.Split(':')[2].Split('.')[0]);
            int miliseconds = int.Parse(input.Split(':')[2].Split('.')[1]);
            long output = 0;

            output += ((hours * 60) * 60) * 1000;
            output += (minutes * 60) * 1000;
            output += seconds * 1000;
            output += miliseconds;

            return output;
        }

        private void setSyllableTime()
        {
            // check if a number is closer to another number
            // the slots are either 60 fps or 30 fps
            // the time period will go up by a incriment of timeperFrame
            // get the miliseconds from the time[i] and divide it by the timeperFrame
            //check if it fits inbetween two slots or is the exact time period of a slot. whichever one occurs you put the item in the closest slot
            // multiply by 1000 divide it by timeperslot then round up or down to get which slot it should go in
            // time per syllable = the amount of syllables in the sentence divivided by the duration of the sentence and multiplied by the amount of syllables
            //check if slot is taken , if not add it , if so move it to the slot ahead. 
            // example :[0] = "andersen even after … any degree or songs |00:00:00.9100000_00:00:04.4400000"
            for(int i = 0; i < sentences.Length;i++)
            {
                string[] words = sentences[i].Split('|')[0].Split(' ');
                uint audioPos = (uint)convertToMilliseconds(sentences[i].Split('|')[1].Split('_')[0].TrimEnd('0'));
                uint audioDur = (uint)convertToMilliseconds(sentences[i].Split('|')[1].Split('_')[1].TrimEnd('0'));

                int slotForStartTime = (int)Math.Round(audioPos / timePerFrame);

                int SyllableCount = 0;

                foreach (string word in words)
                {
                   SyllableCount += hypenator.HyphenateText(word).Split('-').Length;
                }

                int timePerSyllable = (int)audioDur / SyllableCount;

                

                // the syllable will go in the slot audioPos + timeperSyllable * syllable number   timePerSyllable * 

            }

        }
    }
}
