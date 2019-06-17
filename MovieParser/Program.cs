using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MovieParser
{
	public class Program
	{
		public static void Main(string[] arguments)
		{
			if (arguments.Length != 2)
			{
				Console.WriteLine("Usage:");
				Console.WriteLine("<.txt input> <.csv output>");
				return;
			}
			string inputPath = arguments[0];
			string outputPath = arguments[1];
			var ratings = GetRatings(inputPath);
            WriteRatings(ratings, outputPath);
		}

		private static List<MovieRating> GetRatings(string inputPath)
		{
			var lines = File.ReadLines(inputPath);
			var urlPattern = new Regex(@"^(?:(\d{4})-(\d{2})-(\d{2}) )?(https?:\/\/\S+) (.+?)(?:[,|.] (\d{1,2})%)?$");
			var descriptionPattern = new Regex(@"^(.+?)(?:[,. ]+(\d+)%)?$");
			var ratings = new List<MovieRating>();
			foreach (string line in lines)
			{
                if (line.Trim().Length == 0)
                {
                    break;
                }
				var urlMatch = urlPattern.Match(line);
				var descriptionMatch = descriptionPattern.Match(line);
				if (urlMatch.Success)
				{
					var groups = urlMatch.Groups;
					var yearGroup = groups[1];
					var monthGroup = groups[2];
					var dayGroup = groups[3];
					var urlGroup = groups[4];
					var textGroup = groups[5];
					var ratingGroup = groups[6];

					DateTime? date = GetDate(yearGroup, monthGroup, dayGroup);
					string url = urlGroup.Value;
                    url = url.Replace("m.imdb.com", "www.imdb.com");
                    url = url.Replace("http://", "https://");
                    string text = textGroup.Value;
					int? rating = GetRating(ratingGroup);

					var movieRating = new MovieRating
					{
						Date = date,
						Url = url,
						Title = text.Trim(),
						Rating = rating,
					};
					ratings.Add(movieRating);
				}
				else if(descriptionMatch.Success && ratings.Any())
				{
					var groups = descriptionMatch.Groups;
					var textGroup = groups[1];
					var ratingGroup = groups[2];

					string description = textGroup.Value;
					int? rating = GetRating(ratingGroup);
					var latestRating = ratings.Last();
					latestRating.Description = description.Trim();
					if (rating.HasValue)
					{
						latestRating.Rating = rating;
					}
				}
			}
            return ratings;
        }

        private static void WriteRatings(List<MovieRating> ratings, string outputPath)
        {
            using (var streamWriter = new StreamWriter(outputPath))
            {
                foreach (var rating in ratings)
                {
                    var columns = new object[]
                    {
                        rating.Date,
                        rating.Url,
                        rating.Title,
                        rating.Description,
                        rating.Rating,
                    };
                    var columnStrings = columns.Select(column => GetString(column));
                    string line = string.Join('\t', columnStrings);
                    streamWriter.WriteLine(line);
                }
            }
        }

		private static DateTime? GetDate(Group yearGroup, Group monthGroup, Group dayGroup)
		{
			DateTime? date = null;
			if (yearGroup.Success && monthGroup.Success && dayGroup.Success)
			{
				int year = int.Parse(yearGroup.Value);
				int month = int.Parse(monthGroup.Value);
				int day = int.Parse(dayGroup.Value);
				date = new DateTime(year, month, day);
			}
			return date;
		}

		private static int? GetRating(Group ratingGroup)
		{
			int? rating = null;
			if (ratingGroup.Success)
			{
				rating = int.Parse(ratingGroup.Value);
			}
			return rating;
		}

        private static string GetString(object value)
        {
            if (value != null)
            {
                var dateTimeValue = value as DateTime?;
                if (dateTimeValue.HasValue)
                {
                    return dateTimeValue.Value.ToString("d");
                }
                else
                {
                    return value.ToString();
                }
            }
            else
            {
                return string.Empty;
            }
        }
	}
}
