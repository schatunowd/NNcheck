using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using ConsoleApp1ML.Model;
using ConsoleApp1ML.ConsoleApp;
using System.Diagnostics;
using System.Text;

namespace ConsoleApp1
{
    class Program
    {
        static List<string> Norm(List <string> comments)
        {
            List<string> all_str = new List<string>();
            foreach (string str in comments)
            {
                string new_str = Regex.Replace(str, "[^а-яА-Я;0-9 a-zA-Z.]", "");
                all_str.Add(new_str.ToLower());
            }
            return all_str;
        }

        static List<string> LSTMExec(List<string> comments, ref List<string> scores, ref List<string> positive, ref List<string> negative)
        {
            var psi = new ProcessStartInfo();
            psi.FileName = "python";
            var script = @"D:/study/4.2/diplom/test.py";
            //int reviews = 1;
            string arguments = "";
            for (int i = 0; i < comments.Count; i++)
            {
                if (i == comments.Count - 1)
                    arguments += comments[i];
                else
                    arguments += comments[i] + ",";
            }
            psi.Arguments = $"\"{script}\" \"{arguments}\"";
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            var errors = "";
            var results = "";
            using (var process = Process.Start(psi))
            {
                Console.WriteLine(psi.Arguments);
                errors = process.StandardError.ReadToEnd();
                results = process.StandardOutput.ReadToEnd();
            }
            results = results.Substring(1, results.Length - 4);
            results = results.Replace(" ", "");
            string[] subs = results.Split(',');
            for (int i = 0; i < subs.Length; i++)
                scores.Add(subs[i]);

            double last_score = 0;
            foreach (string score in scores)
            {
                if (score == "0")
                    negative.Add(score);
                else
                    positive.Add(score);
                last_score += Convert.ToInt32(score);
            }
            return scores;
        }

        static List<string> AvrgdPerceptronExec(List<string> comments_new, ref List<string> scores, ref List<string> positive, ref List<string> negative)
        {
            foreach (string comment in comments_new)
            {
                ModelInput input = new ModelInput()
                {
                    Review = comment
                };
                ModelOutput result = ConsumeModel.Predict(input);
                scores.Add(result.Prediction);
                if (result.Prediction == "0")
                    negative.Add(result.Prediction);
                else
                    positive.Add(result.Prediction);
            }
            return scores;
        }
        static void Main(string[] args)
        {
            List<string> scores = new List<string>() { };
            List<string> positive = new List<string>();
            List<string> negative = new List<string>();
            Console.WriteLine("Введите название фильма: ");
            string filmName = Console.ReadLine();
            string filmNameEn, fileName;
            string currentTimeForFileName = DateTime.Now.ToString().Replace(" ", "_");
            currentTimeForFileName = currentTimeForFileName.Replace(":", "_");
            List<string> comments = KinopoiskParser.Program.ParserExec(filmName, false, out filmNameEn);
            if (filmNameEn == "")
                fileName = "ruFilm_" + currentTimeForFileName + ".txt";
            else
                fileName = filmNameEn + "_" + currentTimeForFileName + ".txt";
            string path = @"D:\study\4.2\diplom\history\" + fileName;
            List<string> comments_new = Norm(comments);
            if (!File.Exists(path))
            {
                using (StreamWriter sw = File.CreateText(path))
                {
                    foreach (string comment in comments)
                        sw.WriteLine(comment);
                }
            }
            Console.WriteLine("Выберите нейронную сеть:\n1.LSTM\n2.AvrgdPerceptron");
            int param = Convert.ToInt32(Console.ReadLine());
            if (param == 1)
                LSTMExec(comments_new, ref scores, ref positive, ref negative);
            else if (param == 2)
                AvrgdPerceptronExec(comments_new, ref scores, ref positive, ref negative);
            else
                Console.WriteLine("NO FCKN WAY");
            double last_score = 0;
            foreach (string score in scores)
                last_score += Convert.ToInt32(score);

            last_score /= scores.Count;
            last_score *= 10;
            Console.WriteLine("Полученная оценка фильма: " + Math.Round(last_score,1));
            Console.WriteLine("Всего отзывов: " + scores.Count);
            Console.WriteLine("Количество положительно определенных отзывов: " + positive.Count);
            Console.WriteLine("Количество негативно определенных отзывов: " + negative.Count);
        }
    }
}
